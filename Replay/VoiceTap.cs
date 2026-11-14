// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class VoiceTap : MonoBehaviour
    {
        private const float BufferSeconds = 2f;

        private readonly object gate = new object();
        private float[] pending = Array.Empty<float>();
        private int count;

        internal void Awake() => pending = new float[Mathf.CeilToInt(AudioSettings.outputSampleRate * BufferSeconds)];

        internal void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels <= 0) return;
            lock (gate)
            {
                for (int i = 0; i + channels <= data.Length && count < pending.Length; i += channels)
                    pending[count++] = Downmix(data, i, channels);
            }
        }

        internal int Drain(ref float[] into)
        {
            lock (gate)
            {
                if (into.Length < count) into = new float[pending.Length];
                Array.Copy(pending, into, count);
                int drained = count;
                count = 0;
                return drained;
            }
        }

        private static float Downmix(float[] data, int offset, int channels)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++) sum += data[offset + c];
            return sum / channels;
        }
    }
}
