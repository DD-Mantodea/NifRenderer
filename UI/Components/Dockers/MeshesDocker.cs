using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NifRenderer.Extensions;
using NifRenderer.Editing;
using NifRenderer.Utils;
using NifRenderer.UI.Components.Docking;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using RendererCore = NifRenderer.Renderer.Renderer;
using RenderObject = NifRenderer.Renderer.Scene.RenderObject;
using RenderShape = NifRenderer.Renderer.Scene.RenderShape;

namespace NifRenderer.UI.Components.Dockers
{
    public class MeshesDocker : DockPanel
    {
        public const int TitleSize = 15;

        public const int NameSize = 15;

        public const int DetailSize = 12;

        private static int Pad => UIScale.Value(8);

        private static int HeaderHeight => UIScale.Value(30);

        public static int RowHeight => UIScale.Value(18);

        public static int ShapeRowHeight => UIScale.Value(18);

        public static int StatusWidth => UIScale.Value(60);

        public const int IndentBase = 24;

        public const string EyeVisible = "eye.svg";

        public const string EyeHidden = "eye-slash.svg";

        public static int EyeSize => UIScale.Value(12);

        public static Rectangle EyeBox(Rectangle status)
        {
            var size = Math.Min(EyeSize, status.Height - UIScale.Value(4));

            return new Rectangle(status.X + UIScale.Value(20), status.Y + (status.Height - size) / 2, size, size);
        }

        public static void DrawEyeAt(SpriteBatch spriteBatch, Rectangle box, bool visible)
        {
            var color = visible ? UITheme.TextDim : UITheme.TextFaint;

            var icon = SkiaIcon.Load(
                spriteBatch.GraphicsDevice,
                visible ? EyeVisible : EyeHidden,
                box.Width,
                new SKColor(color.R, color.G, color.B, color.A));

            if (icon == null)
                return;

            spriteBatch.Draw(icon, box, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
        }

        public override bool OwnsHeader => true;

        private readonly List<Component> _rows = [];

        public MeshesDocker(DockTabNode node) : base(node)
        {
            BackgroundColor = UITheme.PanelBackground;
        }

        public RendererCore? Renderer => NifDocker.Instance?.Graphics;

        private readonly List<MeshItem> _items = [];

        public IReadOnlyList<MeshItem> Items => _items;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Width = TabNode.Rect.Width;

            Height = TabNode.Rect.Height;

            Position = TabNode.Rect.Location.ToVector2();

            SyncItems();

            foreach (var child in Children)
                child.Update(gameTime);
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type == InputType.MouseLeftDown && !args.Shift && !args.Ctrl)
            {
                var hit = HitComponent(args.MousePosition);

                if (hit == null || ReferenceEquals(hit, this))
                    Renderer?.ClearSelection();
            }

            if (args.Type != InputType.KeyDown)
                return InputResult.Continue;

            switch (args.Key)
            {
                case Keys.Delete:
                    Renderer?.DeleteSelected();

                    return InputResult.Consumed;

                case Keys.A when args.Ctrl:
                    Renderer?.SelectAllObjects();

                    return InputResult.Consumed;
            }

            return InputResult.Continue;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.Draw(spriteBatch, gameTime);

            var area = Rectangle;

            if (area.Width <= Pad * 2 || area.Height <= HeaderHeight + Pad * 2)
                return;

            DrawHeader(spriteBatch, area);

            if (_items.Count == 0)
                UIFont.Draw(spriteBatch, "drop a .nif file here", new Vector2(area.X + Pad + UIScale.Value(4), FirstRowY(area) + UIScale.Value(4)), UITheme.TextFaint, DetailSize);
        }

        private void DrawHeader(SpriteBatch spriteBatch, Rectangle area)
        {
            var renderer = Renderer;

            var band = new Rectangle(area.X + Pad, area.Y + Pad, area.Width - Pad * 2, HeaderHeight);

            SkiaCard.DrawRounded(spriteBatch, band, UITheme.PanelHeader, UITheme.PanelHeader, UIMetrics.WidgetRoundness(0.2f), 0f);

            var textY = band.Y + (band.Height - UIScale.Value(TitleSize)) / 2;

            UIFont.Draw(spriteBatch, "Meshes", new Vector2(band.X + UIScale.Value(8), textY), UITheme.TextPrimary, TitleSize);

            var objects = renderer?.Objects.Count ?? 0;

            var selected = renderer?.SelectedObjectCount ?? 0;

            if (objects == 0)
                return;

            var text = selected > 0 ? $"{objects} / {selected}" : objects.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var size = UIFont.Measure(text, DetailSize);

            UIFont.Draw(spriteBatch, text, new Vector2(band.Right - UIScale.Value(10) - size.X, textY + UIScale.Value(2)), selected > 0 ? UITheme.Accent : UITheme.TextDim, DetailSize);
        }

        private static int FirstRowY(Rectangle area) => area.Y + Pad * 2 + HeaderHeight;

        private void SyncItems()
        {
            var objects = Renderer?.Objects;

            var count = objects?.Count ?? 0;

            if (count != _items.Count || !Matches(objects))
                RebuildItems(objects);
            else if (!ShapesMatch())
                RebuildShapes();

            SyncRows();

            LayoutItems();
        }

        private bool ShapesMatch()
        {
            foreach (var item in _items)
            {
                var shapes = item.Item.Mesh.RenderShapes;

                if (item.ShapeRows.Count != shapes.Count)
                    return false;

                for (var i = 0; i < shapes.Count; i++)
                {
                    if (!ReferenceEquals(item.ShapeRows[i].Shape, shapes[i]))
                        return false;
                }
            }

            return true;
        }

        private void RebuildShapes()
        {
            foreach (var item in _items)
                item.BuildShapes();
        }

        private bool Matches(IReadOnlyList<RenderObject>? objects)
        {
            if (objects == null)
                return _items.Count == 0;

            for (var i = 0; i < _items.Count; i++)
            {
                if (!ReferenceEquals(_items[i].Item, objects[i]))
                    return false;
            }

            return true;
        }

        private void RebuildItems(IReadOnlyList<RenderObject>? objects)
        {
            foreach (var child in Children)
                child.Parent = null;

            Children.Clear();

            _items.Clear();

            _rows.Clear();

            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                var component = new MeshItem(this, obj)
                {
                    Height = RowHeight
                };

                component.BuildShapes();

                _items.Add(component);
            }
        }

        private void SyncRows()
        {
            _rows.Clear();

            foreach (var item in _items)
            {
                _rows.Add(item);

                if (!item.Expanded)
                    continue;

                foreach (var row in item.ShapeRows)
                    _rows.Add(row);
            }

            if (SameRows())
                return;

            Children.Clear();

            foreach (var row in _rows)
            {
                row.Parent = this;

                Children.Add(row);
            }
        }

        private bool SameRows()
        {
            if (Children.Count != _rows.Count)
                return false;

            for (var i = 0; i < _rows.Count; i++)
            {
                if (!ReferenceEquals(Children[i], _rows[i]))
                    return false;
            }

            return true;
        }

        private void LayoutItems()
        {
            var area = Rectangle;

            var bottom = area.Bottom - Pad;

            var largest = 1;

            foreach (var item in _items)
            {
                var count = item.Item.Mesh.Vertices?.Length ?? 0;

                if (count > largest)
                    largest = count;
            }

            var index = 0;

            var y = FirstRowY(area);

            foreach (var item in _items)
            {
                item.Index = index++;

                item.Largest = largest;
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];

                if (y + row.Height > bottom)
                {
                    row.Visible = false;

                    continue;
                }

                row.Visible = true;

                row.Position = new Vector2(area.X, y);

                row.Width = area.Width;

                y += row.Height;
            }
        }
    }

    public class MeshItem(MeshesDocker owner, RenderObject item) : Component
    {
        public readonly MeshesDocker Owner = owner;

        public RenderObject Item = item;

        public int Index;

        public int Largest = 1;

        public bool Expanded;

        public readonly List<ShapeItem> ShapeRows = [];

        public Rectangle ArrowRect => new(Rectangle.X + UIScale.Value(5), Rectangle.Y + (Rectangle.Height - ArrowSize) / 2, ArrowSize, ArrowSize);

        public bool Selected => Owner.Renderer?.IsSelected(Item) ?? false;

        public Rectangle StatusRect => new(Rectangle.Right - MeshesDocker.StatusWidth, Rectangle.Y, MeshesDocker.StatusWidth, Rectangle.Height);

        public Rectangle ContentRect => new(Rectangle.X, Rectangle.Y, Math.Max(1, Rectangle.Width - MeshesDocker.StatusWidth), Rectangle.Height);

        public Rectangle EyeRect => MeshesDocker.EyeBox(StatusRect);

        private static int ArrowSize => UIScale.Value(14);

        private const string ArrowOpen = "caret-down.svg";

        private const string ArrowClosed = "caret-right.svg";

        private void DrawEye(SpriteBatch spriteBatch, bool visible) => MeshesDocker.DrawEyeAt(spriteBatch, EyeRect, visible);

        private void DrawArrowGlyph(SpriteBatch spriteBatch, bool expanded, Color color)
        {
            var box = ArrowRect;

            var icon = SkiaIcon.Load(
                spriteBatch.GraphicsDevice,
                expanded ? ArrowOpen : ArrowClosed,
                box.Width,
                new SKColor(color.R, color.G, color.B, color.A));

            if (icon == null)
                return;

            spriteBatch.Draw(icon, box, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
        }

        public void BuildShapes()
        {
            ShapeRows.Clear();

            for (var i = 0; i < Item.Mesh.RenderShapes.Count; i++)
            {
                ShapeRows.Add(new ShapeItem(this, Item.Mesh.RenderShapes[i])
                {
                    Height = MeshesDocker.ShapeRowHeight,

                    Index = i
                });
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var rect = Rectangle;

            var selected = Selected;

            var odd = Index % 2 == 0;

            Color fill;

            if (selected)
                fill = IsHovering ? UITheme.RowSelectedHover : UITheme.RowSelected;
            else if (IsHovering)
                fill = odd ? UITheme.RowOddHover : UITheme.RowEvenHover;
            else
                fill = odd ? UITheme.RowOdd : UITheme.RowEven;

            spriteBatch.DrawRectangle(rect, fill);

            if (selected)
                spriteBatch.DrawRectangle(new Rectangle(rect.X, rect.Y, UIScale.Value(3), rect.Height), UITheme.Accent);

            DrawArrowGlyph(spriteBatch, Expanded, UITheme.TextPrimary);

            DrawEye(spriteBatch, Item.AnyVisible);

            var textX = rect.X + UIScale.Value(MeshesDocker.IndentBase);

            var content = ContentRect;

            UIManager.Clipped(spriteBatch, content, () =>
                UIFont.Draw(spriteBatch, Item.FileName, new Vector2(textX, rect.Y + UIScale.Value(2)), UITheme.TextPrimary, MeshesDocker.NameSize));
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type == InputType.MouseRightDown)
            {
                BuildMenu(args.MousePosition);

                return InputResult.Consumed;
            }

            if (args.Type != InputType.MouseLeftDown)
                return InputResult.Continue;

            var position = args.MousePosition.ToPoint();

            if (ArrowRect.Contains(position))
            {
                Expanded = !Expanded;

                return InputResult.Consumed;
            }

            if (EyeRect.Contains(position))
            {
                var renderer = Owner.Renderer;

                renderer?.ToggleVisibility(Item);

                return InputResult.Consumed;
            }

            Owner.Renderer?.ClickObject(Item, args.Shift, args.Ctrl);

            return InputResult.Consumed;
        }

        private void BuildMenu(Vector2 position)
        {
            var renderer = Owner.Renderer;

            if (renderer == null)
                return;

            var entries = new List<ContextMenu.Entry>
            {
                ContextMenu.Entry.Of("Export NIF", () => Export(renderer), icon: "box-arrow-up.svg"),

                ContextMenu.Entry.Split(),

                ContextMenu.Entry.Of("Delete", () => Delete(renderer), shortcut: "Del", icon: "x-lg.svg")
            };

            ContextMenu.Show(entries, position);
        }

        private void Delete(RendererCore renderer)
        {
            renderer.ClickObject(Item, false, false);

            renderer.DeleteSelected();
        }

        private void Export(RendererCore renderer)
        {
            if (string.IsNullOrEmpty(Item.Path))
            {

                return;
            }

            var path = SaveDialog.Ask(NifExporter.DefaultPath(Item));

            if (path == null)
            {

                return;
            }


            renderer.ExportObject(Item, path);
        }

    }

    public class ShapeItem(MeshItem owner, RenderShape shape) : Component
    {
        public readonly MeshItem Owner = owner;

        public readonly RenderShape Shape = shape;

        public int Index;

        public bool Selected => Owner.Owner.Renderer?.IsSelected(Shape) ?? false;

        public Rectangle StatusRect => new(Rectangle.Right - MeshesDocker.StatusWidth, Rectangle.Y, MeshesDocker.StatusWidth, Rectangle.Height);

        public Rectangle ContentRect => new(Rectangle.X, Rectangle.Y, Math.Max(1, Rectangle.Width - MeshesDocker.StatusWidth), Rectangle.Height);

        public Rectangle EyeRect => MeshesDocker.EyeBox(StatusRect);

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var rect = Rectangle;

            var selected = Selected;

            Color fill;

            if (selected)
                fill = IsHovering ? UITheme.RowSelectedHover : UITheme.RowSelected;
            else if (IsHovering)
                fill = UITheme.RowEvenHover;
            else
                fill = UITheme.RowEven;

            spriteBatch.DrawRectangle(rect, fill);

            if (selected)
                spriteBatch.DrawRectangle(new Rectangle(rect.X, rect.Y, UIScale.Value(3), rect.Height), UITheme.Accent);

            var textX = rect.X + UIScale.Value(MeshesDocker.IndentBase + 12);

            var content = ContentRect;

            var color = selected ? UITheme.TextPrimary : UITheme.TextDim;

            UIManager.Clipped(spriteBatch, content, () =>
                UIFont.Draw(spriteBatch, Shape.ID, new Vector2(textX, rect.Y + UIScale.Value(2)), color, MeshesDocker.NameSize));

            MeshesDocker.DrawEyeAt(spriteBatch, EyeRect, Shape.Visible);
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type == InputType.MouseRightDown)
            {
                BuildMenu(args.MousePosition);

                return InputResult.Consumed;
            }

            if (args.Type != InputType.MouseLeftDown)
                return InputResult.Continue;

            var position = args.MousePosition.ToPoint();

            if (EyeRect.Contains(position))
            {
                var renderer = Owner.Owner.Renderer;

                if (renderer != null)
                {
                    if (renderer.IsSelected(Shape))
                        renderer.ToggleSelectionVisibility();
                    else
                        renderer.ToggleVisibility(Shape);
                }

                return InputResult.Consumed;
            }

            Owner.Owner.Renderer?.ClickShape(Shape, args.Shift, args.Ctrl);

            return InputResult.Consumed;
        }

        private void BuildMenu(Vector2 position)
        {
            var renderer = Owner.Owner.Renderer;

            if (renderer == null)
                return;

            var entries = new List<ContextMenu.Entry>
            {
                ContextMenu.Entry.Of("Delete", () => Delete(renderer), shortcut: "Del", icon: "x-lg.svg")
            };

            ContextMenu.Show(entries, position);
        }

        private void Delete(RendererCore renderer)
        {
            renderer.ClickShape(Shape, false, false);

            renderer.DeleteSelected();
        }
    }
}
