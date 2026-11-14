// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.GameCamera;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class ReplayPuppet : IDisposable
    {
        private const float VoiceResyncTolerance = 0.12f;
        private const float SnapStep = 0.5f;
        private const float MaxRootCorrection = 2f;
        private const float MaxLimbCorrection = 1f;

        private readonly GameObject root;
        private readonly Transform[] joints;
        private readonly SkinnedMeshRenderer body;
        private readonly Material[] bodyMaterials;
        private readonly MeshRenderer? head;
        private readonly Material[] headMaterials;
        private readonly AudioSource voice;
        private readonly RigAppearance appearance;
        private VoiceSegment? playingSegment;
        private AudioClip? clip;
        private int appliedMaterial = -1;
        private Pose filteredRoot;
        private readonly Pose[] filteredLimbs = new Pose[LimbSample.Count];

        internal ReplayActor Actor { get; }
        internal Transform Body => root.transform;
        internal Pose Eyes { get; private set; }
        internal ActorFrame Current { get; private set; }
        internal bool Visible => root.activeSelf;

        internal ReplayPuppet(ReplayActor actor, RigAppearance appearance)
        {
            Actor = actor;
            this.appearance = appearance;
            RigSkeleton skeleton = actor.Skeleton;
            root = new GameObject($"_GenneCaster Replay {actor.Name}");
            UnityEngine.Object.DontDestroyOnLoad(root);
            joints = BuildJoints(skeleton, root.transform);
            bodyMaterials = (Material[])appearance.BodyMaterials.Clone();
            body = BuildBody(skeleton, appearance, root.transform, joints, bodyMaterials);
            headMaterials = (Material[])appearance.HeadMaterials.Clone();
            head = skeleton.HeadMeshVisible && appearance.HeadMesh != null
                ? Attach("Head", skeleton.HeadJoint, skeleton.Layer, appearance.HeadMesh, headMaterials)
                : null;
            if (appearance.FaceMesh != null) Attach("Face", skeleton.FaceJoint, skeleton.Layer, appearance.FaceMesh, appearance.FaceMaterials);
            voice = BuildVoice(skeleton, appearance, root.transform);
            root.SetActive(false);
        }

        internal void Pose(float time, float step, float rigLerp)
        {
            if (!Actor.TryFindSpan(time, ReplayRecording.SampleInterval, out int index, out float blend))
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }
            bool appeared = !root.activeSelf;
            if (appeared) root.SetActive(true);
            float follow = Follow(step, rigLerp, appeared);
            ActorFrame from = Actor.Frames[index];
            ActorFrame to = blend > 0f ? Actor.Frames[index + 1] : from;
            Transform transform = root.transform;

            var recordedRoot = new Pose(Vector3.Lerp(from.Position, to.Position, blend), PackedRotation.Blend(from.Rotation, to.Rotation, blend));
            var rawRoot = new Pose(Vector3.Lerp(from.RawPosition, to.RawPosition, blend), PackedRotation.Blend(from.RawRotation, to.RawRotation, blend));
            filteredRoot = Approach(filteredRoot, rawRoot, follow);
            Pose shownRoot = Vector3.Distance(filteredRoot.position, recordedRoot.position) <= MaxRootCorrection ? filteredRoot : recordedRoot;
            transform.SetPositionAndRotation(shownRoot.position, shownRoot.rotation);
            transform.localScale = Vector3.one * Mathf.Lerp(from.Scale, to.Scale, blend);
            PoseJoints(from, to, blend);

            var eyes = new Pose(
                transform.TransformPoint(Vector3.Lerp(from.EyePosition, to.EyePosition, blend)),
                transform.rotation * PackedRotation.Blend(from.EyeRotation, to.EyeRotation, blend));
            Eyes = CorrectLimbs(from, to, blend, follow, eyes);
            voice.transform.position = Eyes.position;
            Current = blend < 0.5f ? from : to;
            ApplyMaterial(Current.Material);
        }

        private Pose CorrectLimbs(ActorFrame from, ActorFrame to, float blend, float follow, Pose eyes)
        {
            for (int limb = 0; limb < LimbSample.Count; limb++)
                filteredLimbs[limb] = Approach(filteredLimbs[limb], LimbSample.Raw(from.Limbs[limb], to.Limbs[limb], blend), follow);
            CorrectArm(LimbSample.LeftHand, from, to, blend, RigSkeleton.IkLeftUpper, RigSkeleton.IkLeftLower, RigSkeleton.IkLeftHand);
            CorrectArm(LimbSample.RightHand, from, to, blend, RigSkeleton.IkRightUpper, RigSkeleton.IkRightLower, RigSkeleton.IkRightHand);
            return CorrectHead(from, to, blend, eyes);
        }

        private Pose CorrectHead(ActorFrame from, ActorFrame to, float blend, Pose eyes)
        {
            if (!TryLimbDelta(LimbSample.Head, from, to, blend, out Pose shown, out Pose target)) return eyes;
            Quaternion delta = WorldDelta(shown, target);
            Transform? headBone = Joint(RigSkeleton.IkHead);
            Vector3 pivot = headBone != null ? headBone.position : eyes.position;
            if (headBone != null) headBone.rotation = delta * headBone.rotation;
            return new Pose(pivot + delta * (eyes.position - pivot), delta * eyes.rotation);
        }

        private void CorrectArm(int limb, ActorFrame from, ActorFrame to, float blend, int upperJoint, int lowerJoint, int handJoint)
        {
            Transform? upper = Joint(upperJoint), lower = Joint(lowerJoint), hand = Joint(handJoint);
            if (upper == null || lower == null || hand == null) return;
            if (!TryLimbDelta(limb, from, to, blend, out Pose shown, out Pose target)) return;
            Transform transform = root.transform;
            Vector3 local = transform.InverseTransformPoint(hand.position);
            Vector3 reach = transform.TransformPoint(target.position + target.rotation * (Quaternion.Inverse(shown.rotation) * (local - shown.position)));
            Quaternion grip = WorldDelta(shown, target) * hand.rotation;
            TwoBoneIK.Solve(upper, lower, hand, reach);
            hand.rotation = grip;
        }

        private bool TryLimbDelta(int limb, ActorFrame from, ActorFrame to, float blend, out Pose shown, out Pose target)
        {
            shown = LimbSample.Shown(from.Limbs[limb], to.Limbs[limb], blend);
            target = filteredLimbs[limb];
            return Vector3.Distance(target.position, shown.position) <= MaxLimbCorrection;
        }

        private Quaternion WorldDelta(Pose shown, Pose target)
        {
            Quaternion rootRotation = root.transform.rotation;
            return rootRotation * target.rotation * Quaternion.Inverse(shown.rotation) * Quaternion.Inverse(rootRotation);
        }

        private Transform? Joint(int ikIndex)
        {
            int joint = Actor.Skeleton.IkJoints[ikIndex];
            return joint == RigSkeleton.NoJoint ? null : joints[joint];
        }

        private static Pose Approach(Pose current, Pose target, float follow) =>
            new Pose(Vector3.Lerp(current.position, target.position, follow), Quaternion.Slerp(current.rotation, target.rotation, follow));

        internal void UpdateVoice(float time, bool playing)
        {
            VoiceSegment? segment = playing && root.activeSelf ? Actor.Voice.At(time) : null;
            if (segment == null)
            {
                if (voice.isPlaying) voice.Stop();
                return;
            }
            float offset = time - segment.Start;
            if (segment != playingSegment) Load(segment);
            if (!voice.isPlaying)
            {
                voice.Play();
                voice.time = offset;
            }
            else if (Mathf.Abs(voice.time - offset) > VoiceResyncTolerance) voice.time = offset;
        }

        private static float Follow(float step, float rigLerp, bool appeared)
        {
            if (appeared || rigLerp >= 1f || step < 0f || step > SnapStep) return 1f;
            return 1f - Mathf.Pow(1f - rigLerp, step * Smoothing.ReferenceFrameRate);
        }

        private void PoseJoints(ActorFrame from, ActorFrame to, float blend)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].localRotation = PackedRotation.Blend(from.Joints[i], to.Joints[i], blend);
                joints[i].localPosition = PackedPosition.Blend(from.JointPositions[i], to.JointPositions[i], blend);
                joints[i].localScale = Vector3.one * Mathf.Lerp(Mathf.HalfToFloat(from.JointScales[i]), Mathf.HalfToFloat(to.JointScales[i]), blend);
            }
        }

        private void ApplyMaterial(int index)
        {
            if (index == appliedMaterial || bodyMaterials.Length == 0) return;
            appliedMaterial = index;
            Material[] tags = appearance.TagMaterials;
            Material skin = index > 0 && index < tags.Length && tags[index] != null ? tags[index] : appearance.BodyMaterials[0];
            bodyMaterials[0] = skin;
            body.sharedMaterials = bodyMaterials;
            if (head == null || !appearance.HeadSharesSkin || headMaterials.Length == 0) return;
            headMaterials[0] = skin;
            head.sharedMaterials = headMaterials;
        }

        private void Load(VoiceSegment segment)
        {
            voice.Stop();
            ReleaseClip();
            float[] samples = segment.ToFloats();
            int position = 0;
            clip = AudioClip.Create(
                $"Replay {Actor.Name}", samples.Length, 1, segment.SampleRate, false,
                data =>
                {
                    int count = Mathf.Clamp(samples.Length - position, 0, data.Length);
                    Array.Copy(samples, position, data, 0, count);
                    Array.Clear(data, count, data.Length - count);
                    position += count;
                },
                newPosition => position = newPosition);
            voice.clip = clip;
            playingSegment = segment;
        }

        private void ReleaseClip()
        {
            if (clip != null) UnityEngine.Object.Destroy(clip);
            clip = null;
            playingSegment = null;
        }

        private static Transform[] BuildJoints(RigSkeleton skeleton, Transform root)
        {
            var built = new Transform[skeleton.JointCount];
            for (int i = 0; i < built.Length; i++) built[i] = new GameObject(skeleton.JointNames[i]).transform;
            for (int i = 0; i < built.Length; i++)
            {
                int parent = skeleton.Parents[i];
                built[i].SetParent(parent == RigSkeleton.RootParent ? root : built[parent], false);
                built[i].localPosition = skeleton.RestPositions[i];
                built[i].localScale = Vector3.one * skeleton.RestScales[i];
            }
            return built;
        }

        private static SkinnedMeshRenderer BuildBody(RigSkeleton skeleton, RigAppearance appearance, Transform root, Transform[] joints, Material[] materials)
        {
            var go = new GameObject("Body") { layer = skeleton.Layer };
            go.transform.SetParent(root, false);
            SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = appearance.BodyMesh;
            renderer.bones = joints.Take(skeleton.BoneCount).ToArray();
            renderer.rootBone = skeleton.RootBone >= 0 ? joints[skeleton.RootBone] : root;
            renderer.localBounds = skeleton.BodyBounds;
            renderer.updateWhenOffscreen = true;
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private MeshRenderer? Attach(string name, int joint, int layer, Mesh mesh, Material[] materials)
        {
            if (joint == RigSkeleton.NoJoint) return null;
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(joints[joint], false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private static AudioSource BuildVoice(RigSkeleton skeleton, RigAppearance appearance, Transform root)
        {
            var go = new GameObject("Voice");
            go.transform.SetParent(root, false);
            AudioSource source = go.AddComponent<AudioSource>();
            skeleton.Voice.ApplyTo(source, appearance.VoiceMixer);
            return source;
        }

        public void Dispose()
        {
            ReleaseClip();
            if (root != null) UnityEngine.Object.Destroy(root);
            appearance.Dispose();
        }
    }
}
