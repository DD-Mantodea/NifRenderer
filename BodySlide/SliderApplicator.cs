using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NiflySharp.Blocks;
using NifRenderer.Renderer.Scene;

namespace NifRenderer.BodySlide
{
    public sealed class SliderApplicator
    {
        private sealed class Entry
        {
            public string Slider = "";

            public Dictionary<ushort, Vector3> Diff = [];
        }

        private readonly SliderSet _set;

        private readonly Dictionary<string, List<Entry>> _entries = [];

        private readonly Dictionary<string, List<string>> _names = [];

        private readonly List<string> _all = [];

        public SliderApplicator(SliderSet set)
        {
            _set = set;

            var osd = set.Osd;

            if (osd != null)
            {
                foreach (var slider in set.Definition.Sliders)
                {
                    foreach (var data in slider.Data)
                    {
                        if (string.IsNullOrEmpty(data.Target))
                            continue;

                        if (!osd.Data.TryGetValue(data.Name, out var diff))
                            continue;

                        if (!_entries.TryGetValue(data.Target, out var list))
                        {
                            list = [];

                            _entries[data.Target] = list;
                        }

                        list.Add(new Entry { Slider = slider.Name, Diff = diff });
                    }
                }
            }

            foreach (var shape in set.Definition.Shapes)
            {
                if (!_entries.ContainsKey(shape))
                    _entries[shape] = [];
            }

            foreach (var pair in _entries)
            {
                var names = new List<string>();

                foreach (var entry in pair.Value)
                {
                    if (names.Contains(entry.Slider))
                        continue;

                    names.Add(entry.Slider);

                    if (!_all.Contains(entry.Slider))
                        _all.Add(entry.Slider);
                }

                _names[pair.Key] = names;
            }
        }

        public SliderSet Set => _set;

        public IReadOnlyCollection<string> Targets => _entries.Keys;

        public IReadOnlyList<string> Sliders => _all;

        public bool Matches(string shapeName) => _entries.ContainsKey(shapeName);

        public IReadOnlyList<string> SlidersFor(string shapeName) =>
            _names.TryGetValue(shapeName, out var names) ? names : [];

        public Vector3 OffsetFor(string shapeName, string sliderName, ushort index)
        {
            if (!_entries.TryGetValue(shapeName, out var list))
                return Vector3.Zero;

            var offset = Vector3.Zero;

            foreach (var entry in list)
            {
                if (entry.Slider != sliderName)
                    continue;

                if (entry.Diff.TryGetValue(index, out var diff))
                    offset += diff;
            }

            return offset;
        }

        public int ApplyAll(IEnumerable<RenderShape> shapes, IReadOnlyDictionary<string, float> values)
        {
            var applied = 0;

            foreach (var shape in shapes)
            {
                if (!Matches(shape.ID))
                    continue;

                Apply(shape, values);

                applied++;
            }

            return applied;
        }

        public void Apply(RenderShape shape, IReadOnlyDictionary<string, float> values)
        {
            if (_set.Osd == null)
                return;

            if (shape.Shape is not BSTriShape block)
                return;

            if (!_entries.TryGetValue(shape.ID, out var list))
                return;

            var source = block.VertexPositions;

            if (source.Count == 0)
                return;

            var active = new List<Entry>();

            var weights = new List<float>();

            foreach (var entry in list)
            {
                if (!values.TryGetValue(entry.Slider, out var value) || value == 0f)
                    continue;

                active.Add(entry);

                weights.Add(value);
            }

            Matrix.Invert(ref shape.InverseTransform, out var world);

            var count = shape.VertexCount;

            if (count > source.Count)
                count = source.Count;

            if (count > ushort.MaxValue + 1)
                count = ushort.MaxValue + 1;

            var vertices = shape.Mesh.Vertices;

            var indices = new List<int>(count);

            for (var i = 0; i < count; i++)
            {
                var index = shape.StartIndex + i;

                if (index < 0 || index >= vertices.Length)
                    continue;

                if (shape.IsVertexDeleted(i))
                    continue;

                var position = source[i];

                var local = new Vector3(position.X, position.Y, position.Z);

                if (active.Count > 0)
                {
                    for (var k = 0; k < active.Count; k++)
                    {
                        if (active[k].Diff.TryGetValue((ushort)i, out var diff))
                            local += diff * weights[k];
                    }
                }

                vertices[index].Position = Vector3.Transform(local, world);

                indices.Add(i);
            }

            if (indices.Count > 0)
                shape.RefreshPositions(indices);
        }
    }
}
