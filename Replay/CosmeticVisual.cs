// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class CosmeticVisual
    {
        private const float LeadSwitch = 0.5f;
        private const float ColorTolerance = 0.002f;
        private const int Unset = -1;

        private readonly RecordedCosmetic record;
        private readonly Transform?[] nodes;
        private readonly Renderer?[] renderers;
        private readonly ParticleSystem?[] particles;
        private readonly Light?[] lights;
        private readonly int[] particleNodes;
        private readonly Color[] templateColors;
        private readonly Color[] templateEmissions;
        private readonly Color[] shownColors;
        private readonly Color[] shownEmissions;
        private readonly bool[] tinted;
        private readonly CosmeticNodeFlags[] shownFlags;
        private readonly int[] fromSlots;
        private readonly int[] toSlots;
        private readonly bool[] touched;
        private readonly List<CosmeticNodeChange> fromChanges = new List<CosmeticNodeChange>();
        private readonly List<CosmeticNodeChange> toChanges = new List<CosmeticNodeChange>();
        private List<int> touching = new List<int>();
        private List<int> touchedBefore = new List<int>();
        private byte[]? decodedFrom;
        private byte[]? decodedTo;

        internal Transform Root { get; }

        internal CosmeticVisual(GameObject copy, RecordedCosmetic record)
        {
            Root = copy.transform;
            this.record = record;
            Transform[] copied = CosmeticNodes.Of(Root);
            int[] matches = CosmeticNodes.Align(record.NodeNames, copied);
            int count = matches.Length;
            nodes = new Transform?[count];
            renderers = new Renderer?[count];
            particles = new ParticleSystem?[count];
            lights = new Light?[count];
            templateColors = new Color[count];
            templateEmissions = new Color[count];
            for (int i = 0; i < count; i++)
            {
                if (matches[i] == CosmeticNodes.Unmatched) continue;
                Transform node = copied[matches[i]];
                nodes[i] = node;
                renderers[i] = node.TryGetComponent(out Renderer renderer) ? renderer : null;
                particles[i] = node.TryGetComponent(out ParticleSystem system) ? system : null;
                lights[i] = node.TryGetComponent(out Light light) ? light : null;
                templateColors[i] = renderer != null ? RendererColors.Main(renderer) : Color.white;
                templateEmissions[i] = renderer != null ? RendererColors.Emission(renderer) : Color.black;
            }
            particleNodes = Enumerable.Range(0, count).Where(i => particles[i] != null).ToArray();
            shownColors = (Color[])templateColors.Clone();
            shownEmissions = (Color[])templateEmissions.Clone();
            tinted = new bool[count];
            shownFlags = new CosmeticNodeFlags[count];
            fromSlots = Enumerable.Repeat(Unset, count).ToArray();
            toSlots = Enumerable.Repeat(Unset, count).ToArray();
            touched = new bool[count];
            for (int i = 0; i < count; i++) Show(i, record.Rest[i], record.Rest[i], 0f);
        }

        internal Transform? Node(int index) => index >= 0 && index < nodes.Length ? nodes[index] : null;

        internal void Pose(CosmeticPose from, CosmeticPose to, float blend, bool playing)
        {
            Decode(from.Changes, ref decodedFrom, fromChanges);
            Decode(to.Changes, ref decodedTo, toChanges);
            for (int i = 0; i < fromChanges.Count; i++) Mark(fromChanges[i].Node, fromSlots, i);
            for (int i = 0; i < toChanges.Count; i++) Mark(toChanges[i].Node, toSlots, i);
            foreach (int node in touching)
            {
                CosmeticNodeState start = fromSlots[node] != Unset ? fromChanges[fromSlots[node]].State : record.Rest[node];
                CosmeticNodeState end = toSlots[node] != Unset ? toChanges[toSlots[node]].State : record.Rest[node];
                Show(node, start, end, blend);
            }
            foreach (int node in touchedBefore)
                if (!touched[node]) Show(node, record.Rest[node], record.Rest[node], 0f);
            foreach (int node in touching)
            {
                fromSlots[node] = Unset;
                toSlots[node] = Unset;
                touched[node] = false;
            }
            (touchedBefore, touching) = (touching, touchedBefore);
            touching.Clear();
            foreach (int node in particleNodes) Drive(particles[node]!, (shownFlags[node] & CosmeticNodeFlags.Emitting) != 0, playing);
        }

        private void Decode(byte[] encoded, ref byte[]? decoded, List<CosmeticNodeChange> into)
        {
            if (ReferenceEquals(encoded, decoded)) return;
            CosmeticNodeChange.Decode(encoded, record, into);
            decoded = encoded;
        }

        private void Mark(int node, int[] slots, int slot)
        {
            slots[node] = slot;
            if (touched[node]) return;
            touched[node] = true;
            touching.Add(node);
        }

        private void Show(int index, CosmeticNodeState start, CosmeticNodeState end, float blend)
        {
            Transform? node = nodes[index];
            if (node == null) return;
            CosmeticNodeState lead = blend < LeadSwitch ? start : end;
            CosmeticNodeFlags flags = lead.Flags;
            if (index != 0)
            {
                node.SetLocalPositionAndRotation(Vector3.Lerp(start.Position, end.Position, blend), Quaternion.Slerp(start.Rotation, end.Rotation, blend));
                node.localScale = Vector3.Lerp(start.Scale, end.Scale, blend);
                bool active = (flags & CosmeticNodeFlags.Active) != 0;
                if (node.gameObject.activeSelf != active) node.gameObject.SetActive(active);
            }
            Renderer? renderer = renderers[index];
            if (renderer != null)
            {
                bool visible = (flags & CosmeticNodeFlags.Visible) != 0;
                if (renderer.enabled != visible) renderer.enabled = visible;
                if (renderer is TrailRenderer trail) trail.emitting = (flags & CosmeticNodeFlags.Trailing) != 0;
                Tint(index, renderer, lead.Color, lead.Emission);
            }
            Light? light = lights[index];
            if (light != null) light.enabled = (flags & CosmeticNodeFlags.Lit) != 0;
            shownFlags[index] = flags;
        }

        private void Tint(int index, Renderer renderer, Color color, Color emission)
        {
            if (Same(color, shownColors[index]) && Same(emission, shownEmissions[index])) return;
            shownColors[index] = color;
            shownEmissions[index] = emission;
            if (Same(color, templateColors[index]) && Same(emission, templateEmissions[index]))
            {
                if (tinted[index]) RendererColors.Clear(renderer);
                tinted[index] = false;
                return;
            }
            RendererColors.Apply(renderer, color, emission);
            tinted[index] = true;
        }

        private static void Drive(ParticleSystem system, bool emitting, bool playing)
        {
            if (!system.gameObject.activeInHierarchy) return;
            if (!playing)
            {
                if (!system.isPaused) system.Pause(false);
                return;
            }
            if (emitting)
            {
                if (system.isPaused || !system.isEmitting) system.Play(false);
                return;
            }
            if (system.isPaused) system.Play(false);
            if (system.isEmitting) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) <= ColorTolerance
            && Mathf.Abs(a.g - b.g) <= ColorTolerance
            && Mathf.Abs(a.b - b.b) <= ColorTolerance
            && Mathf.Abs(a.a - b.a) <= ColorTolerance;
    }
}
