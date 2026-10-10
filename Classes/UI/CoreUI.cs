using UnityEngine.InputSystem;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.UI
{
    internal sealed class CoreUI : ObjectComponent
    {
        private PanelOrchestrator? panels;
        public static bool isMain = true;

        internal void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            isMain = !isMain;
            if (isMain) panels?.ShowMain();
        }

        internal void OnGUI()
        {
            if (!isMain) return;
            if (panels == null) panels = new PanelOrchestrator();
            panels.Draw();
        }

        protected override void OnDestroy()
        {
            panels?.Dispose();
            panels = null;
            base.OnDestroy();
        }
    }
}
