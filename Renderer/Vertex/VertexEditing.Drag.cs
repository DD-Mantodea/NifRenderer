using NifRenderer.Editing.Transform;

namespace NifRenderer.Renderer.Vertex
{
    public sealed partial class VertexEditing
    {
        public bool Dragging => TransformOperator.Instance.Active;

        public bool BeginDrag(int screenX, int screenY) =>
            TransformOperator.Instance.Begin(_owner, _selected, new TranslateMode(), screenX, screenY);

        public void UpdateDrag(int screenX, int screenY) => TransformOperator.Instance.Update(screenX, screenY);

        public void CancelDrag() => TransformOperator.Instance.Cancel();

        public EditRecord? EndDrag() => TransformOperator.Instance.Confirm();
    }
}
