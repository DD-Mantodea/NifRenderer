using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NifRenderer.BodySlide;
using NifRenderer.Extensions;
using NifRenderer.Renderer.Scene;
using NifRenderer.UI.Components.Docking;
using NifRenderer.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using RendererCore = NifRenderer.Renderer.Renderer;
using RenderShape = NifRenderer.Renderer.Scene.RenderShape;

namespace NifRenderer.UI.Components.Dockers
{
    public class SlidersDocker : DockPanel
    {
        public const int TitleSize = 15;

        public const int NameSize = 12;

        public const int DetailSize = 12;

        private static int Pad => UIScale.Value(8);

        private static int HeaderHeight => UIScale.Value(30);

        public static int RowHeight => UIScale.Value(20);

        public static int ValueWidth => UIScale.Value(40);

        private const string Filter = "BodySlide 滑条集 (*.osp)|*.osp|所有文件 (*.*)|*.*";

        public static string LastFolder = "";

        private readonly List<SliderRow> _rows = [];

        private static readonly Dictionary<string, float> NoValues = [];

        private static readonly List<RenderShape> NoTargets = [];

        private readonly Dictionary<string, SliderDefinition> _definitions = [];

        private SliderSet? _definitionsFrom;

        private RenderMesh? Mesh => Renderer?.ActiveMesh;

        private Dictionary<string, float> _values => Mesh != null ? Mesh.SliderValues : NoValues;

        private List<RenderShape> _targets => Mesh != null ? Mesh.SliderTargets : NoTargets;

        private SliderSet? _set => Mesh?.Slides;

        private SliderApplicator? _applicator => Mesh?.Applicator;

        private void RefreshDefinitions()
        {
            var set = _set;

            if (ReferenceEquals(set, _definitionsFrom))
                return;

            _definitionsFrom = set;

            _definitions.Clear();

            if (set == null)
                return;

            foreach (var slider in set.Definition.Sliders)
                _definitions[slider.Name] = slider;

            BuildRows();
        }

        private int _scroll;

        private SliderRow? _dragRow;

        private bool _hoverButton;

        private int _contentTop;

        private int _contentBottom;

        public SlidersDocker(DockTabNode node) : base(node)
        {
            BackgroundColor = UITheme.PanelBackground;
        }

        public override string Title => "Sliders";

        public override bool OwnsHeader => true;

        public RendererCore? Renderer => NifDocker.Instance?.Graphics;

        public SliderSet? Set => _set;

        public IReadOnlyList<SliderRow> Rows => _rows;

        public IReadOnlyCollection<string> Targets => _targets.ConvertAll(shape => shape.ID);

        public float ValueOf(string name) => _values.TryGetValue(name, out var value) ? value : 0f;

        private int ScrollMax => Math.Max(0, _rows.Count * RowHeight - Math.Max(0, _contentBottom - _contentTop));

        private readonly TextBox _filter = new() { Visible = true, OnlyNumber = false };

        private string _filterText = "";

        private Rectangle SearchRect
        {
            get
            {
                var area = Rectangle;

                var top = area.Y + Pad * 2 + HeaderHeight;

                var height = UIScale.Value(22);

                return new Rectangle(area.X + Pad, top, Math.Max(1, area.Width - Pad * 2), height);
            }
        }

        private void RefreshFilter()
        {
            var text = _filter.Text ?? "";

            if (text == _filterText)
                return;

            _filterText = text;

            _scroll = 0;

            BuildRows();
        }

        private bool Matches(string name) => _filterText.Length == 0 || name.Contains(_filterText, StringComparison.OrdinalIgnoreCase);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Width = TabNode.Rect.Width;

            Height = TabNode.Rect.Height;

            Position = TabNode.Rect.Location.ToVector2();

            _hoverButton = ButtonRect.Contains(UserInput.GetMousePos().ToPoint());

            _clock += gameTime.ElapsedGameTime.TotalSeconds;

            _scrollFade = UIScrollbar.Ease(_scrollFade, NeedsScroll && (IsHovering || ScrollDragging), (float)gameTime.ElapsedGameTime.TotalSeconds);

            var search = SearchRect;

            _filter.Position = new Vector2(search.X, search.Y);

            _filter.Width = search.Width;

            _filter.Height = search.Height;

            _filter.LeftInset = UIScale.Value(16);

            RefreshFilter();

            RefreshDefinitions();

            LayoutRows();

            _editor.Update(gameTime);

            _filter.Update(gameTime);
        }

        private void LayoutRows()
        {
            var area = Rectangle;

            _contentTop = SearchRect.Bottom + Pad;

            _contentBottom = area.Bottom - Pad;

            _scroll = Math.Clamp(_scroll, 0, ScrollMax);

            var y = _contentTop - _scroll;

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];

                row.Index = i;

                row.Position = new Vector2(area.X, y);

                row.Width = Math.Max(1, area.Width - (NeedsScroll ? UIScrollbar.Thickness : 0));

                row.Visible = y + RowHeight > _contentTop && y < _contentBottom;

                y += RowHeight;
            }
        }

        private readonly TextBox _editor = new() { Visible = false, OnlyNumber = true };

        public bool Editing => TextBox.Active == _editor;

        public Rectangle EditingBox => _editor.Rectangle;

        private float _scrollFade;

        private bool NeedsScroll => _rows.Count * RowHeight > _contentBottom - _contentTop;

        private Rectangle ScrollArea => new(Rectangle.X, _contentTop, Rectangle.Width, Math.Max(1, _contentBottom - _contentTop));

        public bool ScrollDragging => ReferenceEquals(UIScrollbar.DragOwner, this);

        private Rectangle ValueRect(SliderRow row, Rectangle area)
        {
            return new Rectangle(row.Rectangle.Right - ValueWidth, row.Rectangle.Y, ValueWidth, RowHeight);
        }

        public SliderRow? EditingRow { get; private set; }

        private SliderRow? _lastClickRow;

        private double _clock;

        private double _lastClickAt;

        private void ClickRow(SliderRow row)
        {
            var doubleClick = ReferenceEquals(_lastClickRow, row) && _clock - _lastClickAt < 0.4;

            _lastClickRow = row;

            _lastClickAt = _clock;

            if (doubleClick)
                BeginEdit(row);
        }

        private void BeginEdit(SliderRow row)
        {
            var box = ValueRect(row, Rectangle);

            _editor.Position = new Vector2(box.X, box.Y);

            _editor.Width = box.Width;

            _editor.Height = box.Height;

            _editor.Text = row.Value.ToString("0.00", CultureInfo.InvariantCulture);

            _editor.Step = 0.05f;

            _editor.Min = 0f;

            _editor.Max = 1f;

            _editor.Visible = true;

            EditingRow = row;

            _editor.Committed = value =>
            {
                EditingRow = null;

                SetValue(row, value);
            };

            _editor.Cancelled = () => EditingRow = null;

            _editor.SetActive(true);
        }

        public void Load(string path)
        {
            var mesh = Mesh;

            if (mesh == null)
                return;

            var set = SliderSet.Load(path);

            mesh.Slides = set;

            mesh.Applicator = new SliderApplicator(set);

            mesh.SliderValues.Clear();

            _scroll = 0;

            BuildRows();

            CollectTargets();

            Apply();

            LastFolder = System.IO.Path.GetDirectoryName(path) ?? LastFolder;

        }

        private void BuildRows()
        {
            _rows.Clear();

            if (_applicator == null)
                return;

            foreach (var name in _applicator.Sliders)
            {
                if (!Matches(name))
                    continue;

                _rows.Add(new SliderRow(this, name) { Value = ValueOf(name) });
            }

            Apply();
        }

        private void CollectTargets()
        {
            var mesh = Mesh;

            var applicator = _applicator;

            mesh?.SliderTargets.Clear();

            if (mesh == null || applicator == null)
                return;

            foreach (var shape in mesh.RenderShapes)
            {
                if (applicator.Matches(shape.ID))
                    mesh.SliderTargets.Add(shape);
            }
        }

        private void Apply()
        {
            var applicator = _applicator;

            if (applicator == null)
                return;

            CollectTargets();

            applicator.ApplyAll(_targets, _values);
        }

        public (float Min, float Max) RangeOf(string name)
        {
            if (!_definitions.TryGetValue(name, out var definition))
                return (0f, 1f);

            var min = definition.Small / 100f;

            var max = definition.Big / 100f;

            if (max < min)
                (min, max) = (max, min);

            if (Math.Abs(max - min) < 0.0001f)
                max = min + 1f;

            return (min, max);
        }

        public void SetValue(SliderRow row, float value)
        {
            var (min, max) = RangeOf(row.Name);

            row.Value = Math.Clamp(value, min, max);

            _values[row.Name] = row.Value;

            Apply();
        }

        public void Reset(string name)
        {
            _values[name] = 0f;

            foreach (var row in _rows)
            {
                if (row.Name == name)
                    row.Value = 0f;
            }

            Apply();
        }

        public void ResetAll()
        {
            Mesh?.SliderValues.Clear();

            foreach (var row in _rows)
                row.Value = 0f;

            Apply();
        }

        private Rectangle HeaderBand
        {
            get
            {
                var area = Rectangle;

                return new Rectangle(area.X + Pad, area.Y + Pad, Math.Max(1, area.Width - Pad * 2), HeaderHeight);
            }
        }

        private Rectangle ButtonRect
        {
            get
            {
                var band = HeaderBand;

                var size = UIScale.Value(18);

                var inset = UIScale.Value(8);

                return new Rectangle(band.Right - inset - size, band.Y + (band.Height - size) / 2, size, size);
            }
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (Editing)
                return _editor.OnInput(args);

            if (ReferenceEquals(TextBox.Active, _filter))
                return _filter.OnInput(args);

            if (args.Type == InputType.MouseLeftDown && SearchRect.Contains(args.MousePosition.ToPoint()))
            {
                _filter.Text = _filterText;

                _filter.Visible = true;

                _filter.SetActive(true);

                return InputResult.Consumed;
            }

            switch (args.Type)
            {
                case InputType.MouseWheel:
                    if (!Rectangle.Contains(args.MousePosition.ToPoint()))
                        return InputResult.Continue;

                    _scroll = Math.Clamp(_scroll - Math.Sign(args.WheelDelta) * RowHeight * 3, 0, ScrollMax);

                    LayoutRows();

                    return InputResult.Consumed;

                case InputType.MouseLeftDown:
                    if (ButtonRect.Contains(args.MousePosition.ToPoint()))
                    {
                        LoadFromDialog();

                        return InputResult.Consumed;
                    }

                    if (NeedsScroll && _scrollFade > 0.5f && UIScrollbar.Contains(ScrollArea, args.MousePosition.ToPoint(), _rows.Count * RowHeight, ScrollArea.Height))
                    {
                        UIScrollbar.DragOwner = this;

                        _scroll = UIScrollbar.ScrollFrom(ScrollArea, _rows.Count * RowHeight, ScrollArea.Height, args.MousePosition.Y);

                        UIManager.Instance.CapturedComponent = this;

                        LayoutRows();

                        return InputResult.Consumed;
                    }

                    _dragRow = RowAt(args.MousePosition);

                    if (_dragRow == null)
                        return InputResult.Continue;

                    if (ValueRect(_dragRow, Rectangle).Contains(args.MousePosition.ToPoint()))
                    {
                        ClickRow(_dragRow);

                        return InputResult.Consumed;
                    }

                    UIManager.Instance.CapturedComponent = this;

                    DragTo(args.MousePosition.X);

                    return InputResult.Consumed;

                case InputType.MouseMove:
                    if (ScrollDragging)
                    {
                        _scroll = UIScrollbar.ScrollFrom(ScrollArea, _rows.Count * RowHeight, ScrollArea.Height, args.MousePosition.Y);

                        LayoutRows();

                        return InputResult.Consumed;
                    }

                    if (_dragRow == null)
                        return InputResult.Continue;

                    DragTo(args.MousePosition.X);

                    return InputResult.Consumed;

                case InputType.MouseLeftUp:
                    if (ScrollDragging)
                    {
                        UIScrollbar.DragOwner = null;

                        UIManager.Instance.CapturedComponent = null;

                        return InputResult.Consumed;
                    }

                    if (_dragRow == null)
                        return InputResult.Continue;

                    _dragRow = null;

                    UIManager.Instance.CapturedComponent = null;

                    return InputResult.Consumed;

                case InputType.MouseRightDown:
                    var hit = RowAt(args.MousePosition);

                    if (hit == null)
                        return InputResult.Continue;

                    BuildMenu(hit, args.MousePosition);

                    return InputResult.Consumed;
            }

            return InputResult.Continue;
        }

        private void DragTo(float x)
        {
            var row = _dragRow;

            if (row == null)
                return;

            var rect = row.Rectangle;

            var (min, max) = RangeOf(row.Name);

            var t = rect.Width <= 0 ? 0f : Math.Clamp((x - rect.X) / rect.Width, 0f, 1f);

            SetValue(row, min + (max - min) * t);
        }

        private SliderRow? RowAt(Vector2 position)
        {
            var point = position.ToPoint();

            if (point.X < Rectangle.X || point.X >= Rectangle.Right)
                return null;

            if (point.Y < _contentTop || point.Y >= _contentBottom)
                return null;

            foreach (var row in _rows)
            {
                if (row.Visible && row.Rectangle.Contains(point))
                    return row;
            }

            return null;
        }

        private void BuildMenu(SliderRow row, Vector2 position)
        {
            var entries = new List<ContextMenu.Entry>
            {
                ContextMenu.Entry.Of("Reset", () => Reset(row.Name)),

                ContextMenu.Entry.Split(),

                ContextMenu.Entry.Of("Reset All", ResetAll)
            };

            ContextMenu.Show(entries, position);
        }

        private void LoadFromDialog()
        {
            var start = string.IsNullOrEmpty(LastFolder) ? AppContext.BaseDirectory : LastFolder;

            var path = OpenDialog.Ask("打开 BodySlide 滑条集", Filter, System.IO.Path.Combine(start, "sliders.osp"));

            if (path == null)
                return;

            Load(path);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.Draw(spriteBatch, gameTime);

            var area = Rectangle;

            if (area.Width <= Pad * 2 || area.Height <= HeaderHeight + Pad * 2)
                return;

            DrawHeader(spriteBatch, area);

            DrawSearch(spriteBatch);

            if (_rows.Count == 0)
            {
                var hint = _set == null ? "load a .osp slider set" : "no matching shape";

                UIFont.Draw(spriteBatch, hint, new Vector2(area.X + Pad + UIScale.Value(4), _contentTop + UIScale.Value(4)), UITheme.TextFaint, DetailSize);

                return;
            }

            foreach (var row in _rows)
            {
                if (row.Visible)
                    row.Draw(spriteBatch);
            }

            if (_editor.Visible)
                _editor.Draw(spriteBatch, gameTime);

            UIScrollbar.Draw(spriteBatch, ScrollArea, _rows.Count * RowHeight, ScrollArea.Height, _scroll, _scrollFade);
        }

        private void DrawSearch(SpriteBatch spriteBatch)
        {
            _filter.Draw(spriteBatch, new GameTime());

            var box = SearchRect;

            var size = UIScale.Value(12);

            var icon = SkiaIcon.Load(spriteBatch.GraphicsDevice, "search.svg", size, UITheme.TextFaint.ToSk());

            if (icon != null)
            {
                var glyph = new Rectangle(box.X + UIScale.Value(6), box.Y + (box.Height - size) / 2, size, size);

                spriteBatch.Draw(icon, glyph, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
            }
        }

        private void DrawHeader(SpriteBatch spriteBatch, Rectangle area)
        {
            var band = new Rectangle(area.X + Pad, area.Y + Pad, area.Width - Pad * 2, HeaderHeight);

            SkiaCard.DrawRounded(spriteBatch, band, UITheme.PanelHeader, UITheme.PanelHeader, UIMetrics.WidgetRoundness(0.2f), 0f);

            var textY = band.Y + (band.Height - UIScale.Value(TitleSize)) / 2;

            var titleLeft = band.X + UIScale.Value(8);

            var titleRight = ButtonRect.X - UIScale.Value(6);

            if (titleRight > titleLeft)
            {
                UIManager.Clipped(spriteBatch, new Rectangle(titleLeft, band.Y, titleRight - titleLeft, band.Height), () =>
                    UIFont.Draw(spriteBatch, "Sliders", new Vector2(titleLeft, textY), UITheme.TextPrimary, TitleSize));
            }

            DrawButton(spriteBatch);

            if (_set == null)
                return;

            var text = _targets.Count == 0
                ? $"{_rows.Count} sliders"
                : $"{_rows.Count} sliders / {_targets.Count} shapes";

            var titleWidth = (int)UIFont.Measure("Sliders", TitleSize).X;

            var left = titleLeft + titleWidth + UIScale.Value(10);

            var right = ButtonRect.X - UIScale.Value(8);

            if (right <= left)
                return;

            var infoRect = new Rectangle(left, band.Y, right - left, band.Height);

            UIManager.Clipped(spriteBatch, infoRect, () =>
                UIFont.Draw(spriteBatch, text, new Vector2(left, textY + UIScale.Value(2)), _targets.Count > 0 ? UITheme.Accent : UITheme.TextDim, DetailSize));
        }

        private void DrawButton(SpriteBatch spriteBatch)
        {
            var rect = ButtonRect;

            var fill = _hoverButton ? UITheme.RowOddHover : UITheme.RowOdd;

            SkiaCard.DrawRounded(spriteBatch, rect, fill, UITheme.TextFaint, UIMetrics.WidgetRoundness(0.2f), 1f);

            var size = UIScale.Value(12);

            var box = new Rectangle(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);

            var color = _hoverButton ? UITheme.TextPrimary : UITheme.TextDim;

            var icon = SkiaIcon.Load(spriteBatch.GraphicsDevice, "file.svg", size, new SKColor(color.R, color.G, color.B, color.A));

            if (icon != null)
                spriteBatch.Draw(icon, box, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
        }
    }

    public class SliderRow(SlidersDocker owner, string name)
    {
        public readonly SlidersDocker Owner = owner;

        public readonly string Name = name;

        public int Index;

        public float Value;

        public bool Visible;

        public Vector2 Position;

        public int Width = 1;

        public Rectangle Rectangle => new((int)Position.X, (int)Position.Y, Width, SlidersDocker.RowHeight);

        public float Fraction
        {
            get
            {
                var (min, max) = Owner.RangeOf(Name);

                return Math.Clamp((Value - min) / (max - min), 0f, 1f);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var rect = Rectangle;

            var hover = UserInput.GetMouseRectangle().Intersects(rect) && Owner.IsHovering;

            var track = new Rectangle(rect.X, rect.Y + UIScale.Value(2), rect.Width, Math.Max(1, rect.Height - UIScale.Value(4)));

            var odd = Index % 2 == 0;

            Color fill;

            if (hover)
                fill = UITheme.RowEvenHover;
            else
                fill = odd ? UITheme.RowOdd : UITheme.RowEven;

            spriteBatch.DrawRectangle(track, fill);

            var fraction = Fraction;

            var width = (int)MathF.Round(track.Width * fraction);

            if (width > 0)
                spriteBatch.DrawRectangle(new Rectangle(track.X, track.Y, width, track.Height), UITheme.RowSelected);

            var size = SlidersDocker.NameSize;

            var textY = rect.Y + (rect.Height - UIScale.Value(size)) / 2f;

            var value = Value.ToString("0.00", CultureInfo.InvariantCulture);

            var valueRect = new Rectangle(rect.Right - SlidersDocker.ValueWidth, rect.Y, SlidersDocker.ValueWidth, rect.Height);

            var nameRect = new Rectangle(rect.X, rect.Y, Math.Max(1, valueRect.X - rect.X), rect.Height);

            UIManager.Clipped(spriteBatch, nameRect, () =>
                UIFont.Draw(spriteBatch, Name, new Vector2(nameRect.X + UIScale.Value(12), textY), hover ? UITheme.TextPrimary : UITheme.TextDim, size));

            var valueWidth = (int)UIFont.Measure(value, size).X;

            if (!ReferenceEquals(Owner.EditingRow, this))
                UIFont.Draw(spriteBatch, value, new Vector2(valueRect.Right - UIScale.Value(6) - valueWidth, textY), Value == 0f ? UITheme.TextFaint : UITheme.TextPrimary, size);
        }
    }
}
