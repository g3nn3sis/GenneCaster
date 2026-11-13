// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace GenneCaster.Base
{
    public abstract class ObjectComponent : MonoBehaviour
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
