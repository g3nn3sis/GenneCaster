// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player;
using Zetori.Replay;
using UnityEngine;

namespace Zetori.GameCamera.Webs
{
    internal static class WebSubjects
    {
        private const float ChaseRange = 10f;
        private const float ChasedRunnerInterest = 3f;
        private const float ChasingTaggerInterest = 2f;
        private const float TaggerInterest = 1.25f;
        private const float RunnerInterest = 1f;

        internal static void Collect(List<WebSubject> subjects)
        {
            subjects.Clear();
            if (ReplaySession.Player is ReplayPlayer replay) AddPuppets(subjects, replay);
            else AddRigs(subjects);
            for (int i = 0; i < subjects.Count; i++)
            {
                WebSubject subject = subjects[i];
                subjects[i] = new WebSubject(subject.Body, subject.Position, subject.Tagged, InterestOf(subject, subjects));
            }
        }

        private static void AddRigs(List<WebSubject> subjects)
        {
            VRRig[]? rigs = NetworkedPlayerManager.activeRigs;
            if (rigs == null) return;
            AddRigs(subjects, rigs, true);
            if (subjects.Count == 0) AddRigs(subjects, rigs, false);
        }

        private static void AddRigs(List<WebSubject> subjects, VRRig[] rigs, bool othersOnly)
        {
            foreach (VRRig rig in rigs)
            {
                if (rig == null || (othersOnly && rig.isOfflineVRRig)) continue;
                subjects.Add(new WebSubject(rig.transform, rig.transform.position, RigTagState.IsTagged(rig), RunnerInterest));
            }
        }

        private static void AddPuppets(List<WebSubject> subjects, ReplayPlayer replay)
        {
            AddPuppets(subjects, replay, true);
            if (subjects.Count == 0) AddPuppets(subjects, replay, false);
        }

        private static void AddPuppets(List<WebSubject> subjects, ReplayPlayer replay, bool othersOnly)
        {
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (!puppet.Visible || (othersOnly && puppet.Actor.IsLocal)) continue;
                subjects.Add(new WebSubject(puppet.Body, puppet.Body.position, puppet.Current.Tag != ReplayTagState.None, RunnerInterest));
            }
        }

        private static float InterestOf(WebSubject subject, List<WebSubject> subjects)
        {
            bool inChase = NearestOpponentDistance(subject, subjects) <= ChaseRange;
            if (subject.Tagged) return inChase ? ChasingTaggerInterest : TaggerInterest;
            return inChase ? ChasedRunnerInterest : RunnerInterest;
        }

        private static float NearestOpponentDistance(WebSubject subject, List<WebSubject> subjects)
        {
            float nearest = float.PositiveInfinity;
            foreach (WebSubject other in subjects)
            {
                if (other.Tagged == subject.Tagged) continue;
                float distance = Vector3.Distance(subject.Position, other.Position);
                if (distance < nearest) nearest = distance;
            }
            return nearest;
        }
    }
}
