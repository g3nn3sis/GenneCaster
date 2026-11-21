// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class TrackPlayer : IDisposable
    {
        private const float ResyncTolerance = 0.12f;

        private readonly AudioSource source;
        private readonly string clipName;
        private VoiceSegment? playingSegment;
        private AudioClip? clip;

        internal TrackPlayer(AudioSource source, string clipName)
        {
            this.source = source;
            this.clipName = clipName;
        }

        internal Transform Transform => source.transform;

        internal void Update(VoiceTrack track, float time, bool audible)
        {
            VoiceSegment? segment = audible ? track.At(time) : null;
            if (segment == null)
            {
                if (source.isPlaying) source.Stop();
                return;
            }
            float offset = time - segment.Start;
            if (segment != playingSegment) Load(segment);
            if (!source.isPlaying)
            {
                source.Play();
                source.time = offset;
            }
            else if (Mathf.Abs(source.time - offset) > ResyncTolerance) source.time = offset;
        }

        private void Load(VoiceSegment segment)
        {
            source.Stop();
            ReleaseClip();
            float[] samples = segment.ToFloats();
            int position = 0;
            clip = AudioClip.Create(
                clipName, samples.Length, 1, segment.SampleRate, false,
                data =>
                {
                    int count = Mathf.Clamp(samples.Length - position, 0, data.Length);
                    Array.Copy(samples, position, data, 0, count);
                    Array.Clear(data, count, data.Length - count);
                    position += count;
                },
                newPosition => position = newPosition);
            source.clip = clip;
            playingSegment = segment;
        }

        private void ReleaseClip()
        {
            if (clip != null) UnityEngine.Object.Destroy(clip);
            clip = null;
            playingSegment = null;
        }

        public void Dispose() => ReleaseClip();
    }
}
