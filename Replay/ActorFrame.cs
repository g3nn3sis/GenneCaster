// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
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
}
