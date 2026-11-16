// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Game;

namespace Zerori.UI.Panels
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
