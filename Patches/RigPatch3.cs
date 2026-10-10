using HarmonyLib;
using System;
namespace GenneCaster.Patches
{
    [HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
    public static class RigPatch3
    {
        public static bool Prefix(VRRigJobManager __instance, VRRig rig)
        {
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

            return !rig.isLocal;
        }
    }
}
