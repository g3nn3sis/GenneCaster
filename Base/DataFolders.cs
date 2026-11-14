// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx;
using System.IO;

namespace GenneCaster.Base
{
    internal static class DataFolders
    {
        internal const string DefaultConfigName = "Default";
        internal const string ConfigExtension = ".cfg";

        internal static string Root => Path.Combine(Paths.GameRootPath, PluginInfo.Name);
        internal static string Configs => Path.Combine(Root, "Configs");
        internal static string Replays => Path.Combine(Root, "Replays");
        internal static string DefaultConfig => Path.Combine(Configs, DefaultConfigName + ConfigExtension);
    }
}
