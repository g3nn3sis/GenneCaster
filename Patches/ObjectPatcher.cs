// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using GorillaLocomotion;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Zetori.Patches
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
                Log.Exception(e, "Creating the Zetori components failed");
            }
        }
    }
}
