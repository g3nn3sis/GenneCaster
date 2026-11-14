// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class VoiceSegment
    {
        private const float SampleRange = short.MaxValue;

        private readonly List<short> samples = new List<short>();

        internal float Start { get; }
        internal int SampleRate { get; }
        internal int Length => samples.Count;
        internal float End => Start + samples.Count / (float)SampleRate;

        internal VoiceSegment(float start, int sampleRate)
        {
            Start = start;
            SampleRate = sampleRate;
        }

        internal void Append(float[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
                samples.Add((short)Mathf.RoundToInt(Mathf.Clamp(buffer[i], -1f, 1f) * SampleRange));
        }

        internal void TrimEnd(int count)
        {
            int keep = Mathf.Max(0, samples.Count - count);
            samples.RemoveRange(keep, samples.Count - keep);
            samples.TrimExcess();
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Start);
            writer.Write(SampleRate);
            writer.Write(samples.Count);
            foreach (short sample in samples) writer.Write(sample);
        }

        internal static VoiceSegment Read(BinaryReader reader)
        {
            var segment = new VoiceSegment(reader.ReadSingle(), reader.ReadInt32());
            if (segment.SampleRate <= 0) throw new InvalidDataException("Invalid voice sample rate.");
            int count = ReplayIO.ReadCount(reader);
            segment.samples.Capacity = count;
            for (int i = 0; i < count; i++) segment.samples.Add(reader.ReadInt16());
            return segment;
        }

        internal float[] ToFloats()
        {
            var result = new float[samples.Count];
            for (int i = 0; i < result.Length; i++) result[i] = samples[i] / SampleRange;
            return result;
        }
    }

    internal sealed class VoiceTrack
    {
        private const float SilenceThreshold = 0.002f;
        private const float MaxSilenceSeconds = 0.6f;
        private const float MaxJoinGap = 0.25f;

        private readonly List<VoiceSegment> segments = new List<VoiceSegment>();
        private VoiceSegment? open;
        private int trailingSilence;

        internal IReadOnlyList<VoiceSegment> Segments => segments;

        internal void Append(float chunkEnd, float[] buffer, int count, int sampleRate)
        {
            if (count <= 0) return;
            bool silent = Peak(buffer, count) < SilenceThreshold;
            float chunkStart = chunkEnd - count / (float)sampleRate;
            if (open != null && chunkStart - open.End > MaxJoinGap) Close();
            if (open == null)
            {
                if (silent) return;
                open = new VoiceSegment(Mathf.Max(0f, chunkStart), sampleRate);
                segments.Add(open);
            }
            trailingSilence = silent ? trailingSilence + count : 0;
            open.Append(buffer, count);
            if (trailingSilence > MaxSilenceSeconds * sampleRate) Close();
        }

        internal void Close()
        {
            if (open == null) return;
            open.TrimEnd(trailingSilence);
            if (open.Length == 0) segments.Remove(open);
            open = null;
            trailingSilence = 0;
        }

        internal void Write(BinaryWriter writer)
        {
            Close();
            writer.Write(segments.Count);
            foreach (VoiceSegment segment in segments) segment.Write(writer);
        }

        internal void Read(BinaryReader reader)
        {
            int count = ReplayIO.ReadCount(reader);
            for (int i = 0; i < count; i++) segments.Add(VoiceSegment.Read(reader));
        }

        internal VoiceSegment? At(float time)
        {
            int low = 0, high = segments.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                VoiceSegment segment = segments[mid];
                if (time < segment.Start) high = mid - 1;
                else if (time >= segment.End) low = mid + 1;
                else return segment;
            }
            return null;
        }

        private static float Peak(float[] buffer, int count)
        {
            float peak = 0f;
            for (int i = 0; i < count; i++) peak = Mathf.Max(peak, Mathf.Abs(buffer[i]));
            return peak;
        }
    }
}
