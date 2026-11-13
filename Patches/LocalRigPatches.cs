// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;

namespace GenneCaster.Patches
{
    [HarmonyPatch(typeof(VRRig), "OnDisable")]
    internal static class LocalRigDisablePatch
    {
        static bool Prefix(VRRig __instance) => !__instance.isLocal;
    }

    [HarmonyPatch(typeof(VRRig), "Awake")]
    internal static class LocalRigAwakePatch
    {
        private const string LocalRigName = "Local Gorilla Player(Clone)";

        static bool Prefix(VRRig __instance) => __instance.gameObject.name != LocalRigName;
    }

    [HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
    internal static class LocalRigDeregisterPatch
    {
        static bool Prefix(VRRig rig) => rig == null || !rig.isLocal;
    }

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    internal static class HeadMovementPatch
    {
        static void Postfix(VRRig __instance)
        {
            if (!__instance.isLocal) return;
            GorillaTagger tagger = GorillaTagger.Instance;
            if (tagger == null || tagger.headCollider == null) return;
            __instance.head.rigTarget.rotation = tagger.headCollider.transform.rotation;
        }
    }
}
