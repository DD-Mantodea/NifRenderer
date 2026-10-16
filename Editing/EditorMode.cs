namespace NifRenderer.Editing
{
    public enum EditorMode
    {
        Object,

        Edit,

        Slider
    }

    public static class EditorModes
    {
        public static readonly EditorMode[] All =
        [
            EditorMode.Object,

            EditorMode.Edit,

            EditorMode.Slider
        ];

        public static string Label(this EditorMode mode) => mode switch
        {
            EditorMode.Edit => "Edit Mode",

            EditorMode.Slider => "Slider Mode",

            _ => "Object Mode"
        };

        public static string Icon(this EditorMode mode) => mode switch
        {
            EditorMode.Edit => "mode-edit.svg",

            EditorMode.Slider => "mode-slider.svg",

            _ => "mode-object.svg"
        };
    }
}
