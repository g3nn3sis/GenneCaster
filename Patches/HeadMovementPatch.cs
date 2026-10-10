using HarmonyLib;

namespace GenneCaster.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    public static class HeadMovementPatch
    {
        private static void Postfix(VRRig __instance)
        {
            if (!__instance.isLocal)
                return;
            __instance.head.rigTarget.rotation = GorillaTagger.Instance.headCollider.transform.rotation;
        }
    }
}
