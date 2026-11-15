// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx;

namespace GenneCaster.Base
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess(PluginInfo.GameProcess)]
    internal sealed class BaseLoader : BaseUnityPlugin
    {
        void Awake()
        {
            Log.Init(Logger);
            CasterConfig.Bind(ConfigStorage.Open(Config.ConfigFilePath, MetadataHelper.GetMetadata(this)));
            Log.Info($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
        }

        void Start()
        {
            try { HarmonyPatches.ApplyHarmonyPatches(); }
            catch (Exception e) { Log.Exception(e, "Applying Harmony patches failed"); }
        }

        void OnDestroy() => RemovePatches();

        void OnApplicationQuit()
        {
            CasterConfig.SaveOnExit();
            RemovePatches();
        }

        private static void RemovePatches()
        {
            try { HarmonyPatches.RemoveHarmonyPatches(); }
            catch (Exception e) { Log.Exception(e, "Removing Harmony patches failed"); }
        }
    }
}
