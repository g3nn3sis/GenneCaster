// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx;
using GorillaLocomotion;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GenneCaster.Base
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess(PluginInfo.GameProcess)]
    internal class BaseLoader : BaseUnityPlugin
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

    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    internal static class ObjectPatcher
    {
        internal static GameObject? LoaderObject;

        static void Postfix(GTPlayer __instance)
        {
            if (LoaderObject != null || !__instance) return;
            __instance.inOverlay = true;
            try
            {
                Type[] components = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ObjectComponent)) && !t.IsAbstract)
                    .ToArray();
                LoaderObject = new GameObject($"_{PluginInfo.Name}", components);
            }
            catch (Exception e) { Log.Exception(e, "Creating the GenneCaster components failed"); }
        }
    }
}
