using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NifRenderer.Extensions;
using NifRenderer.UI.Components.Dockers;
using NifRenderer.UI.Components.Docking;
using NifRenderer.Utils;
using SkiaGameRendering;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace NifRenderer.UI.Components
{
    public class ScreenContainer : Component
    {
        public static ScreenContainer Instance;

        public ScreenContainer()
        {
            CanHit = true;

            Width = GameHost.Instance.Window.ClientBounds.Width;

            Height = GameHost.Instance.Window.ClientBounds.Height;

            Instance = this;

            var nifTab = new DockTabNode();

            nifTab.AddPanel(new NifDocker(nifTab));

            var infoTab = new DockTabNode();

            Meshes = new MeshesDocker(infoTab);

            infoTab.AddPanel(Meshes);

            Sliders = new SlidersDocker(new DockTabNode());

            _infoTab = infoTab;

            _nifTab = nifTab;

            DockRoot = DefaultLayout();

            UIScale.Changed += OnScaleChanged;

            UserInput.FocusLost += CancelDrags;

            Arrange();
        }

        private void CancelDrags()
        {
            _dragSplitter = null;

            _dragPanel = null;

            _dropTarget = null;

            _moved = false;

            if (ReferenceEquals(UIManager.Instance?.CapturedComponent, this))
                UIManager.Instance!.CapturedComponent = null;
        }

        private DockTabNode? _nifTab;

        private DockSplitNode DefaultLayout()
        {
            var canvas = Canvas;

            var infoWidth = UIScale.Value(Math.Clamp(canvas.Width / (6 * UIScale.Factor), 220, 260));

            var available = Math.Max(1, canvas.Width - DockSplitNode.SplitterThickness);

            var ratio = Math.Clamp(1f - infoWidth / (float)available, DockSplitNode.MinRatio, DockSplitNode.MaxRatio);

            return new DockSplitNode(DockSplitDirection.Horizontal, _nifTab!, _infoTab!, ratio);
        }

        public void ResetLayout()
        {
            foreach (var panel in new DockPanel?[] { Sliders })
                Detach(panel);

            SetPanelDocked(Meshes, true);

            if (_nifTab != null && _infoTab != null)
                DockRoot = DefaultLayout();

            Arrange();
        }

        public int SlotOf(DockPanel? panel)
        {
            if (ReferenceEquals(panel, Meshes))
                return 0;

            if (ReferenceEquals(panel, Sliders))
                return 1;

            return 99;
        }

        public MeshesDocker? Meshes;

        public SlidersDocker? Sliders;

        private DockTabNode? _infoTab;

        public void ApplyModeVisibility()
        {
            var renderer = NifDocker.Instance?.Graphics;

            if (renderer == null)
                return;

            SetPanelDocked(Sliders, renderer.Mode == EditorMode.Slider);
        }

        private void ShowSliders()
        {
            if (DockRoot == null || Sliders == null || _infoTab == null)
                return;

            if (DockTree.FindTab(DockRoot, Sliders) != null)
                return;

            var tab = new DockTabNode();

            tab.AddPanel(Sliders);

            var split = new DockSplitNode(DockSplitDirection.Vertical, _infoTab, tab, 0.5f);

            DockTree.Replace(ref DockRoot, _infoTab, split);

            Arrange();
        }

        private static DockTabNode? DockTabNodeOf(DockNode node, DockPanel panel)
        {
            if (node is DockTabNode tab)
                return tab.Panels.Contains(panel) ? tab : null;

            if (node is DockSplitNode split)
                return DockTabNodeOf(split.First, panel) ?? DockTabNodeOf(split.Second, panel);

            return null;
        }

        private void HideSliders()
        {
            if (DockRoot == null || Sliders == null)
                return;

            if (DockTree.FindTab(DockRoot, Sliders) == null)
                return;

            Detach(Sliders);
        }

        private void OnScaleChanged()
        {
            UIFont.Invalidate();

            SkiaIcon.Clear();

            SkiaCard.Clear();

            Arrange();
        }

        public DockNode? DockRoot;

        private DockPanel? _dragPanel;

        private Point _dragPosition;

        private Point _dragOffset;

        private bool _moved;

        private DockNode? _dropTarget;

        private DockPosition _dropPosition;

        private DockSplitNode? _dragSplitter;

        public static int StatusBarHeight => UIScale.Value(20);

        public static int Padding => UIScale.Value(PaddingBase);

        public static Color ScreenBackground => UITheme.ScreenBackground;

        private const int PaddingBase = 4;

        private Texture2D _cornerMask;

        private int _cornerMaskRadius;

        public Rectangle Canvas
        {
            get
            {
                var bounds = GameHost.Instance.Window.ClientBounds;

                var pad = Padding;

                return new Rectangle(0, 0, Math.Max(1, bounds.Width), Math.Max(1, bounds.Height - StatusBarHeight - pad));
            }
        }

        public Rectangle StatusBar => new(Padding, Canvas.Bottom + Padding, Math.Max(1, GameHost.Instance.Window.ClientBounds.Width - Padding * 2), Math.Max(1, StatusBarHeight - Padding * 2));

        public override void Update(GameTime gameTime)
        {
            FollowViewport();

            Arrange();

            UpdateTabHover();

            UpdateLoading(gameTime);

            base.Update(gameTime);

            foreach (var child in Children.ToArray())
                child.Update(gameTime);
        }

        private void FollowViewport()
        {
            var parameters = GameHost.Instance.GraphicsDevice.PresentationParameters;

            if (Width == parameters.BackBufferWidth && Height == parameters.BackBufferHeight)
                return;

            Width = parameters.BackBufferWidth;

            Height = parameters.BackBufferHeight;
        }

        private void UpdateLoading(GameTime gameTime)
        {
            var renderer = NifDocker.Instance?.Graphics;

            var loading = renderer?.IsLoading ?? false;

            if (loading && renderer != null)
            {
                _loading.Message = renderer.LoadingName;

                _loading.Pending = renderer.PendingLoads;
            }

            _loading.Update(gameTime, loading);
        }

        private readonly LoadingPopup _loading = new();

        private void UpdateTabHover()
        {
            var position = UserInput.GetMousePos().ToPoint();

            var hand = false;

            DockSplitDirection? resize = null;

            hand |= UpdateHover(DockRoot, position, ref resize);

            var cursor = resize switch
            {
                DockSplitDirection.Vertical => MouseCursor.SizeNS,
                DockSplitDirection.Horizontal => MouseCursor.SizeWE,
                _ => hand ? MouseCursor.Hand : MouseCursor.Arrow
            };

            if (cursor == _cursor)
                return;

            _cursor = cursor;

            Mouse.SetCursor(cursor);
        }

        private MouseCursor _cursor = MouseCursor.Arrow;

        private bool UpdateHover(DockNode? node, Point position, ref DockSplitDirection? resize)
        {
            switch (node)
            {
                case null:
                    return false;

                case DockTabNode tab:
                    tab.HoverTab = tab.IsOverTabBar(position) ? tab.HitTab(position) : -1;

                    return tab.HoverTab >= 0;

                case DockSplitNode split:
                    var hand = UpdateHover(split.First, position, ref resize) | UpdateHover(split.Second, position, ref resize);

                    if (split.SplitRect.Contains(position))
                    {
                        resize = split.Direction;

                        hand = true;
                    }

                    return hand;

                default:
                    return false;
            }
        }

        private void Arrange()
        {
            if (DockRoot != null)
                DockLayout.Arrange(DockRoot, Canvas);

        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.Draw(spriteBatch, gameTime);

            DrawPanelCorners(spriteBatch);

            DrawStatusBar(spriteBatch);

            DrawVertexBox(spriteBatch);

            DrawPreview(spriteBatch);

            _loading.Draw(spriteBatch, Rectangle, spriteBatch.GraphicsDevice);

            foreach (var child in Children.ToArray())
            {
                if (child is not ContextMenu menu)
                    continue;

                menu.Draw(spriteBatch, gameTime);

                menu.DrawChildren(spriteBatch, gameTime);
            }
        }

        private void DrawVertexBox(SpriteBatch spriteBatch)
        {
            if (NifDocker.Instance?.DragBox is not { } box)
                return;

            spriteBatch.DrawRectangle(new Rectangle(box.X, box.Y, box.Width, 1), UITheme.Accent);

            spriteBatch.DrawRectangle(new Rectangle(box.X, box.Bottom, box.Width, 1), UITheme.Accent);

            spriteBatch.DrawRectangle(new Rectangle(box.X, box.Y, 1, box.Height), UITheme.Accent);

            spriteBatch.DrawRectangle(new Rectangle(box.Right, box.Y, 1, box.Height), UITheme.Accent);
        }

        private SkiaRenderTarget2D? _borders;

        private int _borderWidth;

        private int _borderHeight;

        private static readonly SKColor BorderIdle = new(59, 59, 59);

        private static readonly SKColor BorderHover = new(72, 72, 72);

        public void DrawPanelBorders()
        {
            var device = GameHost.Instance.GraphicsDevice;

            if (!SkiaRenderer.IsInitialized || DockRoot == null || device.IsDisposed)
                return;

            var width = device.PresentationParameters.BackBufferWidth;

            var height = device.PresentationParameters.BackBufferHeight;

            if (_borders == null || _borderWidth != width || _borderHeight != height)
            {
                _borders?.Dispose();

                _borders = new SkiaRenderTarget2D(device, width, height);

                _borderWidth = width;

                _borderHeight = height;
            }

            _borders.Begin();

            using var idle = new SKPaint
            {
                Color = BorderIdle,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            using var hover = new SKPaint
            {
                Color = BorderHover,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            DrawBorders(DockRoot, _borders.Canvas, UIMetrics.CornerRadius, idle, hover);

            _borders.End();
        }

        private static void DrawBorders(DockNode node, SKCanvas canvas, float radius, SKPaint idle, SKPaint hover)
        {
            switch (node)
            {
                case DockTabNode tab:
                {
                    var paint = tab.ActivePanel?.IsHovering == true ? hover : idle;

                    var rect = tab.Rect;

                    var inset = 0f;

                    var bounds = new SKRect(rect.Left + inset, rect.Top + inset, rect.Right - inset, rect.Bottom - inset);

                    canvas.DrawRoundRect(bounds, radius, radius, paint);

                    break;
                }

                case DockSplitNode split:
                {
                    DrawBorders(split.First, canvas, radius, idle, hover);

                    DrawBorders(split.Second, canvas, radius, idle, hover);

                    break;
                }
            }
        }

        private void DrawPanelCorners(SpriteBatch spriteBatch)
        {
            if (DockRoot == null)
                return;

            RoundPanels(DockRoot, spriteBatch);
        }

        private void RoundPanels(DockNode node, SpriteBatch spriteBatch)
        {
            switch (node)
            {
                case DockTabNode tab:
                    RoundRect(spriteBatch, tab.Rect);
                    break;

                case DockSplitNode split:
                    RoundPanels(split.First, spriteBatch);
                    RoundPanels(split.Second, spriteBatch);
                    break;
            }
        }

        private void RoundRect(SpriteBatch spriteBatch, Rectangle rect)
        {
            var radius = UIMetrics.CornerRadius;

            if (rect.Width < radius * 2 || rect.Height < radius * 2)
                return;

            if (_cornerMask == null || _cornerMaskRadius != radius)
            {
                _cornerMask?.Dispose();

                _cornerMask = BuildCornerMask(radius, ScreenBackground);

                _cornerMaskRadius = radius;
            }

            var source = new Rectangle(0, 0, radius, radius);

            spriteBatch.Draw(_cornerMask, new Rectangle(rect.X, rect.Y, radius, radius), source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            spriteBatch.Draw(_cornerMask, new Rectangle(rect.Right - radius, rect.Y, radius, radius), source, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);

            spriteBatch.Draw(_cornerMask, new Rectangle(rect.Right - radius, rect.Bottom - radius, radius, radius), source, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f);

            spriteBatch.Draw(_cornerMask, new Rectangle(rect.X, rect.Bottom - radius, radius, radius), source, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
        }

        private static Texture2D BuildCornerMask(int radius, Color gap)
        {
            var device = GameHost.Instance.GraphicsDevice;

            var texture = new Texture2D(device, radius, radius);

            var data = new Color[radius * radius];

            for (var y = 0; y < radius; y++)
            {
                for (var x = 0; x < radius; x++)
                {
                    var dx = radius - x - 0.5f;

                    var dy = radius - y - 0.5f;

                    var inside = dx * dx + dy * dy <= radius * radius;

                    data[y * radius + x] = inside ? Color.Transparent : gap;
                }
            }

            texture.SetData(data);

            return texture;
        }

        private void DrawStatusBar(SpriteBatch spriteBatch)
        {
            var canvas = Canvas;

            var strip = new Rectangle(0, canvas.Bottom, Rectangle.Width, Math.Max(0, Rectangle.Bottom - canvas.Bottom));

            if (strip.Height > 0)
                spriteBatch.DrawRectangle(strip, UITheme.ScreenBackground);

            var bar = new Rectangle(UIScale.Value(6), canvas.Height + UIScale.Value(4), canvas.Width - UIScale.Value(12), Math.Max(1, StatusBarHeight));

            if (bar.Width <= 0 || bar.Height <= 0)
                return;

            var renderer = NifDocker.Instance?.Graphics;

            var padding = UIScale.Value(12);

            var size = 15;

            var textY = bar.Y + (bar.Height - size) / 2;

            var x = bar.X + padding;

            var objects = renderer?.Objects.Count ?? 0;

            var selected = renderer?.SelectedShapeCount ?? 0;

            var meshes = 0;

            var vertices = 0;

            var triangles = 0;

            if (renderer != null)
            {
                foreach (var item in renderer.Objects)
                {
                    meshes += item.Mesh.RenderShapes.Count;

                    vertices += item.Mesh.Vertices?.Length ?? 0;

                    foreach (var shape in item.Mesh.RenderShapes)
                        triangles += shape.TriangleCount;
                }
            }

            x = DrawStatusItem(spriteBatch, x, textY, "objects", objects.ToString(), UITheme.StatusLabel, UITheme.StatusValue, size);

            x = DrawStatusItem(spriteBatch, x, textY, "selected", selected.ToString(), UITheme.StatusLabel, selected > 0 ? UITheme.Accent : UITheme.StatusLabel, size);

            x = DrawStatusItem(spriteBatch, x, textY, "shapes", meshes.ToString(), UITheme.StatusLabel, UITheme.StatusValue, size);

            x = DrawStatusItem(spriteBatch, x, textY, "verts", vertices.Format(), UITheme.StatusLabel, UITheme.StatusValue, size);

            x = DrawStatusItem(spriteBatch, x, textY, "tris", triangles.Format(), UITheme.StatusLabel, UITheme.StatusValue, size);

            if (renderer != null)
            {
                DrawStatusItem(spriteBatch, x, textY, "viewport", $"{renderer.ViewportRect.Width} x {renderer.ViewportRect.Height}", UITheme.StatusLabel, UITheme.StatusValue, size);
            }
        }

        private static int DrawStatusItem(SpriteBatch spriteBatch, int x, int y, string label, string value, Color labelColor, Color valueColor, int size)
        {
            UIFont.Draw(spriteBatch, label, new Vector2(x, y), labelColor, size);

            var labelWidth = UIFont.Measure(label, size).X;

            var valueX = x + (int)labelWidth + UIScale.Value(6);

            UIFont.Draw(spriteBatch, value, new Vector2(valueX, y), valueColor, size);

            var valueWidth = UIFont.Measure(value, size).X;

            return valueX + (int)valueWidth + UIScale.Value(22);
        }

        public override void DrawChildren(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (DockRoot != null)
                DrawNode(DockRoot, spriteBatch, gameTime);

            foreach (var child in Children.ToArray())
            {
                if (child is ContextMenu or DockPanel)
                    continue;

                child.Draw(spriteBatch, gameTime);

                child.DrawChildren(spriteBatch, gameTime);
            }
        }

        private static void DrawNode(DockNode node, SpriteBatch spriteBatch, GameTime gameTime)
        {
            switch (node)
            {
                case DockTabNode tab:
                    tab.DrawActivePanel(spriteBatch, gameTime);

                    break;

                case DockSplitNode split:
                    DrawNode(split.First, spriteBatch, gameTime);

                    DrawNode(split.Second, spriteBatch, gameTime);

                    DrawSplitter(spriteBatch, split.SplitRect);

                    break;
            }
        }

        private static void DrawSplitter(SpriteBatch spriteBatch, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            spriteBatch.DrawRectangle(rect, UITheme.ScreenBackground);
        }

        public static void UpdateNode(DockNode? node, GameTime gameTime)
        {
            switch (node)
            {
                case DockTabNode tab:
                    tab.ActivePanel?.Update(gameTime);

                    break;

                case DockSplitNode split:
                    UpdateNode(split.First, gameTime);

                    UpdateNode(split.Second, gameTime);

                    break;
            }
        }

        private void DrawPreview(SpriteBatch spriteBatch)
        {
            if (_dragPanel == null || !_moved)
                return;

            if (_dropTarget == null)
            {
                var width = Math.Max(60, _dragOffset.X * 2);

                var height = DockTabNode.TabBarHeight;

                var ghost = new Rectangle(_dragPosition.X - _dragOffset.X, _dragPosition.Y - _dragOffset.Y, width, height);

                spriteBatch.DrawRectangle(ghost, UITheme.DropGhost * 0.6f);

                spriteBatch.DrawBorder(ghost, 2, UITheme.DropGhostBorder);

                return;
            }

            var rect = _dropTarget.ContentRect;

            var preview = _dropPosition switch
            {
                DockPosition.Left => new Rectangle(rect.X, rect.Y, rect.Width / 2, rect.Height),

                DockPosition.Right => new Rectangle(rect.X + rect.Width / 2, rect.Y, rect.Width / 2, rect.Height),

                DockPosition.Top => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2),

                DockPosition.Bottom => new Rectangle(rect.X, rect.Y + rect.Height / 2, rect.Width, rect.Height / 2),

                _ => rect
            };

            spriteBatch.DrawRectangle(preview, UITheme.DropPreview * 0.55f);

            spriteBatch.DrawBorder(preview, 3, UITheme.DropPreviewBorder);
        }

        private void ApplyDrop(DockPanel panel, DockNode target, DockPosition position)
        {
            if (DockRoot == null)
                return;

            if (target is DockTabNode current && current.Panels.Count == 1 && current.Panels[0] == panel)
            {
                current.SelectPanel(panel);

                return;
            }

            if (position == DockPosition.Center)
                position = DockPosition.Bottom;

            Detach(panel);

            DockTree.InsertPanel(ref DockRoot, target, position, panel);

            Arrange();
        }

        public bool IsPanelDocked(DockPanel? panel) => panel != null && DockRoot != null && DockTree.FindTab(DockRoot, panel) != null;

        public void SetPanelDocked(DockPanel? panel, bool docked)
        {
            if (panel == null)
                return;

            if (docked)
                ShowPanel(panel);
            else
                Detach(panel);
        }

        public void TogglePanel(DockPanel? panel)
        {
            if (panel == null)
                return;

            var docked = IsPanelDocked(panel);


            SetPanelDocked(panel, !docked);

        }

        private void ShowPanel(DockPanel panel)
        {
            if (_nifTab == null || _infoTab == null)
                return;

            if (DockTree.FindTab(DockRoot, panel) != null)
                return;

            if (ReferenceEquals(DockRoot, _nifTab))
                DockRoot = DefaultLayout();

            if (!ColumnHasPanels())
            {
                _infoTab.AddPanel(panel);

                _infoTab.SelectPanel(panel);

                Arrange();

                return;
            }

            var slot = SlotOf(panel);

            var column = InfoColumn;

            var host = SlotHost(column, slot);

            if (host != null)
            {
                DockTree.InsertPanel(ref DockRoot, host, DockPosition.Top, panel);

                Arrange();

                return;
            }

            var lowest = LowestTab(column) ?? _infoTab;

            var newTab = new DockTabNode();

            newTab.AddPanel(panel);

            var split = new DockSplitNode(DockSplitDirection.Vertical, lowest, newTab, 0.5f);

            DockTree.Replace(ref DockRoot, lowest, split);

            Arrange();

            if (DockTree.FindTab(DockRoot, panel) != null)
                return;


            DockRoot = DefaultLayout();

            _infoTab.AddPanel(panel);

            _infoTab.SelectPanel(panel);

            Arrange();
        }

        private DockNode? InfoColumn => DockRoot is DockSplitNode { Direction: DockSplitDirection.Horizontal } root ? root.Second : _infoTab;

        private DockNode? SlotHost(DockNode? node, int slot)
        {
            switch (node)
            {
                case null:
                    return null;

                case DockTabNode tab:
                    return tab.Panels.Count > 0 && SlotOf(tab.Panels[0]) > slot ? tab : null;

                case DockSplitNode split:
                    return SlotHost(split.First, slot) ?? SlotHost(split.Second, slot);

                default:
                    return null;
            }
        }

        private static DockTabNode? LowestTab(DockNode? node)
        {
            switch (node)
            {
                case null:
                    return null;

                case DockTabNode tab:
                    return tab;

                case DockSplitNode split:
                    return split.Direction == DockSplitDirection.Vertical
                        ? LowestTab(split.Second) ?? LowestTab(split.First)
                        : LowestTab(split.First) ?? LowestTab(split.Second);

                default:
                    return null;
            }
        }

        public void Detach(DockPanel panel)
        {
            var tab = DockTree.FindTab(DockRoot, panel);

            if (tab == null)
                return;

            tab.RemovePanel(panel);

            if (DockRoot != null)
                DockTree.Collapse(ref DockRoot, tab);

            if (_nifTab != null && !ColumnHasPanels())
                DockRoot = _nifTab;

            Arrange();
        }

        private bool ColumnHasPanels()
        {
            var column = InfoColumn;

            return column != null && HasPanels(column);
        }

        private static bool HasPanels(DockNode node)
        {
            switch (node)
            {
                case DockTabNode tab:
                    return tab.Panels.Count > 0;

                case DockSplitNode split:
                    return HasPanels(split.First) || HasPanels(split.Second);

                default:
                    return false;
            }
        }

        private DockTabNode? TabAt(Point position)
        {
            if (DockRoot != null && DockTree.HitNode(DockRoot, position) is DockTabNode dockTab)
                return dockTab;

            return null;
        }

        private void UpdateDropTarget()
        {
            _dropTarget = null;

            if (DockRoot != null && DockTree.HitNode(DockRoot, _dragPosition) is DockNode node)
            {
                var position = DockHit.GetPosition(node.Rect, _dragPosition.ToVector2());

                if (position != null)
                {
                    _dropTarget = node;

                    _dropPosition = position.Value;
                }

                return;
            }

        }

        private void FinishDrag()
        {
            var panel = _dragPanel;

            var moved = _moved;

            var target = _dropTarget;

            var position = _dropPosition;

            _dragPanel = null;

            _moved = false;

            _dropTarget = null;

            UIManager.Instance.CapturedComponent = null;

            if (panel == null || !moved)
                return;

            if (target != null)
            {
                ApplyDrop(panel, target, position);

                return;
            }

        }

        private bool HandleTabBar(InputEventArgs args)
        {
            if (TabAt(args.MousePosition.ToPoint()) is not DockTabNode tab)
                return false;

            if (!tab.IsOverTabBar(args.MousePosition.ToPoint()))
                return false;

            if (tab.ActivePanel is { } headerPanel && headerPanel.HandlesHeaderInput(args.MousePosition.ToPoint()))
                return false;

            var index = tab.HitTab(args.MousePosition.ToPoint());

            if (index < 0 || index >= tab.Panels.Count)
                return true;

            var panel = tab.Panels[index];

            switch (args.Type)
            {
                case InputType.MouseLeftDown:
                    tab.SelectPanel(panel);

                    _dragPanel = panel;

                    _moved = false;

                    _dragPosition = args.MousePosition.ToPoint();

                    _dragOffset = new Point(_dragPosition.X - tab.TabRects[index].X, Math.Max(1, tab.TabRects[index].Width / 2));

                    UIManager.Instance.CapturedComponent = this;

                    return true;

                case InputType.MouseMove:
                    if (!_moved)
                    {
                        var dx = Math.Abs(args.MousePosition.X - _dragPosition.X);

                        var dy = Math.Abs(args.MousePosition.Y - _dragPosition.Y);

                        if (dx + dy < 2)
                            return true;

                        _moved = true;
                    }

                    _dragPosition = args.MousePosition.ToPoint();

                    UpdateDropTarget();

                    return true;

                case InputType.MouseLeftUp:
                    _dragPanel = null;

                    _moved = false;

                    _dropTarget = null;

                    UIManager.Instance.CapturedComponent = null;

                    return true;
            }

            return true;
        }        private bool HandleShortcut(InputEventArgs args)
        {
            if (args.Type != InputType.KeyDown || !UserInput.Ctrl)
                return false;

            var renderer = NifDocker.Instance?.Graphics;

            if (renderer == null)
                return false;

            switch (args.Key)
            {
                case Keys.Z when !UserInput.Shift:
                    return renderer.Undo();

                case Keys.Z:
                case Keys.Y:
                    return renderer.Redo();

                default:
                    return false;
            }
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (_loading.Visible && args.Type == InputType.MouseLeftDown)
                return InputResult.Consumed;

            if (_dragPanel != null && _moved)
            {
                if (args.Type == InputType.MouseMove)
                {
                    _dragPosition = args.MousePosition.ToPoint();

                    UpdateDropTarget();

                    return InputResult.Consumed;
                }

                if (args.Type == InputType.MouseLeftUp)
                {
                    FinishDrag();

                    return InputResult.Consumed;
                }
            }

            if (HandleTabBar(args))
                return InputResult.Consumed;

            if (HandleShortcut(args))
                return InputResult.Consumed;

            switch (args.Type)
            {
                case InputType.MouseLeftDown:
                    _dragSplitter = FindSplitterAt(DockRoot, args.MousePosition);

                    if (_dragSplitter == null)
                        return InputResult.Continue;

                    UIManager.Instance.CapturedComponent = this;

                    return InputResult.Consumed;

                case InputType.MouseMove:
                    if (_dragSplitter == null)
                        return InputResult.Continue;

                    UpdateSplitter(_dragSplitter, args.MousePosition);

                    return InputResult.Consumed;

                case InputType.MouseLeftUp:
                    if (_dragSplitter == null)
                        return InputResult.Continue;

                    _dragSplitter = null;

                    UIManager.Instance.CapturedComponent = null;

                    return InputResult.Consumed;
            }

            return InputResult.Continue;
        }

        private DockSplitNode? FindSplitterAt(DockNode? node, Vector2 pos)
        {
            if (node is not DockSplitNode split)
                return null;

            if (split.SplitRect.Contains(pos))
                return split;

            return FindSplitterAt(split.First, pos) ?? FindSplitterAt(split.Second, pos);
        }

        private void UpdateSplitter(DockSplitNode split, Vector2 mouse)
        {
            var rect = split.Rect;

            var splitter = split.SplitRect;

            if (split.Direction == DockSplitDirection.Horizontal)
            {
                var available = rect.Width - splitter.Width;

                if (available <= 0)
                    return;

                var ratio = (mouse.X - rect.X) / available;

                split.Ratio = Math.Clamp(ratio, DockSplitNode.MinRatio, DockSplitNode.MaxRatio);
            }
            else
            {
                var available = rect.Height - splitter.Height;

                if (available <= 0)
                    return;

                var ratio = (mouse.Y - rect.Y) / available;

                split.Ratio = Math.Clamp(ratio, DockSplitNode.MinRatio, DockSplitNode.MaxRatio);
            }

            Arrange();
        }
    }
}
