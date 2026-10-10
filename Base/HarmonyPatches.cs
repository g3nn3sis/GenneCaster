using System.Reflection;
using HarmonyLib;

namespace GenneCaster.Base
{
    internal class HarmonyPatches
    {
        private static Harmony ?instance;
        public const string InstanceId = PluginInfo.GUID;

        private static bool isPatched;
        public static bool IsPatched
        {
            get { return isPatched; }
            private set { isPatched = value; }
        }

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                instance ??= new Harmony(InstanceId);
                instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
            }
        }
        internal static void RemoveHarmonyPatches()
        {
            if (instance is not null && IsPatched)
            {
                instance.UnpatchSelf();
                IsPatched = false;
            }
        }
    }
}
