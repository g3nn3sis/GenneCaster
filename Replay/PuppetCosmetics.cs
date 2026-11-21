// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class PuppetCosmetics : IDisposable
    {
        private const float LeadSwitch = 0.5f;

        private readonly ReplayActor actor;
        private readonly Transform root;
        private readonly Transform[] joints;
        private readonly int layer;
        private readonly CosmeticLibrary library;
        private readonly Dictionary<int, List<CosmeticVisual>> worn = new Dictionary<int, List<CosmeticVisual>>();
        private readonly Dictionary<int, int> shown = new Dictionary<int, int>();
        private readonly List<(CosmeticSoundTrack Sound, TrackPlayer Player)> sounds = new List<(CosmeticSoundTrack, TrackPlayer)>();

        internal PuppetCosmetics(ReplayActor actor, Transform root, Transform[] joints, int layer, CosmeticLibrary library)
        {
            this.actor = actor;
            this.root = root;
            this.joints = joints;
            this.layer = layer;
            this.library = library;
            foreach (CosmeticSoundTrack sound in actor.Sounds) sounds.Add((sound, BuildSound(sound)));
        }

        internal void Pose(ActorFrame from, ActorFrame to, float blend, bool playing)
        {
            bool fromLeads = blend < LeadSwitch;
            CosmeticPose[] lead = fromLeads ? from.Cosmetics : to.Cosmetics;
            CosmeticPose[] trail = fromLeads ? to.Cosmetics : from.Cosmetics;
            shown.Clear();
            foreach (CosmeticPose pose in lead)
            {
                shown.TryGetValue(pose.Key, out int occurrence);
                shown[pose.Key] = occurrence + 1;
                CosmeticVisual? visual = Visual(pose.Key, occurrence);
                if (visual == null) continue;
                Place(visual.Root, pose.Joint);
                if (TryMatch(trail, pose, occurrence, out CosmeticPose partner))
                {
                    CosmeticPose start = fromLeads ? pose : partner;
                    CosmeticPose end = fromLeads ? partner : pose;
                    CosmeticPose.Apply(visual.Root, start, end, blend);
                    visual.Pose(start, end, blend, playing);
                }
                else
                {
                    CosmeticPose.Apply(visual.Root, pose, pose, 0f);
                    visual.Pose(pose, pose, 0f, playing);
                }
            }
            HideUnshown();
        }

        internal void UpdateSounds(float time, bool audible)
        {
            foreach ((CosmeticSoundTrack sound, TrackPlayer player) in sounds)
            {
                Transform? node = ShownNode(sound.Cosmetic, sound.Node);
                player.Transform.position = node != null ? node.position : root.position;
                player.Update(sound.Track, time, audible);
            }
        }

        private TrackPlayer BuildSound(CosmeticSoundTrack sound)
        {
            var holder = new GameObject("Cosmetic Sound");
            holder.transform.SetParent(root, false);
            AudioSource source = holder.AddComponent<AudioSource>();
            sound.Settings.ApplyTo(source, library.Mixer(sound.Mixer));
            return new TrackPlayer(source, $"Replay {actor.Name} cosmetic");
        }

        private Transform? ShownNode(int cosmetic, int node)
        {
            if (!worn.TryGetValue(cosmetic, out List<CosmeticVisual> visuals) || visuals.Count == 0) return null;
            CosmeticVisual first = visuals[0];
            return first.Root.gameObject.activeSelf ? first.Node(node) : null;
        }

        private CosmeticVisual? Visual(int key, int occurrence)
        {
            if (!worn.TryGetValue(key, out List<CosmeticVisual> visuals))
            {
                visuals = new List<CosmeticVisual>();
                worn.Add(key, visuals);
            }
            while (visuals.Count <= occurrence)
            {
                RecordedCosmetic cosmetic = actor.Cosmetics[key];
                GameObject? template = library.Template(cosmetic.Key);
                if (template == null) return null;
                GameObject copy = UnityEngine.Object.Instantiate(template, root, false);
                foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
                visuals.Add(new CosmeticVisual(copy, cosmetic));
            }
            return visuals[occurrence];
        }

        private void Place(Transform visual, int joint)
        {
            Transform parent = joint == RigSkeleton.RootParent ? root : joints[joint];
            if (visual.parent != parent) visual.SetParent(parent, false);
            if (!visual.gameObject.activeSelf) visual.gameObject.SetActive(true);
        }

        private static bool TryMatch(CosmeticPose[] poses, CosmeticPose pose, int occurrence, out CosmeticPose match)
        {
            int seen = 0;
            foreach (CosmeticPose candidate in poses)
            {
                if (candidate.Key != pose.Key || seen++ != occurrence) continue;
                match = candidate;
                return candidate.Joint == pose.Joint;
            }
            match = pose;
            return false;
        }

        private void HideUnshown()
        {
            foreach (KeyValuePair<int, List<CosmeticVisual>> pair in worn)
            {
                shown.TryGetValue(pair.Key, out int count);
                for (int i = count; i < pair.Value.Count; i++)
                {
                    GameObject visual = pair.Value[i].Root.gameObject;
                    if (visual.activeSelf) visual.SetActive(false);
                }
            }
        }

        public void Dispose()
        {
            foreach ((CosmeticSoundTrack _, TrackPlayer player) in sounds) player.Dispose();
            sounds.Clear();
        }
    }
}
