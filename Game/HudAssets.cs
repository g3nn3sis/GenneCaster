// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using TMPro;
using UnityEngine;

namespace Zerori.Game
{
    internal static class HudAssets
    {
        private const string BundleResource = "Zerori.Bundles.zerori.bundle";

        private static AssetBundle? bundle;
        private static bool attempted;

        internal static GameObject? LeaderboardPrefab { get; private set; }
        internal static GameObject? ClosestLavaPrefab { get; private set; }
        internal static GameObject? ScoreboardPrefab { get; private set; }

        internal static bool Ready
        {
            get
            {
                if (!attempted) Load();
                return LeaderboardPrefab != null && ClosestLavaPrefab != null && ScoreboardPrefab != null;
            }
        }

        private static void Load()
        {
            attempted = true;
            try
            {
                byte[]? bytes = EmbeddedResources.Read(BundleResource);
                if (bytes == null)
                {
                    Log.Error($"Missing embedded asset bundle {BundleResource}.");
                    return;
                }
                bundle = AssetBundle.LoadFromMemory(bytes);
                if (bundle == null)
                {
                    Log.Error("Failed to load the Zerori HUD asset bundle from memory.");
                    return;
                }
                LeaderboardPrefab = bundle.LoadAsset<GameObject>("GenneCasterLeaderboard");
                ClosestLavaPrefab = bundle.LoadAsset<GameObject>("GenneCasterClosestLava");
                ScoreboardPrefab = bundle.LoadAsset<GameObject>("GenneCasterScoreboard");
                if (LeaderboardPrefab == null || ClosestLavaPrefab == null || ScoreboardPrefab == null)
                    Log.Error("The HUD asset bundle is missing one or more canvas prefabs.");
                else
                    Log.Info("Loaded the Zerori HUD asset bundle.");
            }
            catch (Exception e) { Log.Exception(e, "Loading the HUD asset bundle failed"); }
        }

        internal static TMP_FontAsset? CurrentFont
        {
            get
            {
                TMP_FontAsset[] fonts = AssetManager.discoveredFonts;
                if (fonts.Length == 0) return null;
                return fonts[Mathf.Clamp(CasterConfig.HudFont.Value, 0, fonts.Length - 1)];
            }
        }

        internal static void ApplyFont(GameObject root)
        {
            TMP_FontAsset? font = CurrentFont;
            if (font == null) return;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                text.font = font;
        }
    }
}
