namespace NifRenderer.Renderer.Scene
{
    public class RenderObject
    {
        public RenderMesh Mesh { get; }

        public string Path { get; }

        public string FileName { get; }

        public string Name { get; set; }

        public bool AllVisible
        {
            get
            {
                foreach (var shape in Mesh.RenderShapes)
                {
                    if (!shape.Visible)
                        return false;
                }

                return Mesh.RenderShapes.Count > 0;
            }
        }

        public bool AnyVisible
        {
            get
            {
                foreach (var shape in Mesh.RenderShapes)
                {
                    if (shape.Visible)
                        return true;
                }

                return false;
            }
        }

        public void SetVisible(bool visible)
        {
            foreach (var shape in Mesh.RenderShapes)
                shape.Visible = visible;
        }

        public RenderObject(RenderMesh mesh, string path, string name)
        {
            Mesh = mesh;

            Path = path;

            FileName = string.IsNullOrEmpty(path) ? "unnamed" : System.IO.Path.GetFileName(path);

            Name = string.IsNullOrEmpty(name) ? FileName : name;
        }

        public override string ToString() => Name;
    }
}
