// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zerori.GameCamera.Webs
{
    internal static class WebDirector
    {
        private const float ProximityRange = 8f;
        private const float NodeSwitchMargin = 1.3f;
        private const float SubjectSwitchMargin = 1.25f;

        internal static int ChooseNode(IReadOnlyList<Vector3> nodes, List<WebSubject> subjects, int current)
        {
            int best = current;
            float bestScore = NodeScore(nodes[current], subjects) * NodeSwitchMargin;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == current) continue;
                float score = NodeScore(nodes[i], subjects);
                if (score <= bestScore) continue;
                best = i;
                bestScore = score;
            }
            return best;
        }

        internal static Transform? ChooseSubject(Vector3 node, List<WebSubject> subjects, Transform? current)
        {
            Transform? best = null;
            float bestScore = 0f;
            float currentScore = 0f;
            foreach (WebSubject subject in subjects)
            {
                float score = Appeal(node, subject);
                if (subject.Body == current) currentScore = score;
                if (score <= bestScore) continue;
                best = subject.Body;
                bestScore = score;
            }
            bool keepsCurrent = currentScore > 0f && currentScore * SubjectSwitchMargin >= bestScore;
            return keepsCurrent ? current : best;
        }

        private static float NodeScore(Vector3 node, List<WebSubject> subjects)
        {
            float best = 0f;
            foreach (WebSubject subject in subjects) best = Mathf.Max(best, Appeal(node, subject));
            return best;
        }

        private static float Appeal(Vector3 node, WebSubject subject)
        {
            float reach = Vector3.Distance(node, subject.Position) / ProximityRange;
            return subject.Interest / (1f + reach * reach);
        }
    }
}
