namespace NifRenderer.UI.Common
{
    public static class UIMetrics
    {
        private const int CornerRadiusBase = 10;

        private const int WidgetUnitBase = 20;

        public static int CornerRadius => UIScale.Value(CornerRadiusBase);

        public static int WidgetUnit => UIScale.Value(WidgetUnitBase);

        public static int ModeTextSize => 13;

        public static int ModeIconSize => UIScale.Value(11);

        public static int ModeIconInset => UIScale.Value(6);

        public static int ModeTextInset => UIScale.Value(22);

        public static int ModeButtonHeight => UIScale.Value(19);

        public static int ModeRowHeight => UIScale.Value(16);

        public static int ModePadding => UIScale.Value(4);

        public static int ModeGap => UIScale.Value(3);

        public static float WidgetRoundness(float roundness) => WidgetUnit * roundness;
    }
}
