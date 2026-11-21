// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Replay
{
    internal readonly struct CosmeticNodeChange
    {
        internal int Node { get; }
        internal CosmeticNodeState State { get; }

        internal CosmeticNodeChange(int node, CosmeticNodeState state)
        {
            Node = node;
            State = state;
        }

        internal static byte[] Encode(List<CosmeticNodeChange> changes, RecordedCosmetic cosmetic, MemoryStream scratch)
        {
            if (changes.Count == 0) return Array.Empty<byte>();
            scratch.SetLength(0);
            using (var writer = new BinaryWriter(scratch, System.Text.Encoding.UTF8, true))
            {
                writer.Write((ushort)changes.Count);
                foreach (CosmeticNodeChange change in changes)
                {
                    writer.Write((ushort)change.Node);
                    change.State.WriteAgainst(writer, cosmetic.Rest[change.Node]);
                }
            }
            return scratch.ToArray();
        }

        internal static void Decode(byte[] encoded, RecordedCosmetic cosmetic, List<CosmeticNodeChange> into)
        {
            into.Clear();
            if (encoded.Length == 0) return;
            using var reader = new BinaryReader(new MemoryStream(encoded, false));
            int count = reader.ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                int node = reader.ReadUInt16();
                if (node >= cosmetic.Rest.Length) throw new InvalidDataException("Cosmetic part index out of range.");
                into.Add(new CosmeticNodeChange(node, CosmeticNodeState.ReadAgainst(reader, cosmetic.Rest[node])));
            }
        }
    }
}
