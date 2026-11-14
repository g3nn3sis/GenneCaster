// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal readonly struct PackedRotation
    {
        private const float Range = short.MaxValue;

        private readonly short x, y, z, w;

        internal PackedRotation(Quaternion rotation)
        {
            Quaternion unit = rotation.normalized;
            x = Pack(unit.x);
            y = Pack(unit.y);
            z = Pack(unit.z);
            w = Pack(unit.w);
        }

        private PackedRotation(short x, short y, short z, short w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            writer.Write(w);
        }

        internal static PackedRotation Read(BinaryReader reader) =>
            new PackedRotation(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());

        internal Quaternion Unpack() => new Quaternion(x / Range, y / Range, z / Range, w / Range).normalized;

        internal static Quaternion Blend(PackedRotation from, PackedRotation to, float t) =>
            Quaternion.Slerp(from.Unpack(), to.Unpack(), t);

        private static short Pack(float component) => (short)Mathf.RoundToInt(Mathf.Clamp(component, -1f, 1f) * Range);
    }
}
