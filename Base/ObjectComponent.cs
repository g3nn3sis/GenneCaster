// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Base
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
