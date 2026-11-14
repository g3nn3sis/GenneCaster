// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal enum ReplayEventKind { Joined, Left, Tagged, TimeOfDay, Rain }

    internal enum ReplayTagState : byte { None, Lava, Rock }

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

    internal struct ActorFrame
    {
        internal float Time;
        internal Vector3 Position;
        internal PackedRotation Rotation;
        internal Vector3 RawPosition;
        internal PackedRotation RawRotation;
        internal LimbSample[] Limbs;
        internal float Scale;
        internal Vector3 EyePosition;
        internal PackedRotation EyeRotation;
        internal PackedRotation[] Joints;
        internal PackedPosition[] JointPositions;
        internal ushort[] JointScales;
        internal int Material;
        internal int Fps;
        internal ReplayTagState Tag;

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Time);
            ReplayIO.Write(writer, Position);
            Rotation.Write(writer);
            ReplayIO.Write(writer, RawPosition);
            RawRotation.Write(writer);
            foreach (LimbSample limb in Limbs) limb.Write(writer);
            writer.Write(Scale);
            ReplayIO.Write(writer, EyePosition);
            EyeRotation.Write(writer);
            foreach (PackedRotation joint in Joints) joint.Write(writer);
            foreach (PackedPosition position in JointPositions) position.Write(writer);
            foreach (ushort scale in JointScales) writer.Write(scale);
            writer.Write(Material);
            writer.Write((short)Mathf.Clamp(Fps, short.MinValue, short.MaxValue));
            writer.Write((byte)Tag);
        }

        private static LimbSample[] ReadLimbs(BinaryReader reader)
        {
            var limbs = new LimbSample[LimbSample.Count];
            for (int i = 0; i < limbs.Length; i++) limbs[i] = LimbSample.Read(reader);
            return limbs;
        }

        internal static ActorFrame Read(BinaryReader reader, RigSkeleton skeleton)
        {
            var frame = new ActorFrame
            {
                Time = reader.ReadSingle(),
                Position = ReplayIO.ReadVector3(reader),
                Rotation = PackedRotation.Read(reader),
                RawPosition = ReplayIO.ReadVector3(reader),
                RawRotation = PackedRotation.Read(reader),
                Limbs = ReadLimbs(reader),
                Scale = reader.ReadSingle(),
                EyePosition = ReplayIO.ReadVector3(reader),
                EyeRotation = PackedRotation.Read(reader),
                Joints = new PackedRotation[skeleton.JointCount],
                JointPositions = new PackedPosition[skeleton.JointCount],
                JointScales = new ushort[skeleton.JointCount],
            };
            for (int i = 0; i < frame.Joints.Length; i++) frame.Joints[i] = PackedRotation.Read(reader);
            for (int i = 0; i < frame.JointPositions.Length; i++) frame.JointPositions[i] = PackedPosition.Read(reader);
            for (int i = 0; i < frame.JointScales.Length; i++) frame.JointScales[i] = reader.ReadUInt16();
            frame.Material = reader.ReadInt32();
            frame.Fps = reader.ReadInt16();
            frame.Tag = (ReplayTagState)reader.ReadByte();
            return frame;
        }
    }

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
