// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class LiveCosmetic
    {
        private const float PositionTolerance = 0.001f;
        private const float ScaleTolerance = 0.001f;
        private const float RotationTolerance = 0.000002f;
        private const float ColorTolerance = 0.002f;

        private readonly RecordedCosmetic record;
        private readonly Transform?[] nodes;
        private readonly Renderer?[] renderers;
        private readonly ParticleSystem?[] particles;
        private readonly Light?[] lights;
        private readonly Vector3[] restPositions;
        private readonly Quaternion[] restRotations;
        private readonly Vector3[] restScales;
        private readonly Color[] restColors;
        private readonly Color[] restEmissions;
        private readonly List<CosmeticNodeChange> changes = new List<CosmeticNodeChange>();
        private readonly List<(VoiceTap Tap, CosmeticSoundTrack Sound)> taps = new List<(VoiceTap, CosmeticSoundTrack)>();

        internal int Cosmetic { get; }
        internal GameObject Root { get; }

        internal LiveCosmetic(GameObject root, ReplayActor actor, int cosmetic)
        {
            Root = root;
            Cosmetic = cosmetic;
            record = actor.Cosmetics[cosmetic];
            Transform[] live = CosmeticNodes.Of(root.transform);
            if (!record.Described) record.Describe(live.Select(node => node.name).ToArray(), live.Select(Sample).ToArray());
            int[] matches = CosmeticNodes.Align(record.NodeNames, live);
            int count = matches.Length;
            nodes = new Transform?[count];
            renderers = new Renderer?[count];
            particles = new ParticleSystem?[count];
            lights = new Light?[count];
            for (int i = 0; i < count; i++)
            {
                if (matches[i] == CosmeticNodes.Unmatched) continue;
                Transform node = live[matches[i]];
                nodes[i] = node;
                renderers[i] = node.TryGetComponent(out Renderer renderer) ? renderer : null;
                particles[i] = node.TryGetComponent(out ParticleSystem system) ? system : null;
                lights[i] = node.TryGetComponent(out Light light) ? light : null;
            }
            restPositions = record.Rest.Select(state => state.Position).ToArray();
            restRotations = record.Rest.Select(state => state.Rotation).ToArray();
            restScales = record.Rest.Select(state => state.Scale).ToArray();
            restColors = record.Rest.Select(state => state.Color).ToArray();
            restEmissions = record.Rest.Select(state => state.Emission).ToArray();
        }

        internal void Listen(ReplayActor actor, HashSet<CosmeticSoundTrack> claimed)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform? node = nodes[i];
                if (node == null || !node.TryGetComponent(out AudioSource source)) continue;
                CosmeticSoundTrack sound = actor.SoundTrack(Cosmetic, i, source);
                if (!claimed.Add(sound)) continue;
                taps.Add((node.gameObject.AddComponent<VoiceTap>(), sound));
            }
        }

        internal void Drain(float audioNow, ref float[] buffer, int sampleRate)
        {
            foreach ((VoiceTap tap, CosmeticSoundTrack sound) in taps)
            {
                if (tap == null) continue;
                int count = tap.Drain(ref buffer);
                sound.Track.Append(audioNow, buffer, count, sampleRate);
            }
        }

        internal void Release(HashSet<CosmeticSoundTrack> claimed)
        {
            foreach ((VoiceTap tap, CosmeticSoundTrack sound) in taps)
            {
                if (tap != null) UnityEngine.Object.Destroy(tap);
                sound.Track.Close();
                claimed.Remove(sound);
            }
            taps.Clear();
        }

        internal byte[] Changes(MemoryStream scratch)
        {
            changes.Clear();
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform? node = nodes[i];
                if (node == null) continue;
                node.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
                Vector3 scale = node.localScale;
                CosmeticNodeFlags flags = FlagsOf(node, renderers[i], particles[i], lights[i]);
                Renderer? renderer = renderers[i];
                Color color = renderer != null ? RendererColors.Main(renderer) : Color.white;
                Color emission = renderer != null ? RendererColors.Emission(renderer) : Color.black;
                bool moved = i != 0 && (!Near(position, restPositions[i], PositionTolerance) || !SameTurn(rotation, restRotations[i]) || !Near(scale, restScales[i], ScaleTolerance));
                if (!moved && flags == record.Rest[i].Flags && SameColor(color, restColors[i]) && SameColor(emission, restEmissions[i])) continue;
                var state = new CosmeticNodeState(position, rotation, scale, flags, color, emission);
                changes.Add(new CosmeticNodeChange(i, moved ? state : state.PlacedAs(record.Rest[i])));
            }
            return CosmeticNodeChange.Encode(changes, record, scratch);
        }

        private static CosmeticNodeState Sample(Transform node)
        {
            node.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
            Renderer? renderer = node.TryGetComponent(out Renderer found) ? found : null;
            ParticleSystem? system = node.TryGetComponent(out ParticleSystem foundSystem) ? foundSystem : null;
            Light? light = node.TryGetComponent(out Light foundLight) ? foundLight : null;
            return new CosmeticNodeState(
                position,
                rotation,
                node.localScale,
                FlagsOf(node, renderer, system, light),
                renderer != null ? RendererColors.Main(renderer) : Color.white,
                renderer != null ? RendererColors.Emission(renderer) : Color.black);
        }

        private static CosmeticNodeFlags FlagsOf(Transform node, Renderer? renderer, ParticleSystem? system, Light? light)
        {
            CosmeticNodeFlags flags = CosmeticNodeFlags.None;
            if (node.gameObject.activeSelf) flags |= CosmeticNodeFlags.Active;
            if (renderer != null && renderer.enabled) flags |= CosmeticNodeFlags.Visible;
            if (system != null && system.isEmitting) flags |= CosmeticNodeFlags.Emitting;
            if (light != null && light.enabled) flags |= CosmeticNodeFlags.Lit;
            if (renderer is TrailRenderer trail && trail.emitting) flags |= CosmeticNodeFlags.Trailing;
            return flags;
        }

        private static bool Near(Vector3 value, Vector3 rest, float tolerance) =>
            (value - rest).sqrMagnitude <= tolerance * tolerance * (1f + rest.sqrMagnitude);

        private static bool SameTurn(Quaternion value, Quaternion rest) => 1f - Mathf.Abs(Quaternion.Dot(value, rest)) <= RotationTolerance;

        private static bool SameColor(Color value, Color rest) =>
            Mathf.Abs(value.r - rest.r) <= ColorTolerance
            && Mathf.Abs(value.g - rest.g) <= ColorTolerance
            && Mathf.Abs(value.b - rest.b) <= ColorTolerance
            && Mathf.Abs(value.a - rest.a) <= ColorTolerance;
    }
}
