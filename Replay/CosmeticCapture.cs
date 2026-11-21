// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class CosmeticCapture : IDisposable
    {
        private sealed class WornItem
        {
            internal readonly CosmeticSO Cosmetic;
            internal readonly ECosmeticSelectSide Side;
            internal readonly CosmeticKey? Key;
            internal readonly bool StandsInForWearables;
            internal readonly GorillaSkinToggle? SkinToggle;
            internal LiveCosmetic? Live;

            internal WornItem(CosmeticSO cosmetic, ECosmeticSelectSide side, CosmeticKey? key, bool standsInForWearables, GorillaSkinToggle? skinToggle)
            {
                Cosmetic = cosmetic;
                Side = side;
                Key = key;
                StandsInForWearables = standsInForWearables;
                SkinToggle = skinToggle;
            }

            internal string PlayFabId => Cosmetic.info.playFabID;
        }

        private readonly VRRig rig;
        private readonly ReplayActor actor;
        private readonly Transform[] joints;
        private readonly Dictionary<Transform, int> jointIndices;
        private readonly int[] reachJoints;
        private readonly Dictionary<GameObject, WornItem?> identified = new Dictionary<GameObject, WornItem?>();
        private readonly Dictionary<(string PlayFabId, ECosmeticSelectSide Side), WornItem> standIns = new Dictionary<(string, ECosmeticSelectSide), WornItem>();
        private readonly List<LiveCosmetic> lives = new List<LiveCosmetic>();
        private readonly HashSet<CosmeticSoundTrack> claimed = new HashSet<CosmeticSoundTrack>();
        private readonly List<CosmeticPose> poses = new List<CosmeticPose>();
        private readonly MemoryStream scratch = new MemoryStream();
        private string skinSource = string.Empty;

        internal CosmeticCapture(VRRig rig, ReplayActor actor, Transform[] joints)
        {
            this.rig = rig;
            this.actor = actor;
            this.joints = joints;
            jointIndices = new Dictionary<Transform, int>(joints.Length);
            for (int i = 0; i < joints.Length; i++) jointIndices[joints[i]] = i;
            RigSkeleton skeleton = actor.Skeleton;
            reachJoints = new[]
                {
                    skeleton.IkJoints[RigSkeleton.IkLeftHand],
                    skeleton.IkJoints[RigSkeleton.IkRightHand],
                    skeleton.IkJoints[RigSkeleton.IkHead],
                    skeleton.RootBone,
                }
                .Where(joint => joint >= 0 && joint < joints.Length)
                .ToArray();
        }

        internal CosmeticPose[] Capture()
        {
            poses.Clear();
            standIns.Clear();
            skinSource = string.Empty;
            Transform[] bones = GTHardCodedBones.TryGetBoneXforms(rig, out Transform[] found, out _) ? found : Array.Empty<Transform>();
            Collect(rig.cosmetics, false, bones);
            Collect(rig.overrideCosmetics, true, bones);
            foreach (WornItem standIn in standIns.Values) AddStandIn(standIn, bones);
            return poses.Count == 0 ? Array.Empty<CosmeticPose>() : poses.ToArray();
        }

        internal int CaptureSkin()
        {
            GorillaSkin? skin = GorillaSkin.GetActiveSkin(rig, out bool useDefault);
            if (useDefault || skin == null) return RecordedSkin.None;
            bool fromCosmetic = skinSource.Length > 0 && skin == rig.CurrentCosmeticSkin;
            string assetName = fromCosmetic ? string.Empty : skin.name;
            if (!fromCosmetic && assetName.Length == 0) return RecordedSkin.None;
            var recorded = new RecordedSkin(
                fromCosmetic ? skinSource : string.Empty,
                assetName,
                new PackedColor(RendererColors.MaterialMain(skin.bodyMaterial)),
                new PackedColor(RendererColors.MaterialMain(skin.chestMaterial)));
            return actor.SkinIndex(recorded);
        }

        internal void Drain(float audioNow, ref float[] buffer, int sampleRate)
        {
            ForgetDestroyed();
            foreach (LiveCosmetic live in lives) live.Drain(audioNow, ref buffer, sampleRate);
        }

        private void Collect(List<GameObject>? items, bool overrides, Transform[] bones)
        {
            if (items == null) return;
            foreach (GameObject item in items)
            {
                if (item == null || !item.activeInHierarchy) continue;
                WornItem? worn = Identify(item, overrides, bones);
                if (worn == null) continue;
                if (worn.SkinToggle != null && worn.SkinToggle.applied) skinSource = worn.PlayFabId;
                if (worn.Key is CosmeticKey key) AddLive(item, worn, key);
                else if (worn.StandsInForWearables) standIns[(worn.PlayFabId, worn.Side)] = worn;
            }
        }

        private WornItem? Identify(GameObject item, bool overrides, Transform[] bones)
        {
            if (identified.TryGetValue(item, out WornItem? known)) return known;
            WornItem? worn = null;
            if (CosmeticCatalog.TryParse(item.name, out string playFabId, out ECosmeticSelectSide side)
                && CosmeticCatalog.Find(playFabId) is CosmeticSO cosmetic
                && CosmeticCatalog.IsAttached(cosmetic))
            {
                Transform parent = item.transform.parent;
                CosmeticKey? key = overrides ? AnchoredKey(cosmetic, CosmeticKind.LocalRig, side, parent, bones, true)
                    : item.GetComponent<HoldableObject>() != null ? HoldableKey(cosmetic, side)
                    : AnchoredKey(cosmetic, CosmeticKind.Wearable, side, parent, bones, false);
                bool standsIn = overrides && key == null && !HasLoadableParts(cosmetic, CosmeticKind.LocalRig);
                GorillaSkinToggle? toggle = item.GetComponentInChildren<GorillaSkinToggle>(true);
                if (key != null || standsIn || toggle != null) worn = new WornItem(cosmetic, side, key, standsIn, toggle);
            }
            identified[item] = worn;
            return worn;
        }

        private static CosmeticKey? HoldableKey(CosmeticSO cosmetic, ECosmeticSelectSide side)
        {
            int part = CosmeticCatalog.HoldablePart(cosmetic, side);
            return part < 0 ? null : new CosmeticKey(cosmetic.info.playFabID, CosmeticKind.Holdable, part, 0);
        }

        private static CosmeticKey? AnchoredKey(CosmeticSO cosmetic, CosmeticKind kind, ECosmeticSelectSide side, Transform? parent, Transform[] bones, bool exactOnly)
        {
            CosmeticPart[] parts = CosmeticCatalog.PartsOf(cosmetic, kind);
            CosmeticKey? fallback = null;
            for (int part = 0; part < parts.Length; part++)
            {
                CosmeticAttachInfo[]? anchors = parts[part].attachAnchors;
                if (anchors == null || !CosmeticCatalog.Loadable(parts[part])) continue;
                for (int anchor = 0; anchor < anchors.Length; anchor++)
                {
                    if (anchors[anchor].selectSide.Value != side) continue;
                    var key = new CosmeticKey(cosmetic.info.playFabID, kind, part, anchor);
                    int bone = (int)anchors[anchor].parentBone;
                    if (parent != null && bone > 0 && bone < bones.Length && bones[bone] == parent) return key;
                    fallback ??= key;
                }
            }
            return exactOnly ? null : fallback;
        }

        private static bool HasLoadableParts(CosmeticSO cosmetic, CosmeticKind kind) =>
            CosmeticCatalog.PartsOf(cosmetic, kind).Any(CosmeticCatalog.Loadable);

        private void AddLive(GameObject item, WornItem worn, CosmeticKey key)
        {
            int index = actor.CosmeticIndex(key);
            if (index > ushort.MaxValue) return;
            if (worn.Live == null)
            {
                worn.Live = new LiveCosmetic(item, actor, index);
                worn.Live.Listen(actor, claimed);
                lives.Add(worn.Live);
            }
            Transform transform = item.transform;
            int joint = JointAbove(transform);
            if (joint == RigSkeleton.RootParent && key.Kind == CosmeticKind.Holdable) joint = ClosestReachJoint(transform.position);
            poses.Add(Pose(index, joint, transform.localToWorldMatrix, worn.Live.Changes(scratch)));
        }

        private void AddStandIn(WornItem worn, Transform[] bones)
        {
            CosmeticPart[] parts = CosmeticCatalog.PartsOf(worn.Cosmetic, CosmeticKind.Wearable);
            for (int part = 0; part < parts.Length; part++)
            {
                CosmeticAttachInfo[]? anchors = parts[part].attachAnchors;
                if (anchors == null || !CosmeticCatalog.Loadable(parts[part])) continue;
                for (int anchor = 0; anchor < anchors.Length; anchor++)
                {
                    CosmeticAttachInfo attach = anchors[anchor];
                    int bone = (int)attach.parentBone;
                    if (attach.selectSide.Value != worn.Side || bone <= 0 || bone >= bones.Length || bones[bone] == null) continue;
                    int index = actor.CosmeticIndex(new CosmeticKey(worn.PlayFabId, CosmeticKind.Wearable, part, anchor));
                    if (index > ushort.MaxValue) continue;
                    XformOffset offset = attach.offset;
                    Matrix4x4 world = bones[bone].localToWorldMatrix * Matrix4x4.TRS(offset.pos, offset.rot, offset.scale);
                    poses.Add(Pose(index, JointAbove(bones[bone]), world, Array.Empty<byte>()));
                }
            }
        }

        private CosmeticPose Pose(int index, int joint, Matrix4x4 world, byte[] changes)
        {
            Transform space = joint == RigSkeleton.RootParent ? rig.transform : joints[joint];
            return CosmeticPose.FromMatrix(index, joint, space.worldToLocalMatrix * world, changes);
        }

        private int JointAbove(Transform start)
        {
            Transform root = rig.transform;
            for (Transform? current = start; current != null && current != root; current = current.parent)
                if (jointIndices.TryGetValue(current, out int joint)) return joint;
            return RigSkeleton.RootParent;
        }

        private int ClosestReachJoint(Vector3 position)
        {
            int closest = RigSkeleton.RootParent;
            float closestDistance = float.PositiveInfinity;
            foreach (int joint in reachJoints)
            {
                float distance = (joints[joint].position - position).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = joint;
            }
            return closest;
        }

        private void ForgetDestroyed()
        {
            if (!lives.Any(live => live.Root == null)) return;
            for (int i = lives.Count - 1; i >= 0; i--)
            {
                if (lives[i].Root != null) continue;
                lives[i].Release(claimed);
                lives.RemoveAt(i);
            }
            foreach (GameObject gone in identified.Keys.Where(item => item == null).ToArray()) identified.Remove(gone);
        }

        public void Dispose()
        {
            foreach (LiveCosmetic live in lives) live.Release(claimed);
            lives.Clear();
            scratch.Dispose();
        }
    }
}
