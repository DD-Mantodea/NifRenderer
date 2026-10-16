using System;
using System.Linq;

NifRenderer.Utils.LogUtils.Enabled = args.Contains("--debug") || args.Contains("-d");




NifRenderer.Shader.FxCompiler.Prepare("Content/Shaders/NifRender.fx", "Content/Shaders/NifRender.mgfx");

using var game = new NifRenderer.GameHost(args);

game.Run();
