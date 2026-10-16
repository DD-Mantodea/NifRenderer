using System.Collections.Generic;
using NifRenderer.Renderer;

namespace NifRenderer.Editing.Records
{
    public sealed class EditModeRecord : EditRecord
    {
        private readonly VertexEditing _editing;

        private readonly RenderShape[] _before;

        private readonly RenderShape[] _after;

        public EditModeRecord(VertexEditing editing, IReadOnlyList<RenderShape> before, IReadOnlyList<RenderShape> after)
        {
            _editing = editing;

            _before = [.. before];

            _after = [.. after];
        }

        public override void Redo() => Apply(_after);

        public override void Undo() => Apply(_before);

        private void Apply(RenderShape[] targets)
        {
            if (targets.Length == 0)
                _editing.Exit();
            else
                _editing.Enter(targets);
        }
    }
}
