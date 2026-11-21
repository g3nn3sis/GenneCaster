// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal readonly struct RecordedSkin : IEquatable<RecordedSkin>
    {
        internal const int None = -1;

        private const int HashFactor = 31;

        private readonly PackedColor bodyColor;
        private readonly PackedColor chestColor;

        internal string Source { get; }
        internal string AssetName { get; }

        internal RecordedSkin(string source, string assetName, PackedColor bodyColor, PackedColor chestColor)
        {
            Source = source;
            AssetName = assetName;
            this.bodyColor = bodyColor;
            this.chestColor = chestColor;
        }

        internal Color BodyColor => bodyColor.Unpack();
        internal Color ChestColor => chestColor.Unpack();

        public bool Equals(RecordedSkin other) =>
            Source == other.Source && AssetName == other.AssetName && bodyColor.Matches(other.bodyColor) && chestColor.Matches(other.chestColor);

        public override bool Equals(object? obj) => obj is RecordedSkin other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return Source.GetHashCode() * HashFactor + AssetName.GetHashCode(); }
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Source);
            writer.Write(AssetName);
            bodyColor.Write(writer);
            chestColor.Write(writer);
        }

        internal static RecordedSkin Read(BinaryReader reader) =>
            new RecordedSkin(reader.ReadString(), reader.ReadString(), PackedColor.Read(reader), PackedColor.Read(reader));
    }
}
