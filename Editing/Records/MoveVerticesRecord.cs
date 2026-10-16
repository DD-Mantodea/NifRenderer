using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NifRenderer.Renderer;

namespace NifRenderer.Editing.Records
{
    public sealed class MoveVerticesRecord : EditRecord
    {
        private readonly RenderShape[] _shapes;

        private readonly int[] _indices;

        private readonly Vector3[] _before;

        private readonly Vector3[] _after;

        private readonly RenderShape[] _touched;

        private readonly int[][] _touchedIndices;

        public MoveVerticesRecord(RenderShape[] shapes, int[] indices, Vector3[] before, Vector3[] after)
        {
            _shapes = shapes;

            _indices = indices;

            _before = before;

            _after = after;

            var grouped = new Dictionary<RenderShape, List<int>>();

            for (var i = 0; i < shapes.Length && i < indices.Length; i++)
            {
                if (!grouped.TryGetValue(shapes[i], out var list))
                {
                    list = [];

                    grouped[shapes[i]] = list;
                }

                list.Add(indices[i]);
            }

            _touched = [.. grouped.Keys];

            _touchedIndices = new int[_touched.Length][];

            for (var i = 0; i < _touched.Length; i++)
                _touchedIndices[i] = [.. grouped[_touched[i]]];
        }

        public override void Redo() => Apply(_after);

        public override void Undo() => Apply(_before);

        private void Apply(Vector3[] positions)
        {
            for (var i = 0; i < _indices.Length; i++)
            {
                var shape = _shapes[i];

                var mesh = shape.Mesh;

                var index = shape.StartIndex + _indices[i];

                if (index < 0 || index >= mesh.Vertices.Length)
                    continue;

                mesh.Vertices[index].Position = positions[i];
            }

            for (var i = 0; i < _touched.Length; i++)
                _touched[i].RefreshPositions(_touchedIndices[i]);
        }
    }
}
