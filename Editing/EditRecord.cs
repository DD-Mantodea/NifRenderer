using NiflySharp;

namespace NifRenderer.Editing
{
    public abstract class EditRecord
    {
        public abstract void Redo();

        public abstract void Undo();

        public virtual void Export(INiShape shape)
        {
        }
    }
}
