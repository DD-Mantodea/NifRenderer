using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NiflySharp;
using NiflySharp.Blocks;
using NifRenderer.BodySlide;
using NifRenderer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NifRenderer.Renderer.Scene
{
    public class RenderMesh : IDisposable
    {
        public NifFile File;

        public VertexPositionColorNormalTexture[] Vertices;

        public List<RenderShape> RenderShapes = [];

        public int TexturedCount;

        public int MissingTextureCount;

        public Vector3 BoundMin;

        public Vector3 BoundMax;

        public SliderSet? Slides;

        public SliderApplicator? Applicator;

        public Dictionary<string, float> SliderValues = [];

        public List<RenderShape> SliderTargets = [];

        public Vector3 BoundCenter => (BoundMin + BoundMax) * 0.5f;

        public float BoundRadius => (BoundMax - BoundMin).Length() * 0.5f;

        public RenderMesh(NifFile file) : this(file, null)
        {
        }

        public RenderMesh(NifFile file, string path)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));

            var shapes = file.GetShapes();

            var vertices = new List<VertexPositionColorNormalTexture>();

            var index = 0;

            foreach (var shape in shapes)
            {
                var shapeTransform =
                    Matrix.CreateScale(shape.Scale) *
                    MatrixUtils.From33(shape.Rotation) *
                    Matrix.CreateTranslation(shape.Translation);

                var positions = new List<Vector3>();

                if (shape is BSTriShape bs)
                    positions = [.. bs.VertexPositions];
                else if (shape.GeometryData != null)
                    positions = [.. shape.GeometryData.Vertices];
                else
                    continue;

                vertices.AddRange([.. positions.Select(v => new VertexPositionColorNormalTexture(Vector3.Transform(v, shapeTransform), Color.White, new(), new()))]);

                var renderShape = new RenderShape(this, shape)
                {
                    InverseTransform = Matrix.Invert(shapeTransform),

                    StartIndex = index,

                    VertexCount = positions.Count,

                    TriangleCount = shape.Triangles.Count
                };

                renderShape.ReadAlphaProperty();

                var normals = new Dictionary<int, List<Vector3>>();

                foreach (var tri in shape.Triangles)
                {
                    var v1 = vertices[index + tri.V1];
                    var v2 = vertices[index + tri.V2];
                    var v3 = vertices[index + tri.V3];

                    var normal = RendererUtils.GetTriNormal(v1, v2, v3);

                    if (normals.TryGetValue(index + tri.V1, out var l1))
                        l1.Add(normal);
                    else normals.Add(index + tri.V1, [normal]);

                    if (normals.TryGetValue(index + tri.V2, out var l2))
                        l2.Add(normal);
                    else normals.Add(index + tri.V2, [normal]);

                    if (normals.TryGetValue(index + tri.V3, out var l3))
                        l3.Add(normal);
                    else normals.Add(index + tri.V3, [normal]);
                }

                foreach (var pair in normals)
                {
                    var vertex = vertices[pair.Key];

                    var normal = pair.Value.Aggregate(Vector3.Zero, (a, b) => a + b);

                    normal.Normalize();

                    vertices[pair.Key] = new(vertex.Position, vertex.Color, normal, new());
                }

                RenderShapes.Add(renderShape);

                index = vertices.Count;
            }

            Vertices = [.. vertices];

            foreach (var renderShape in RenderShapes)
                renderShape.RebuildBuffers();

            ComputeBound();
        }

        private void ComputeBound()
        {
            if (Vertices.Length == 0)
                return;

            var min = Vertices[0].Position;

            var max = min;

            foreach (var vertex in Vertices)
            {
                min = Vector3.Min(min, vertex.Position);

                max = Vector3.Max(max, vertex.Position);
            }

            BoundMin = min;

            BoundMax = max;
        }

        public IEnumerable<RenderShape> VisibleShapes()
        {
            foreach (var shape in RenderShapes)
                yield return shape;
        }

        public void Draw(GraphicsDevice device, Effect effect)
        {
            foreach (var shape in RenderShapes)
            {
                if (shape.Blended || !shape.Visible)
                    continue;

                DrawShape(device, effect, shape);
            }
        }

        public void DrawBlended(GraphicsDevice device, Effect effect)
        {
            foreach (var shape in RenderShapes)
            {
                if (!shape.Blended || !shape.Visible)
                    continue;

                DrawShape(device, effect, shape);
            }
        }

        private static void DrawShape(GraphicsDevice device, Effect effect, RenderShape shape)
        {
            var textured = shape.Texture != null;

            effect.CurrentTechnique = effect.Techniques[textured ? "Textured" : "Colored"];

            effect.Parameters["xTexture"]?.SetValue(shape.Texture);

            effect.Parameters["xAlphaTest"]?.SetValue(textured ? shape.AlphaTestValue : 0f);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                shape.Draw(device);
            }

            device.SetVertexBuffer(null);
        }

        public void DrawWireframe(GraphicsDevice device, Effect effect, Color color)
        {
            DrawWireframe(device, effect, color, null);
        }

        public void DrawWireframe(GraphicsDevice device, Effect effect, Color color, Predicate<RenderShape>? filter)
        {
            effect.CurrentTechnique = effect.Techniques["ColoredNoShading"];

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (var shape in RenderShapes)
                {
                    if (!shape.Visible)
                        continue;

                    if (filter != null && !filter(shape))
                        continue;

                    shape.DrawWireframe(device, color);

                    device.SetVertexBuffer(null);
                }
            }
        }

        public void DrawWireframe(GraphicsDevice device, Effect effect, Func<RenderShape, int, Color> colorOf, Predicate<RenderShape>? filter)
        {
            effect.CurrentTechnique = effect.Techniques["EditLine"];

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (var shape in RenderShapes)
                {
                    if (!shape.Visible)
                        continue;

                    if (filter != null && !filter(shape))
                        continue;

                    var target = shape;

                    shape.DrawWireframe(device, index => colorOf(target, index));

                    device.SetVertexBuffer(null);
                }
            }
        }

        public void DrawMask(GraphicsDevice device, Effect effect)
        {
            effect.CurrentTechnique = effect.Techniques["OutlineID"];

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (var shape in RenderShapes)
                {
                    if (shape.Visible)
                        shape.Draw(device);
                }
            }
        }

        public void RemoveShape(RenderShape target)
        {
            var index = RenderShapes.IndexOf(target);

            if (index < 0)
                return;

            RemoveShapeAt(index);

            target.Texture?.Dispose();

            target.Texture = null;
        }

        public VertexPositionColorNormalTexture[] ShapeSlice(RenderShape target)
        {
            var count = target.VertexCount;

            var start = target.StartIndex;

            if (count <= 0 || start < 0 || start + count > Vertices.Length)
                return [];

            var slice = new VertexPositionColorNormalTexture[count];

            Array.Copy(Vertices, start, slice, 0, count);

            return slice;
        }

        public void Dispose()
        {
            foreach (var shape in RenderShapes)
                shape.Dispose();

            RenderShapes.Clear();

            Slides = null;

            Applicator = null;

            SliderValues.Clear();

            SliderTargets.Clear();
        }

        public void RemoveShapeAt(int index)
        {
            if (index < 0 || index >= RenderShapes.Count)
                return;

            var target = RenderShapes[index];

            var start = target.StartIndex;

            var count = target.VertexCount;

            var kept = new List<VertexPositionColorNormalTexture>(Vertices.Length - count);

            for (var i = 0; i < Vertices.Length; i++)
            {
                if (i < start || i >= start + count)
                    kept.Add(Vertices[i]);
            }

            Vertices = [.. kept];

            RenderShapes.RemoveAt(index);

            for (var i = index; i < RenderShapes.Count; i++)
                RenderShapes[i].StartIndex -= count;

            ComputeBound();
        }

        public void InsertShape(int index, RenderShape target, VertexPositionColorNormalTexture[] slice)
        {
            index = Math.Clamp(index, 0, RenderShapes.Count);

            var start = index < RenderShapes.Count ? RenderShapes[index].StartIndex : Vertices.Length;

            var combined = new VertexPositionColorNormalTexture[Vertices.Length + slice.Length];

            Array.Copy(Vertices, 0, combined, 0, start);

            Array.Copy(slice, 0, combined, start, slice.Length);

            Array.Copy(Vertices, start, combined, start + slice.Length, Vertices.Length - start);

            Vertices = combined;

            for (var i = index; i < RenderShapes.Count; i++)
                RenderShapes[i].StartIndex += slice.Length;

            target.StartIndex = start;

            target.VertexCount = slice.Length;

            RenderShapes.Insert(index, target);

            ComputeBound();
        }

        public RenderShape? RayPick(Vector3 origin, Vector3 direction, out float distance)
        {
            distance = -1f;

            if (Vertices == null || Vertices.Length == 0)
                return null;

            RenderShape? best = null;

            var bestDistance = float.MaxValue;

            foreach (var candidate in RenderShapes)
            {
                if (!candidate.Visible)
                    continue;

                var hit = RayHitShape(candidate, origin, direction);

                if (hit < 0f || hit >= bestDistance)
                    continue;

                bestDistance = hit;

                best = candidate;
            }

            if (best == null)
                return null;

            distance = bestDistance;

            return best;
        }

        private float RayHitShape(RenderShape shape, Vector3 origin, Vector3 direction)
        {
            var best = float.MaxValue;

            var start = shape.StartIndex;

            foreach (var triangle in shape.Shape.Triangles)
            {
                if (shape.IsVertexDeleted(triangle.V1) || shape.IsVertexDeleted(triangle.V2) || shape.IsVertexDeleted(triangle.V3))
                    continue;

                var hit = GeometryUtils.RayTriangle(
                    origin,
                    direction,
                    Vertices[start + triangle.V1].Position,
                    Vertices[start + triangle.V2].Position,
                    Vertices[start + triangle.V3].Position,
                    out var distance);

                if (hit && distance < best)
                    best = distance;
            }

            return best == float.MaxValue ? -1f : best;
        }

        public bool IntersectsBound(Vector3 origin, Vector3 direction)
        {
            var radius = BoundRadius;

            if (radius <= 0f)
                return false;

            var offset = BoundCenter - origin;

            var along = Vector3.Dot(offset, direction);

            var perpendicularSquared = offset.LengthSquared() - along * along;

            return perpendicularSquared <= radius * radius;
        }

        public float RayIntersects(Vector3 origin, Vector3 direction)
        {
            if (Vertices == null || Vertices.Length == 0)
                return -1f;

            var best = float.MaxValue;

            foreach (var shape in RenderShapes)
            {
                var hit = RayHitShape(shape, origin, direction);

                if (hit > 0f && hit < best)
                    best = hit;
            }

            return best == float.MaxValue ? -1f : best;
        }
    }
}
