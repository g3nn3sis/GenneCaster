// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Game;
using GenneCaster.Player;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class ReplayPlayer : IDisposable
    {
        internal const float MinRigLerp = 0.01f;
        internal const float MaxRigLerp = 1f;

        private readonly List<ReplayPuppet> puppets;
        private readonly LiveWorldMask mask = new LiveWorldMask();
        private readonly TimeManager.Snapshot? liveEnvironment;
        private EnvironmentState? appliedEnvironment;
        private int spectated;
        private float posedCursor;

        internal ReplayRecording Recording { get; }
        internal float Cursor { get; private set; }
        internal bool Playing { get; set; } = true;
        internal bool Freecam { get; set; }
        internal IReadOnlyList<ReplayPuppet> Puppets => puppets;
        private float rigLerp;

        internal float RigLerp
        {
            get => rigLerp;
            set => rigLerp = Mathf.Clamp(value, MinRigLerp, MaxRigLerp);
        }

        internal ReplayPuppet? Spectated
        {
            get
            {
                ReplayPuppet? puppet = spectated >= 0 && spectated < puppets.Count ? puppets[spectated] : null;
                return puppet != null && puppet.Visible ? puppet : null;
            }
        }

        internal ReplayPlayer(ReplayRecording recording)
        {
            Recording = recording;
            puppets = BuildPuppets(recording);
            RigLerp = NetworkedPlayerManager.LiveRigLerp;
            liveEnvironment = TimeManager.Capture();
        }

        private static List<ReplayPuppet> BuildPuppets(ReplayRecording recording)
        {
            var built = new List<ReplayPuppet>(recording.Actors.Count);
            foreach (ReplayActor actor in recording.Actors)
            {
                RigAppearance? appearance = RigAppearance.Resolve(actor.Skeleton, actor.Color);
                if (appearance == null)
                {
                    Log.Warning($"Replay: {actor.Name}'s body could not be rebuilt and is skipped.");
                    continue;
                }
                built.Add(new ReplayPuppet(actor, appearance));
            }
            return built;
        }

        internal void Tick(float deltaTime)
        {
            if (Playing)
            {
                Cursor += deltaTime;
                if (Cursor >= Recording.Duration)
                {
                    Cursor = Recording.Duration;
                    Playing = false;
                }
            }
            mask.Enforce();
            float step = Cursor - posedCursor;
            posedCursor = Cursor;
            foreach (ReplayPuppet puppet in puppets)
            {
                puppet.Pose(Cursor, step, RigLerp);
                puppet.UpdateVoice(Cursor, Playing);
            }
            ApplyEnvironment();
            if (Spectated == null) SpectateNextVisible(1);
        }

        internal void Seek(float time) => Cursor = Mathf.Clamp(time, 0f, Recording.Duration);

        internal void TogglePlaying()
        {
            if (!Playing && Cursor >= Recording.Duration) Cursor = 0f;
            Playing = !Playing;
        }

        internal void Spectate(int index)
        {
            if (index < 0 || index >= puppets.Count) return;
            spectated = index;
            Freecam = false;
        }

        internal void SpectateLocal() => SpectateWhere(p => p.Actor.IsLocal);

        internal void SpectateOther(int otherIndex)
        {
            int seen = 0;
            SpectateWhere(p => !p.Actor.IsLocal && seen++ == otherIndex);
        }

        internal int OtherOrdinal(ReplayPuppet puppet)
        {
            int ordinal = 0;
            foreach (ReplayPuppet candidate in puppets)
            {
                if (candidate == puppet) return ordinal;
                if (!candidate.Actor.IsLocal) ordinal++;
            }
            return -1;
        }

        private void SpectateWhere(Func<ReplayPuppet, bool> match)
        {
            for (int i = 0; i < puppets.Count; i++)
            {
                if (!match(puppets[i])) continue;
                if (puppets[i].Visible) Spectate(i);
                return;
            }
        }

        internal void SpectateNext(int direction)
        {
            Freecam = false;
            SpectateNextVisible(direction);
        }

        private void SpectateNextVisible(int direction)
        {
            int count = puppets.Count;
            for (int step = 1; step <= count; step++)
            {
                int index = ((spectated + direction * step) % count + count) % count;
                if (!puppets[index].Visible) continue;
                spectated = index;
                return;
            }
        }

        internal bool IsSpectating(ReplayPuppet puppet) => !Freecam && Spectated == puppet;

        private void ApplyEnvironment()
        {
            EnvironmentState? state = Recording.EnvironmentAt(Cursor);
            if (state is not EnvironmentState target || appliedEnvironment.Equals(target)) return;
            appliedEnvironment = target;
            TimeManager.Apply(target.TimeIndex, target.Raining);
        }

        public void Dispose()
        {
            foreach (ReplayPuppet puppet in puppets) puppet.Dispose();
            puppets.Clear();
            mask.Dispose();
            if (liveEnvironment != null) TimeManager.Restore(liveEnvironment);
        }
    }
}
