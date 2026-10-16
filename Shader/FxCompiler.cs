using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace NifRenderer.Shader
{
    public class FxCompiler
    {
        public static EffectProcessor Processor = new();

        public static byte[] CompileToBytes(string file)
        {
            var identity = new ContentIdentity(file);

            var effectContent = new EffectContent()
            {
                Identity = identity
            };

            var compiled = Processor.Process(effectContent, new FxCompilerContext());

            return compiled.GetEffectCode();
        }

        public static Effect Compile(string file, GraphicsDevice device)
        {
            var full = Path.IsPathRooted(file) ? file : Path.Combine(AppContext.BaseDirectory, file);

            return new Effect(device, CompileToBytes(full));
        }

        public static bool Prepare(string file, string cache)
        {
            var source = Path.IsPathRooted(file) ? file : Path.Combine(AppContext.BaseDirectory, file);

            var target = Path.IsPathRooted(cache) ? cache : Path.Combine(AppContext.BaseDirectory, cache);

            if (!File.Exists(source))
                return false;

            if (File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(source))
                return true;

            try
            {
                var bytes = CompileToBytes(source);

                var folder = Path.GetDirectoryName(target);

                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllBytes(target, bytes);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Effect Load(string file, string cache, GraphicsDevice device)
        {
            var source = Path.IsPathRooted(file) ? file : Path.Combine(AppContext.BaseDirectory, file);

            var target = Path.IsPathRooted(cache) ? cache : Path.Combine(AppContext.BaseDirectory, cache);

            if (File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(source))
                return new Effect(device, File.ReadAllBytes(target));

            return new Effect(device, CompileToBytes(source));
        }
    }

    public class FxCompilerLogger : ContentBuildLogger
    {
        public override void LogMessage(string message, params object[] messageArgs)
        {
        }

        public override void LogImportantMessage(string message, params object[] messageArgs)
        {
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
        }
    }

    public class FxCompilerContext : ContentProcessorContext
    {
        private readonly FxCompilerLogger _logger = new();

        public override string BuildConfiguration => throw new NotImplementedException();

        public override string IntermediateDirectory => throw new NotImplementedException();

        public override ContentBuildLogger Logger => _logger;

        public override ContentIdentity SourceIdentity => throw new NotImplementedException();

        public override string OutputDirectory => throw new NotImplementedException();

        public override string OutputFilename => "runtime.mgfxo";

        public override OpaqueDataDictionary Parameters => throw new NotImplementedException();

        public override string ProjectDirectory => throw new NotImplementedException();

        public override TargetPlatform TargetPlatform => TargetPlatform.DesktopGL;

        public override GraphicsProfile TargetProfile => GraphicsProfile.Reach;

        public override void AddDependency(string filename) => throw new NotImplementedException();

        public override void AddOutputFile(string filename) => throw new NotImplementedException();

        public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary? processorParameters, string? importerName) => throw new NotImplementedException();

        public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, IContentImporter importer, IContentProcessor processor) => throw new NotImplementedException();

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary? processorParameters, string? importerName, string? assetName) => throw new NotImplementedException();

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, IContentImporter importer, IContentProcessor processor, string? assetName = null) => throw new NotImplementedException();

        public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters) => throw new NotImplementedException();

        public override TOutput Convert<TInput, TOutput>(TInput input, IContentProcessor processor) => throw new NotImplementedException();
    }
}
