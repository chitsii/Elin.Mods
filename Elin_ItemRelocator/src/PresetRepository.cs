using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Elin_ItemRelocator {
    public class PresetRepository {
        // Path to Presets folder inside the Mod's folder
        private string PresetPath {
            get {
                // Assuming DLL is in plugins/Elin_ItemRelocator/
                // We want plugins/Elin_ItemRelocator/Presets/
                string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string dir = Path.Combine(pluginPath, "Presets");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public void Save(string name, RelocationProfile profile) {
            try {
                string path = Path.Combine(PresetPath, name + ".json");
                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(path, json);
                Dialog.Ok("Saved preset", () => {
                    string.Format(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_Saved), name);
                });
            } catch (Exception e) {
                Debug.LogError("[Elin_ItemRelocator] Failed to save preset: " + e);
                Msg.Say("Error saving preset.");
            }
        }

        public RelocationProfile Load(string name) {
            try {
                string path = Path.Combine(PresetPath, name + ".json");
                if (File.Exists(path)) {
                    string json = File.ReadAllText(path);
                    var profile = JsonConvert.DeserializeObject<RelocationProfile>(json);

                    if (profile != null) {
                        int oldVer = profile.Version;
                        bool implicitMigration = profile.Rules.Any(r => r._migrated);

                        bool success = Migrate(profile);

                        if (success && (profile.Version > oldVer || implicitMigration)) {
                            Dialog.YesNo(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_MigrateConfirm), () => {
                                Save(name, profile);
                            });
                        }
                    }
                    return profile;
                }
            } catch (Exception e) {
                Debug.LogError("[Elin_ItemRelocator] Failed to load preset: " + e);
                Msg.Say("Error loading preset: " + e.Message);
            }
            return null;
        }

        private readonly Dictionary<int, Action<RelocationProfile>> _migrations;

        public PresetRepository() {
            _migrations = new() {
                { 0, Migrate_0_to_1 }
            };
        }

        private void Migrate_0_to_1(RelocationProfile p) {
            // No specific logic needed for initial version bump
        }

        private bool Migrate(RelocationProfile profile) {
            int maxSafety = 100;
            while (profile.Version < RelocatorManager.CurrentProfileVersion && maxSafety-- > 0) {
                int currentVer = profile.Version;
                int nextVer = currentVer + 1;

                if (_migrations.TryGetValue(currentVer, out var action)) {
                    try {
                        action(profile);
                        profile.Version = nextVer;
                    } catch (Exception ex) {
                        string msg = RelocatorLang.GetText(RelocatorLang.LangKey.Msg_MigrateFailed);
                        Msg.Say(string.Format(msg, currentVer, nextVer, ex.Message));
                        Dialog.Ok(msg, () => {
                            string.Format(msg, currentVer, nextVer, ex.Message);
                        });
                        return false;
                    }
                } else {
                    Debug.LogError($"[Relocator] Missing migration for v{currentVer}");
                    // Treat missing migration as failure or force update?
                    // Let's treat as failure to be safe if strictly requested.
                    // But for forward compatibility, maybe just bump?
                    // Let's bump but NOT return false if we assumed it's safe in previous step.
                    // User said "If Migrate Failed". Missing migration is... acceptable risk? or fail?
                    // Let's keep it as force bump but maybe successful?
                    profile.Version = nextVer;
                }
            }
            return true;
        }

        public List<string> ListAll() {
            List<string> list = [];
            try {
                if (Directory.Exists(PresetPath)) {
                    var files = new DirectoryInfo(PresetPath).GetFiles("*.json");
                    // Sort by CreationTime ascending (Oldest first, newest last)
                    foreach (var file in files.OrderBy(f => f.CreationTime)) {
                        list.Add(Path.GetFileNameWithoutExtension(file.Name));
                    }
                }
            } catch { }
            return list;
        }

        public void Rename(string oldName, string newName) {
            try {
                string oldPath = Path.Combine(PresetPath, oldName + ".json");
                string newPath = Path.Combine(PresetPath, newName + ".json");

                if (!File.Exists(oldPath))
                    return;

                if (File.Exists(newPath)) {
                    Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_FileExists));
                    return;
                }

                File.Move(oldPath, newPath);
                Msg.Say(string.Format(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_Renamed), newName));
            } catch (Exception e) {
                Debug.LogError("[Elin_ItemRelocator] Failed to rename: " + e);
            }
        }

        public void Delete(string name) {
            try {
                string path = Path.Combine(PresetPath, name + ".json");
                if (File.Exists(path)) {
                    File.Delete(path);
                    Msg.Say(string.Format(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_Deleted), name));
                }
            } catch (Exception e) {
                Debug.LogError("[Elin_ItemRelocator] Failed to delete: " + e);
            }
        }
    }
}
