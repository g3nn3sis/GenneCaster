// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Game
{
    internal static class HudPositions
    {
        internal static readonly HudPosition[] Corners = { HudPosition.BottomLeft, HudPosition.BottomRight, HudPosition.TopRight };
        internal static readonly HudPosition[] ScoreboardChoices = { HudPosition.TopCenter, HudPosition.BottomLeft, HudPosition.BottomRight, HudPosition.TopRight };

        internal static HudPosition[] ChoicesFor(HudSlot slot) => slot == HudSlot.Scoreboard ? ScoreboardChoices : Corners;

        internal static string Name(HudPosition position) => position switch
        {
            HudPosition.TopCenter => "Top Center",
            HudPosition.BottomLeft => "Bottom Left",
            HudPosition.BottomRight => "Bottom Right",
            HudPosition.TopRight => "Top Right",
            _ => position.ToString()
        };

        internal static bool IsTop(HudPosition position) => position == HudPosition.TopCenter || position == HudPosition.TopRight;

        internal static bool IsLeft(HudPosition position) => position == HudPosition.BottomLeft;

        internal static bool Contains(HudPosition[] choices, HudPosition position) => IndexOf(choices, position) >= 0;

        internal static int IndexOf(HudPosition[] choices, HudPosition position)
        {
            for (int i = 0; i < choices.Length; i++)
                if (choices[i] == position) return i;
            return -1;
        }
    }
}
