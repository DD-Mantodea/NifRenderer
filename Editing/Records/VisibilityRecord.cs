using System.Collections.Generic;
using NifRenderer.Renderer;

namespace NifRenderer.Editing.Records
{
    public sealed class VisibilityRecord : EditRecord
    {
        private readonly RenderShape[] _shapes;

        private readonly bool[] _before;

        private readonly bool[] _after;

        public VisibilityRecord(IReadOnlyList<RenderShape> shapes, bool visible)
        {
            _shapes = [.. shapes];

            _before = new bool[_shapes.Length];

            _after = new bool[_shapes.Length];

            for (var i = 0; i < _shapes.Length; i++)
            {
                _before[i] = _shapes[i].Visible;

                _after[i] = visible;
            }
        }

        public override void Redo() => Apply(_after);

        public override void Undo() => Apply(_before);

        private void Apply(bool[] states)
        {
            for (var i = 0; i < _shapes.Length; i++)
                _shapes[i].Visible = states[i];
        }
    }
}
