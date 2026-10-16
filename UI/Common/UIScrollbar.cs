using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;

namespace NifRenderer.UI.Common
{
    public static class UIScrollbar
    {
        public static object? DragOwner;

        public static int Thickness => UIScale.Value(6);

        public static int MinThumb => UIScale.Value(24);

        private static float Roundness => UIMetrics.WidgetRoundness(0.5f);

        private static Color Fade(Color color, float alpha) =>
            new(color.R, color.G, color.B, (byte)Math.Clamp(color.A * alpha, 0f, 255f));

        public static float Ease(float current, bool visible, float dt, float rate = 12f) =>
            current + ((visible ? 1f : 0f) - current) * Math.Min(1f, dt * rate);

        public static Rectangle Track(Rectangle area)
        {
            var thickness = Thickness;

            return new Rectangle(area.Right - thickness, area.Y, thickness, Math.Max(1, area.Height));
        }

        public static Rectangle Thumb(Rectangle area, int contentHeight, int viewHeight, int scroll)
        {
            var track = Track(area);

            if (contentHeight <= viewHeight || contentHeight <= 0)
                return Rectangle.Empty;

            var height = Math.Max(MinThumb, (int)(track.Height * (viewHeight / (float)contentHeight)));

            var travel = Math.Max(0, track.Height - height);

            var max = Math.Max(1, contentHeight - viewHeight);

            var offset = (int)(travel * (Math.Clamp(scroll, 0, max) / (float)max));

            return new Rectangle(track.X + 1, track.Y + offset, Math.Max(1, track.Width - 2), height);
        }

        public static void Draw(SpriteBatch spriteBatch, Rectangle area, int contentHeight, int viewHeight, int scroll, float alpha)
        {
            if (alpha <= 0.01f || contentHeight <= viewHeight || contentHeight <= 0)
                return;

            var track = Track(area);

            var radius = Roundness;

            SkiaCard.DrawRounded(spriteBatch, track, Fade(UITheme.RowEven, alpha * 0.6f), Fade(UITheme.HeaderButtonOutline, alpha * 0.5f), radius, 1f);

            SkiaCard.DrawRounded(spriteBatch, Thumb(area, contentHeight, viewHeight, scroll), Fade(UITheme.TextFaint, alpha), Fade(UITheme.TextFaint, alpha), radius, 0f);
        }

        public static bool Contains(Rectangle area, Point position, int contentHeight, int viewHeight) =>
            contentHeight > viewHeight && contentHeight > 0 && Track(area).Contains(position);

        public static int ScrollFrom(Rectangle area, int contentHeight, int viewHeight, float mouseY)
        {
            var track = Track(area);

            var height = Thumb(area, contentHeight, viewHeight, 0).Height;

            var travel = Math.Max(1, track.Height - height);

            var max = Math.Max(0, contentHeight - viewHeight);

            var t = Math.Clamp((mouseY - track.Y - height / 2f) / travel, 0f, 1f);

            return (int)MathF.Round(t * max);
        }
    }
}
