using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Renderer;
using NifRenderer.Utils;
using NiflySharp;
using NiflySharp.Blocks;
using NiflySharp.Structs;

namespace NifRenderer.Files
{
    public static class NifExporter
    {
        private static readonly List<string> Warnings = [];

        public static int CountDeletedVertices(RenderObject item)
        {
            var total = 0;

            foreach (var shape in item.Mesh.RenderShapes)
                total += shape.DeletedVertices.Count;

            return total;
        }

        public static string DefaultPath(RenderObject item)
        {
            if (string.IsNullOrEmpty(item.Path))
                return "";

            var directory = Path.GetDirectoryName(item.Path) ?? AppContext.BaseDirectory;

            return Path.Combine(directory, Path.GetFileNameWithoutExtension(item.Path) + ".nif");
        }

        public static string Export(RenderObject item, string target)
        {
            if (string.IsNullOrEmpty(item.Path))
                return "源文件路径未知，无法导出";

            if (string.IsNullOrEmpty(target))
                return "未指定导出路径";

            Warnings.Clear();

            NifFile file;

            try
            {
                file = new NifFile();

                file.Load(item.Path);
            }
            catch (Exception exception)
            {

                return $"重新读取源文件失败: {exception.Message}";
            }

            var byName = new Dictionary<string, INiShape>();

            foreach (var shape in file.GetShapes())
                byName.TryAdd(shape.Name.String, shape);

            foreach (var renderShape in item.Mesh.RenderShapes)
            {
                if (!byName.TryGetValue(renderShape.ID, out var shape))
                {
                    Warnings.Add($"{renderShape.ID}: 源文件里没有同名 shape");

                    continue;
                }

                Apply(renderShape, shape, file);
            }

            var options = new NifFileSaveOptions
            {
                RemoveUnreferencedBlocks = false,

                SortBlocks = false,

                UpdateBounds = true
            };


            file.Save(target, options);


            if (Warnings.Count > 0)
                return $"已导出 {target}（{Warnings.Count} 处警告: {string.Join("; ", Warnings)}）";

            return $"已导出 {target}";
        }

        private static void Apply(RenderShape renderShape, INiShape target, NifFile file)
        {
            if (target is not BSTriShape shape)
                return;

            var count = renderShape.VertexCount;

            if (count == 0)
                return;

            var kept = new List<int>(count);

            var remap = new int[count];

            for (var i = 0; i < count; i++)
            {
                if (renderShape.IsVertexDeleted(i))
                {
                    remap[i] = -1;

                    continue;
                }

                remap[i] = kept.Count;

                kept.Add(i);
            }

            var source = renderShape.Mesh.Vertices;

            var changed = kept.Count != count;

            if (!WriteVertexDataSSE(shape, renderShape, kept, count, source))
                WriteVertexData(shape, renderShape, kept, count, source);

            if (changed)
                RemapTriangles(shape, remap, count, file);

            shape.UpdateBounds();
        }

        private static bool WriteVertexDataSSE(BSTriShape shape, RenderShape renderShape, List<int> kept, int count, VertexPositionColorNormalTexture[] source)
        {
            var data = shape.VertexDataSSE;

            if (data == null || data.Count != count)
                return false;

            var list = new List<BSVertexDataSSE>(kept.Count);

            foreach (var i in kept)
            {
                var item = data[i];

                var local = Vector3.Transform(source[renderShape.StartIndex + i].Position, renderShape.InverseTransform);

                item.Vertex = new System.Numerics.Vector3(local.X, local.Y, local.Z);

                list.Add(item);
            }

            shape.SetVertexDataSSE(list);

            return true;
        }

        private static bool WriteVertexData(BSTriShape shape, RenderShape renderShape, List<int> kept, int count, VertexPositionColorNormalTexture[] source)
        {
            var data = shape.VertexData;

            if (data == null || data.Count != count)
                return false;

            var list = new List<BSVertexData>(kept.Count);

            foreach (var i in kept)
            {
                var item = data[i];

                var local = Vector3.Transform(source[renderShape.StartIndex + i].Position, renderShape.InverseTransform);

                item.Vertex = new System.Numerics.Vector3(local.X, local.Y, local.Z);

                list.Add(item);
            }

            shape.SetVertexData(list);

            return true;
        }

        private static void RemapTriangles(BSTriShape shape, int[] remap, int count, NifFile file)
        {
            var triangles = new List<Triangle>(shape.Triangles.Count);

            foreach (var tri in shape.Triangles)
            {
                if (tri.V1 >= count || tri.V2 >= count || tri.V3 >= count)
                    continue;

                var a = remap[tri.V1];

                var b = remap[tri.V2];

                var c = remap[tri.V3];

                if (a < 0 || b < 0 || c < 0)
                    continue;

                triangles.Add(new Triangle((ushort)a, (ushort)b, (ushort)c));
            }

            shape.SetTriangles(file.Header.Version, triangles);

            PreparePartitionTriangles(shape, file);

            try
            {
                file.UpdateSkinPartitions(shape);
            }
            catch (Exception e)
            {
                Warnings.Add($"UpdateSkinPartitions({shape.Name.String}): {e.GetType().Name} {e.Message}");
            }
        }

        private static void PreparePartitionTriangles(INiShape shape, NifFile file)
        {
            var skinInstance = file.GetBlock<NiSkinInstance>(shape.SkinInstanceRef);

            if (skinInstance == null)
                return;

            var skinPartition = file.GetBlock<NiSkinPartition>(skinInstance.SkinPartition);

            if (skinPartition?.Partitions == null)
                return;

            var partitions = skinPartition.Partitions;

            for (var i = 0; i < partitions.Count; i++)
            {
                var partition = partitions[i];

                if (partition.NumStrips > 0)
                    partition.ConvertStripsToTriangles();

                if ((partition.TrianglesCopy?.Count ?? 0) == 0)
                    partition.GenerateTrueTrianglesFromMappedTriangles();

                partitions[i] = partition;
            }
        }
    }
}
