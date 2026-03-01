using System;
using UnityEngine;
using UnityEngine.Events; // Required for UnityAction

namespace Elin_ItemRelocator {
    public class RelocatorMenu {
        private UIContextMenu _menu;

        // Private constructor to force factory usage
        private RelocatorMenu(UIContextMenu menu) {
            _menu = menu;
        }

        // Factory Method
        public static RelocatorMenu Create() {
            return new RelocatorMenu(EClass.ui.CreateContextMenu("ContextMenu"));
        }

        // Add a simple button
        public RelocatorMenu AddButton(string text, Action onClick) {
            _menu.AddButton(text, onClick);
            return this;
        }

        // Add a child menu (Nested/Cascading)
        // Uses an Action<RelocatorMenu> to define the child's content inline
        public RelocatorMenu AddChild(string text, Action<RelocatorMenu> childSetup) {
            // AddChild returns the child UIContextMenu
            var childMenuRaw = _menu.AddChild(text);
            var childWrapper = new RelocatorMenu(childMenuRaw);

            // Execute the setup action for the child
            childSetup(childWrapper);

            return this;
        }

        // Add a check item (Wrapper for AddToggle)
        public RelocatorMenu AddCheck(string text, bool isOn, Action<bool> onToggle) {
            // AddToggle takes UnityAction<bool>, so we wrap the Action<bool>
            // or cast it if compatible (Delegate types mismatch usually requires wrapper)
            _menu.AddToggle(text, isOn, (val) => onToggle(val));
            return this;
        }

        // Add a separator line
        public RelocatorMenu AddSeparator() {
            _menu.AddSeparator(0);
            return this;
        }

        // Show the menu (Root only usually)
        public void Show() {
            _menu.Show();
        }
    }
}
