// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class ReplayActor
    {
        private const float MaxFrameGapFactor = 3f;

        private readonly List<RecordedCosmetic> cosmetics = new List<RecordedCosmetic>();
        private readonly Dictionary<CosmeticKey, int> cosmeticIndices = new Dictionary<CosmeticKey, int>();
        private readonly List<RecordedSkin> skins = new List<RecordedSkin>();
        private readonly Dictionary<RecordedSkin, int> skinIndices = new Dictionary<RecordedSkin, int>();
        private readonly List<CosmeticSoundTrack> sounds = new List<CosmeticSoundTrack>();

        internal int Id { get; }
        internal string Name { get; }
        internal Color Color { get; }
        internal RigSkeleton Skeleton { get; }
        internal List<ActorFrame> Frames { get; } = new List<ActorFrame>();
        internal VoiceTrack Voice { get; } = new VoiceTrack();
        internal bool IsLocal { get; }
        internal int Platform { get; set; }
        internal IReadOnlyList<RecordedCosmetic> Cosmetics => cosmetics;
        internal IReadOnlyList<RecordedSkin> Skins => skins;
        internal IReadOnlyList<CosmeticSoundTrack> Sounds => sounds;

        internal ReplayActor(int id, string name, Color color, RigSkeleton skeleton, bool isLocal)
        {
            Id = id;
            Name = name;
            Color = color;
            Skeleton = skeleton;
            IsLocal = isLocal;
        }

        internal int CosmeticIndex(CosmeticKey key)
        {
            if (cosmeticIndices.TryGetValue(key, out int index)) return index;
            index = cosmetics.Count;
            cosmetics.Add(new RecordedCosmetic(key));
            cosmeticIndices[key] = index;
            return index;
        }

        internal int SkinIndex(RecordedSkin skin)
        {
            if (skinIndices.TryGetValue(skin, out int index)) return index;
            if (skins.Count >= short.MaxValue) return RecordedSkin.None;
            index = skins.Count;
            skins.Add(skin);
            skinIndices[skin] = index;
            return index;
        }

        internal CosmeticSoundTrack SoundTrack(int cosmetic, int node, AudioSource source)
        {
            foreach (CosmeticSoundTrack existing in sounds)
                if (existing.Cosmetic == cosmetic && existing.Node == node) return existing;
            string mixer = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : string.Empty;
            var created = new CosmeticSoundTrack(cosmetic, node, VoiceSettings.From(source), mixer);
            sounds.Add(created);
            return created;
        }

        internal void WriteCosmetics(BinaryWriter writer)
        {
            writer.Write(cosmetics.Count);
            foreach (RecordedCosmetic cosmetic in cosmetics) cosmetic.Write(writer);
            writer.Write(skins.Count);
            foreach (RecordedSkin skin in skins) skin.Write(writer);
        }

        internal void ReadCosmetics(BinaryReader reader)
        {
            int count = ReplayIO.ReadCount(reader);
            for (int i = 0; i < count; i++)
            {
                RecordedCosmetic cosmetic = RecordedCosmetic.Read(reader);
                cosmeticIndices[cosmetic.Key] = cosmetics.Count;
                cosmetics.Add(cosmetic);
            }
            int skinCount = ReplayIO.ReadCount(reader);
            if (skinCount > short.MaxValue) throw new InvalidDataException("Too many skins.");
            for (int i = 0; i < skinCount; i++) skins.Add(RecordedSkin.Read(reader));
        }

        internal void WriteSounds(BinaryWriter writer)
        {
            writer.Write(sounds.Count);
            foreach (CosmeticSoundTrack sound in sounds) sound.Write(writer);
        }

        internal void ReadSounds(BinaryReader reader)
        {
            int count = ReplayIO.ReadCount(reader);
            for (int i = 0; i < count; i++) sounds.Add(CosmeticSoundTrack.Read(reader, cosmetics));
        }

        internal bool TryFindSpan(float time, float sampleInterval, out int index, out float blend)
        {
            index = 0;
            blend = 0f;
            if (Frames.Count == 0 || time < Frames[0].Time || time > Frames[Frames.Count - 1].Time) return false;
            int low = 0, high = Frames.Count - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (Frames[mid].Time <= time) low = mid;
                else high = mid - 1;
            }
            index = low;
            if (index == Frames.Count - 1) return true;
            float gap = Frames[index + 1].Time - Frames[index].Time;
            if (gap > sampleInterval * MaxFrameGapFactor) return time - Frames[index].Time <= sampleInterval;
            blend = gap > 0f ? (time - Frames[index].Time) / gap : 0f;
            return true;
        }
    }
}
