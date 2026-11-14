// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Player;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace GenneCaster.Replay
{
    internal sealed class RigAppearance : IDisposable
    {
        private static readonly FieldInfo? VoiceField = AccessTools.Field(typeof(VRRig), "voiceAudio");

        internal Mesh BodyMesh { get; }
        internal Material[] BodyMaterials { get; }
        internal Material[] TagMaterials { get; }
        internal Mesh? FaceMesh { get; }
        internal Material[] FaceMaterials { get; }
        internal Mesh? HeadMesh { get; }
        internal Material[] HeadMaterials { get; }
        internal bool HeadSharesSkin { get; }
        internal AudioMixerGroup? VoiceMixer { get; }

        private RigAppearance(Mesh bodyMesh, Material[] bodyMaterials, Material[] tagMaterials, Mesh? faceMesh, Material[] faceMaterials, HeadParts head, AudioMixerGroup? voiceMixer)
        {
            HeadMesh = head.Mesh;
            HeadMaterials = head.Materials ?? Array.Empty<Material>();
            HeadSharesSkin = head.SharesSkin;
            BodyMesh = bodyMesh;
            BodyMaterials = bodyMaterials;
            TagMaterials = tagMaterials;
            FaceMesh = faceMesh;
            FaceMaterials = faceMaterials;
            VoiceMixer = voiceMixer;
        }

        internal static AudioSource? VoiceOf(VRRig rig) => VoiceField?.GetValue(rig) as AudioSource;

        internal static SkinnedMeshRenderer? ActiveBody(VRRig rig)
        {
            SkinnedMeshRenderer? active = rig.bodyRenderer != null ? rig.bodyRenderer.ActiveBody : null;
            return active != null ? active : rig.mainSkin;
        }

        internal static RigAppearance? Resolve(RigSkeleton skeleton, Color color)
        {
            VRRig? template = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            if (template == null) return null;
            SkinnedMeshRenderer? body = FindBody(template, skeleton);
            if (body == null) return null;
            Material[] tagMaterials = Copy(template.materialsToChangeTo);
            Material[] bodyMaterials = Copy(body.sharedMaterials);
            Material? skin = template.materialsToChangeTo != null && template.materialsToChangeTo.Length > 0 && template.materialsToChangeTo[0] != null
                ? new Material(template.materialsToChangeTo[0])
                : null;
            if (bodyMaterials.Length > 0)
            {
                if (skin != null)
                {
                    if (bodyMaterials[0] != null) UnityEngine.Object.Destroy(bodyMaterials[0]);
                    bodyMaterials[0] = skin;
                }
                if (bodyMaterials[0] != null) bodyMaterials[0].color = color;
            }
            else if (skin != null) UnityEngine.Object.Destroy(skin);
            MeshRenderer? face = template.faceSkin;
            MeshFilter? faceFilter = face != null ? face.GetComponent<MeshFilter>() : null;
            bool hasFace = skeleton.FaceJoint != RigSkeleton.NoJoint && faceFilter != null && faceFilter.sharedMesh != null;
            return new RigAppearance(
                body.sharedMesh,
                bodyMaterials,
                tagMaterials,
                hasFace ? faceFilter!.sharedMesh : null,
                hasFace ? Copy(face!.sharedMaterials) : Array.Empty<Material>(),
                skeleton.HeadMeshVisible ? HeadPartsOf(template, body, bodyMaterials) : default,
                FindVoiceMixer());
        }

        private readonly struct HeadParts
        {
            internal readonly Mesh? Mesh;
            internal readonly Material[] Materials;
            internal readonly bool SharesSkin;

            internal HeadParts(Mesh mesh, Material[] materials, bool sharesSkin)
            {
                Mesh = mesh;
                Materials = materials;
                SharesSkin = sharesSkin;
            }
        }

        private static HeadParts HeadPartsOf(VRRig template, SkinnedMeshRenderer body, Material[] bodyMaterials)
        {
            GameObject? headMesh = template.headMesh;
            MeshFilter? filter = headMesh != null ? headMesh.GetComponent<MeshFilter>() : null;
            MeshRenderer? renderer = headMesh != null ? headMesh.GetComponent<MeshRenderer>() : null;
            if (filter == null || filter.sharedMesh == null || renderer == null) return default;
            Material[] source = renderer.sharedMaterials;
            Material[] bodySource = body.sharedMaterials;
            bool sharesSkin = source.Length > 0 && bodySource.Length > 0 && source[0] != null && source[0] == bodySource[0] && bodyMaterials.Length > 0;
            Material[] materials = Copy(source);
            if (sharesSkin)
            {
                if (materials[0] != null) UnityEngine.Object.Destroy(materials[0]);
                materials[0] = bodyMaterials[0];
            }
            return new HeadParts(filter.sharedMesh, materials, sharesSkin);
        }

        private static SkinnedMeshRenderer? FindBody(VRRig template, RigSkeleton skeleton)
        {
            var candidates = new List<SkinnedMeshRenderer?> { ActiveBody(template), template.mainSkin };
            if (template.bodyRenderer != null)
                foreach (GorillaBodyType type in Enum.GetValues(typeof(GorillaBodyType)))
                    candidates.Add(SafeBody(template.bodyRenderer, type));
            SkinnedMeshRenderer? fallback = null;
            foreach (SkinnedMeshRenderer? candidate in candidates)
            {
                if (candidate == null || candidate.sharedMesh == null || candidate.sharedMesh.bindposes.Length != skeleton.BoneCount) continue;
                if (candidate.sharedMesh.name == skeleton.BodyMeshName) return candidate;
                fallback ??= candidate;
            }
            return fallback;
        }

        private static SkinnedMeshRenderer? SafeBody(GorillaBodyRenderer renderer, GorillaBodyType type)
        {
            try { return renderer.GetBody(type); }
            catch (Exception e)
            {
                Log.ExceptionOnce($"replay-body-{type}", e, $"Reading the {type} body failed");
                return null;
            }
        }

        private static AudioMixerGroup? FindVoiceMixer()
        {
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                AudioSource? voice = rig != null ? VoiceOf(rig) : null;
                if (voice != null && voice.outputAudioMixerGroup != null) return voice.outputAudioMixerGroup;
            }
            return null;
        }

        private static Material[] Copy(Material[]? materials) =>
            materials == null
                ? Array.Empty<Material>()
                : materials.Select(m => m != null ? new Material(m) : null!).ToArray();

        public void Dispose()
        {
            IEnumerable<Material> ownHead = HeadSharesSkin ? HeadMaterials.Skip(1) : HeadMaterials;
            foreach (Material material in BodyMaterials.Concat(TagMaterials).Concat(FaceMaterials).Concat(ownHead))
                if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
