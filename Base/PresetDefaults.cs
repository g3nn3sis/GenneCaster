// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Base
{
    internal static class PresetDefaults
    {
        private const char Separator = '=';

        private static readonly Dictionary<PresetPlatform, string> Selected = new Dictionary<PresetPlatform, string>();
        private static bool loaded;

        internal static string? Get(PresetPlatform platform)
        {
            EnsureLoaded();
            return Selected.TryGetValue(platform, out string? name) ? name : null;
        }

        internal static void Set(PresetPlatform platform, string? name)
        {
            EnsureLoaded();
            if (name == null) Selected.Remove(platform);
            else Selected[platform] = name;
            Save();
        }

        internal static void Renamed(string name, string newName) => Replace(name, newName);

        internal static void Removed(string name) => Replace(name, null);

        private static void Replace(string name, string? replacement)
        {
            EnsureLoaded();
            bool changed = false;
            foreach (PresetPlatform platform in Selected.Keys.ToArray())
            {
                if (!string.Equals(Selected[platform], name, StringComparison.OrdinalIgnoreCase)) continue;
                if (replacement == null) Selected.Remove(platform);
                else Selected[platform] = replacement;
                changed = true;
            }
            if (changed) Save();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(DataFolders.PresetDefaults)) return;
                foreach (string line in File.ReadAllLines(DataFolders.PresetDefaults)) ReadLine(line);
            }
            catch (Exception e) { Log.Exception(e, "Reading the default presets failed"); }
        }

        private static void ReadLine(string line)
        {
            int separator = line.IndexOf(Separator);
            if (separator <= 0) return;
            string name = line.Substring(separator + 1).Trim();
            if (name.Length > 0 && Enum.TryParse(line.Substring(0, separator).Trim(), out PresetPlatform platform))
                Selected[platform] = name;
        }

        private static void Save()
        {
            try { File.WriteAllLines(DataFolders.PresetDefaults, Selected.Select(pair => $"{pair.Key}{Separator}{pair.Value}")); }
            catch (Exception e) { Log.Exception(e, "Saving the default presets failed"); }
        }
    }
}
