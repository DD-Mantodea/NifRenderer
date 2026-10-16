using NifRenderer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NifRenderer.Resolver
{
    public static class TexturePathResolver
    {
        private static readonly string[] Extensions = [".dds", ".png"];

        private static bool _scanned;

        private static List<string> _modDirectories = [];

        public static List<string> Sources { get; } = [];

        private static readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Queue<string> _cacheOrder = new();

        private const int MaxCacheEntries = 4096;

        public static string Resolve(string nifPath, string texturePath)
        {
            var relative = Normalize(texturePath);

            if (relative == null)
                return null;

            if (_cache.TryGetValue(relative, out var cached))
                return cached;

            var resolved = Search(nifPath, relative);

            if (resolved != null)
            {
                _cache[relative] = resolved;

                _cacheOrder.Enqueue(relative);

                while (_cache.Count > MaxCacheEntries && _cacheOrder.Count > 0)
                    _cache.Remove(_cacheOrder.Dequeue());
            }

            return resolved;
        }

        private static string Search(string nifPath, string relative)
        {
            var candidates = Candidates(relative);

            foreach (var directory in Roots(nifPath))
            {
                foreach (var candidate in candidates)
                {
                    var full = Path.Combine(directory, candidate);

                    if (File.Exists(full))
                        return full;
                }
            }

            return null;
        }

        public static bool LooksLikeFile(string texturePath)
        {
            var normalized = Normalize(texturePath);

            return normalized != null && HasKnownExtension(normalized);
        }

        private static bool HasKnownExtension(string path)
        {
            var extension = Path.GetExtension(path);

            return extension.Equals(".dds", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            var cleaned = texturePath.Trim().Trim('"').Replace('/', '\\').TrimStart('\\');

            if (cleaned.Length == 0 || cleaned.StartsWith('.'))
                return null;

            return cleaned;
        }

        private static List<string> Candidates(string relative)
        {
            var candidates = new List<string>(5) { relative };

            if (HasKnownExtension(relative))
            {
                foreach (var other in Extensions)
                {
                    if (!Path.GetExtension(relative).Equals(other, StringComparison.OrdinalIgnoreCase))
                        candidates.Add(Path.ChangeExtension(relative, other));
                }
            }
            else
            {
                foreach (var known in Extensions)
                    candidates.Add(relative + known);
            }

            var baseCount = candidates.Count;

            for (var i = 0; i < baseCount; i++)
            {
                var candidate = candidates[i];

                if (!candidate.StartsWith("data\\", StringComparison.OrdinalIgnoreCase))
                    candidates.Add(Path.Combine("Data", candidate));

                if (!candidate.StartsWith("textures\\", StringComparison.OrdinalIgnoreCase))
                    candidates.Add(Path.Combine("textures", candidate));
            }

            return candidates;
        }

        private static IEnumerable<string> Roots(string nifPath)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in Sources)
            {
                if (!string.IsNullOrWhiteSpace(source) && seen.Add(source))
                    yield return source;
            }

            var directory = Path.GetDirectoryName(nifPath);

            if (!string.IsNullOrEmpty(directory))
            {
                var current = directory;

                for (var depth = 0; depth < 5 && !string.IsNullOrEmpty(current); depth++)
                {
                    if (seen.Add(current))
                        yield return current;

                    current = Path.GetDirectoryName(current);
                }
            }

            var fromEnvironment = Environment.GetEnvironmentVariable("NIFRENDERER_TEXTURE_ROOTS");

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                foreach (var part in fromEnvironment.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = part.Trim().Trim('"');

                    if (trimmed.Length > 0 && seen.Add(trimmed))
                        yield return trimmed;
                }
            }

            foreach (var mod in ModDirectories(nifPath))
            {
                if (seen.Add(mod))
                    yield return mod;
            }
        }

        private static List<string> ModDirectories(string nifPath)
        {
            Scan(nifPath);

            return _modDirectories;
        }

        private static void Scan(string nifPath)
        {
            if (_scanned)
                return;

            _scanned = true;

            var modsDirectory = FindModsDirectory(nifPath);

            if (modsDirectory == null)
                return;

            try
            {
                var installed = Directory.EnumerateDirectories(modsDirectory).ToList();

                var installDirectory = Path.GetDirectoryName(modsDirectory);

                var loadOrder = LoadOrder(installDirectory);

                _modDirectories = loadOrder is { Count: > 0 }
                    ? OrderByLoadOrder(installed, loadOrder)
                    : installed;

                if (!string.IsNullOrEmpty(installDirectory))
                {
                    var overwrite = Path.Combine(installDirectory, "overwrite");

                    if (Directory.Exists(overwrite))
                        _modDirectories.Add(overwrite);
                }
            }
            catch (Exception exception)
            {

                _modDirectories = [];
            }
        }

        private static List<string> OrderByLoadOrder(List<string> installed, List<string> loadOrder)
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var directory in installed)
                byName.TryAdd(Path.GetFileName(directory), directory);

            var enabledNames = new HashSet<string>(loadOrder, StringComparer.OrdinalIgnoreCase);

            var disabled = installed.Where(d => !enabledNames.Contains(Path.GetFileName(d))).ToList();

            var enabled = new List<string>(loadOrder.Count);

            foreach (var name in loadOrder)
            {
                if (byName.TryGetValue(name, out var directory))
                    enabled.Add(directory);
            }

            return [.. disabled, .. enabled];
        }

        private static List<string> LoadOrder(string installDirectory)
        {
            if (string.IsNullOrEmpty(installDirectory))
                return null;

            var name = SelectedProfile(installDirectory);

            if (name == null)
                return null;

            var modList = Path.Combine(installDirectory, "profiles", name, "modlist.txt");

            if (!File.Exists(modList))
                return null;

            var enabled = new List<string>();

            try
            {
                foreach (var line in File.ReadLines(modList, Encoding.UTF8))
                {
                    if (line.Length == 0 || line[0] != '+')
                        continue;

                    var mod = line[1..].Trim();

                    if (mod.Length > 0)
                        enabled.Add(mod);
                }
            }
            catch (Exception exception)
            {

                return null;
            }

            enabled.Reverse();

            return enabled;
        }

        private static string SelectedProfile(string installDirectory)
        {
            if (string.IsNullOrEmpty(installDirectory))
                return null;

            var settings = Path.Combine(installDirectory, "ModOrganizer.ini");

            if (!File.Exists(settings))
                return null;

            try
            {
                foreach (var line in File.ReadLines(settings, Encoding.UTF8))
                {
                    var trimmed = line.Trim();

                    if (!trimmed.StartsWith("selected_profile", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var separator = trimmed.IndexOf('=');

                    if (separator < 0)
                        continue;

                    var value = trimmed[(separator + 1)..].Trim();

                    if (value.StartsWith("@ByteArray(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
                        value = value["@ByteArray(".Length..^1];

                    return value.Trim();
                }
            }
            catch (Exception exception)
            {
            }

            return null;
        }

        private static string FindModsDirectory(string nifPath)
        {
            var starts = new List<string>(2);

            var directory = Path.GetDirectoryName(nifPath);

            if (!string.IsNullOrEmpty(directory))
                starts.Add(directory);

            if (!string.IsNullOrEmpty(Environment.CurrentDirectory))
                starts.Add(Environment.CurrentDirectory);

            foreach (var start in starts)
            {
                var current = start;

                for (var depth = 0; depth < 14 && !string.IsNullOrEmpty(current); depth++)
                {
                    var mods = Path.Combine(current, "mods");

                    if (File.Exists(Path.Combine(current, "ModOrganizer.ini")) && Directory.Exists(mods))
                        return mods;

                    current = Path.GetDirectoryName(current);
                }
            }

            return null;
        }
    }
}
