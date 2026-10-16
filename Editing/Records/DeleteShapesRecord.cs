using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Renderer;
using NifRenderer.Utils;

namespace NifRenderer.Editing.Records
{
    public sealed class DeleteShapesRecord : EditRecord
    {
        private sealed class ShapeEntry
        {
            public RenderMesh Mesh;

            public int Index;

            public RenderShape Shape;

            public VertexPositionColorNormalTexture[] Slice;
        }

        private sealed class ObjectEntry
        {
            public RenderObject Item;

            public int Index;
        }

        private readonly Renderer.Renderer _owner;

        private readonly List<ShapeEntry> _shapes = [];

        private readonly List<ObjectEntry> _objects = [];

        private readonly List<RenderShape> _selection = [];

        private bool _captured;

        public DeleteShapesRecord(Renderer.Renderer owner, IReadOnlyList<RenderShape> shapes, IEnumerable<RenderShape> selection)
        {
            _owner = owner;

            foreach (var shape in shapes)
            {
                if (shape?.Mesh == null)
                    continue;

                _shapes.Add(new ShapeEntry
                {
                    Mesh = shape.Mesh,
                    Index = shape.Mesh.RenderShapes.IndexOf(shape),
                    Shape = shape,
                    Slice = shape.Mesh.ShapeSlice(shape)
                });
            }

            _selection.AddRange(selection);
        }

        public override void Redo()
        {
            SortShapes();

            for (var i = _shapes.Count - 1; i >= 0; i--)
            {
                var entry = _shapes[i];

                if (entry.Index >= 0)
                    entry.Mesh.RemoveShapeAt(entry.Index);
            }

            if (!_captured)
            {
                _captured = true;

                CaptureEmptied();
            }

            for (var i = _objects.Count - 1; i >= 0; i--)
                _owner.RemoveObjectAt(_objects[i].Index);

            foreach (var shape in _selection)
                _owner.RemoveFromSelection(shape);
        }

        public override void Undo()
        {
            foreach (var entry in _objects)
                _owner.InsertObjectAt(entry.Index, entry.Item);

            SortShapes();

            foreach (var entry in _shapes)
                entry.Mesh.InsertShape(entry.Index, entry.Shape, entry.Slice);

            foreach (var shape in _selection)
                _owner.AddToSelection(shape);
        }

        private void SortShapes()
        {
            if (_sorted)
                return;

            _sorted = true;

            _shapes.Sort(static (a, b) =>
            {
                var mesh = a.Mesh.GetHashCode().CompareTo(b.Mesh.GetHashCode());

                return mesh != 0 ? mesh : a.Index.CompareTo(b.Index);
            });
        }

        private bool _sorted;

        private void CaptureEmptied()
        {
            var objects = _owner.ObjectList;

            for (var i = 0; i < objects.Count; i++)
            {
                if (objects[i].Mesh.RenderShapes.Count == 0)
                    _objects.Add(new ObjectEntry { Item = objects[i], Index = i });
            }
        }
    }
}
