// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.Base
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
