// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Replay
{
    internal readonly struct CosmeticKey : IEquatable<CosmeticKey>
    {
        private const int HashFactor = 31;

        internal string PlayFabId { get; }
        internal CosmeticKind Kind { get; }
        internal int Part { get; }
        internal int Anchor { get; }

        internal CosmeticKey(string playFabId, CosmeticKind kind, int part, int anchor)
        {
            PlayFabId = playFabId;
            Kind = kind;
            Part = part;
            Anchor = anchor;
        }

        internal CosmeticKey Prefab => new CosmeticKey(PlayFabId, Kind, Part, 0);

        public bool Equals(CosmeticKey other) =>
            PlayFabId == other.PlayFabId && Kind == other.Kind && Part == other.Part && Anchor == other.Anchor;

        public override bool Equals(object? obj) => obj is CosmeticKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayFabId.GetHashCode();
                hash = hash * HashFactor + (int)Kind;
                hash = hash * HashFactor + Part;
                return hash * HashFactor + Anchor;
            }
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(PlayFabId);
            writer.Write((byte)Kind);
            writer.Write(Part);
            writer.Write(Anchor);
        }

        internal static CosmeticKey Read(BinaryReader reader)
        {
            string playFabId = reader.ReadString();
            byte kind = reader.ReadByte();
            if (!Enum.IsDefined(typeof(CosmeticKind), kind)) throw new InvalidDataException($"Unknown cosmetic kind {kind}.");
            return new CosmeticKey(playFabId, (CosmeticKind)kind, reader.ReadInt32(), reader.ReadInt32());
        }
    }
}
