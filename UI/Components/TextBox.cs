using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NifRenderer.Extensions;
using NifRenderer.UI.Common;

namespace NifRenderer.UI.Components
{
    public class TextBox : Component
    {
        public static TextBox? Active;

        public string Text = "";

        public int Caret;

        public int Anchor;

        public bool OnlyNumber;

        public float Step = 1f;

        public float Min = float.MinValue;

        public float Max = float.MaxValue;

        public Action<float>? Committed;

        public Action? Cancelled;

        public int LeftInset;

        public int FontSize = 12;

        public Color TextColor = UITheme.TextPrimary;

        public Color CaretColor = UITheme.TextPrimary;

        public Color SelectionColor = UITheme.RowSelected;

        public Color BoxColor = UITheme.PanelHeader;

        private static int Pad => UIScale.Value(4);

        private int _blink;

        private readonly Dictionary<Keys, int> _held = [];

        private int _repeatDelay = 24;

        private int _repeatRate = 3;

        public bool HasSelection => Caret != Anchor;

        private int SelectionStart => Math.Min(Caret, Anchor);

        private int SelectionLength => Math.Abs(Caret - Anchor);

        private int TextLeft => Rectangle.X + Pad + LeftInset;

        public void SetActive(bool active)
        {
            if (active)
            {
                Active?.Finish(false);

                Active = this;

                _blink = 0;

                Caret = Text.Length;

                Anchor = Caret;

                UIManager.Instance.CapturedComponent = this;

                UIManager.Instance.FocusedComponent = this;

                return;
            }

            Release();

            if (ReferenceEquals(Active, this))
                Active = null;
        }

        private void Release()
        {
            if (ReferenceEquals(UIManager.Instance?.CapturedComponent, this))
                UIManager.Instance!.CapturedComponent = null;

            if (ReferenceEquals(UIManager.Instance?.FocusedComponent, this))
                UIManager.Instance!.FocusedComponent = null;

            _held.Clear();

            Visible = false;
        }

        public void Finish(bool commit)
        {
            if (ReferenceEquals(Active, this))
                Active = null;

            if (commit)
            {
                Commit();

                return;
            }

            Cancelled?.Invoke();

            Release();
        }

        private void Commit()
        {
            var value = 0f;

            var parsed = !OnlyNumber || float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

            Release();

            if (parsed && OnlyNumber)
                Committed?.Invoke(Math.Clamp(value, Min, Max));
        }

        public override void Update(GameTime gameTime)
        {
            Width = Rectangle.Width;

            Height = Rectangle.Height;

            if (!ReferenceEquals(Active, this))
                return;

            _blink = (_blink + 1) % 60;

            var held = new List<Keys>(_held.Keys);

            foreach (var key in held)
            {
                _held[key]++;

                if (_held[key] < _repeatDelay)
                    continue;

                if ((_held[key] - _repeatDelay) % _repeatRate != 0)
                    continue;

                ApplyKey(key, false);
            }
        }

        public override InputResult OnInput(InputEventArgs args)
        {
            if (!ReferenceEquals(Active, this))
                return InputResult.Continue;

            switch (args.Type)
            {
                case InputType.TextInput:
                    Insert(args.Character);

                    return InputResult.Consumed;

                case InputType.KeyUp:
                    _held.Remove(args.Key);

                    return InputResult.Consumed;

                case InputType.MouseLeftDown:
                    if (!Rectangle.Contains(args.MousePosition.ToPoint()))
                    {
                        Finish(true);

                        return InputResult.Continue;
                    }

                    Anchor = Caret = CaretFrom(args.MousePosition.X);

                    _blink = 0;

                    UIManager.Instance.CapturedComponent = this;

                    return InputResult.Consumed;

                case InputType.MouseMove:
                    if (!UserInput.CurrentMouseState.MouseLeft())
                        return InputResult.Continue;

                    Caret = CaretFrom(args.MousePosition.X);

                    return InputResult.Consumed;

                case InputType.MouseLeftUp:
                    UIManager.Instance.CapturedComponent = null;

                    return InputResult.Consumed;

                case InputType.KeyDown:
                    if (args.Key == Keys.Escape)
                    {
                        Finish(false);

                        return InputResult.Consumed;
                    }

                    if (args.Key == Keys.Enter || args.Key == Keys.Tab)
                    {
                        Finish(true);

                        return InputResult.Consumed;
                    }

                    if (!_held.ContainsKey(args.Key))
                        _held[args.Key] = 0;

                    ApplyKey(args.Key, true);

                    return InputResult.Consumed;
            }

            return InputResult.Consumed;
        }

        private void ApplyKey(Keys key, bool just)
        {
            switch (key)
            {
                case Keys.A:
                    if (UserInput.Ctrl)
                    {
                        Anchor = 0;

                        Caret = Text.Length;
                    }

                    return;

                case Keys.C:
                    if (UserInput.Ctrl && HasSelection)
                        Clipboard.SetClipboardText(Text.Substring(SelectionStart, SelectionLength));

                    return;

                case Keys.X:
                    if (UserInput.Ctrl && HasSelection)
                    {
                        Clipboard.SetClipboardText(Text.Substring(SelectionStart, SelectionLength));

                        DeleteSelection();
                    }

                    return;

                case Keys.V:
                    if (UserInput.Ctrl)
                        Insert(Clipboard.GetClipboardText() ?? "");

                    return;

                case Keys.Left:
                    Move(UserInput.Shift ? Caret - 1 : Math.Min(Caret, Anchor) - 1, UserInput.Shift);

                    return;

                case Keys.Right:
                    Move(UserInput.Shift ? Caret + 1 : Math.Max(Caret, Anchor) + 1, UserInput.Shift);

                    return;

                case Keys.Home:
                    Move(0, UserInput.Shift);

                    return;

                case Keys.End:
                    Move(Text.Length, UserInput.Shift);

                    return;

                case Keys.Back:
                    Backspace();

                    return;

                case Keys.Delete:
                    DeleteSelection();

                    return;

                case Keys.Up:
                case Keys.OemPlus:
                case Keys.Add:
                    Nudge(1f);

                    return;

                case Keys.Down:
                case Keys.OemMinus:
                case Keys.Subtract:
                    Nudge(-1f);

                    return;
            }

            if (just && OnlyNumber && key is >= Keys.NumPad0 and <= Keys.NumPad9)
                Insert((char)('0' + (key - Keys.NumPad0)));
        }

        private void Move(int index, bool extend)
        {
            Caret = Math.Clamp(index, 0, Text.Length);

            if (!extend)
                Anchor = Caret;

            _blink = 0;
        }

        private void Nudge(float direction)
        {
            if (!float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                value = 0f;

            value = Math.Clamp(value + direction * Step, Min, Max);

            Text = value.ToString("0.###", CultureInfo.InvariantCulture);

            Caret = Anchor = Text.Length;
        }

        private void Backspace()
        {
            if (DeleteSelection())
                return;

            if (Caret == 0)
                return;

            Text = Text.Remove(Caret - 1, 1);

            Caret = Anchor = Caret - 1;
        }

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            var start = SelectionStart;

            Text = Text.Remove(start, SelectionLength);

            Caret = Anchor = start;

            return true;
        }

        private void Insert(string text)
        {
            if (text.Length == 0)
                return;

            DeleteSelection();

            var accepted = "";

            foreach (var c in text)
            {
                if (OnlyNumber && !(char.IsDigit(c) || c is '.' or '-' or '+'))
                    continue;

                if (char.IsControl(c))
                    continue;

                accepted += c;
            }

            if (accepted.Length == 0)
                return;

            Text = Text.Insert(Caret, accepted);

            Caret = Anchor = Caret + accepted.Length;

            _blink = 0;
        }

        private void Insert(char c) => Insert(c.ToString());

        private int CaretFrom(float mouseX)
        {
            var text = new string(Text);

            for (var i = 0; i <= text.Length; i++)
            {
                var width = UIFont.Measure(text[..i], FontSize).X;

                var next = i < text.Length ? UIFont.Measure(text[..(i + 1)], FontSize).X : width;

                if (mouseX < TextLeft + (width + next) / 2f)
                    return i;
            }

            return text.Length;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!Visible)
                return;

            var box = Rectangle;

            var active = ReferenceEquals(Active, this);

            var hover = box.Contains(UserInput.GetMousePos().ToPoint());

            var outline = hover || active ? UITheme.Accent : UITheme.HeaderButtonOutline;

            var fill = hover || active ? UITheme.RowOddHover : BoxColor;

            SkiaCard.DrawRounded(spriteBatch, box, fill, outline, UIMetrics.WidgetRoundness(0.15f), 1f);

            if (active && HasSelection)
            {
                var start = UIFont.Measure(Text[..SelectionStart], FontSize).X;

                var width = UIFont.Measure(Text.Substring(SelectionStart, SelectionLength), FontSize).X;

                spriteBatch.DrawRectangle(new Rectangle((int)(TextLeft + start), box.Y + Pad, Math.Max(1, (int)width), box.Height - Pad * 2), SelectionColor);
            }

            var textY = box.Y + (box.Height - UIScale.Value(FontSize)) / 2f;

            UIFont.Draw(spriteBatch, Text, new Vector2(TextLeft, textY), TextColor, FontSize);

            if (!active || _blink > 30)
                return;

            var caret = UIFont.Measure(Text[..Caret], FontSize).X;

            spriteBatch.DrawRectangle(new Rectangle((int)(TextLeft + caret), box.Y + Pad, 1, box.Height - Pad * 2), CaretColor);
        }
    }
}
