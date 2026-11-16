// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zetori.Base;
using UnityEngine;

namespace Zetori.Game
{
    internal static class HudLayout
    {
        private const int SlotCount = (int)HudSlot.Count;

        private static readonly HudArrangement arrangement = new HudArrangement();
        private static readonly HudPosition[] wanted = new HudPosition[SlotCount];
        private static readonly bool[] enabled = new bool[SlotCount];
        private static int resolvedFrame = -1;

        internal static HudPosition PositionOf(HudSlot slot)
        {
            Resolve();
            return arrangement[slot];
        }

        internal static void Cycle(HudSlot slot)
        {
            Resolve();
            PositionEntry(slot).Value = arrangement.Next(slot);
            resolvedFrame = -1;
        }

        private static void Resolve()
        {
            if (resolvedFrame == Time.frameCount) return;
            resolvedFrame = Time.frameCount;
            for (int i = 0; i < SlotCount; i++)
            {
                wanted[i] = PositionEntry((HudSlot)i).Value;
                enabled[i] = EnabledEntry((HudSlot)i).Value;
            }
            arrangement.Resolve(wanted, enabled);
            for (int i = 0; i < SlotCount; i++)
            {
                HudPosition settled = arrangement[(HudSlot)i];
                if (enabled[i] && settled != wanted[i]) PositionEntry((HudSlot)i).Value = settled;
            }
        }

        private static ConfigEntry<HudPosition> PositionEntry(HudSlot slot) => slot switch
        {
            HudSlot.Scoreboard => CasterConfig.ScoreboardPosition,
            HudSlot.MiniMap => CasterConfig.MiniMapPosition,
            HudSlot.KillFeed => CasterConfig.KillFeedPosition,
            _ => CasterConfig.LeaderboardPosition
        };

        private static ConfigEntry<bool> EnabledEntry(HudSlot slot) => slot switch
        {
            HudSlot.Scoreboard => CasterConfig.ScoreboardEnabled,
            HudSlot.MiniMap => CasterConfig.MiniMapEnabled,
            HudSlot.KillFeed => CasterConfig.KillFeedEnabled,
            _ => CasterConfig.LeaderboardEnabled
        };
    }
}
