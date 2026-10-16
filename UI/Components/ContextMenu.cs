using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;

namespace NifRenderer.UI.Components
{
    public sealed class ContextMenu : Component
    {
        public sealed class Entry
        {
            public string Label = "";

            public string Shortcut = "";

            public string Icon = "";

            public Action? Action;

            public bool Enabled = true;

            public bool Separator;

            public static Entry Of(string label, Action action, bool enabled = true, string icon = "", string shortcut = "")
                => new() { Label = label, Action = action, Enabled = enabled, Icon = icon, Shortcut = shortcut };

            public static Entry Split() => new() { Separator = true };
        }

        private const int TextSizeBase = 15;

        private const int MenuPaddingBase = 4;

        private const int RowPadYBase = 3;

        private const int SeparatorHeightBase = 7;

        private const int ItemInsetBase = 3;

        private const int RadiusBase = 5;

        private const int MinWidthBase = 150;

        private const int ShortcutGapBase = 24;

        private const float TextOffsetRatio = 0.25f;

        private const float HoverBlend = 0.2f;

        private readonly List<Entry> _entries;

        private int _hover = -1;

        private int _rowHeight;


        private static ContextMenu? _current;

        private ContextMenu(List<Entry> entries)
        {
            _entries = entries;
        }

        public static void Show(IReadOnlyList<Entry> entries, Vector2 position)
        {
            Close();

            if (UIManager.Instance == null || entries.Count == 0)
                return;

            var menu = new ContextMenu([.. entries]);

            var size = menu.Measure();

            var screen = UIManager.Instance.Root.Rectangle;

            var x = Math.Clamp((int)position.X, screen.X, Math.Max(screen.X, screen.Right - size.X));

            var y = Math.Clamp((int)position.Y, screen.Y, Math.Max(screen.Y, screen.Bottom - size.Y));

            menu.Position = new Vector2(x, y);

            menu.Width = size.X;

            menu.Height = size.Y;

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

        public static bool IsOpen => _current != null;

        private Point Measure()
        {
            _rowHeight = (int)MathF.Ceiling(UIFont.Measure("Ag", TextSizeBase).Y) + UIScale.Value(RowPadYBase) * 2;

            var shortcutWidth = 0;

            var labelWidth = 0;

            foreach (var entry in _entries)
            {
                if (entry.Separator)
                    continue;

                labelWidth = Math.Max(labelWidth, (int)UIFont.Measure(entry.Label, TextSizeBase).X);

                if (!string.IsNullOrEmpty(entry.Shortcut))
                    shortcutWidth = Math.Max(shortcutWidth, (int)UIFont.Measure(entry.Shortcut, TextSizeBase).X);
            }

            var textLeft = (int)(_rowHeight * TextOffsetRatio) + _rowHeight;

            var width = UIScale.Value(MinWidthBase);

            if (labelWidth > 0)
            {
                width = Math.Max(
                    width,
                    textLeft + labelWidth + (shortcutWidth > 0 ? shortcutWidth + UIScale.Value(ShortcutGapBase) : 0) + (int)(_rowHeight * TextOffsetRatio));
            }

            var height = UIScale.Value(MenuPaddingBase) * 2;

            foreach (var entry in _entries)
                height += entry.Separator ? UIScale.Value(SeparatorHeightBase) : _rowHeight;

            return new Point(width, height);
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (args.Type == InputType.KeyDown && args.Key == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                Close();

                return InputResult.Consumed;
            }

            if (args.Type == InputType.MouseLeftDown)
            {
                var index = IndexAt(args.MousePosition);

                if (index >= 0 && _entries[index].Enabled)
                {
                    var action = _entries[index].Action;

                    Close();

                    action?.Invoke();

                    return InputResult.Consumed;
                }

                if (!Rectangle.Contains(args.MousePosition.ToPoint()))
                    Close();

                return InputResult.Consumed;
            }

            if (args.Type == InputType.MouseRightDown)
            {
                Close();

                return InputResult.Consumed;
            }

            return InputResult.Continue;
        }

        private int IndexAt(Vector2 position)
        {
            if (!Rectangle.Contains(position.ToPoint()))
                return -1;

            var y = Rectangle.Y + UIScale.Value(MenuPaddingBase);

            for (var i = 0; i < _entries.Count; i++)
            {
                var height = _entries[i].Separator ? UIScale.Value(SeparatorHeightBase) : _rowHeight;

                if (position.Y >= y && position.Y < y + height)
                    return _entries[i].Separator ? -1 : i;

                y += height;
            }

            return -1;
        }

        public override void Update(GameTime gameTime)
        {
            _hover = IndexAt(UserInput.GetMousePos());
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var bounds = Rectangle;

            var device = GameHost.Instance.GraphicsDevice;

            var texture = SkiaCard.Rounded(
                device,
                bounds.Width,
                bounds.Height,
                UIScale.Value(RadiusBase) + 2,
                ToSk(UITheme.PopupBody),
                ToSk(UITheme.PopupBorder),
                1);

            if (texture != null)
                spriteBatch.Draw(texture, bounds, Color.White);
            else
                spriteBatch.DrawRectangle(bounds, UITheme.PopupBody);

            var y = bounds.Y + UIScale.Value(MenuPaddingBase);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                var height = entry.Separator ? UIScale.Value(SeparatorHeightBase) : _rowHeight;

                if (entry.Separator)
                {
                    var inset = UIScale.Value(ItemInsetBase);

                    var line = new Rectangle(
                        bounds.X + UIScale.Value(MenuPaddingBase) + inset,
                        y + height / 2,
                        bounds.Width - (UIScale.Value(MenuPaddingBase) + inset) * 2,
                        1);

                    spriteBatch.DrawRectangle(line, UITheme.PopupBorder);

                    y += height;

                    continue;
                }

                var row = new Rectangle(
                    bounds.X + UIScale.Value(MenuPaddingBase),
                    y,
                    bounds.Width - UIScale.Value(MenuPaddingBase) * 2,
                    height);

                DrawRow(spriteBatch, device, entry, row, _hover == i);

                y += height;
            }
        }

        private void DrawRow(SpriteBatch spriteBatch, GraphicsDevice device, Entry entry, Rectangle row, bool hover)
        {
            var textColor = entry.Enabled ? UITheme.TextPrimary : WithAlpha(UITheme.TextPrimary, 128);

            if (hover && entry.Enabled)
            {
                var fill = Blend(UITheme.PopupBody, UITheme.TextPrimary, HoverBlend);

                var inset = UIScale.Value(ItemInsetBase);

                var highlight = new Rectangle(row.X + inset, row.Y, row.Width - inset * 2, row.Height);

                var round = SkiaCard.Rounded(
                    device,
                    highlight.Width,
                    highlight.Height,
                    UIScale.Value(RadiusBase),
                    ToSk(fill),
                    ToSk(fill),
                    0);

                if (round != null)
                    spriteBatch.Draw(round, highlight, Color.White);
                else
                    spriteBatch.DrawRectangle(highlight, fill);

                textColor = Color.White;
            }

            var front = (int)(row.Height * TextOffsetRatio);

            if (!string.IsNullOrEmpty(entry.Icon))
            {
                    var iconSize = Math.Max(8, (int)(row.Height * 0.62f));

                    var icon = SkiaIcon.Load(
                        device,
                        entry.Icon,
                        iconSize,
                        new SkiaSharp.SKColor(textColor.R, textColor.G, textColor.B, textColor.A));

                    if (icon != null)
                    {
                        var box = new Rectangle(
                            row.X + front + (row.Height - iconSize) / 2,
                            row.Y + (row.Height - iconSize) / 2,
                            iconSize,
                            iconSize);

                        spriteBatch.Draw(icon, box, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
                    }
                }

            front += row.Height;

            var textLeft = row.X + front;

            var textY = row.Y + 3.5f;

            UIFont.Draw(spriteBatch, entry.Label, new Vector2(textLeft, textY), textColor, TextSizeBase);

            if (string.IsNullOrEmpty(entry.Shortcut))
                return;

            var shortcutWidth = UIFont.Measure(entry.Shortcut, TextSizeBase).X;

            var shortcutX = row.Right - (int)(row.Height * TextOffsetRatio) - shortcutWidth;

            UIFont.Draw(
                spriteBatch,
                entry.Shortcut,
                new Vector2(shortcutX, textY),
                entry.Enabled ? UITheme.TextDim : WithAlpha(UITheme.TextDim, 128),
                TextSizeBase);
        }

        private static Color Blend(Color a, Color b, float t) => new(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)255);

        private static Color WithAlpha(Color color, byte alpha) => new(color.R, color.G, color.B, alpha);

        private static SkiaSharp.SKColor ToSk(Color color) => new(color.R, color.G, color.B, color.A);
    }
}
