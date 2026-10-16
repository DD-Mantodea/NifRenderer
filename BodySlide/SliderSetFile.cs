using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NifRenderer.BodySlide
{
    public sealed class SliderDataRef
    {
        public string Name = "";

        public string Target = "";

        public bool Local;

        public string File = "";
    }

    public sealed class SliderDefinition
    {
        public string Name = "";

        public bool Invert;

        public int Small;

        public int Big;

        public int Default;

        public bool Hidden;

        public bool Clamp;

        public bool Zap;

        public bool Uv;

        public string ZapToggles = "";

        public readonly List<SliderDataRef> Data = [];

        public string Source = "";
    }

    public sealed class SliderSetFile
    {
        public string Name = "";

        public string DataFolder = "";

        public string SourceFile = "";

        public string OutputPath = "";

        public string OutputFile = "";

        public bool GenWeights;

        public bool PreventMorphFile;

        public bool KeepZappedShapes;

        public readonly List<string> Shapes = [];

        public readonly List<SliderDefinition> Sliders = [];

        public static SliderSetFile Read(string path)
        {
            var document = XDocument.Load(path);

            var file = new SliderSetFile();

            var set = document.Root?.Element("SliderSet");

            if (set == null)
                return file;

            file.Name = (string?)set.Attribute("name") ?? "";

            file.DataFolder = (string?)set.Element("DataFolder") ?? "";

            file.SourceFile = (string?)set.Element("SourceFile") ?? "";

            file.OutputPath = (string?)set.Element("OutputPath") ?? "";

            var output = set.Element("OutputFile");

            if (output != null)
            {
                file.OutputFile = output.Value;

                file.GenWeights = Bool(output, "GenWeights");

                file.PreventMorphFile = Bool(output, "PreventMorphFile");

                file.KeepZappedShapes = Bool(output, "KeepZappedShapes");
            }

            foreach (var shape in set.Elements("Shape"))
                file.Shapes.Add(shape.Value);

            foreach (var slider in set.Elements("Slider"))
            {
                var definition = new SliderDefinition
                {
                    Name = (string?)slider.Attribute("name") ?? "",
                    Invert = Bool(slider, "invert"),
                    Small = slider.Attribute("small") != null ? Int(slider, "small") : 0,
                    Big = slider.Attribute("big") != null ? Int(slider, "big") : 100,
                    Hidden = Bool(slider, "hidden"),
                    Clamp = Bool(slider, "clamp"),
                    Zap = Bool(slider, "zap"),
                    Uv = Bool(slider, "uv"),
                    ZapToggles = (string?)slider.Attribute("zaptoggles") ?? ""
                };

                definition.Default = slider.Attribute("default") != null ? Int(slider, "default") : definition.Big;

                foreach (var data in slider.Elements("Data"))
                {
                    definition.Data.Add(new SliderDataRef
                    {
                        Name = (string?)data.Attribute("name") ?? "",
                        Target = (string?)data.Attribute("target") ?? "",
                        Local = Bool(data, "local"),
                        File = data.Value
                    });
                }

                file.Sliders.Add(definition);
            }

            return file;
        }

        private static bool Bool(XElement element, string name) =>
            string.Equals((string?)element.Attribute(name), "true", System.StringComparison.OrdinalIgnoreCase);

        private static int Int(XElement element, string name)
        {
            var value = (string?)element.Attribute(name);

            return int.TryParse(value, out var parsed) ? parsed : 0;
        }
    }

    public sealed class SliderSet
    {
        public SliderSetFile Definition = new();

        public OsdFile? Osd;

        public static SliderSet Load(string ospPath)
        {
            var set = new SliderSet
            {
                Definition = SliderSetFile.Read(ospPath)
            };

            var folder = Path.GetDirectoryName(ospPath);

            var shapeData = Path.Combine(folder ?? "", "..", "ShapeData", set.Definition.DataFolder);

            var osd = Path.Combine(Path.GetFullPath(shapeData), set.Definition.DataFolder + ".osd");

            if (File.Exists(osd))
                set.Osd = OsdFile.Read(osd);

            return set;
        }

        public IEnumerable<SliderDefinition> Usable =>
            Definition.Sliders.Where(s => s.Data.Count > 0);
    }
}
