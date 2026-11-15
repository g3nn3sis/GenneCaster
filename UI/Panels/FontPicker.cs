// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Game;
using UnityEngine;

namespace GenneCaster.UI.Panels
{
    internal static class FontPicker
    {
        internal static int Draw(Panel panel, int fontIndex)
        {
            int result = fontIndex;
            panel.Label($"Font: {AssetManager.FontName(fontIndex)}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous Font", () => result = Shift(fontIndex, -1));
                panel.Button("Next Font", () => result = Shift(fontIndex, 1));
            });
            return result;
        }

        private static int Shift(int fontIndex, int amount)
        {
            int count = AssetManager.discoveredFonts.Length;
            if (count == 0) return fontIndex;
            int current = Mathf.Clamp(fontIndex, 0, count - 1);
            return (current + count + amount) % count;
        }
    }
}
