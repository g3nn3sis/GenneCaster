// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.Replay
{
    internal sealed class ReplayRecording
    {
        internal const float SampleInterval = 1f / 20f;

        internal List<ReplayActor> Actors { get; } = new List<ReplayActor>();
        internal List<ReplayEvent> Events { get; } = new List<ReplayEvent>();
        internal List<(float Time, EnvironmentState State)> Environment { get; } = new List<(float, EnvironmentState)>();
        internal float Duration { get; set; }

        internal ReplayActor? Actor(int id) => id >= 0 && id < Actors.Count ? Actors[id] : null;

        internal EnvironmentState? EnvironmentAt(float time)
        {
            EnvironmentState? state = null;
            foreach ((float at, EnvironmentState value) in Environment)
            {
                if (at > time) break;
                state = value;
            }
            return state;
        }

        internal string Describe(ReplayEvent e)
        {
            string name = Actor(e.ActorId)?.Name ?? "Player";
            return e.Kind switch
            {
                ReplayEventKind.Joined => $"{name} joined",
                ReplayEventKind.Left => $"{name} left",
                ReplayEventKind.Tagged => $"{name} TAGGED",
                ReplayEventKind.TimeOfDay => "Time of day changed",
                ReplayEventKind.Rain => e.Value != 0 ? "Rain started" : "Rain stopped",
                _ => string.Empty
            };
        }
    }
}
