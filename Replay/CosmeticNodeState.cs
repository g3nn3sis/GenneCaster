// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal readonly struct CosmeticNodeState
    {
        [Flags]
        private enum Fields : byte
        {
            None = 0,
            Position = 1,
            Rotation = 2,
            Scale = 4,
            Flags = 8,
            Color = 16,
            Emission = 32,
        }

        private readonly PackedPosition position;
        private readonly PackedRotation rotation;
        private readonly PackedPosition scale;
        private readonly PackedColor color;
        private readonly PackedColor emission;

        internal CosmeticNodeFlags Flags { get; }

        internal CosmeticNodeState(Vector3 position, Quaternion rotation, Vector3 scale, CosmeticNodeFlags flags, Color color, Color emission)
            : this(new PackedPosition(position), new PackedRotation(rotation), new PackedPosition(scale), flags, new PackedColor(color), new PackedColor(emission))
        {
        }

        private CosmeticNodeState(PackedPosition position, PackedRotation rotation, PackedPosition scale, CosmeticNodeFlags flags, PackedColor color, PackedColor emission)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.color = color;
            this.emission = emission;
            Flags = flags;
        }

        internal Vector3 Position => position.Unpack();
        internal Quaternion Rotation => rotation.Unpack();
        internal Vector3 Scale => scale.Unpack();
        internal Color Color => color.Unpack();
        internal Color Emission => emission.Unpack();

        internal CosmeticNodeState PlacedAs(CosmeticNodeState placement) =>
            new CosmeticNodeState(placement.position, placement.rotation, placement.scale, Flags, color, emission);

        internal bool LooksLike(CosmeticNodeState other) =>
            Flags == other.Flags && color.Matches(other.color) && emission.Matches(other.emission);

        internal void WriteFull(BinaryWriter writer)
        {
            position.Write(writer);
            rotation.Write(writer);
            scale.Write(writer);
            writer.Write((byte)Flags);
            color.Write(writer);
            emission.Write(writer);
        }

        internal static CosmeticNodeState ReadFull(BinaryReader reader) =>
            new CosmeticNodeState(PackedPosition.Read(reader), PackedRotation.Read(reader), PackedPosition.Read(reader), (CosmeticNodeFlags)reader.ReadByte(), PackedColor.Read(reader), PackedColor.Read(reader));

        internal void WriteAgainst(BinaryWriter writer, CosmeticNodeState rest)
        {
            Fields fields = FieldsDifferingFrom(rest);
            writer.Write((byte)fields);
            if ((fields & Fields.Position) != 0) position.Write(writer);
            if ((fields & Fields.Rotation) != 0) rotation.Write(writer);
            if ((fields & Fields.Scale) != 0) scale.Write(writer);
            if ((fields & Fields.Flags) != 0) writer.Write((byte)Flags);
            if ((fields & Fields.Color) != 0) color.Write(writer);
            if ((fields & Fields.Emission) != 0) emission.Write(writer);
        }

        internal static CosmeticNodeState ReadAgainst(BinaryReader reader, CosmeticNodeState rest)
        {
            var fields = (Fields)reader.ReadByte();
            return new CosmeticNodeState(
                (fields & Fields.Position) != 0 ? PackedPosition.Read(reader) : rest.position,
                (fields & Fields.Rotation) != 0 ? PackedRotation.Read(reader) : rest.rotation,
                (fields & Fields.Scale) != 0 ? PackedPosition.Read(reader) : rest.scale,
                (fields & Fields.Flags) != 0 ? (CosmeticNodeFlags)reader.ReadByte() : rest.Flags,
                (fields & Fields.Color) != 0 ? PackedColor.Read(reader) : rest.color,
                (fields & Fields.Emission) != 0 ? PackedColor.Read(reader) : rest.emission);
        }

        private Fields FieldsDifferingFrom(CosmeticNodeState rest)
        {
            Fields fields = Fields.None;
            if (Position != rest.Position) fields |= Fields.Position;
            if (Rotation != rest.Rotation) fields |= Fields.Rotation;
            if (Scale != rest.Scale) fields |= Fields.Scale;
            if (Flags != rest.Flags) fields |= Fields.Flags;
            if (!color.Matches(rest.color)) fields |= Fields.Color;
            if (!emission.Matches(rest.emission)) fields |= Fields.Emission;
            return fields;
        }
    }
}
