using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;
using NifRenderer.UI;
using SkiaSharp;
using System;

namespace NifRenderer.UI.Components
{
    public sealed class LoadingPopup
    {
        private const int CardWidthBase = 268;

        private const int CardHeightBase = 84;

        private const int ScrimAlpha = 130;

        private const int BorderBase = 2;

        private const int RadiusBase = 12;

        private static int CardWidth => UIScale.Value(CardWidthBase);

        private static int CardHeight => UIScale.Value(CardHeightBase);

        private float _loadingTime;

        private float _fade;

        public bool Visible { get; private set; }

        public string Message = "";

        public int Pending;

        public void Update(GameTime gameTime, bool loading)
        {
            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (loading)
                _loadingTime += delta;
            else
                _loadingTime = 0f;

            Visible = loading && _loadingTime >= 0.12f;

            _fade = Math.Clamp(_fade + (Visible ? delta : -delta) * 8f, 0f, 1f);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, GraphicsDevice device)
        {
            if (_fade <= 0.001f)
                return;

            var card = new Rectangle(
                bounds.Center.X - CardWidth / 2,
                bounds.Center.Y - CardHeight / 2,
                CardWidth,
                CardHeight);

            spriteBatch.DrawRectangle(bounds, WithAlpha(Color.Black, ScrimAlpha / 255f * _fade));

            var texture = SkiaCard.Rounded(
                device,
                card.Width,
                card.Height,
                UIScale.Value(RadiusBase),
                ToSk(UITheme.PopupBody),
                ToSk(UITheme.PopupBorder),
                UIScale.Value(BorderBase));

            if (texture != null)
                spriteBatch.Draw(texture, card, WithAlpha(Color.White, _fade));
            else
                spriteBatch.DrawRectangle(card, WithAlpha(UITheme.PopupBody, _fade));

            DrawText(spriteBatch, card);
        }

        private void DrawText(SpriteBatch spriteBatch, Rectangle card)
        {
            var available = card.Width - UIScale.Value(36);

            DrawCentered(spriteBatch, card, "LOADING", UIScale.Value(15), WithAlpha(UITheme.TextPrimary, _fade), 15);

            var detail = string.IsNullOrEmpty(Message) ? "..." : UIFont.Ellipsize(Message, available, 12);

            DrawCentered(spriteBatch, card, detail, UIScale.Value(39), WithAlpha(UITheme.TextDim, _fade), 12);

            if (Pending > 1)
                DrawCentered(spriteBatch, card, $"{Pending - 1} more queued", UIScale.Value(57), WithAlpha(UITheme.TextFaint, _fade), 12);
        }

        private static void DrawCentered(SpriteBatch spriteBatch, Rectangle card, string text, int offsetY, Color color, int size)
        {
            var width = UIFont.Measure(text, size).X;

            UIFont.Draw(spriteBatch, text, new Vector2(card.X + (card.Width - width) / 2f, card.Y + offsetY), color, size);
        }

        private static SKColor ToSk(Color color) => new(color.R, color.G, color.B, color.A);

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.R, color.G, color.B, (byte)Math.Clamp(MathF.Round(255f * alpha), 0f, 255f));
    }
}
