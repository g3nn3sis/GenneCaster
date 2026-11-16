// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Replay;

namespace Zerori.Player
{
    internal static class RigTagState
    {
        internal static ReplayTagState Of(VRRig rig)
        {
            if (rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying) return ReplayTagState.Lava;
            if (rig.rockParticleSystem != null && rig.rockParticleSystem.isPlaying) return ReplayTagState.Rock;
            return ReplayTagState.None;
        }

        internal static bool IsTagged(VRRig rig) => Of(rig) != ReplayTagState.None;

        internal static bool IsLava(VRRig rig) => Of(rig) == ReplayTagState.Lava;
    }
}
