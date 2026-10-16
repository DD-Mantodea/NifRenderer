using BCnEncoder.Decoder;
using BCnEncoder.Shared.ImageFiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Resolver;
using NiflySharp;
using NiflySharp.Blocks;
using System;
using System.Collections.Generic;
using System.IO;

namespace NifRenderer.Utils
{
    public static class Texture2DUtils
    {
        private static readonly BcDecoder Decoder = CreateDecoder();

        private static BcDecoder CreateDecoder()
        {
            var decoder = new BcDecoder
            {
                Options =
                {
                    IsParallel = true,
                    TaskCount = Math.Min(Environment.ProcessorCount, 4)
                }
            };

            return decoder;
        }

        public static Texture2D FromDdsPath(GraphicsDevice device, string path)
        {
            var pixels = DecodeDds(path, out var width, out var height);

            return Upload(device, width, height, pixels);
        }

        public static bool IsDds(string path) => Path.GetExtension(path).Equals(".dds", StringComparison.OrdinalIgnoreCase);

        public static byte[] DecodeDds(string path, out int width, out int height)
        {
            using var stream = File.OpenRead(path);

            var dds = DdsFile.Load(stream);

            width = (int)dds.header.dwWidth;

            height = (int)dds.header.dwHeight;

            return Decoder.IsSupportedFormat(dds)
                ? DecodeCompressed(dds, width, height)
                : ConvertUncompressed(dds, width, height);
        }

        public static Texture2D Upload(GraphicsDevice device, int width, int height, byte[] pixels)
        {
            var texture = new Texture2D(device, width, height);

            texture.SetData(pixels);

            return texture;
        }

        private static byte[] DecodeCompressed(DdsFile dds, int width, int height)
        {
            var decoded = Decoder.Decode(dds);

            var pixels = new byte[width * height * 4];

            for (var i = 0; i < decoded.Length; i++)
            {
                var color = decoded[i];

                var index = i * 4;

                pixels[index] = color.r;
                pixels[index + 1] = color.g;
                pixels[index + 2] = color.b;
                pixels[index + 3] = color.a;
            }

            return pixels;
        }

        private static byte[] ConvertUncompressed(DdsFile dds, int width, int height)
        {
            var data = dds.Faces[0].MipMaps[0].Data;

            var length = width * height * 4;

            if (data == null || data.Length < length)
                throw new InvalidDataException($"Unsupported DDS format ({data?.Length ?? 0} bytes for {width}x{height}).");

            var pixels = new byte[length];

            for (var i = 0; i < length; i += 4)
            {
                pixels[i] = data[i + 2];
                pixels[i + 1] = data[i + 1];
                pixels[i + 2] = data[i];
                pixels[i + 3] = data[i + 3];
            }

            return pixels;
        }

        public static Texture2D FromPngPath(GraphicsDevice device, string path)
        {
            using var stream = File.OpenRead(path);

            return Texture2D.FromStream(device, stream);
        }

        public static Texture2D FromPath(GraphicsDevice device, string path)
        {
            var extension = Path.GetExtension(path);

            if (extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
                return FromDdsPath(device, path);

            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                return FromPngPath(device, path);

            throw new NotSupportedException($"Unsupported texture format '{extension}' ('{path}').");
        }

        public static string GetTexturePath(NifFile file, INiShape shape)
        {
            var paths = GetTexturePaths(file, shape);

            foreach (var path in paths)
            {
                if (TexturePathResolver.LooksLikeFile(path))
                    return path;
            }

            return null;
        }

        public static List<string> GetTexturePaths(NifFile file, INiShape shape)
        {
            var paths = new List<string>();

            var textureSet = GetTextureSet(file, shape);

            if (textureSet?.Textures == null)
                return paths;

            foreach (var texture in textureSet.Textures)
            {
                var path = texture?.Content;

                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path.Trim());
            }

            return paths;
        }

        public static List<NiflySharp.Structs.TexCoord> GetUVs(INiShape shape)
        {
            if (shape is BSTriShape bs)
                return [.. bs.UVs];

            if (shape.GeometryData != null)
                return [.. shape.GeometryData.UVSets];

            return [];
        }

        private static BSShaderTextureSet GetTextureSet(NifFile file, INiShape shape)
        {
            var shader = file.GetShader(shape);

            if (shader == null || !shader.HasTextureSet)
                return null;

            return file.GetBlock(shader.TextureSetRef);
        }
    }
}
