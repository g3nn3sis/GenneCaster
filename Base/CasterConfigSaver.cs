// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace GenneCaster.Base
{
    internal sealed class CasterConfigSaver : ObjectComponent
    {
        internal void Update() => CasterConfig.Tick();

        protected override void OnApplicationQuit()
        {
            CasterConfig.SaveOnExit();
            base.OnApplicationQuit();
        }
    }
}
