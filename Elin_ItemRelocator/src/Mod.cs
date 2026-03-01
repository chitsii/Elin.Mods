using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_ItemRelocator {
    /// <summary>
    /// Marker component to identify layers belonging to this mod.
    /// </summary>
    public class RelocatorLayerMarker : MonoBehaviour { }

    [BepInPlugin(ID, TITLE, VERSION)]
    public class Elin_ItemRelocator : BaseUnityPlugin {
        public const string ID = "Elin_ItemRelocator";
        public const string TITLE = "Item Relocator";
        public const string VERSION = "1.0.0";

        public static Elin_ItemRelocator Instance { get; private set; }
        internal new static ManualLogSource Logger;

        private void Awake() {
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo("[Elin_ItemRelocator] Awake Called");
            Harmony harmony = new(ID);
            harmony.PatchAll();
            Logger.LogInfo("[Elin_ItemRelocator] Harmony Patched");
        }

        [HarmonyPatch(typeof(Game), "OnLoad")]
        class PatchGameLoad {
            static void Postfix() {
                Logger.LogInfo("[Elin_ItemRelocator] Game Loaded");
                RelocatorManager.Instance.Init();
            }
        }

        [HarmonyPatch(typeof(UIInventory), "RefreshMenu")]
        class PatchUIInventory {
            static void Postfix(UIInventory __instance) {
                // Logger.LogInfo("[Elin_ItemRelocator] Postfix Start");
                if (__instance.window is null)
                    return;
                var owner = __instance.owner;
                if (owner is null) { return; } // Reduced noise
                if (owner.Container is null) { return; }

                Thing containerThing = owner.Container.Thing;
                if (containerThing is null) {
                    if (owner.Container.isChara)
                        return;
                    return;
                }
                if (!containerThing.trait.IsContainer)
                    return;

                // Enforce using buttonSort as the source
                UIButton anchorBtn = __instance.window.buttonSort;
                if (anchorBtn is null) {
                    Logger.LogInfo("[Elin_ItemRelocator] ButtonSort not found, aborting.");
                    return;
                }

                Transform parent = anchorBtn.transform.parent;
                Transform existing = parent.Find("ButtonRelocate");
                UIButton btnRelocate;

                if (existing is not null) {
                    btnRelocate = existing.GetComponent<UIButton>();
                } else {
                    btnRelocate = UnityEngine.Object.Instantiate(anchorBtn, parent);
                    btnRelocate.gameObject.name = "ButtonRelocate";
                    btnRelocate.transform.SetAsLastSibling();
                    Logger.LogInfo($"[Elin_ItemRelocator] Created New Button for: {containerThing.Name}");
                }

                if (btnRelocate is null)
                    return;

                Logger.LogInfo($"[Elin_ItemRelocator] Customizing Button: {btnRelocate.name}");

                // Aggressively Hide Icon
                // The Sort button uses the root Image as the icon (e.g. pipo-emotion_122)
                // We must make it transparent but keep it enabled for Raycast (Clicks)
                if (btnRelocate.image is not null) {
                    // Check if it looks like an icon (not a generic background)
                    // Or just force it clear since we are adding text
                    btnRelocate.image.sprite = null;
                    btnRelocate.image.color = Color.clear;
                    Logger.LogInfo("[Elin_ItemRelocator] Cleared root image (Icon) to transparent.");
                }

                // Also hide any child images that might be overlays
                foreach (var img in btnRelocate.GetComponentsInChildren<Image>(true)) {
                    if (img.gameObject == btnRelocate.gameObject)
                        continue; // Already handled above

                    img.enabled = false;
                    img.gameObject.SetActive(false);
                    Logger.LogInfo($"[Elin_ItemRelocator] Hidden Child Image: {img.name}");
                }

                // Compatibility with previous dedicated icon logic
                if (btnRelocate.icon is not null) {
                    btnRelocate.icon.enabled = false;
                    btnRelocate.icon.sprite = null;
                    btnRelocate.icon.color = Color.clear;
                    btnRelocate.icon.gameObject.SetActive(false);
                }

                if (btnRelocate.mainText is not null) {
                    btnRelocate.mainText.SetActive(true);
                    btnRelocate.mainText.SetText("★");
                } else {
                    var t = btnRelocate.GetComponentInChildren<UIText>(true);
                    if (t is not null) {
                        t.SetActive(true);
                        t.SetText("★");
                    } else {
                        Logger.LogInfo("[Elin_ItemRelocator] No Text Component Found! Creating one...");
                        // Find a source text to clone (e.g. from Window Caption or any sibling)
                        UIText sourceText = __instance.window.GetComponentInChildren<UIText>(true);
                        if (sourceText is not null) {
                            // Instantiate copy of the GAME OBJECT, then get component
                            GameObject newObj = UnityEngine.Object.Instantiate(sourceText.gameObject, btnRelocate.transform);
                            UIText newText = newObj.GetComponent<UIText>();
                            if (newText is not null) {
                                newObj.name = "Text_Relocate";
                                newText.text = "★";
                                // "Matcha" Color (Muted Green) ~ #398982ff
                                // R=2, G=89, B=82 => 0.02, 0.35, 0.32
                                newText.color = new(0.15f, 0.55f, 0.52f);
                                newText.alignment = TextAnchor.MiddleCenter;
                                newText.resizeTextForBestFit = true;
                                newText.resizeTextMinSize = 10;
                                newText.resizeTextMaxSize = 20;

                                // Reset Rect logic
                                RectTransform rt = newText.rectTransform;
                                rt.anchorMin = Vector2.zero;
                                rt.anchorMax = Vector2.one;
                                rt.sizeDelta = Vector2.zero;
                                rt.anchoredPosition = Vector2.zero;

                                newObj.SetActive(true);
                                btnRelocate.mainText = newText; // Correctly assign Component
                            }
                        } else {
                            Logger.LogInfo("[Elin_ItemRelocator] Failed to find source text to clone.");
                        }
                    }
                }

                btnRelocate.refObj = null;
                btnRelocate.onClick.RemoveAllListeners();
                btnRelocate.onClick.AddListener(() => LayerItemRelocator.Open(containerThing));

                // --- Icon Customization (Optional) ---
                // If you want to use an Icon instead of Text, set this to true
                bool useIcon = false;

                if (useIcon) {
                    // Enable Icon Caption (found in dump)
                    Transform iconTrans = btnRelocate.transform.Find("icon caption");
                    if (iconTrans is not null) {
                        Image iconImg = iconTrans.GetComponent<Image>();
                        if (iconImg is not null) {
                            iconImg.enabled = true;
                            iconImg.gameObject.SetActive(true);
                            // You can change sprite here: iconImg.sprite = ...
                            Logger.LogInfo("[Elin_ItemRelocator] Enabled Icon.");
                        }
                    }
                    if (btnRelocate.mainText is not null)
                        btnRelocate.mainText.SetActive(false);
                }

                // --- Tooltip Fix ---
                try {
                    string finalTooltip = RelocatorLang.GetText(RelocatorLang.LangKey.Relocate);
                    var fieldInfo = typeof(UIButton).GetField("tooltip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                    if (fieldInfo is not null) {
                        // Create fresh instance
                        var newTooltip = Activator.CreateInstance(fieldInfo.FieldType);
                        fieldInfo.SetValue(btnRelocate, newTooltip);

                        // Set 'text' field directly
                        var textField = newTooltip.GetType().GetField("text", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (textField is not null) {
                            textField.SetValue(newTooltip, finalTooltip);
                        }

                        // Set 'enable' field directly (optional but safer)
                        var enableField = newTooltip.GetType().GetField("enable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (enableField is not null) {
                            enableField.SetValue(newTooltip, true);
                        }
                    }
                    // Redundant saftey call
                    btnRelocate.SetTooltip(finalTooltip);
                } catch {
                    // Silent failure or min log
                }

                btnRelocate.gameObject.SetActive(true);

                // Fix Layout Overlap
                // Move to the very end of the list
                btnRelocate.transform.SetAsLastSibling();

                // Check for LayoutGroup
                LayoutGroup group = parent.GetComponent<LayoutGroup>();
                if (group is null) {
                    // Manual Positioning (Simplified)
                    float lowestY = float.MaxValue;
                    float height = 32f;
                    float spacing = 5f;

                    RectTransform myRect = btnRelocate.GetComponent<RectTransform>();
                    RectTransform anchorRect = anchorBtn.GetComponent<RectTransform>();
                    if (anchorRect is not null) {
                        height = anchorRect.rect.height;
                    }

                    foreach (RectTransform child in parent) {
                        if (child == myRect)
                            continue;
                        if (!child.gameObject.activeSelf)
                            continue;

                        if (child.anchoredPosition.y < lowestY) {
                            lowestY = child.anchoredPosition.y;
                        }
                    }

                    if (lowestY == float.MaxValue)
                        lowestY = 0;

                    // Place below the lowest item
                    myRect.anchoredPosition = new(anchorRect.anchoredPosition.x, lowestY - height - spacing);
                } else {
                    // Use LayoutElement logic for LayoutGroups
                    LayoutElement le = btnRelocate.GetComponent<LayoutElement>();
                    if (le is null)
                        le = btnRelocate.gameObject.AddComponent<LayoutElement>();

                    // Get width from anchor button
                    float targetWidth = 32f;
                    float targetHeight = 32f;
                    RectTransform anchorRect = anchorBtn.GetComponent<RectTransform>();
                    if (anchorRect is not null) {
                        targetWidth = anchorRect.rect.width;
                        targetHeight = anchorRect.rect.height;
                        if (targetWidth < 10)
                            targetWidth = 32f;
                    }

                    le.ignoreLayout = false;
                    le.minWidth = targetWidth;
                    le.preferredWidth = targetWidth;
                    le.minHeight = targetHeight;
                    le.preferredHeight = targetHeight;
                    le.flexibleWidth = 0;
                    le.flexibleHeight = 0;

                    // Force Rebuilds
                    LayoutRebuilder.ForceRebuildLayoutImmediate(btnRelocate.transform as RectTransform);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
                    if (__instance.window != null && __instance.window.GetComponent<RectTransform>() != null) {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.window.GetComponent<RectTransform>());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Layer), "OnRightClick")]
        class PatchLayerRightClick {
            static bool Prefix(Layer __instance) {
                // Determine if this is OUR layer based on the marker component
                if (__instance.GetComponent<RelocatorLayerMarker>() != null) {
                    // Skip default Close()
                    return false;
                }
                return true;
            }
        }
    }
}
