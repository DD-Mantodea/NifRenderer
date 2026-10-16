using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace NifRenderer.BodySlide
{
    public sealed class OsdFile
    {
        private const uint Header = 0x4F534400;

        public uint Version = 1;

        public readonly Dictionary<string, Dictionary<ushort, Vector3>> Data = [];

        public static OsdFile Read(string path)
        {
            using var stream = File.OpenRead(path);

            using var reader = new BinaryReader(stream);

            var file = new OsdFile();

            if (reader.ReadUInt32() != Header)
                throw new InvalidDataException($"not an osd file: {path}");

            file.Version = reader.ReadUInt32();

            var count = reader.ReadUInt32();

            for (var i = 0; i < count; i++)
            {
                var nameLength = reader.ReadByte();

                var name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

                var diffSize = reader.ReadUInt16();

                var diffs = new Dictionary<ushort, Vector3>(diffSize);

                for (var d = 0; d < diffSize; d++)
                {
                    var index = reader.ReadUInt16();

                    var x = reader.ReadSingle();

                    var y = reader.ReadSingle();

                    var z = reader.ReadSingle();

                    diffs[index] = new Vector3(x, y, z);
                }

                file.Data[name] = diffs;
            }

            return file;
        }

        public void Write(string path)
        {
            using var stream = File.Create(path);

            using var writer = new BinaryWriter(stream);

            writer.Write(Header);

            writer.Write(Version);

            writer.Write((uint)Data.Count);

            foreach (var (name, diffs) in Data)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(name);

                writer.Write((byte)bytes.Length);

                writer.Write(bytes);

                writer.Write((ushort)diffs.Count);

                foreach (var (index, diff) in diffs)
                {
                    writer.Write(index);

                    writer.Write(diff.X);

                    writer.Write(diff.Y);

                    writer.Write(diff.Z);
                }
            }
        }

        public int VertexCount(string slider)
        {
            if (!Data.TryGetValue(slider, out var diffs))
                return 0;

            var highest = -1;

            foreach (var index in diffs.Keys)
                if (index > highest)
                    highest = index;

            return highest + 1;
        }

        public Vector3 Offset(string slider, ushort index)
        {
            if (!Data.TryGetValue(slider, out var diffs))
                return Vector3.Zero;

            return diffs.TryGetValue(index, out var diff) ? diff : Vector3.Zero;
        }
    }
}
