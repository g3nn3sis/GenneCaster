// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zerori.Replay
{
    internal sealed class ReplayActor
    {
        private const float MaxFrameGapFactor = 3f;

        internal int Id { get; }
        internal string Name { get; }
        internal Color Color { get; }
        internal RigSkeleton Skeleton { get; }
        internal List<ActorFrame> Frames { get; } = new List<ActorFrame>();
        internal VoiceTrack Voice { get; } = new VoiceTrack();
        internal bool IsLocal { get; }
        internal int Platform { get; set; }

        internal ReplayActor(int id, string name, Color color, RigSkeleton skeleton, bool isLocal)
        {
            Id = id;
            Name = name;
            Color = color;
            Skeleton = skeleton;
            IsLocal = isLocal;
        }

        internal bool TryFindSpan(float time, float sampleInterval, out int index, out float blend)
        {
            index = 0;
            blend = 0f;
            if (Frames.Count == 0 || time < Frames[0].Time || time > Frames[Frames.Count - 1].Time) return false;
            int low = 0, high = Frames.Count - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (Frames[mid].Time <= time) low = mid;
                else high = mid - 1;
            }
            index = low;
            if (index == Frames.Count - 1) return true;
            float gap = Frames[index + 1].Time - Frames[index].Time;
            if (gap > sampleInterval * MaxFrameGapFactor) return time - Frames[index].Time <= sampleInterval;
            blend = gap > 0f ? (time - Frames[index].Time) / gap : 0f;
            return true;
        }
    }
}
