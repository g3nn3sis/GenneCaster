// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Reflection;
using HarmonyLib;

namespace Zetori.Base
{
    internal static class HarmonyPatches
    {
        private static Harmony? instance;

        internal static bool IsPatched { get; private set; }

        internal static void ApplyHarmonyPatches()
        {
            if (IsPatched) return;
            instance ??= new Harmony(PluginInfo.GUID);
            instance.PatchAll(Assembly.GetExecutingAssembly());
            IsPatched = true;
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance is null || !IsPatched) return;
            instance.UnpatchSelf();
            IsPatched = false;
        }
    }
}
