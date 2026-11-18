// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx;
using System.IO;

namespace Zetori.Base
{
    internal static class DataFolders
    {
        internal const string DefaultConfigName = "Default";
        internal const string ConfigExtension = ".cfg";

        internal static string Root => Path.Combine(Paths.GameRootPath, PluginInfo.Name);
        internal static string Configs => Path.Combine(Root, "Configs");
        internal static string Replays => Path.Combine(Root, "Replays");
        internal static string Webs => Path.Combine(Root, "Webs");
        internal static string DefaultConfig => Path.Combine(Configs, DefaultConfigName + ConfigExtension);
        internal static string PresetDefaults => Path.Combine(Root, "PresetDefaults.txt");
    }
}
