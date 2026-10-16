using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NifRenderer.Renderer.Vertex;

namespace NifRenderer.Editing.Transform
{
    public abstract class TransformSet
    {
        public Vector3 Pivot { get; protected set; }

        public Vector3 ViewDirection { get; protected set; }

        public Vector3 RotationAxis { get; set; }

        public abstract int Count { get; }

        public abstract void Apply(Renderer.Renderer owner, TransformMode mode, Vector3 value);

        public abstract void Restore(Renderer.Renderer owner);

        public abstract EditRecord? BuildRecord(TransformMode mode, Vector3 value);
    }

    public sealed class TransformTargets : TransformSet
    {
        public readonly List<VertexRef> Order = [];

        public readonly Dictionary<VertexRef, Vector3> Origin = [];

        public override int Count => Order.Count;

        public static TransformTargets? Capture(Renderer.Renderer owner, IEnumerable<VertexRef> selected)
        {
            var targets = new TransformTargets();

            var sum = Vector3.Zero;

            foreach (var vertex in selected)
            {
                targets.Order.Add(vertex);

                targets.Origin[vertex] = owner.VertexPosition(vertex);

                sum += targets.Origin[vertex];
            }

            if (targets.Order.Count == 0)
                return null;

            targets.Pivot = owner.IsEditMode ? sum / targets.Order.Count : Vector3.Zero;

            targets.ViewDirection = owner.ViewDirection;

            targets.RotationAxis = owner.ViewDirection;

            return targets;
        }

        public override void Apply(Renderer.Renderer owner, TransformMode mode, Vector3 value)
        {
            var positions = new Dictionary<RenderShape, List<int>>();

            foreach (var vertex in Order)
            {
                var mesh = vertex.Shape.Mesh;

                var index = vertex.Shape.StartIndex + vertex.Index;

                if (index < 0 || index >= mesh.Vertices.Length)
                    continue;

                mesh.Vertices[index].Position = mode.Apply(Origin[vertex], Pivot, RotationAxis, value);

                if (!positions.TryGetValue(vertex.Shape, out var indices))
                {
                    indices = [];

                    positions[vertex.Shape] = indices;
                }

                indices.Add(vertex.Index);
            }

            foreach (var (shape, indices) in positions)
                shape.RefreshPositions(indices);

            owner.Vertices.MarkDotsDirty();
        }

        public override void Restore(Renderer.Renderer owner)
        {
            var positions = new Dictionary<RenderShape, List<int>>();

            foreach (var vertex in Order)
            {
                var mesh = vertex.Shape.Mesh;

                var index = vertex.Shape.StartIndex + vertex.Index;

                if (index < 0 || index >= mesh.Vertices.Length)
                    continue;

                mesh.Vertices[index].Position = Origin[vertex];

                if (!positions.TryGetValue(vertex.Shape, out var indices))
                {
                    indices = [];

                    positions[vertex.Shape] = indices;
                }

                indices.Add(vertex.Index);
            }

            foreach (var (shape, indices) in positions)
                shape.RefreshPositions(indices);

            owner.Vertices.MarkDotsDirty();
        }

        public override EditRecord BuildRecord(TransformMode mode, Vector3 value)
        {
            var shapes = new RenderShape[Order.Count];

            var indices = new int[Order.Count];

            var before = new Vector3[Order.Count];

            var after = new Vector3[Order.Count];

            for (var i = 0; i < Order.Count; i++)
            {
                var vertex = Order[i];

                shapes[i] = vertex.Shape;

                indices[i] = vertex.Index;

                before[i] = Origin[vertex];

                after[i] = mode.Apply(Origin[vertex], Pivot, RotationAxis, value);
            }

            return new MoveVerticesRecord(shapes, indices, before, after);
        }
    }
}
