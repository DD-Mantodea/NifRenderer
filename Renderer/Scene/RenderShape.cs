using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NiflySharp;
using NifRenderer.Utils;
using System;
using System.Collections.Generic;

namespace NifRenderer.Renderer.Scene
{
    public class RenderShape(RenderMesh mesh, INiShape shape) : IDisposable
    {
        public RenderMesh Mesh = mesh;

        public INiShape Shape = shape;

        public string ID = shape.Name.String;

        public Texture2D Texture;

        public bool Visible = true;

        public int VertexCount;

        public bool AlphaBlend;

        public bool Blended => AlphaBlend && Texture != null;

        public bool AlphaTest;

        public byte AlphaThreshold = 128;

        public int TriangleCount;

        public readonly HashSet<int> DeletedVertices = [];

        public int SourceTriangleCount => Shape.Triangles.Count;

        public bool IsVertexDeleted(int index) => DeletedVertices.Contains(index);

        public int StartIndex;

        public VertexPositionColorNormalTexture[] ShapeVertices = [];

        public VertexPositionColor[] OverlayVertices = [];

        public int[] TriangleIndices = [];

        public int[] EdgeIndices = [];

        public int[] PointIndices = [];

        public Matrix InverseTransform = Matrix.Identity;

        public VertexPositionColorNormalTexture[] MeshVertices => Mesh.Vertices;

        public float AlphaTestValue => AlphaTest ? AlphaThreshold / 255f : 0f;

        private VertexBuffer? _vertexBuffer;

        private DynamicVertexBuffer? _overlayBuffer;

        private IndexBuffer? _triangleIndexBuffer;

        private IndexBuffer? _edgeIndexBuffer;

        private IndexBuffer? _pointIndexBuffer;

        private int _vertexCapacity;

        private int _overlayCapacity;

        private int _triangleIndexCapacity;

        private int _edgeIndexCapacity;

        private int _pointIndexCapacity;

        private bool _dirty = true;

        private Color _wireColor;

        private bool _wireColorSet;

        private bool _overlayDirty = true;

        private bool _indexDirty = true;

        public void ReadAlphaProperty()
        {
            if (!Shape.HasAlphaProperty)
                return;

            var property = Mesh.File.GetBlock(Shape.AlphaPropertyRef);

            if (property == null)
                return;

            AlphaBlend = property.Flags.AlphaBlend;

            AlphaTest = property.Flags.AlphaTest;

            AlphaThreshold = property.Threshold;
        }

        public void Draw(GraphicsDevice device)
        {
            if (TriangleCount <= 0 || TriangleIndices.Length == 0)
                return;

            UploadVertex(device);

            UploadIndex(device);

            if (_triangleIndexBuffer == null)
                return;

            device.SetVertexBuffer(_vertexBuffer);

            device.Indices = _triangleIndexBuffer;

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, TriangleCount);

            device.Indices = null;
        }

        private void UploadVertex(GraphicsDevice device)
        {
            if (_vertexBuffer == null || _vertexCapacity < ShapeVertices.Length || _vertexBuffer.IsDisposed)
            {
                _vertexBuffer?.Dispose();

                _vertexCapacity = ShapeVertices.Length;

                _vertexBuffer = new VertexBuffer(device, VertexPositionColorNormalTexture.VertexDeclaration, _vertexCapacity, BufferUsage.WriteOnly);

                _dirty = true;
            }

            if (_dirty)
            {
                _vertexBuffer.SetData(ShapeVertices);

                _dirty = false;
            }
        }

        private void UploadIndex(GraphicsDevice device)
        {
            if (_indexDirty)
            {
                _triangleIndexBuffer = BuildIndexBuffer(device, _triangleIndexBuffer, ref _triangleIndexCapacity, TriangleIndices);

                _edgeIndexBuffer = BuildIndexBuffer(device, _edgeIndexBuffer, ref _edgeIndexCapacity, EdgeIndices);

                _pointIndexBuffer = BuildIndexBuffer(device, _pointIndexBuffer, ref _pointIndexCapacity, PointIndices);

                _indexDirty = false;
            }
        }

        private static IndexBuffer? BuildIndexBuffer(GraphicsDevice device, IndexBuffer? buffer, ref int capacity, int[] indices)
        {
            if (indices.Length == 0)
                return buffer;

            if (buffer == null || capacity < indices.Length || buffer.IsDisposed)
            {
                buffer?.Dispose();

                capacity = Math.Max(indices.Length, 16);

                buffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, capacity, BufferUsage.WriteOnly);
            }

            buffer.SetData(indices);

            return buffer;
        }

        public void MarkWireDirty() => _overlayDirty = true;

        public void DrawWireframe(GraphicsDevice device, Color color)
        {
            if (_wireColorSet && _wireColor == color)
            {
                DrawLines(device);

                return;
            }

            for (var i = 0; i < OverlayVertices.Length; i++)
                OverlayVertices[i].Color = color;

            _wireColor = color;

            _wireColorSet = true;

            _overlayDirty = true;

            UploadOverlay(device);

            DrawLines(device);
        }

        public void DrawWireframe(GraphicsDevice device, Func<int, Color> colorOf)
        {
            if (_overlayDirty)
            {
                for (var i = 0; i < OverlayVertices.Length; i++)
                    OverlayVertices[i].Color = colorOf(i);

                UploadOverlay(device);
            }

            DrawLines(device);
        }

        private void UploadOverlay(GraphicsDevice device)
        {
            if (OverlayVertices.Length == 0)
                return;

            var ready = _overlayBuffer != null && !_overlayBuffer.IsDisposed && _overlayCapacity >= OverlayVertices.Length;

            if (!_overlayDirty && ready)
                return;

            if (!ready)
            {
                _overlayBuffer?.Dispose();

                _overlayCapacity = OverlayVertices.Length;

                _overlayBuffer = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, _overlayCapacity, BufferUsage.WriteOnly);
            }

            _overlayBuffer.SetData(OverlayVertices);

            _overlayDirty = false;
        }

        private void DrawLines(GraphicsDevice device)
        {
            if (EdgeIndices.Length == 0)
                return;

            UploadOverlay(device);

            UploadIndex(device);

            if (_edgeIndexBuffer == null)
                return;

            device.SetVertexBuffer(_overlayBuffer);

            device.Indices = _edgeIndexBuffer;

            device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, EdgeIndices.Length / 2);

            device.Indices = null;
        }

        public RenderShape Textured(GraphicsDevice device, string path)
        {
            Texture = Texture2DUtils.FromPath(device, path);

            ApplyUVs();

            return this;
        }

        public void ApplyUVs()
        {
            var uvs = Texture2DUtils.GetUVs(Shape);

            if (uvs.Count == 0)
                return;

            var meshVertices = Mesh.Vertices;

            for (var i = 0; i < uvs.Count; i++)
            {
                var vertex = meshVertices[StartIndex + i];

                var uv = uvs[i];

                meshVertices[StartIndex + i] = new(vertex.Position, vertex.Color, vertex.Normal, new(uv.U, uv.V));
            }

            RebuildBuffers();
        }

        public void Dispose()
        {
            Texture?.Dispose();

            Texture = null;

            ReleaseBuffers();
        }

        public void ReleaseBuffers()
        {
            _vertexBuffer?.Dispose();

            _vertexBuffer = null;

            _vertexCapacity = 0;

            _overlayBuffer?.Dispose();

            _overlayBuffer = null;

            _overlayCapacity = 0;

            _triangleIndexBuffer?.Dispose();

            _triangleIndexBuffer = null;

            _edgeIndexBuffer?.Dispose();

            _edgeIndexBuffer = null;

            _pointIndexBuffer?.Dispose();

            _pointIndexBuffer = null;

            _triangleIndexCapacity = 0;

            _edgeIndexCapacity = 0;

            _pointIndexCapacity = 0;

            _indexDirty = true;
        }

        public void RebuildBuffers()
        {
            var meshVertices = Mesh.Vertices;

            ShapeVertices = new VertexPositionColorNormalTexture[VertexCount];

            OverlayVertices = new VertexPositionColor[VertexCount];

            for (var i = 0; i < VertexCount; i++)
            {
                var vertex = meshVertices[StartIndex + i];

                ShapeVertices[i] = new(vertex.Position, Color.White, vertex.Normal, vertex.TextureCoordinate);

                OverlayVertices[i] = new(vertex.Position, Color.Red);
            }

            var points = new List<int>(VertexCount);

            for (var i = 0; i < VertexCount; i++)
            {
                if (!DeletedVertices.Contains(i))
                    points.Add(i);
            }

            PointIndices = [.. points];

            var triangles = Shape.Triangles;

            var triangleIndices = new List<int>(triangles.Count * 3);

            var keys = new long[triangles.Count * 3];

            var keyCount = 0;

            var active = 0;

            foreach (var tri in triangles)
            {
                if (tri.V1 >= VertexCount || tri.V2 >= VertexCount || tri.V3 >= VertexCount)
                    continue;

                if (DeletedVertices.Contains(tri.V1) || DeletedVertices.Contains(tri.V2) || DeletedVertices.Contains(tri.V3))
                    continue;

                triangleIndices.Add(tri.V1);

                triangleIndices.Add(tri.V2);

                triangleIndices.Add(tri.V3);

                keys[keyCount++] = EdgeKey(tri.V1, tri.V2);

                keys[keyCount++] = EdgeKey(tri.V2, tri.V3);

                keys[keyCount++] = EdgeKey(tri.V3, tri.V1);

                active++;
            }

            TriangleCount = active;

            TriangleIndices = [.. triangleIndices];

            EdgeIndices = Dedup(keys, keyCount);

            _dirty = true;

            _overlayDirty = true;

            _wireColorSet = false;

            _indexDirty = true;
        }

        private static long EdgeKey(int a, int b)
        {
            var lo = Math.Min(a, b);

            var hi = Math.Max(a, b);

            return ((long)lo << 32) | (uint)hi;
        }

        private static int[] Dedup(long[] keys, int count)
        {
            if (count == 0)
                return [];

            Array.Sort(keys, 0, count);

            var indices = new List<int>(count * 2);

            var last = -1L;

            for (var i = 0; i < count; i++)
            {
                if (i > 0 && keys[i] == last)
                    continue;

                last = keys[i];

                indices.Add((int)(keys[i] >> 32));

                indices.Add((int)(keys[i] & 0xFFFFFFFF));
            }

            return [.. indices];
        }

        public bool RefreshPositions(IReadOnlyList<int> meshIndices)
        {
            foreach (var meshIndex in meshIndices)
            {
                if (meshIndex < 0 || meshIndex >= ShapeVertices.Length)
                    continue;

                var position = Mesh.Vertices[StartIndex + meshIndex].Position;

                ShapeVertices[meshIndex].Position = position;

                OverlayVertices[meshIndex].Position = position;
            }

            _dirty = true;

            _overlayDirty = true;

            return true;
        }
    }
}
