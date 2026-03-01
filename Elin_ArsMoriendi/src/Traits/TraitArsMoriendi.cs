namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Trait for the Ars Moriendi forbidden tome.
    /// Book item with two inventory interactions:
    ///   - "Open" (CanUse/OnUse) → IMGUI GUI for knowledge/ritual/servants
    ///   - "Read" (CanRead/OnRead) → Vanilla book viewer for lore
    /// CWL loads this via SourceThing trait: "ArsMoriendi"
    /// (Elin auto-prepends "Trait" -> "TraitArsMoriendi", CWL resolves namespace)
    /// </summary>
    public class TraitArsMoriendi : Trait
    {
        private static Chara? _activeMerchant;

        private static string T(string jp, string en, string cn) =>
            Lang.langCode == "CN" ? cn : Lang.isJP ? jp : en;

        // ── Inventory: "Open" → IMGUI GUI ──
        public override string LangUse => T("死霊術を探究する", "Study Necromancy", "探究死灵术");
        public override bool CanUse(Chara c) => true;
        public override bool OnUse(Chara c)
        {
            OpenTome();
            return false; // no turn consumption
        }

        // ── Inventory: "Read" → Vanilla book viewer ──
        public override bool CanRead(Chara c) => !c.isBlind;
        public override void OnRead(Chara c)
        {
            // Future: switch file path based on quest progression
            EClass.ui.AddLayer<LayerHelp>("LayerBook")
                .book.Show("Book/ars_moriendi", null,
                    T("禁断の書 アルス・モリエンディ", "Ars Moriendi", "禁书 Ars Moriendi"), null);
        }

        // ── Placed: click action ──
        public override void TrySetAct(ActPlan p)
        {
            p.TrySetAct(T("死霊術を探究する", "Study Necromancy", "探究死灵术"), delegate
            {
                OpenTome();
                return false;
            }, owner);

            p.TrySetAct(T("Modアンインストール", "Uninstall Mod", "卸载Mod"), delegate
            {
                Dialog.YesNo(
                    T("Ars Moriendi の全データをセーブから削除します。\n従者は全員失われます。この操作は取り消せません。",
                      "Remove all Ars Moriendi data from this save.\nAll servants will be lost. This cannot be undone.",
                      "将从存档中删除 Ars Moriendi 的全部数据。\n所有仆从将会失去。此操作无法撤销。"),
                    () => Dialog.YesNo(
                        T("本当に実行しますか？\nこの操作は取り消せません。",
                          "Are you sure?\nThis cannot be undone.",
                          "确定要执行吗？\n此操作无法撤销。"),
                        () =>
                        {
                            var result = ModUninstaller.Execute();
                            var msg = Lang.langCode == "CN"
                                ? $"卸载完成：仆从{result.ServantsRemoved}体解放, NPC{result.NpcsRemoved}体移除, 标记{result.FlagsRemoved}个删除, 咒文{result.ElementsRemoved}个删除"
                                : Lang.isJP
                                ? $"アンインストール完了: 従者{result.ServantsRemoved}体解放, NPC{result.NpcsRemoved}体除去, フラグ{result.FlagsRemoved}個削除, 呪文{result.ElementsRemoved}個削除"
                                : $"Uninstall complete: {result.ServantsRemoved} servants released, {result.NpcsRemoved} NPCs removed, {result.FlagsRemoved} flags cleared, {result.ElementsRemoved} spells removed";
                            Msg.Say(msg);
                        }));
                return false;
            }, owner);

#if DEBUG
            p.TrySetAct("[DEBUG] Spawn Materials", delegate
            {
                SpawnDebugMaterials();
                return false;
            }, owner);
            p.TrySetAct("[DEBUG] Spawn Ritual Materials", delegate
            {
                SpawnRitualMaterials();
                return false;
            }, owner);
            p.TrySetAct("[DEBUG] Unlock All Spells", delegate
            {
                var mgr = NecromancyManager.Instance;
                const int debugSpellStock = 1000;
                foreach (var spell in NecromancyManager.SpellUnlocks)
                {
                    if (!mgr.IsUnlocked(spell.Alias))
                        mgr.Unlock(spell.Alias);
                }
                mgr.EnsureUnlockedSpellStocksAtLeast(debugSpellStock);
                Msg.Say(T("全ての呪文を解禁し、ストックを1000まで補充しました。",
                    "All spells unlocked, and stocks were refilled to 1000.",
                    "已解锁所有咒文，并将蓄力补充至1000。"));
                EClass.pc.pos.PlayEffect("buff");
                return false;
            }, owner);
#endif
        }

        private void OpenTome()
        {
            ModLog.Log("OpenTome: interacted with Ars Moriendi tome");
            EClass.pc.pos.PlaySound("ars_moriendi_se_open");
            NecromancyManager.Instance.EnsureUnlockedSpellsGranted();
            NecromancyManager.Instance.RefreshServantVisuals();
            // TODO: Move quest progression trigger out of Trait (item responsibility)
            // into a quest event dispatcher when quest-line integration is generalized.
            bool dramaPlaying = NecromancyManager.Instance.QuestPath.TryAdvanceOnTomeOpen();
            if (!dramaPlaying)
                ArsMoriendiGUI.Show();
        }

        public static void SummonMerchant()
        {
            if (_activeMerchant != null && !_activeMerchant.isDead)
            {
                if (_activeMerchant.currentZone == EClass._zone && _activeMerchant.ExistsOnMap)
                {
                    Msg.Say(T("商人は既にここにいる。",
                        "The merchant is already here.",
                        "商人已经在这里了。"));
                    return;
                }
            }

            var merchant = NpcPresenceUtil.ShowNearPlayer("ars_hecatia");
            if (merchant == null) return;

            // Recover only from death; other status conditions are preserved.
            if (merchant.isDead)
                merchant.Revive();

            merchant.hostility = Hostility.Friend;
            merchant.c_originalHostility = Hostility.Friend;

            _activeMerchant = merchant;

            EClass.pc.pos.PlayEffect("teleport");
            EClass.pc.pos.PlaySound("teleport");
            Msg.Say(T("禁断の書から商人が現れた。",
                "A merchant appears from the forbidden tome.",
                "商人从禁书中出现了。"));

            ModLog.Log("Merchant summoned: {0} (uid={1})", merchant.Name, merchant.uid);
        }

#if DEBUG
        private void SpawnDebugMaterials()
        {
            var pc = EClass.pc;
            // Souls (3 each)
            pc.AddThing(ThingGen.Create("ars_soul_weak").SetNum(3));
            pc.AddThing(ThingGen.Create("ars_soul_normal").SetNum(3));
            pc.AddThing(ThingGen.Create("ars_soul_strong").SetNum(3));
            pc.AddThing(ThingGen.Create("ars_soul_legendary").SetNum(1));
            // Heart
            pc.AddThing(ThingGen.Create("heart").SetNum(3));
            // Booze (for ritual spirit slot)
            pc.AddThing(ThingGen.Create("booze"));
            // Cursed item (for ritual cursed slot)
            var cursedItem = ThingGen.Create("dagger");
            cursedItem.SetBlessedState(BlessedState.Cursed);
            pc.AddThing(cursedItem);
            // Corpses (low-level creatures)
            string[] corpseIds = { "putit", "kobold", "goblin" };
            foreach (var id in corpseIds)
            {
                var chara = CharaGen.Create(id);
                var corpse = ThingGen.Create("_meat");
                corpse.MakeFoodFrom(chara);
                pc.AddThing(corpse);
            }

            Msg.Say("DEBUG: Spawned test materials.");
            ModLog.Log("DEBUG: Spawned souls, hearts, corpses, booze, and cursed item");
        }

        private void SpawnRitualMaterials()
        {
            var pc = EClass.pc;
            pc.AddThing(ThingGen.Create("ars_soul_legendary").SetNum(99));
            pc.AddThing(ThingGen.Create("heart").SetNum(99));
            pc.AddThing(ThingGen.Create("blood_angel").SetNum(3));
            pc.AddThing(ThingGen.Create("336").SetNum(10));
            pc.AddThing(ThingGen.Create("mercury").SetNum(3));
            var cursedItem = ThingGen.Create("1081").SetNum(3);
            cursedItem.SetBlessedState(BlessedState.Cursed);
            pc.AddThing(cursedItem);

            Msg.Say("DEBUG: Spawned apotheosis ritual materials.");
            ModLog.Log("DEBUG: Spawned ritual materials (legendary soul x99, heart x99, blood_angel x3, 336 x10, mercury x3, cursed 1081 x3)");
        }

#endif
    }
}
