// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zerori.Base
{
    internal abstract class ObjectComponent : MonoBehaviour
    {
        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Log.Debug($"{GetType().Name} initialized.");
        }

        protected virtual void OnDestroy() => Log.Debug($"{GetType().Name} destroyed.");

        protected virtual void OnApplicationQuit() { }
    }
}
