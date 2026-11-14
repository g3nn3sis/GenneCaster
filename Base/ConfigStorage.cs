// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace GenneCaster.Base
{
    internal static class ConfigStorage
    {
        private const string LegacyPresetFolder = "GenneCaster-presets";

        internal static ConfigFile Open(string legacyConfigPath, BepInPlugin owner)
        {
            try
            {
                Directory.CreateDirectory(DataFolders.Configs);
                MigrateLegacy(legacyConfigPath);
            }
            catch (Exception e) { Log.Exception(e, "Moving the old config into the GenneCaster folder failed"); }
            return new ConfigFile(DataFolders.DefaultConfig, true, owner);
        }

        private static void MigrateLegacy(string legacyConfigPath)
        {
            if (!File.Exists(DataFolders.DefaultConfig) && File.Exists(legacyConfigPath))
            {
                File.Move(legacyConfigPath, DataFolders.DefaultConfig);
                Log.Info($"Moved the config to {DataFolders.DefaultConfig}.");
            }
            string? legacyFolder = Path.GetDirectoryName(legacyConfigPath);
            string presets = legacyFolder != null ? Path.Combine(legacyFolder, LegacyPresetFolder) : string.Empty;
            if (presets.Length == 0 || !Directory.Exists(presets)) return;
            foreach (string preset in Directory.GetFiles(presets, "*" + DataFolders.ConfigExtension))
            {
                string destination = Path.Combine(DataFolders.Configs, Path.GetFileName(preset));
                if (Presets.IsReserved(Path.GetFileNameWithoutExtension(preset)) || File.Exists(destination)) continue;
                File.Move(preset, destination);
            }
            if (!Directory.EnumerateFileSystemEntries(presets).Any()) Directory.Delete(presets);
            Log.Info($"Moved presets to {DataFolders.Configs}.");
        }
    }
}
