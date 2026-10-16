using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NifRenderer.Editing;
using NifRenderer.Extensions;

namespace NifRenderer.UI.Components
{
    public sealed class ModeMenuItem : Component
    {
        private const float Roundness = 0.2f;

        public readonly EditorMode EditorMode;

        public readonly string Label;

        public readonly bool Selected;

        public Action<EditorMode>? Chosen;

        public ModeMenuItem(EditorMode mode, string label, bool selected)
        {
            EditorMode = mode;

            Label = label;

            Selected = selected;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var rect = Rectangle;

            var box = rect;

            var highlighted = Selected || IsHovering;

            if (highlighted)
            {
                var fill = Selected ? UITheme.MenuItemSelected : UITheme.MenuItemHover;

                var radius = UIMetrics.WidgetRoundness(Roundness);

                var rounded = SkiaCard.Rounded(spriteBatch.GraphicsDevice, box.Width, box.Height, radius, fill.ToSk(), fill.ToSk(), 0f);

                if (rounded != null)
                    spriteBatch.Draw(rounded, box, Color.White);
                else
                    spriteBatch.DrawRectangle(box, fill);
            }

            var color = highlighted ? UITheme.MenuItemTextSelected : UITheme.MenuItemText;

            var iconSize = UIMetrics.ModeIconSize;

            var glyph = new Rectangle(box.X + UIMetrics.ModeIconInset, box.Y + (box.Height - iconSize) / 2, iconSize, iconSize);

            var icon = SkiaIcon.Load(spriteBatch.GraphicsDevice, EditorMode.Icon(), glyph.Width, new SkiaSharp.SKColor(color.R, color.G, color.B, color.A));

            if (icon != null)
                spriteBatch.Draw(icon, glyph, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);

            var textSize = UIMetrics.ModeTextSize;

            var textY = box.Y + (box.Height - UIScale.Value(textSize)) / 2f;

            UIFont.Draw(spriteBatch, Label, new Vector2(box.X + UIMetrics.ModeTextInset, textY), color, textSize);
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type != InputType.MouseLeftDown)
                return InputResult.Continue;

            Chosen?.Invoke(EditorMode);

            return InputResult.Consumed;
        }
    }

    public sealed class ModeMenu : Component
    {
        private const float Roundness = 0.2f;

        private static ModeMenu? _current;

        private readonly List<ModeMenuItem> _items = [];

        private int _rowHeight;

        private int _padding;

        private int _gap;

        public static bool IsOpen => _current != null;

        public static void Show(IReadOnlyList<EditorMode> modes, EditorMode selected, Rectangle anchor, Action<EditorMode> chosen)
        {
            Close();

            if (UIManager.Instance == null || modes.Count == 0)
                return;

            var menu = new ModeMenu
            {
                _rowHeight = UIMetrics.ModeRowHeight,

                _padding = UIMetrics.ModePadding,

                _gap = UIMetrics.ModeGap
            };

            var width = 160;

            foreach (var mode in modes)
            {
                var label = mode.Label();

                width = Math.Max(width, menu._padding * 2 + UIMetrics.ModeTextInset + (int)UIFont.Measure(label, UIMetrics.ModeTextSize).X);
            }

            var height = menu._padding * 2 + menu._rowHeight * modes.Count + menu._gap * (modes.Count - 1);

            var screen = UIManager.Instance.Root.Rectangle;

            var x = Math.Clamp(anchor.X, screen.X, Math.Max(screen.X, screen.Right - width));

            var y = anchor.Bottom + UIScale.Value(3);

            if (y + height > screen.Bottom)
                y = Math.Max(screen.Y, anchor.Y - height - UIScale.Value(3));

            menu.Position = new Vector2(x, y);

            menu.Width = width;

            menu.Height = height;

            var row = y + menu._padding;

            foreach (var mode in modes)
            {
                var item = new ModeMenuItem(mode, mode.Label(), mode == selected)
                {
                    Chosen = chosen,

                    Position = new Vector2(x + menu._padding, row),

                    Width = width - menu._padding * 2,

                    Height = menu._rowHeight
                };

                row += menu._rowHeight + menu._gap;

                menu._items.Add(item);

                menu.RegisterChild(item);
            }

            UIManager.Instance.Root.RegisterChild(menu);

            UIManager.Instance.CapturedComponent = menu;

            _current = menu;
        }

        public static void Close()
        {
            if (_current == null)
                return;

            var menu = _current;

            _current = null;

            UIManager.Instance?.Root.RemoveChild(menu);

            if (ReferenceEquals(UIManager.Instance?.CapturedComponent, menu))
                UIManager.Instance!.CapturedComponent = null;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            foreach (var item in _items)
                item.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var bounds = Rectangle;

            var rounded = SkiaCard.Rounded(
                spriteBatch.GraphicsDevice,
                bounds.Width,
                bounds.Height,
                UIMetrics.WidgetRoundness(Roundness),
                UITheme.MenuBackground.ToSk(),
                UITheme.MenuOutline.ToSk(),
                1f);

            if (rounded != null)
                spriteBatch.Draw(rounded, bounds, Color.White);
            else
                spriteBatch.DrawRectangle(bounds, UITheme.MenuBackground);

            DrawChildren(spriteBatch, gameTime);
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type == InputType.KeyDown && args.Key == Keys.Escape)
            {
                Close();

                return InputResult.Consumed;
            }

            switch (args.Type)
            {
                case InputType.MouseMove:
                    return InputResult.Consumed;

                case InputType.MouseLeftDown:
                case InputType.MouseLeftUp:
                    foreach (var item in _items)
                    {
                        if (!item.Rectangle.Contains(args.MousePosition.ToPoint()))
                            continue;

                        if (args.Type == InputType.MouseLeftDown)
                            Close();

                        return item.OnInput(args);
                    }

                    if (args.Type == InputType.MouseLeftDown)
                        Close();

                    return InputResult.Consumed;

                case InputType.MouseRightDown:
                    Close();

                    return InputResult.Consumed;
            }

            return InputResult.Consumed;
        }
    }
}
