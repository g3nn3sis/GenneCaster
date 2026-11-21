// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal readonly struct PackedColor
    {
        private readonly ushort r, g, b, a;

        internal PackedColor(Color color)
        {
            r = Mathf.FloatToHalf(color.r);
            g = Mathf.FloatToHalf(color.g);
            b = Mathf.FloatToHalf(color.b);
            a = Mathf.FloatToHalf(color.a);
        }

        private PackedColor(ushort r, ushort g, ushort b, ushort a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        internal Color Unpack() => new Color(Mathf.HalfToFloat(r), Mathf.HalfToFloat(g), Mathf.HalfToFloat(b), Mathf.HalfToFloat(a));

        internal bool Matches(PackedColor other) => r == other.r && g == other.g && b == other.b && a == other.a;

        internal void Write(BinaryWriter writer)
        {
            writer.Write(r);
            writer.Write(g);
            writer.Write(b);
            writer.Write(a);
        }

        internal static PackedColor Read(BinaryReader reader) =>
            new PackedColor(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
    }
}
