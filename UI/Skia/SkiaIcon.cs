using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Utils;
using SkiaGameRendering;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;

namespace NifRenderer.UI.Skia
{
    public static class SkiaIcon
    {
        private static readonly Dictionary<string, Entry> Cache = [];

        private static readonly Queue<string> Order = new();

        private const int MaxEntries = 96;

        private sealed class Entry
        {
            public SkiaRenderTarget2D Target;

            public Texture2D Texture;

            public SKSvg Svg;

            public SKPicture Picture;

            public SKPaint Paint;
        }

        private static void Bound()
        {
            while (Cache.Count > MaxEntries && Order.Count > 0)
            {
                var oldest = Order.Dequeue();

                if (!Cache.Remove(oldest, out var evicted))
                    continue;

                evicted.Target?.Dispose();

                evicted.Paint?.Dispose();

                evicted.Svg?.Dispose();
            }
        }

        public static Texture2D? Load(GraphicsDevice device, string name, int size, SKColor color)
        {
            if (size <= 0)
                return null;

            var key = $"{name}@{size}@{color}";

            if (Cache.TryGetValue(key, out var hit))
                return hit.Texture;

            var folder = name.Contains('/') || name.Contains('\\') ? name : Path.Combine("Icons", name);

            var path = Path.Combine(AppContext.BaseDirectory, "Content", folder);

            if (!File.Exists(path))
            {

                return null;
            }

            try
            {
                var svg = new SKSvg();

                var picture = svg.Load(path);

                if (picture == null)
                    throw new InvalidDataException($"SVG 解析失败: {path}");

                var target = new SkiaRenderTarget2D(device, size, size);

                target.Begin();

                target.Canvas.Clear(SKColors.Transparent);

                var bounds = picture.CullRect;

                var scale = size / Math.Max(bounds.Width, bounds.Height);

                target.Canvas.Save();

                target.Canvas.Scale(scale);

                target.Canvas.Translate(-bounds.Left, -bounds.Top);

                var paint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.SrcIn)
                };

                target.Canvas.DrawPicture(picture, paint);

                target.Canvas.Restore();

                target.EndWithoutDrawing();

                Cache[key] = new Entry
                {
                    Target = target,
                    Texture = target.Texture,
                    Svg = svg,
                    Picture = picture,
                    Paint = paint
                };

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
            {
                entry.Target?.Dispose();

                entry.Paint?.Dispose();

                entry.Svg?.Dispose();
            }

            Cache.Clear();

            Order.Clear();
        }
    }
}
