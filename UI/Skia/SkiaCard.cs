using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;
using NifRenderer.Utils;
using SkiaGameRendering;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace NifRenderer.UI.Skia
{
    public static class SkiaCard
    {
        private static readonly Dictionary<string, Entry> Cache = [];

        private static readonly Queue<string> Order = new();

        private const int MaxEntries = 64;

        private sealed class Entry
        {
            public SkiaRenderTarget2D Target;

            public Texture2D Texture;
        }

        private static void Bound()
        {
            while (Cache.Count > MaxEntries && Order.Count > 0)
            {
                var oldest = Order.Dequeue();

                if (Cache.Remove(oldest, out var evicted))
                    evicted.Target?.Dispose();
            }
        }

        public static SKColor ToSk(this Color color) => new(color.R, color.G, color.B, color.A);

        public static void DrawRounded(SpriteBatch spriteBatch, Rectangle rect, Color fill, Color border, float radius, float borderWidth)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            var texture = Rounded(spriteBatch.GraphicsDevice, rect.Width, rect.Height, radius, fill.ToSk(), border.ToSk(), borderWidth);

            if (texture != null)
                spriteBatch.Draw(texture, rect, Color.White);
            else
                spriteBatch.DrawRectangle(rect, fill);
        }

        public static Texture2D? Rounded(GraphicsDevice device, int width, int height, float radius, SKColor fill, SKColor border, float borderWidth)
        {
            if (width <= 0 || height <= 0)
                return null;

            var key = $"{width}x{height}@{radius}@{fill}@{border}@{borderWidth}";

            if (Cache.TryGetValue(key, out var hit))
                return hit.Texture;

            try
            {
                var target = new SkiaRenderTarget2D(device, width, height);

                target.Begin();

                target.Canvas.Clear(SKColors.Transparent);

                var inset = borderWidth * 0.5f;

                var rect = new SKRect(inset, inset, width - inset, height - inset);

                using (var paint = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill })
                    target.Canvas.DrawRoundRect(rect, radius, radius, paint);

                if (borderWidth > 0f)
                {
                    using var stroke = new SKPaint { Color = border, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = borderWidth };

                    target.Canvas.DrawRoundRect(rect, radius, radius, stroke);
                }

                target.EndWithoutDrawing();

                Cache[key] = new Entry { Target = target, Texture = target.Texture };

                Order.Enqueue(key);

                Bound();

                return target.Texture;
            }
            catch (Exception exception)
            {

                return null;
            }
        }

        public static void Clear()
        {
            foreach (var entry in Cache.Values)
                entry.Target?.Dispose();

            Cache.Clear();

            Order.Clear();
        }
    }
}
