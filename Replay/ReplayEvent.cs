// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.Replay
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
