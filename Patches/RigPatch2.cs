using HarmonyLib;
namespace GenneCaster.Patches
{
    [HarmonyPatch(typeof(VRRig), "Awake")]
    public class RigPatch2
    {
        public static bool Prefix(VRRig __instance) => __instance.gameObject.name != "Local Gorilla Player(Clone)";
    }
}
