// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Game;

namespace Zetori.UI.Panels
{
    internal static class PositionPicker
    {
        private static readonly Action[] Cycle = BuildActions();

        internal static void Draw(Panel panel, HudSlot slot) =>
            panel.Button($"Position: {HudPositions.Name(HudLayout.PositionOf(slot))}", Cycle[(int)slot]);

        private static Action[] BuildActions()
        {
            var actions = new Action[(int)HudSlot.Count];
            for (int i = 0; i < actions.Length; i++)
            {
                var slot = (HudSlot)i;
                actions[i] = () => HudLayout.Cycle(slot);
            }
            return actions;
        }
    }
}
