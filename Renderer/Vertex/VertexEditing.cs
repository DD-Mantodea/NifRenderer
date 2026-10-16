using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NifRenderer.Renderer.Vertex
{
    public enum EditMode
    {
        Object,

        Vertex
    }

    public readonly record struct VertexRef(RenderShape Shape, int Index);

    public sealed partial class VertexEditing
    {
        public const int VertexSize = 7;

        public static readonly Color WireColor = new(0, 0, 0);

        public static readonly Color SelectColor = new(255, 122, 0);

        public static readonly Color EdgeSelectColor = new(255, 153, 0);

        public static readonly Color ActiveColor = new(255, 255, 255);

        public const float PickRadius = 8f;

        private readonly Renderer _owner;

        private readonly HashSet<VertexRef> _selected = [];

        private readonly Dictionary<RenderShape, bool[]> _marks = [];

        private VertexRef? _active;

        private RenderShape? _lastShape;

        private bool[]? _lastMarks;

        private readonly List<VertexRef> _buffer = [];

        private VertexPositionColorTexture[] _quadArray = new VertexPositionColorTexture[4096];

        private int _quadCount;

        private bool _dotsDirty = true;


        private DynamicVertexBuffer? _vertexBuffer;

        private int _bufferCapacity;

        private readonly List<RenderShape> _targets = [];

        public VertexEditing(Renderer owner)
        {
            _owner = owner;
        }

        public EditMode Mode { get; private set; } = EditMode.Object;

        public bool Active => Mode == EditMode.Vertex;

        public bool HasSelection => _selected.Count > 0;

        public int SelectionCount => _selected.Count;

        public IReadOnlyList<RenderShape> Targets => _targets;

        public IReadOnlyCollection<VertexRef> Selected => _selected;

        private Rectangle? _box;

        public bool ContainsTarget(RenderShape shape) => _targets.Contains(shape);

        public void Toggle()
        {
            var before = _targets.ToArray();

            var after = Mode == EditMode.Vertex ? [] : _owner.SelectedShapes().ToArray();

            if (after.Length == 0 && Mode != EditMode.Vertex)
                return;

            var record = new EditModeRecord(this, before, after);

            record.Redo();

            _owner.History.Record(record);
        }

        public void Enter(IReadOnlyList<RenderShape> targets)
        {
            _targets.Clear();

            _targets.AddRange(targets);

            _selected.Clear();

            _marks.Clear();

            _lastShape = null;

            _lastMarks = null;

            Mode = targets.Count > 0 ? EditMode.Vertex : EditMode.Object;

            Touch();
        }

        public void Exit()
        {
            var leaving = _targets.ToArray();

            Mode = EditMode.Object;

            _targets.Clear();

            _selected.Clear();

            _marks.Clear();

            _lastShape = null;

            _lastMarks = null;

            _dotsDirty = true;

            foreach (var shape in leaving)
                shape.MarkWireDirty();

            ReleaseBuffer();
        }


        public bool Contains(VertexRef vertex) => _selected.Contains(vertex);

        public void Clear()
        {
            _selected.Clear();

            foreach (var marks in _marks.Values)
                Array.Clear(marks);

            _lastShape = null;

            _lastMarks = null;

            _active = null;

            Touch();
        }

        public void Select(VertexRef? vertex)
        {
            _selected.Clear();

            foreach (var marks in _marks.Values)
                Array.Clear(marks);

            _lastShape = null;

            _lastMarks = null;

            _active = vertex;

            if (vertex.HasValue)
            {
                _selected.Add(vertex.Value);

                Marks(vertex.Value.Shape)[vertex.Value.Index] = true;
            }

            Touch();
        }

        public void Toggle(VertexRef? vertex)
        {
            if (!vertex.HasValue)
                return;

            var value = vertex.Value;

            var marks = Marks(value.Shape);

            if (_selected.Remove(value))
            {
                marks[value.Index] = false;

                if (_active.HasValue && _active.Value == value)
                    _active = null;

                return;
            }

            _selected.Add(value);

            marks[value.Index] = true;

            _active = value;

            Touch();
        }

        public void SelectIn(Rectangle box)
        {
            _selected.Clear();

            foreach (var marks in _marks.Values)
                Array.Clear(marks);

            _lastShape = null;

            _lastMarks = null;

            _active = null;

            foreach (var vertex in Collect(box))
            {
                _selected.Add(vertex);

                Marks(vertex.Shape)[vertex.Index] = true;
            }

            Touch();
        }

        public void SelectAll()
        {
            _selected.Clear();

            foreach (var marks in _marks.Values)
                Array.Clear(marks);

            _lastShape = null;

            _lastMarks = null;

            _active = null;

            foreach (var shape in _targets)
            {
                if (!shape.Visible)
                    continue;

                var marks = Marks(shape);

                for (var i = 0; i < shape.VertexCount; i++)
                {
                    if (shape.IsVertexDeleted(i))
                        continue;

                    _selected.Add(new VertexRef(shape, i));

                    marks[i] = true;
                }
            }

            Touch();
        }

        public void MarkDotsDirty() => _dotsDirty = true;

        public void PruneSelection()
        {
            var removed = _selected.RemoveWhere(v => v.Shape.IsVertexDeleted(v.Index));

            if (removed == 0)
                return;

            if (_active.HasValue && _active.Value.Shape.IsVertexDeleted(_active.Value.Index))
                _active = null;

            foreach (var marks in _marks.Values)
                Array.Clear(marks);

            _lastShape = null;

            _lastMarks = null;

            foreach (var vertex in _selected)
                Marks(vertex.Shape)[vertex.Index] = true;

            _dotsDirty = true;
        }

        public void PruneTargets()
        {
            if (!Active)
                return;

            var removed = _targets.RemoveAll(shape => shape.Mesh == null || !shape.Mesh.RenderShapes.Contains(shape));

            if (removed == 0)
                return;

            _marks.Clear();

            _selected.Clear();

            _active = null;

            _dotsDirty = true;

            if (_targets.Count == 0)
                Mode = EditMode.Object;
        }

        private void Touch()
        {
            _dotsDirty = true;

            foreach (var shape in _targets)
                shape.MarkWireDirty();
        }

        private bool[] Marks(RenderShape shape)
        {
            if (ReferenceEquals(shape, _lastShape) && _lastMarks != null && _lastMarks.Length == shape.VertexCount)
                return _lastMarks;

            if (!_marks.TryGetValue(shape, out var marks) || marks.Length != shape.VertexCount)
            {
                marks = new bool[shape.VertexCount];

                _marks[shape] = marks;

                _lastShape = null;

                _lastMarks = null;
            }

            _lastShape = shape;

            _lastMarks = marks;

            return marks;
        }

        private bool IsMarked(RenderShape shape, int index)
        {
            var marks = Marks(shape);

            return index >= 0 && index < marks.Length && marks[index];
        }

        public Color DotColor(RenderShape shape, int index)
        {
            if (IsTransforming(shape, index))
                return TransformColor;

            if (_active.HasValue && _active.Value.Shape == shape && _active.Value.Index == index)
                return ActiveColor;

            return IsMarked(shape, index) ? SelectColor : WireColor;
        }

        private static readonly Color TransformColor = new(255, 255, 255);

        private static bool IsTransforming(RenderShape shape, int index) =>
            TransformOperator.Instance.Active
            && (TransformOperator.Instance.Targets as TransformTargets) != null
            && (TransformOperator.Instance.Targets as TransformTargets).Origin.ContainsKey(new VertexRef(shape, index));

        public Color LineColor(RenderShape shape, int index) =>
            IsTransforming(shape, index) ? TransformColor :
            IsMarked(shape, index) ? EdgeSelectColor : WireColor;

        private int DotSize(RenderShape shape, int index) => VertexSize;

        public void BeginBox(Point start) => _box = new Rectangle(start.X, start.Y, 0, 0);

        public void UpdateBox(Point start, Point current)
        {
            var x = Math.Min(start.X, current.X);

            var y = Math.Min(start.Y, current.Y);

            _box = new Rectangle(x, y, Math.Abs(current.X - start.X), Math.Abs(current.Y - start.Y));
        }

        public void EndBox() => _box = null;

        private List<VertexRef> Collect(Rectangle box)
        {
            _buffer.Clear();

            var viewport = _owner.Viewport;

            if (viewport.Width <= 0 || viewport.Height <= 0)
                return _buffer;

            foreach (var shape in _targets)
            {
                if (!shape.Visible)
                    continue;

                var vertices = shape.Mesh.Vertices;

                for (var i = 0; i < shape.VertexCount; i++)
                {
                    if (shape.IsVertexDeleted(i))
                        continue;

                    var index = shape.StartIndex + i;

                    if (index < 0 || index >= vertices.Length)
                        continue;

                    if (!_owner.TryProject(vertices[index].Position, viewport, out var x, out var y))
                        continue;

                    if (box.Contains((int)x, (int)y))
                        _buffer.Add(new VertexRef(shape, i));
                }
            }

            return _buffer;
        }

        public VertexRef? Pick(int screenX, int screenY)
        {
            var viewport = _owner.Viewport;

            if (viewport.Width <= 0 || viewport.Height <= 0)
                return null;

            VertexRef? best = null;

            var bestDistance = PickRadius * PickRadius;

            foreach (var shape in _targets)
            {
                if (!shape.Visible)
                    continue;

                var vertices = shape.Mesh.Vertices;

                for (var i = 0; i < shape.VertexCount; i++)
                {
                    if (shape.IsVertexDeleted(i))
                        continue;

                    var index = shape.StartIndex + i;

                    if (index < 0 || index >= vertices.Length)
                        continue;

                    if (!_owner.TryProject(vertices[index].Position, viewport, out var x, out var y))
                        continue;

                    var dx = x - screenX;

                    var dy = y - screenY;

                    var distance = dx * dx + dy * dy;

                    if (distance > bestDistance)
                        continue;

                    bestDistance = distance;

                    best = new VertexRef(shape, i);
                }
            }

            return best;
        }

        public int DeleteSelected()
        {
            if (_selected.Count == 0)
                return 0;

            var removed = 0;

            foreach (var group in _selected.GroupBy(v => v.Shape))
            {
                var indices = group.Select(v => v.Index).Distinct().OrderBy(i => i).ToArray();

                if (indices.Length == 0)
                    continue;

                var record = new DeleteVerticesRecord(group.Key, indices);

                record.Redo();

                _owner.History.Record(record);

                removed += indices.Length;
            }

            Clear();

            foreach (var shape in _targets)
                shape.RebuildBuffers();

            _dotsDirty = true;

            return removed;
        }

        public void Draw(GraphicsDevice device, Rectangle viewport, Effect effect)
        {
            if (_dotsDirty)
            {
                BuildDots();

                _dotsDirty = false;
            }

            if (_quadCount == 0)
                return;

            device.BlendState = BlendState.Opaque;

            device.DepthStencilState = DepthStencilState.Default;

            device.RasterizerState = _owner.SolidState;

            effect.Parameters["xPointSize"]?.SetValue(new Vector2(VertexSize));

            effect.Parameters["xPointScale"]?.SetValue(new Vector2(2f / Math.Max(1, viewport.Width), -2f / Math.Max(1, viewport.Height)));

            effect.CurrentTechnique = effect.Techniques["RoundPoint"];

            EnsureBuffer(device, _quadCount);

            _vertexBuffer.SetData(_quadArray, 0, _quadCount);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                device.SetVertexBuffer(_vertexBuffer);

                pass.Apply();

                device.DrawPrimitives(PrimitiveType.TriangleList, 0, _quadCount / 3);
            }

            device.SetVertexBuffer(null);

            device.DepthStencilState = DepthStencilState.Default;
        }

        private void EnsureBuffer(GraphicsDevice device, int count)
        {
            if (_vertexBuffer != null && _bufferCapacity >= count && !_vertexBuffer.IsDisposed)
                return;

            _vertexBuffer?.Dispose();

            _bufferCapacity = Math.Max(count, 4096);

            _vertexBuffer = new DynamicVertexBuffer(device, VertexPositionColorTexture.VertexDeclaration, _bufferCapacity, BufferUsage.WriteOnly);
        }

        public void ReleaseBuffer()
        {
            _vertexBuffer?.Dispose();

            _vertexBuffer = null;

            _bufferCapacity = 0;
        }

        private void BuildDots()
        {
            _quadCount = 0;

            var needed = 0;

            foreach (var shape in _targets)
            {
                if (shape.Visible)
                    needed += shape.VertexCount * 6;
            }

            if (_quadArray.Length < needed)
                _quadArray = new VertexPositionColorTexture[needed + 4096];

            foreach (var shape in _targets)
            {
                if (!shape.Visible)
                    continue;

                var vertices = shape.Mesh.Vertices;

                for (var i = 0; i < shape.VertexCount; i++)
                {
                    if (shape.IsVertexDeleted(i))
                        continue;

                    var index = shape.StartIndex + i;

                    if (index < 0 || index >= vertices.Length)
                        continue;

                    AppendQuad(vertices[index].Position, DotColor(shape, i));
                }
            }
        }

        private void AppendQuad(Vector3 world, Color color)
        {
            _quadArray[_quadCount++] = new(world, color, new Vector2(0f, 0f));
            _quadArray[_quadCount++] = new(world, color, new Vector2(1f, 0f));
            _quadArray[_quadCount++] = new(world, color, new Vector2(0f, 1f));

            _quadArray[_quadCount++] = new(world, color, new Vector2(1f, 0f));
            _quadArray[_quadCount++] = new(world, color, new Vector2(1f, 1f));
            _quadArray[_quadCount++] = new(world, color, new Vector2(0f, 1f));
        }
    }
}
