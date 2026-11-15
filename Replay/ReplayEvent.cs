// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace GenneCaster.Replay
{
    internal enum ReplayEventKind { Joined, Left, Tagged, TimeOfDay, Rain }

    internal readonly struct ReplayEvent
    {
        internal float Time { get; }
        internal ReplayEventKind Kind { get; }
        internal int ActorId { get; }
        internal int Value { get; }

        internal ReplayEvent(float time, ReplayEventKind kind, int actorId, int value = 0)
        {
            Time = time;
            Kind = kind;
            ActorId = actorId;
            Value = value;
        }
    }
}
