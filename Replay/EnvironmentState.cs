// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace GenneCaster.Replay
{
    internal readonly struct EnvironmentState : IEquatable<EnvironmentState>
    {
        internal int TimeIndex { get; }
        internal bool Raining { get; }

        internal EnvironmentState(int timeIndex, bool raining)
        {
            TimeIndex = timeIndex;
            Raining = raining;
        }

        public bool Equals(EnvironmentState other) => TimeIndex == other.TimeIndex && Raining == other.Raining;
        public override bool Equals(object? obj) => obj is EnvironmentState other && Equals(other);
        public override int GetHashCode() => TimeIndex * 2 + (Raining ? 1 : 0);
    }
}
