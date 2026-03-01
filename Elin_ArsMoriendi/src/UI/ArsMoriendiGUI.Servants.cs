using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static partial class ArsMoriendiGUI
    {
        // ── Servant Tab State ──
        private static Chara? _selectedServant;
        private static int _modSelectedAttr = 70; // Default: STR
        private static string _modSelectedSoulId = "ars_soul_weak";
        private static int _modSoulCount = 1;
        private static int _modSelectedSlot = -1;
        private static HashSet<Thing> _augmentCorpses = new();
        private static Vector2 _corpseScrollPos;
        private static string? _autoSelectMessage;
        private static int _modPanelTab; // 0=注入, 1=増設
        private static bool _showThresholdDetail;
        private static bool _quickInjectMode;
        private static ActionResult? _lastResult;

        private class ActionResult
        {
            public string Title = "";
            public string Detail = "";
            public Color AccentColor;
        }

        private static void ResetServantState()
        {
            _selectedServant = null;
            _augmentCorpses.Clear();
            _corpseScrollPos = Vector2.zero;
        }

        // ── Servant Tab ──

        private static void DrawServantTab()
        {
            var mgr = NecromancyManager.Instance;

            GUILayout.Label(LangHelper.Get("servantFlavor"), _flavorStyle);
            GUILayout.Space(6);
            DrawDivider();
            GUILayout.Space(6);

            // ── Summary ──
            var allServants = mgr.GetAllServants();
            int aliveCount = 0;
            int deadCount = 0;
            int stashedCount = 0;
            foreach (var (_, isAlive) in allServants)
            {
                if (!isAlive)
                {
                    deadCount++;
                    continue;
                }

                aliveCount++;
            }
            foreach (var (chara, isAlive) in allServants)
            {
                if (!isAlive) continue;
                if (mgr.IsServantStashed(chara.uid))
                    stashedCount++;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("◆ 従者数", "◆ Servants", "◆ 仆从数"), _headerStyle, GUILayout.Width(140));
            string countText = $"{allServants.Count}{L("体", "", "体")} (● {aliveCount}  ◇ {stashedCount}  ○ {deadCount})";
            GUILayout.Label(countText, _labelStyle);
            GUILayout.FlexibleSpace();

            GUI.enabled = aliveCount > 0 && !_pendingConfirm;
            if (GUILayout.Button(L("退避(全)", "Stash All", "全部退避"), _buttonStyle, GUILayout.Width(92)))
            {
                mgr.StashAllActiveServants();
            }

            GUI.enabled = stashedCount > 0 && !_pendingConfirm;
            if (GUILayout.Button(L("招集(全)", "Recall All", "全部招集"), _buttonStyle, GUILayout.Width(92)))
            {
                mgr.RecallStashedServants();
            }
            GUI.enabled = true;

            // Widget toggle
            bool widgetOn = ModConfig.ShowServantWidget.Value;
            string widgetLabel = widgetOn
                ? L("◉ ウィジェット ON", "◉ Widget ON", "◉ 小部件 ON")
                : L("○ ウィジェット OFF", "○ Widget OFF", "○ 小部件 OFF");
            var widgetStyle = widgetOn ? _selectedButtonStyle : _buttonStyle;
            if (GUILayout.Button(widgetLabel, widgetStyle, GUILayout.Width(130)))
            {
                ModConfig.ShowServantWidget.Value = !widgetOn;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawDivider();
            GUILayout.Space(6);

            // ── Servant Roster ──
            if (allServants.Count == 0)
            {
                GUILayout.Label(
                    L("現在従者はいません。儀式タブで死体と魂から従者を蓋らせましょう。",
                      "No servants. Use the Ritual tab to raise servants from corpses and souls.",
                      "当前没有仆从。请在仪式标签页中用尸体和灵魂复活仆从。"),
                    _descStyle);
                return;
            }

            // Validate selected servant still exists
            if (_selectedServant != null && !allServants.Any(s => s.chara == _selectedServant))
                _selectedServant = null;

            foreach (var (chara, isAlive) in allServants)
            {
                var captured = chara;
                bool isModTarget = _selectedServant == chara;

                GUILayout.BeginHorizontal();

                // Character icon
                GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Width(24), GUILayout.Height(24));
                var servantIconRect = GUILayoutUtility.GetLastRect();
                DrawCharaIcon(servantIconRect, chara: captured);

                // Status icon
                if (isAlive)
                    GUILayout.Label("●", _goodStyle, GUILayout.Width(18));
                else
                    GUILayout.Label("○", _badStyle, GUILayout.Width(18));

                GUILayout.Label(chara.GetName(NameStyle.Full), _labelStyle, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Lv.{chara.LV}", _labelStyle, GUILayout.Width(50));

                bool isStashed = isAlive && mgr.IsServantStashed(chara.uid);
                if (isAlive)
                {
                    if (isStashed)
                        GUILayout.Label(L("退避", "Stashed", "退避"), _warningStyle, GUILayout.Width(52));
                    else
                        GUILayout.Label(LangHelper.Get("statusAlive"), _goodStyle, GUILayout.Width(52));
                }
                else
                {
                    GUILayout.Label(LangHelper.Get("statusDead"), _badStyle, GUILayout.Width(52));
                }

                // Enhancement level display
                var enh = mgr.GetEnhancement(chara.uid);
                int threshold = NecromancyManager.GetRampageThreshold();
                var enhStyle = enh.EnhancementLevel <= threshold ? _descStyle :
                enh.EnhancementLevel <= threshold + 3 ? _warningStyle : _badStyle;
                GUILayout.Label($"{L("強化", "Enh", "强化")}:{enh.EnhancementLevel}", enhStyle, GUILayout.Width(55));

                if (isAlive)
                {
                    GUI.enabled = !_pendingConfirm;
                    string stashLabel = isStashed ? L("招集", "Recall", "招集") : L("退避", "Stash", "退避");
                    if (GUILayout.Button(stashLabel, _buttonStyle, GUILayout.Width(Lang.langCode == "CN" || Lang.isJP ? 55 : 72)))
                    {
                        mgr.SetServantStashedState(captured, !isStashed);
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Space(Lang.langCode == "CN" || Lang.isJP ? 59 : 76);
                }

                // Modify button
                GUI.enabled = !_pendingConfirm;
                var modStyle = isModTarget ? _selectedButtonStyle : _buttonStyle;
                if (GUILayout.Button(L("改造", "Modify", "改造"), modStyle, GUILayout.Width(Lang.langCode == "CN" || Lang.isJP ? 55 : 72)))
                {
                    _selectedServant = isModTarget ? null : captured;
                    _augmentCorpses.Clear();
                    _modSelectedSlot = -1;
                    _modPanelTab = 0;
                    _quickInjectMode = false;
                }

                // Release button
                if (GUILayout.Button(L("解放", "Release", "解放"), _buttonStyle, GUILayout.Width(Lang.langCode == "CN" || Lang.isJP ? 55 : 72)))
                {
                    ShowReleaseConfirmation(mgr, captured);
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            // ── Modification Sub-Panel ──
            if (_selectedServant != null)
            {
                GUILayout.Space(8);
                DrawDivider();
                GUILayout.Space(6);
                DrawModificationPanel(mgr, _selectedServant);
            }
        }

        private static void ShowReleaseConfirmation(NecromancyManager mgr, Chara servant)
        {
            string confirmMsg = $"{servant.GetName(NameStyle.Full)}\n{L("を解放する (取り消し不可)", "Release (cannot be undone)", "解放 (无法撤销)")}";

            ShowConfirmDialog(confirmMsg, () =>
            {
                mgr.ReleaseServant(servant);
                LangHelper.Say("servantReleased");
                if (_selectedServant == servant) _selectedServant = null;
            });
        }

        // ── Modification Panel ──

        private static void DrawModificationPanel(NecromancyManager mgr, Chara servant)
        {
            var enh = mgr.GetEnhancement(servant.uid);
            int threshold = NecromancyManager.GetRampageThreshold();
            int rampageChance = mgr.GetRampageChance(servant.uid);

            // Header with rampage info
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L("◆ {0} の改造", "◆ Modify {0}", "◆ 改造 {0}"),
                servant.GetName(NameStyle.Full)), _headerStyle);
            GUILayout.FlexibleSpace();

            // Rampage risk display
            var riskStyle = rampageChance == 0 ? _goodStyle :
                rampageChance <= 30 ? _warningStyle : _badStyle;
            string riskText = $"{L("強化Lv", "Enh.Lv", "强化Lv")}: {enh.EnhancementLevel} / {L("閾値", "Threshold", "阈值")}: {threshold}";
            GUILayout.Label(riskText, riskStyle);
            if (rampageChance > 0)
            {
                GUILayout.Label($"  ⚠ {L("暴走率", "Rampage", "暴走率")}: {rampageChance}%", _badStyle);
            }
            if (GUILayout.Button(_showThresholdDetail ? "[−]" : "[?]", _buttonStyle, GUILayout.Width(28), GUILayout.Height(20)))
            {
                _showThresholdDetail = !_showThresholdDetail;
            }
            GUILayout.EndHorizontal();

            // Threshold breakdown detail
            if (_showThresholdDetail)
            {
                int mag = EClass.pc?.MAG ?? 0;
                int kills = EClass.player?.stats?.kills ?? 0;
                var (magPart, killsPart, _) = NecromancyCalculations.GetRampageThresholdBreakdown(mag, kills);
                string detail = $"  {L("閾値", "Threshold", "阈值")} = MAG/5={magPart} + √Kills={killsPart}  (MAG={mag}, Kills={kills})";
                GUILayout.Label(detail, _descStyle);
                if (rampageChance > 0)
                {
                    GUILayout.Label($"  {L("暴走結果", "Rampage outcomes", "暴走结果")}: ★{L("闇の覚醒", "Dark Awakening", "暗之觉醒")}35% / △{L("暴走", "Berserk", "暴走")}35% / ×{L("自爆", "Self-Destruct", "自爆")}15% / ★{L("変異覚醒", "Mutation", "变异觉醒")}15%", _descStyle);
                }
            }
            GUILayout.Space(4);

            // ── Result Card ──
            if (_lastResult != null)
                DrawResultCard();

            // ── Sub-tab toolbar ──
            var subTabNames = new[] {
                L("▸ 魂注入", "▸ Inject", "▸ 灵魂注入"),
                L("▸ 部位増設", "▸ Augment", "▸ 部位增设")
            };
            int prevTab = _modPanelTab;
            _modPanelTab = GUILayout.Toolbar(_modPanelTab, subTabNames, _toolbarStyle, GUILayout.Height(26));
            if (_modPanelTab != prevTab)
            {
                // Reset opposite tab's selection state
                if (_modPanelTab == 0) { _modSelectedSlot = -1; _augmentCorpses.Clear(); _autoSelectMessage = null; }
                else { _modSelectedAttr = 70; }
            }
            GUILayout.Space(4);

            if (_modPanelTab == 0)
            {
            // ── Attribute Enhancement ──
            GUILayout.Label(L("▸ アトリビュート強化", "▸ Attribute Enhancement", "▸ 属性强化"), _headerStyle);
            GUILayout.Space(2);

            // Attribute selection + stats display
            foreach (var attrId in NecromancyManager.AttributeIds)
            {
                int injCount = mgr.GetAttrInjectionCount(servant.uid, attrId);
                double eff = mgr.GetNextInjectionEfficiency(servant.uid, attrId);
                int currentVal = servant.elements?.Value(attrId) ?? 0;

                GUILayout.BeginHorizontal();
                bool selected = _modSelectedAttr == attrId;
                var style = selected ? _selectedButtonStyle : _buttonStyle;
                if (GUILayout.Button(LangHelper.GetAttrName(attrId), style, GUILayout.Width(45)))
                {
                    _modSelectedAttr = attrId;
                }
                GUILayout.Label($"{L("現在", "Cur", "当前")}: {currentVal}", _labelStyle, GUILayout.Width(70));
                GUILayout.Label($"{L("累積", "Total", "累计")}: {injCount}", _descStyle, GUILayout.Width(80));
                GUILayout.Label($"{L("効率", "Eff", "效率")}: {eff:P0}", eff < 0.5 ? _warningStyle : _descStyle, GUILayout.Width(70));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            // Soul type selection for injection
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("使用する魂:", "Soul to use:", "使用灵魂:"), _summaryLabelStyle, GUILayout.Width(100));
            foreach (var soulId in NecromancyManager.SoulIds)
            {
                int owned = mgr.CountItemsInInventory(EClass.pc, soulId);
                if (owned == 0 && _modSelectedSoulId != soulId) continue;
                bool sel = _modSelectedSoulId == soulId;
                var style = sel ? _selectedButtonStyle : _buttonStyle;
                if (GUILayout.Button($"{LangHelper.GetSoulName(soulId)} ({owned})", style, GUILayout.Width(120)))
                {
                    _modSelectedSoulId = soulId;
                    _modSoulCount = Math.Min(_modSoulCount, owned);
                }
            }
            GUILayout.EndHorizontal();

            // Soul count
            int maxSoulCount = mgr.CountItemsInInventory(EClass.pc, _modSelectedSoulId);
            int maxInjectCount = Math.Min(maxSoulCount, NecromancyManager.MaxSoulsPerInjection);
            if (_modSoulCount > maxSoulCount) _modSoulCount = maxSoulCount;
            if (_modSoulCount > maxInjectCount) _modSoulCount = maxInjectCount;

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("数量:", "Qty:", "数量:"), _summaryLabelStyle, GUILayout.Width(100));
            if (maxInjectCount > 0)
            {
                float slVal = GUILayout.HorizontalSlider(_modSoulCount, 1, Math.Max(1, maxInjectCount), GUILayout.Width(150));
                _modSoulCount = Math.Max(1, Mathf.RoundToInt(slVal));
                GUILayout.Label($"x{_modSoulCount}/{maxInjectCount}", _labelStyle, GUILayout.Width(70));
            }
            else
            {
                GUILayout.Label(L("魂がない", "No souls", "没有灵魂"), _badStyle);
            }
            GUILayout.EndHorizontal();

            // Preview
            if (maxSoulCount > 0 && NecromancyManager.SoulUnitTable.TryGetValue(_modSelectedSoulId, out int spPer))
            {
                int injCount = mgr.GetAttrInjectionCount(servant.uid, _modSelectedAttr);
                double previewEff = NecromancyCalculations.CalculateInjectionEfficiency(injCount, _modSoulCount);
                int previewBoost = NecromancyCalculations.CalculateInjectionBoost(spPer, _modSoulCount, injCount);
                GUILayout.Label($"  → {LangHelper.GetAttrName(_modSelectedAttr)} +{previewBoost} ({L("効率", "Eff", "效率")}: {previewEff:P0})", _goodStyle);
            }

            int projectedInjectRampageChance = mgr.GetRampageChance(servant.uid, Math.Max(1, _modSoulCount));

            // Inject button + Quick Inject toggle
            GUILayout.BeginHorizontal();
            GUI.enabled = maxSoulCount > 0 && !_pendingConfirm;
            if (GUILayout.Button(L("注入する", "Inject", "注入"), _buttonStyle, GUILayout.Width(120)))
            {
                var capturedServant = servant;
                int capturedAttr = _modSelectedAttr;
                string capturedSoul = _modSelectedSoulId;
                int capturedCount = _modSoulCount;

                Action doInject = () =>
                {
                    _lastResult = null;
                    int beforeVal = capturedServant.elements?.Value(capturedAttr) ?? 0;

                    var rampage = mgr.CheckRampage(capturedServant, capturedCount);
                    double multiplier = (rampage == RampageResult.DarkAwakening) ? 2.0 : 1.0;

                    int boost = mgr.InjectAttribute(capturedServant, capturedAttr, capturedSoul, capturedCount, multiplier);
                    if (boost > 0)
                    {
                        LangHelper.Say("enhanceAttrSuccess", capturedServant);
                        EClass.pc.pos.PlayEffect("buff");
                        EClass.pc.pos.PlaySound("buff");

                        int afterVal = capturedServant.elements?.Value(capturedAttr) ?? 0;
                        int injCount = mgr.GetAttrInjectionCount(capturedServant.uid, capturedAttr);
                        double eff = mgr.GetNextInjectionEfficiency(capturedServant.uid, capturedAttr);
                        _lastResult = new ActionResult
                        {
                            Title = $"{LangHelper.GetAttrName(capturedAttr)} +{boost}",
                            Detail = $"{LangHelper.GetAttrName(capturedAttr)}: {beforeVal} → {afterVal} ({L("効率", "Eff", "效率")}: {eff:P0})",
                            AccentColor = SpectralGreen
                        };

                        if (rampage != null)
                        {
                            HandleRampage(mgr, capturedServant, rampage.Value);
                        }
                    }
                };

                if (_quickInjectMode && projectedInjectRampageChance == 0)
                {
                    doInject();
                }
                else
                {
                    string msg = $"{LangHelper.GetAttrName(capturedAttr)} ← {LangHelper.GetSoulName(capturedSoul)} x{capturedCount}";
                    if (projectedInjectRampageChance > 0)
                        msg += $"\n⚠ {L("暴走率", "Rampage", "暴走率")}: {projectedInjectRampageChance}%";
                    ShowConfirmDialog(msg, doInject);
                }
            }

            // Quick Inject toggle
            var quickStyle = _quickInjectMode ? _selectedButtonStyle : _buttonStyle;
            string quickLabel = _quickInjectMode
                ? L("◉ Quick", "◉ Quick", "◉ Quick")
                : L("○ Quick", "○ Quick", "○ Quick");
            if (GUILayout.Button(quickLabel, quickStyle, GUILayout.Width(80)))
            {
                _quickInjectMode = !_quickInjectMode;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            } // end _modPanelTab == 0 (Inject)

            if (_modPanelTab == 1)
            {
            // ── Body Augmentation ──
            GUILayout.Label(L("▸ 部位増設", "▸ Body Augmentation", "▸ 部位增设"), _headerStyle);
            GUILayout.Space(2);

            var augEnh = mgr.GetEnhancement(servant.uid);
            int totalAdded = augEnh.AddedBodyParts;
            bool totalAtMax = totalAdded >= NecromancyManager.MaxTotalAddedParts;
            GUILayout.Label($"{L("合計増設", "Total Added", "合计增设")}: {totalAdded}/{NecromancyManager.MaxTotalAddedParts}",
                totalAtMax ? _warningStyle : _descStyle);
            GUILayout.Space(2);

            // Slot selection
            foreach (var slot in NecromancyManager.AugmentableSlots)
            {
                int currentCount = NecromancyManager.CountBodySlots(servant, slot.SlotId);
                int added = mgr.GetSlotAdditions(servant.uid, slot.SlotId);

                GUILayout.BeginHorizontal();
                bool slotSelected = _modSelectedSlot == slot.SlotId;
                var slotStyle = slotSelected ? _selectedButtonStyle : _buttonStyle;
                GUI.enabled = !totalAtMax && !_pendingConfirm;
                if (GUILayout.Button(slot.GetSlotName(), slotStyle, GUILayout.Width(60)))
                {
                    _modSelectedSlot = slotSelected ? -1 : slot.SlotId;
                    _augmentCorpses.Clear();
                }
                GUI.enabled = true;
                GUILayout.Label($"({L("現在", "Cur", "当前")}: {currentCount}, +{added})",
                    _descStyle, GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }

            // Material corpse selection (if a slot is selected)
            if (_modSelectedSlot > 0)
            {
                var selectedSlot = Array.Find(NecromancyManager.AugmentableSlots, s => s.SlotId == _modSelectedSlot);
                if (selectedSlot != null)
                {
                    GUILayout.Space(4);

                    var availableCorpses = mgr.FindCorpses();
                    // Clean up stale refs
                    _augmentCorpses.RemoveWhere(t => !availableCorpses.Contains(t));

                    var compatibleCorpses = availableCorpses
                        .Where(c => NecromancyManager.CorpseHasBodyPart(c, selectedSlot.FigureName))
                        .ToList();

                    GUILayout.Label(string.Format(L("▸ {0}の素材死体を選択 ({1}体)",
                        "▸ Select material corpses for {0} ({1})",
                        "▸ 选择{0}的素材尸体 ({1}体)"),
                        selectedSlot.GetSlotName(), compatibleCorpses.Count), _descStyle);
                    GUILayout.Space(2);

                    if (compatibleCorpses.Count == 0)
                    {
                        GUILayout.Label(string.Format(
                            L("適合する素材がありません。({0}を持つ種族の死体が必要)",
                              "No suitable materials. (Requires corpse of a race with {0})",
                              "没有合适的素材。(需要拥有{0}的种族的尸体)"),
                            selectedSlot.GetSlotName()), _badStyle);
                    }
                    else
                    {
                        // Auto-select / Clear buttons
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(L("自動選択", "Auto-select", "自动选择"), _buttonStyle, GUILayout.Width(90)))
                        {
                            _augmentCorpses.Clear();
                            int resonance = mgr.GetSlotResonance(servant.uid, _modSelectedSlot);
                            int needed = 1;
                            while (needed < compatibleCorpses.Count
                                   && NecromancyManager.CalculateAugmentRate(needed, resonance) < 0.95)
                                needed++;
                            var sorted = compatibleCorpses.OrderBy(c => GetCorpseLv(c)).ToList();
                            int selected = Math.Min(needed, sorted.Count);
                            for (int i = 0; i < selected; i++)
                                _augmentCorpses.Add(sorted[i]);
                            double autoRate = NecromancyManager.CalculateAugmentRate(selected, resonance);
                            _autoSelectMessage = string.Format(
                                L("弱い順に{0}体を選択 (成功率: ~{1:P0})",
                                  "{0} weakest selected (rate: ~{1:P0})",
                                  "已按弱到强选择{0}体 (成功率: ~{1:P0})"),
                                selected, autoRate);
                        }
                        if (GUILayout.Button(L("解除", "Clear", "清除"), _buttonStyle, GUILayout.Width(60)))
                        {
                            _augmentCorpses.Clear();
                            _autoSelectMessage = null;
                        }
                        GUILayout.EndHorizontal();

                        if (_autoSelectMessage != null)
                            GUILayout.Label(_autoSelectMessage, _descStyle);

                        GUILayout.Space(2);

                        // Scrollable corpse list
                        _corpseScrollPos = GUILayout.BeginScrollView(
                            _corpseScrollPos,
                            _scrollStyle,
                            GUILayout.MaxHeight(800),
                            GUILayout.ExpandWidth(true));
                        foreach (var corpse in compatibleCorpses)
                        {
                            bool isSelected = _augmentCorpses.Contains(corpse);
                            GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(34));
                            string toggle = isSelected ? "[✓]" : "[  ]";
                            if (GUILayout.Button(toggle, _buttonStyle, GUILayout.Width(42), GUILayout.Height(26)))
                            {
                                if (isSelected) _augmentCorpses.Remove(corpse);
                                else _augmentCorpses.Add(corpse);
                                _autoSelectMessage = null;
                            }

                            GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Width(28), GUILayout.Height(28));
                            var iconRect = GUILayoutUtility.GetLastRect();
                            DrawCharaIcon(iconRect, sourceId: corpse.c_idRefCard);

                            GUILayout.Label($"{corpse.GetName(NameStyle.Full)} x{corpse.Num}", _labelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Label($"Lv.{GetCorpseLv(corpse)}", _descStyle, GUILayout.Width(52));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }

                    // Success rate preview with resonance
                    if (_augmentCorpses.Count > 0)
                    {
                        int resonance = mgr.GetSlotResonance(servant.uid, _modSelectedSlot);
                        double rate = NecromancyManager.CalculateAugmentRate(_augmentCorpses.Count, resonance);
                        string rateText = $"{L("成功率", "Success rate", "成功率")}: {rate:P0} ({_augmentCorpses.Count}{L("個投入", " corpses", "个投入")})";
                        if (resonance > 0)
                            rateText += $"  ({L("共鳴", "Resonance", "共鸣")}: +{resonance * 5}%)";
                        GUILayout.Label(rateText, rate >= 0.3 ? _goodStyle : _warningStyle);
                    }

                    // Augment button
                    GUI.enabled = _augmentCorpses.Count > 0 && !_pendingConfirm;
                    if (GUILayout.Button(L("増設する", "Augment", "增设"), _buttonStyle, GUILayout.Width(120)))
                    {
                        var capturedServant = servant;
                        int capturedSlotId = _modSelectedSlot;
                        var capturedCorpses = new List<Thing>(_augmentCorpses);
                        int capturedResonance = mgr.GetSlotResonance(servant.uid, capturedSlotId);
                        double rate = NecromancyManager.CalculateAugmentRate(capturedCorpses.Count, capturedResonance);

                        string msg = string.Format(
                            "{0} ← {1} ({2}: {3:P0})",
                            selectedSlot.GetSlotName(),
                            $"{capturedCorpses.Count}{L("個の死体", " corpses", "个尸体")}",
                            L("成功率", "Rate", "成功率"),
                            rate);
                        if (rampageChance > 0)
                            msg += $"\n⚠ {L("暴走率", "Rampage", "暴走率")}: {rampageChance}%";

                        ShowConfirmDialog(msg, () =>
                        {
                            _lastResult = null;
                            var augSlot = Array.Find(NecromancyManager.AugmentableSlots, s => s.SlotId == capturedSlotId);
                            string slotName = augSlot?.GetSlotName() ?? $"Slot{capturedSlotId}";
                            bool success = mgr.AugmentBodyPart(capturedServant, capturedSlotId, capturedCorpses);
                            if (success)
                            {
                                LangHelper.Say("augmentSuccess", capturedServant);
                                EClass.pc.pos.PlayEffect("mutation");
                                EClass.pc.pos.PlaySound("mutation");
                                _lastResult = new ActionResult
                                {
                                    Title = $"{slotName} {L("増設成功", "Augment Success", "增设成功")}",
                                    Detail = $"{L("成功率", "Rate", "成功率")}: {rate:P0}, {L("共鳴リセット", "Resonance reset", "共鸣重置")}",
                                    AccentColor = SpectralGreen
                                };

                                var rampage = mgr.CheckRampage(capturedServant);
                                if (rampage != null)
                                {
                                    HandleRampage(mgr, capturedServant, rampage.Value);
                                }
                            }
                            else
                            {
                                LangHelper.Say("augmentFailed");
                                EClass.pc.pos.PlaySound("fail");
                                int newResonance = mgr.GetSlotResonance(capturedServant.uid, capturedSlotId);
                                _lastResult = new ActionResult
                                {
                                    Title = $"{slotName} {L("増設失敗", "Augment Failed", "增设失败")}",
                                    Detail = $"{L("素材消費済", "Materials consumed", "素材已消耗")}, {L("共鳴", "Resonance", "共鸣")}: +{newResonance * 5}%",
                                    AccentColor = BloodCrimson
                                };
                            }
                            _augmentCorpses.Clear();
                        });
                    }
                    GUI.enabled = true;
                }
            }
            } // end _modPanelTab == 1 (Augment)
        }

        private static void HandleRampage(NecromancyManager mgr, Chara servant, RampageResult result)
        {
            mgr.ExecuteRampage(servant, result);

            switch (result)
            {
                case RampageResult.DarkAwakening:
                    LangHelper.Say("rampageDarkAwakening", servant);
                    EClass.pc.pos.PlayEffect("buff");
                    EClass.pc.pos.PlaySound("buff");
                    _lastResult = new ActionResult
                    {
                        Title = L("★ 闇の覚醒", "★ Dark Awakening", "★ 暗之觉醒"),
                        Detail = L("強化効果2倍 + MAG+5", "Enhancement doubled + MAG+5", "强化效果2倍 + MAG+5"),
                        AccentColor = GildedGold
                    };
                    return;
                case RampageResult.Berserk:
                    LangHelper.Say("rampageBerserk", servant);
                    _lastResult = new ActionResult
                    {
                        Title = L("△ 暴走", "△ Berserk", "△ 暴走"),
                        Detail = L("一時的に敵対化", "Temporarily hostile", "临时敌对化"),
                        AccentColor = AmberWarning
                    };
                    break;
                case RampageResult.SelfDestruct:
                    LangHelper.Say("rampageSelfDestruct", servant);
                    _lastResult = new ActionResult
                    {
                        Title = L("× 自爆", "× Self-Destruct", "× 自爆"),
                        Detail = L("従者が爆発した", "Servant exploded", "仆从爆炸了"),
                        AccentColor = BloodCrimson
                    };
                    _selectedServant = null;
                    break;
                case RampageResult.MutationAwakening:
                    LangHelper.Say("rampageMutationAwakening", servant);
                    EClass.pc.pos.PlayEffect("mutation");
                    EClass.pc.pos.PlaySound("mutation");
                    _lastResult = new ActionResult
                    {
                        Title = L("★ 変異覚醒", "★ Mutation Awakening", "★ 变异觉醒"),
                        Detail = L("ランダム部位追加 + 一時暴走", "Random body part + temporary berserk", "随机部位追加 + 临时暴走"),
                        AccentColor = GildedGold
                    };
                    return;
            }

            EClass.pc.pos.PlayEffect("curse");
            EClass.pc.pos.PlaySound("curse");
        }

        private static void DrawResultCard()
        {
            if (_lastResult == null) return;

            if (UiTheme.Ars.UseSimpleResultCard)
            {
                GUILayout.Space(2);
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label(_lastResult.Title, _labelStyle);
                GUILayout.Label(_lastResult.Detail, _descStyle);
                if (GUILayout.Button("✕", _closeButtonStyle, GUILayout.Width(28), GUILayout.Height(20)))
                    _lastResult = null;
                GUILayout.EndVertical();
                GUILayout.Space(2);
                return;
            }

            GUILayout.Space(2);
            var cardRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(42), GUILayout.ExpandWidth(true));

            // Background
            GUI.DrawTexture(cardRect, GetSolidTex(CharredVellum));
            // Left accent border
            GUI.DrawTexture(new Rect(cardRect.x, cardRect.y, 3, cardRect.height), GetSolidTex(_lastResult.AccentColor));

            // Title
            var titleRect = new Rect(cardRect.x + 10, cardRect.y + 4, cardRect.width - 40, 18);
            GUI.Label(titleRect, _lastResult.Title, new GUIStyle(_labelStyle) { normal = { textColor = _lastResult.AccentColor } });

            // Detail
            var detailRect = new Rect(cardRect.x + 10, cardRect.y + 22, cardRect.width - 40, 16);
            GUI.Label(detailRect, _lastResult.Detail, _descStyle);

            // Dismiss button
            var dismissRect = new Rect(cardRect.x + cardRect.width - 24, cardRect.y + 4, 20, 18);
            if (GUI.Button(dismissRect, "✕", _closeButtonStyle))
            {
                _lastResult = null;
            }

            GUILayout.Space(2);
        }
    }
}
