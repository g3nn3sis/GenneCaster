// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Replay
{
    internal sealed class RecordedCosmetic
    {
        internal CosmeticKey Key { get; }
        internal string[] NodeNames { get; private set; } = Array.Empty<string>();
        internal CosmeticNodeState[] Rest { get; private set; } = Array.Empty<CosmeticNodeState>();
        internal bool Described => NodeNames.Length > 0;

        internal RecordedCosmetic(CosmeticKey key) => Key = key;

        internal void Describe(string[] nodeNames, CosmeticNodeState[] rest)
        {
            if (Described || nodeNames.Length != rest.Length || nodeNames.Length > ushort.MaxValue) return;
            NodeNames = nodeNames;
            Rest = rest;
        }

        internal void Write(BinaryWriter writer)
        {
            Key.Write(writer);
            writer.Write(NodeNames.Length);
            for (int i = 0; i < NodeNames.Length; i++)
            {
                writer.Write(NodeNames[i]);
                Rest[i].WriteFull(writer);
            }
        }

        internal static RecordedCosmetic Read(BinaryReader reader)
        {
            var cosmetic = new RecordedCosmetic(CosmeticKey.Read(reader));
            int count = ReplayIO.ReadCount(reader);
            if (count > ushort.MaxValue) throw new InvalidDataException("Too many cosmetic parts.");
            var names = new string[count];
            var rest = new CosmeticNodeState[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = reader.ReadString();
                rest[i] = CosmeticNodeState.ReadFull(reader);
            }
            cosmetic.Describe(names, rest);
            return cosmetic;
        }
    }
}
