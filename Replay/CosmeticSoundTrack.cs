// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Replay
{
    internal sealed class CosmeticSoundTrack
    {
        internal int Cosmetic { get; }
        internal int Node { get; }
        internal VoiceSettings Settings { get; }
        internal string Mixer { get; }
        internal VoiceTrack Track { get; } = new VoiceTrack();

        internal CosmeticSoundTrack(int cosmetic, int node, VoiceSettings settings, string mixer)
        {
            Cosmetic = cosmetic;
            Node = node;
            Settings = settings;
            Mixer = mixer;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Cosmetic);
            writer.Write(Node);
            Settings.Write(writer);
            writer.Write(Mixer);
            Track.Write(writer);
        }

        internal static CosmeticSoundTrack Read(BinaryReader reader, IReadOnlyList<RecordedCosmetic> cosmetics)
        {
            int cosmetic = reader.ReadInt32();
            int node = reader.ReadInt32();
            if (cosmetic < 0 || cosmetic >= cosmetics.Count) throw new InvalidDataException("Cosmetic sound index out of range.");
            if (node < 0 || node >= cosmetics[cosmetic].Rest.Length) throw new InvalidDataException("Cosmetic sound part out of range.");
            var sound = new CosmeticSoundTrack(cosmetic, node, VoiceSettings.Read(reader), reader.ReadString());
            sound.Track.Read(reader);
            return sound;
        }
    }
}
