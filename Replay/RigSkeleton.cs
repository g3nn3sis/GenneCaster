// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class RigSkeleton
    {
        internal const int RootParent = -1;
        internal const int NoJoint = -1;
        internal const int IkHead = 0;
        internal const int IkLeftUpper = 1;
        internal const int IkLeftLower = 2;
        internal const int IkLeftHand = 3;
        internal const int IkRightUpper = 4;
        internal const int IkRightLower = 5;
        internal const int IkRightHand = 6;
        private const int IkJointCount = 7;

        private static readonly FieldInfo? IkField = AccessTools.Field(typeof(VRRig), "myIk");

        internal string BodyMeshName { get; }
        internal int BoneCount { get; }
        internal string[] JointNames { get; }
        internal int[] Parents { get; }
        internal Vector3[] RestPositions { get; }
        internal float[] RestScales { get; }
        internal int RootBone { get; }
        internal int Layer { get; }
        internal Bounds BodyBounds { get; }
        internal int FaceJoint { get; }
        internal int HeadJoint { get; }
        internal bool HeadMeshVisible { get; }
        internal VoiceSettings Voice { get; }
        internal int[] IkJoints { get; }
        internal int JointCount => Parents.Length;

        private RigSkeleton(string bodyMeshName, int boneCount, string[] jointNames, int[] parents, Vector3[] restPositions, float[] restScales, int rootBone, int layer, Bounds bodyBounds, int faceJoint, int headJoint, bool headMeshVisible, VoiceSettings voice, int[] ikJoints)
        {
            IkJoints = ikJoints;
            BodyMeshName = bodyMeshName;
            BoneCount = boneCount;
            JointNames = jointNames;
            Parents = parents;
            RestPositions = restPositions;
            RestScales = restScales;
            RootBone = rootBone;
            Layer = layer;
            BodyBounds = bodyBounds;
            FaceJoint = faceJoint;
            HeadJoint = headJoint;
            HeadMeshVisible = headMeshVisible;
            Voice = voice;
        }

        internal static bool TryCapture(VRRig rig, AudioSource? voice, out RigSkeleton? skeleton, out Transform[] joints)
        {
            skeleton = null;
            joints = Array.Empty<Transform>();
            SkinnedMeshRenderer? body = RigAppearance.ActiveBody(rig);
            if (body == null || body.sharedMesh == null) return false;
            Transform[] bones = body.bones;
            if (bones == null || bones.Length == 0 || bones.Any(b => b == null)) return false;
            Transform root = rig.transform;
            var list = new List<Transform>(bones);
            var headChain = new HashSet<Transform>();
            Transform?[] ik = IkTransforms(rig);
            foreach (Transform? anchor in HeadAnchors(rig).Concat(ik)) AddChain(anchor, root, list, headChain);
            Transform[] all = list.ToArray();
            int[] parents = all.Select(j => NearestJointAncestor(all, root, j)).ToArray();
            var restPositions = new Vector3[all.Length];
            var restScales = new float[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                Transform parent = parents[i] == RootParent ? root : all[parents[i]];
                restPositions[i] = parent.InverseTransformPoint(all[i].position);
                restScales[i] = RelativeScale(all[i], parent);
            }
            joints = all;
            GameObject? headMesh = rig.headMesh;
            skeleton = new RigSkeleton(
                body.sharedMesh.name,
                bones.Length,
                all.Select(j => j.name).ToArray(),
                parents,
                restPositions,
                restScales,
                body.rootBone != null ? Array.IndexOf(all, body.rootBone) : RootParent,
                body.gameObject.layer,
                body.localBounds,
                rig.faceSkin != null ? Array.IndexOf(all, rig.faceSkin.transform) : NoJoint,
                headMesh != null ? Array.IndexOf(all, headMesh.transform) : NoJoint,
                IsRendered(headMesh),
                VoiceSettings.From(voice),
                ik.Select(t => t != null ? Array.IndexOf(all, t) : NoJoint).ToArray());
            return true;
        }

        internal bool Matches(Transform[] joints) => joints.Length == JointCount;

        internal Transform ParentOf(Transform[] joints, Transform root, int index) =>
            Parents[index] == RootParent ? root : joints[Parents[index]];

        internal void Write(BinaryWriter writer)
        {
            writer.Write(BodyMeshName);
            writer.Write(BoneCount);
            writer.Write(JointCount);
            for (int i = 0; i < JointCount; i++)
            {
                writer.Write(JointNames[i]);
                writer.Write(Parents[i]);
                ReplayIO.Write(writer, RestPositions[i]);
                writer.Write(RestScales[i]);
            }
            writer.Write(RootBone);
            writer.Write(Layer);
            ReplayIO.Write(writer, BodyBounds.center);
            ReplayIO.Write(writer, BodyBounds.size);
            writer.Write(FaceJoint);
            writer.Write(HeadJoint);
            writer.Write(HeadMeshVisible);
            Voice.Write(writer);
            foreach (int joint in IkJoints) writer.Write(joint);
        }

        internal static RigSkeleton Read(BinaryReader reader)
        {
            string bodyMeshName = reader.ReadString();
            int boneCount = ReplayIO.ReadCount(reader);
            int count = ReplayIO.ReadCount(reader);
            if (boneCount > count) throw new InvalidDataException("More bones than joints.");
            var names = new string[count];
            var parents = new int[count];
            var restPositions = new Vector3[count];
            var restScales = new float[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = reader.ReadString();
                parents[i] = ReadJoint(reader, count, RootParent);
                restPositions[i] = ReplayIO.ReadVector3(reader);
                restScales[i] = reader.ReadSingle();
            }
            int rootBone = ReadJoint(reader, count, RootParent);
            int layer = reader.ReadInt32();
            var bounds = new Bounds(ReplayIO.ReadVector3(reader), ReplayIO.ReadVector3(reader));
            int faceJoint = ReadJoint(reader, count, NoJoint);
            int headJoint = ReadJoint(reader, count, NoJoint);
            bool headMeshVisible = reader.ReadBoolean();
            VoiceSettings voice = VoiceSettings.Read(reader);
            var ikJoints = new int[IkJointCount];
            for (int i = 0; i < ikJoints.Length; i++) ikJoints[i] = ReadJoint(reader, count, NoJoint);
            return new RigSkeleton(bodyMeshName, boneCount, names, parents, restPositions, restScales, rootBone, layer, bounds, faceJoint, headJoint, headMeshVisible, voice, ikJoints);
        }

        private static int ReadJoint(BinaryReader reader, int count, int none)
        {
            int index = reader.ReadInt32();
            if (index != none && (index < 0 || index >= count)) throw new InvalidDataException("Joint index out of range.");
            return index;
        }

        private static IEnumerable<Transform?> HeadAnchors(VRRig rig)
        {
            yield return rig.head?.rigTarget;
            yield return rig.headConstraint;
            yield return rig.headMesh != null ? rig.headMesh.transform : null;
            yield return rig.faceSkin != null ? rig.faceSkin.transform : null;
        }

        private static Transform?[] IkTransforms(VRRig rig)
        {
            if (IkField?.GetValue(rig) is not GorillaIK ik || ik == null) return new Transform?[IkJointCount];
            return new[] { ik.headBone, ik.leftUpperArm, ik.leftLowerArm, ik.leftHand, ik.rightUpperArm, ik.rightLowerArm, ik.rightHand };
        }

        private static void AddChain(Transform? anchor, Transform root, List<Transform> joints, HashSet<Transform> chain)
        {
            if (anchor == null || anchor == root || !anchor.IsChildOf(root)) return;
            for (Transform current = anchor; current != root; current = current.parent)
            {
                if (!chain.Add(current)) return;
                if (!joints.Contains(current)) joints.Add(current);
            }
        }

        private static bool IsRendered(GameObject? headMesh)
        {
            if (headMesh == null || !headMesh.activeInHierarchy) return false;
            MeshRenderer? renderer = headMesh.GetComponent<MeshRenderer>();
            MeshFilter? filter = headMesh.GetComponent<MeshFilter>();
            return renderer != null && renderer.enabled && filter != null && filter.sharedMesh != null;
        }

        private static int NearestJointAncestor(Transform[] joints, Transform root, Transform start)
        {
            for (Transform? current = start.parent; current != null && current != root; current = current.parent)
            {
                int index = Array.IndexOf(joints, current);
                if (index >= 0) return index;
            }
            return RootParent;
        }

        internal static float RelativeScale(Transform child, Transform parent)
        {
            float parentScale = parent.lossyScale.x;
            return Mathf.Approximately(parentScale, 0f) ? 1f : child.lossyScale.x / parentScale;
        }
    }
}
