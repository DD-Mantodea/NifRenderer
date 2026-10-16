using Microsoft.Xna.Framework;

namespace NifRenderer.UI.Common
{
    public static class UITheme
    {
        public static readonly Color ScreenBackground = new(24, 24, 24);

        public static readonly Color Accent = new(96, 162, 238);

        public static readonly Color AxisX = new(255, 51, 82);

        public static readonly Color AxisY = new(139, 220, 0);

        public static readonly Color AxisZ = new(40, 144, 255);

        public static readonly Color ViewportBackground = new(63, 63, 63);

        public static readonly Color GridLine = new(79, 79, 79);

        public static readonly Color StatusLabel = new(0x83, 0x83, 0x83);

        public static readonly Color StatusValue = new(0xff, 0xff, 0xff);

        public static readonly Color TextPrimary = new(232, 236, 242);

        public static readonly Color TextDim = new(146, 154, 166);

        public static readonly Color TextFaint = new(104, 110, 120);

        public static readonly Color PanelBackground = new(40, 40, 40);

        public static readonly Color PanelHeader = new(29, 29, 29);

        public static readonly Color RowOdd = new(43, 43, 43);

        public static readonly Color RowOddHover = new(70, 70, 70);

        public static readonly Color RowEvenHover = new(68, 68, 68);

        public static readonly Color RowSelected = new(51, 77, 128);

        public static readonly Color RowSelectedHover = new(77, 100, 144);

        public static Color RowEven => PanelBackground;

        public static readonly Color PopupBody = new(24, 24, 24);

        public static readonly Color PopupBorder = new(58, 58, 58);

        public static readonly Color DropGhost = new(120, 180, 255);

        public static readonly Color DropGhostBorder = new(200, 230, 255);

        public static readonly Color DropPreview = new(80, 150, 255);

        public static readonly Color DropPreviewBorder = new(160, 210, 255);

        public static readonly Color HeaderBackground = new Color(0x30, 0x30, 0x30) * AlphaHeader;

        public static readonly Color HeaderButton = new(0x28, 0x28, 0x28);

        public static readonly Color HeaderButtonOutline = new(0x3d, 0x3d, 0x3d);

        public static readonly Color HeaderButtonHover = new(0x30, 0x30, 0x30);

        public static readonly Color HeaderButtonText = new(0xe6, 0xe6, 0xe6);

        public static readonly Color HeaderButtonTextHover = new(0xff, 0xff, 0xff);

        public static readonly Color MenuBackground = new(0x18, 0x18, 0x18);

        public static readonly Color MenuOutline = new(0x24, 0x24, 0x24);

        public static readonly Color MenuItemSelected = new(0x47, 0x72, 0xb3);

        public static readonly Color MenuItemText = new(0xdd, 0xdd, 0xdd);

        public static readonly Color MenuItemTextSelected = new(0xff, 0xff, 0xff);

        private const float AlphaHeader = 0xb3 / 255f;

        public static readonly Color MenuItemHover = Mix(MenuBackground, MenuItemText, 0.2f);

        public static Color Premultiply(Color color) => color * (color.A / 255f);

        public static Color Mix(Color a, Color b, float t) => new(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)255);
    }
}
