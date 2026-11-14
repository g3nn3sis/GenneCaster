// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace GenneCaster.Replay
{
    internal readonly struct VoiceSettings
    {
        private const float DefaultMinDistance = 1f;
        private const float DefaultMaxDistance = 25f;

        private readonly AudioRolloffMode rolloff;
        private readonly float spatialBlend, minDistance, maxDistance, volume;

        private VoiceSettings(AudioRolloffMode rolloff, float spatialBlend, float minDistance, float maxDistance, float volume)
        {
            this.rolloff = rolloff;
            this.spatialBlend = spatialBlend;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.volume = volume;
        }

        internal static VoiceSettings From(AudioSource? source) =>
            source == null
                ? new VoiceSettings(AudioRolloffMode.Logarithmic, 1f, DefaultMinDistance, DefaultMaxDistance, 1f)
                : new VoiceSettings(source.rolloffMode, source.spatialBlend, source.minDistance, source.maxDistance, source.volume);

        internal void ApplyTo(AudioSource source, AudioMixerGroup? mixer)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.outputAudioMixerGroup = mixer;
            source.rolloffMode = rolloff;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.volume = volume;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write((int)rolloff);
            writer.Write(spatialBlend);
            writer.Write(minDistance);
            writer.Write(maxDistance);
            writer.Write(volume);
        }

        internal static VoiceSettings Read(BinaryReader reader) =>
            new VoiceSettings((AudioRolloffMode)reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
