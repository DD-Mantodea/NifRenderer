using System.Collections.Generic;
using NifRenderer.Renderer;

namespace NifRenderer.Editing.Records
{
    public sealed class DeleteVerticesRecord : EditRecord
    {
        private readonly RenderShape _shape;

        private readonly int[] _indices;

        private readonly bool[] _before;

        public DeleteVerticesRecord(RenderShape shape, int[] indices)
        {
            _shape = shape;

            _indices = indices;

            _before = new bool[indices.Length];

            for (var i = 0; i < indices.Length; i++)
                _before[i] = shape.IsVertexDeleted(indices[i]);
        }

        public override void Redo()
        {
            foreach (var index in _indices)
                _shape.DeletedVertices.Add(index);

            _shape.RebuildBuffers();
        }

        public override void Undo()
        {
            for (var i = 0; i < _indices.Length; i++)
            {
                if (_before[i])
                    continue;

                _shape.DeletedVertices.Remove(_indices[i]);
            }

            _shape.RebuildBuffers();
        }

        public IReadOnlyList<int> Indices => _indices;

        public RenderShape Shape => _shape;
    }
}
