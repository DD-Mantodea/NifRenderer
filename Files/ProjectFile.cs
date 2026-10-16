using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using NifRenderer.Renderer;
using NifRenderer.Utils;
using NiflySharp.Blocks;
using RendererCore = NifRenderer.Renderer.Renderer;

namespace NifRenderer.Files
{
    public static class ProjectFile
    {
        public const string FormatId = "NifRendererProject";

        public const int CurrentVersion = 1;

        public sealed class ProjectObject
        {
            public string Path { get; set; } = "";

            public long Size { get; set; }

            public long Modified { get; set; }

            public List<ProjectShape> Shapes { get; set; } = [];
        }

        public sealed class ProjectShape
        {
            public string Name { get; set; } = "";

            public bool Visible { get; set; } = true;

            public bool Deleted { get; set; }

            public List<int> DeletedVertices { get; set; } = [];

            public List<int> MovedIndices { get; set; } = [];

            public List<float> MovedPositions { get; set; } = [];
        }

        public sealed class Project
        {
            public string Format { get; set; } = FormatId;

            public int Version { get; set; } = CurrentVersion;

            public List<ProjectObject> Objects { get; set; } = [];
        }

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,

            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        public static Project Capture(RendererCore renderer)
        {
            var project = new Project();

            foreach (var item in renderer.Objects)
            {
                if (string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path))
                    continue;

                var info = new FileInfo(item.Path);

                var target = new ProjectObject
                {
                    Path = item.Path,

                    Size = info.Length,

                    Modified = info.LastWriteTimeUtc.Ticks
                };

                foreach (var shape in item.Mesh.RenderShapes)
                    target.Shapes.Add(CaptureShape(shape));

                project.Objects.Add(target);
            }

            return project;
        }

        private static ProjectShape CaptureShape(RenderShape renderShape)
        {
            var target = new ProjectShape
            {
                Name = renderShape.ID,

                Visible = renderShape.Visible
            };

            foreach (var index in renderShape.DeletedVertices)
                target.DeletedVertices.Add(index);

            target.DeletedVertices.Sort();

            if (renderShape.Shape is not BSTriShape shape)
                return target;

            var original = shape.VertexPositions;

            if (original.Count != renderShape.VertexCount)
                return target;

            var mesh = renderShape.Mesh.Vertices;

            var forward = Matrix.Invert(renderShape.InverseTransform);

            for (var i = 0; i < renderShape.VertexCount; i++)
            {
                var local = original[i];

                var world = mesh[renderShape.StartIndex + i].Position;

                var back = Vector3.Transform(new Vector3(local.X, local.Y, local.Z), forward);

                if ((back - world).LengthSquared() < 1e-10f)
                    continue;

                target.MovedIndices.Add(i);

                target.MovedPositions.Add(world.X);

                target.MovedPositions.Add(world.Y);

                target.MovedPositions.Add(world.Z);
            }

            return target;
        }

        public static string Save(RendererCore renderer, string path)
        {
            var project = Capture(renderer);

            var json = JsonSerializer.Serialize(project, Options);

            File.WriteAllText(path, json);

            return $"已保存项目 {path}（{project.Objects.Count} 个模型）";
        }

        public static Project? Read(string path)
        {
            if (!File.Exists(path))
                return null;

            var project = JsonSerializer.Deserialize<Project>(File.ReadAllText(path), Options);

            if (project == null || project.Format != FormatId)
                return null;

            return project;
        }

        public static string DescribeDrift(Project project)
        {
            var messages = new List<string>();

            foreach (var item in project.Objects)
            {
                if (!File.Exists(item.Path))
                {
                    messages.Add($"源文件不存在: {item.Path}");

                    continue;
                }

                var info = new FileInfo(item.Path);

                if (info.Length != item.Size)
                    messages.Add($"源文件已改变: {Path.GetFileName(item.Path)}");
            }

            return string.Join("; ", messages);
        }
    }
}
