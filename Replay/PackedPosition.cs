// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal readonly struct PackedPosition
    {
        private readonly ushort x, y, z;

        internal PackedPosition(Vector3 position)
        {
            x = Mathf.FloatToHalf(position.x);
            y = Mathf.FloatToHalf(position.y);
            z = Mathf.FloatToHalf(position.z);
        }

        private PackedPosition(ushort x, ushort y, ushort z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        internal Vector3 Unpack() => new Vector3(Mathf.HalfToFloat(x), Mathf.HalfToFloat(y), Mathf.HalfToFloat(z));

        internal static Vector3 Blend(PackedPosition from, PackedPosition to, float t) => Vector3.Lerp(from.Unpack(), to.Unpack(), t);

        internal void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }

        internal static PackedPosition Read(BinaryReader reader) =>
            new PackedPosition(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
    }
}
