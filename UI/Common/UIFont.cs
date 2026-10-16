using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using SysEnv = System.Environment;

namespace NifRenderer.UI.Common
{
    public static class UIFont
    {
        private const string FileName = "JetBrainsMono-Regular.ttf";

        private static readonly string LocalFonts = Path.Combine(SysEnv.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");

        private static readonly string[] Candidates =
        [
            Path.Combine("Content", "Fonts", FileName),
            Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", FileName),
            Path.Combine(LocalFonts, FileName),
            Path.Combine(LocalFonts, "JetBrainsMonoNL-Regular.ttf")
        ];

        private static readonly Dictionary<int, SpriteFontBase> Cache = [];

        private static FontSystem? _system;

        private static bool _loaded;

        public static string Source { get; private set; } = "none";

        public static bool Available => SystemReady();

        private static FontSystem System
        {
            get
            {
                if (_system != null)
                    return _system;

                _system = new FontSystem();

                foreach (var path in Candidates)
                {
                    if (!File.Exists(path))
                        continue;

                    _system.AddFont(File.ReadAllBytes(path));

                    Source = Path.GetFileName(path);

                    _loaded = true;

                    break;
                }

                return _system;
            }
        }

        private static bool SystemReady()
        {
            _ = System;

            return _loaded;
        }

        public static SpriteFontBase Font(int size)
        {
            var pixelSize = Math.Max(6, (int)MathF.Round(size * UIScale.Factor));

            if (Cache.TryGetValue(pixelSize, out var cached))
                return cached;

            var font = System.GetFont(pixelSize);

            Cache[pixelSize] = font;

            return font;
        }

        public static void Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, int size)
        {
            if (!SystemReady() || string.IsNullOrEmpty(text))
                return;

            spriteBatch.DrawString(Font(size), text, position, color);
        }

        public static Vector2 Measure(string text, int size)
        {
            if (!SystemReady() || string.IsNullOrEmpty(text))
                return Vector2.Zero;

            return Font(size).MeasureString(text);
        }

        public static string Truncate(string text, int maxWidth, int size)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return "";

            if (Measure(text, size).X <= maxWidth)
                return text;

            var low = 0;

            var high = text.Length;

            while (low < high)
            {
                var mid = (low + high + 1) / 2;

                if (Measure(text[..mid], size).X <= maxWidth)
                    low = mid;
                else
                    high = mid - 1;
            }

            return text[..low];
        }

        public static string Ellipsize(string text, int maxWidth, int size)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return "";

            if (Measure(text, size).X <= maxWidth)
                return text;

            const string Ellipsis = "...";

            var budget = maxWidth - Measure(Ellipsis, size).X;

            if (budget <= 0)
                return Ellipsis;

            var low = 0;

            var high = text.Length;

            while (low < high)
            {
                var mid = (low + high + 1) / 2;

                if (Measure(text[..mid], size).X <= budget)
                    low = mid;
                else
                    high = mid - 1;
            }

            return low <= 0 ? Ellipsis : text[..low] + Ellipsis;
        }

        public static void Invalidate()
        {
            Cache.Clear();
        }
    }
}
