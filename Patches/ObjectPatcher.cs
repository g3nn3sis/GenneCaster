// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GorillaLocomotion;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GenneCaster.Patches
{
    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    internal static class ObjectPatcher
    {
        internal static GameObject? LoaderObject;

        private static bool failed;

        static void Postfix(GTPlayer __instance)
        {
            if (failed || LoaderObject != null || !__instance) return;
            __instance.inOverlay = true;
            try
            {
                Type[] components = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ObjectComponent)) && !t.IsAbstract)
                    .ToArray();
                LoaderObject = new GameObject($"_{PluginInfo.Name}", components);
            }
            catch (Exception e)
            {
                failed = true;
                Log.Exception(e, "Creating the GenneCaster components failed");
            }
        }
    }
}
