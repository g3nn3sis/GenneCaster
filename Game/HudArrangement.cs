// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.Game
{
    internal sealed class HudArrangement
    {
        private const int SlotCount = (int)HudSlot.Count;

        private readonly HudPosition[] positions = new HudPosition[SlotCount];
        private readonly bool[] occupying = new bool[SlotCount];
        private readonly bool[] wasEnabled = new bool[SlotCount];
        private readonly int[] order = new int[SlotCount];

        internal HudArrangement()
        {
            for (int i = 0; i < SlotCount; i++) order[i] = i;
        }

        internal HudPosition this[HudSlot slot] => positions[(int)slot];

        internal void Resolve(HudPosition[] wanted, bool[] enabled)
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (enabled[slot] && !wasEnabled[slot]) MoveToBack(slot);
                wasEnabled[slot] = enabled[slot];
                occupying[slot] = false;
            }
            foreach (int slot in order)
            {
                HudPosition[] choices = HudPositions.ChoicesFor((HudSlot)slot);
                HudPosition preferred = HudPositions.Contains(choices, wanted[slot]) ? wanted[slot] : choices[0];
                positions[slot] = enabled[slot] ? Claim(slot, preferred, choices) : preferred;
                occupying[slot] = enabled[slot];
            }
        }

        internal HudPosition Next(HudSlot slot)
        {
            int index = (int)slot;
            HudPosition[] choices = HudPositions.ChoicesFor(slot);
            int start = HudPositions.IndexOf(choices, positions[index]);
            for (int step = 1; step < choices.Length; step++)
            {
                HudPosition candidate = choices[(start + step) % choices.Length];
                if (!Taken(candidate, index)) return candidate;
            }
            return positions[index];
        }

        private HudPosition Claim(int slot, HudPosition preferred, HudPosition[] choices)
        {
            if (!Taken(preferred, slot)) return preferred;
            foreach (HudPosition choice in choices)
                if (!Taken(choice, slot)) return choice;
            int scoreboard = (int)HudSlot.Scoreboard;
            if (slot == scoreboard || !occupying[scoreboard] || positions[scoreboard] == HudPosition.TopCenter) return preferred;
            HudPosition freed = positions[scoreboard];
            positions[scoreboard] = HudPosition.TopCenter;
            return freed;
        }

        private bool Taken(HudPosition position, int except)
        {
            for (int slot = 0; slot < SlotCount; slot++)
                if (slot != except && occupying[slot] && positions[slot] == position) return true;
            return false;
        }

        private void MoveToBack(int slot)
        {
            int index = Array.IndexOf(order, slot);
            for (int i = index; i < SlotCount - 1; i++) order[i] = order[i + 1];
            order[SlotCount - 1] = slot;
        }
    }
}
