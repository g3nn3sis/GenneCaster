// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal readonly struct CosmeticPose
    {
        private const float MinimumAxisLength = 0.0001f;
        private const int MaxEncodedChanges = 1024 * 1024;

        private readonly PackedPosition position;
        private readonly PackedRotation rotation;
        private readonly PackedPosition scale;

        internal int Key { get; }
        internal int Joint { get; }
        internal byte[] Changes { get; }

        private CosmeticPose(int key, int joint, PackedPosition position, PackedRotation rotation, PackedPosition scale, byte[] changes)
        {
            Key = key;
            Joint = joint;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            Changes = changes;
        }

        internal static CosmeticPose FromMatrix(int key, int joint, Matrix4x4 local, byte[] changes)
        {
            Vector3 right = local.GetColumn(0);
            Vector3 up = local.GetColumn(1);
            Vector3 forward = local.GetColumn(2);
            var size = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (Vector3.Dot(Vector3.Cross(right, up), forward) < 0f) size.x = -size.x;
            Quaternion turn = up.magnitude < MinimumAxisLength || forward.magnitude < MinimumAxisLength
                ? Quaternion.identity
                : Quaternion.LookRotation(forward, up);
            return new CosmeticPose(key, joint, new PackedPosition(local.GetColumn(3)), new PackedRotation(turn), new PackedPosition(size), changes);
        }

        internal static void Apply(Transform target, CosmeticPose from, CosmeticPose to, float blend)
        {
            target.localPosition = PackedPosition.Blend(from.position, to.position, blend);
            target.localRotation = PackedRotation.Blend(from.rotation, to.rotation, blend);
            target.localScale = PackedPosition.Blend(from.scale, to.scale, blend);
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Key);
            writer.Write((short)Joint);
            position.Write(writer);
            rotation.Write(writer);
            scale.Write(writer);
            writer.Write(Changes.Length);
            writer.Write(Changes);
        }

        internal static CosmeticPose Read(BinaryReader reader, IReadOnlyList<RecordedCosmetic> cosmetics, int jointCount, List<CosmeticNodeChange> scratch)
        {
            int key = reader.ReadUInt16();
            int joint = reader.ReadInt16();
            if (key >= cosmetics.Count) throw new InvalidDataException("Cosmetic index out of range.");
            if (joint != RigSkeleton.RootParent && (joint < 0 || joint >= jointCount)) throw new InvalidDataException("Cosmetic joint out of range.");
            PackedPosition position = PackedPosition.Read(reader);
            PackedRotation rotation = PackedRotation.Read(reader);
            PackedPosition scale = PackedPosition.Read(reader);
            int length = reader.ReadInt32();
            if (length < 0 || length > MaxEncodedChanges) throw new InvalidDataException("Implausible cosmetic change size.");
            byte[] changes = length == 0 ? Array.Empty<byte>() : reader.ReadBytes(length);
            if (changes.Length != length) throw new EndOfStreamException();
            CosmeticNodeChange.Decode(changes, cosmetics[key], scratch);
            return new CosmeticPose(key, joint, position, rotation, scale, changes);
        }
    }
}
