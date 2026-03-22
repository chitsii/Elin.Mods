using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static partial class ArsMoriendiGUI
    {
        // ── Ritual Tab State ──
        private static Thing? _selectedCorpse;
        private static bool _showAllCorpses;
        private static bool _showRitualFormulaDetail;
        private static Dictionary<string, int> _soulSliders = new();

        private static void ResetRitualState()
        {
            _selectedCorpse = null;
            _showAllCorpses = false;
            _showRitualFormulaDetail = false;
            _soulSliders.Clear();
        }

        // ── Ritual Tab ──

        private static void DrawRitualTab()
        {
            var mgr = NecromancyManager.Instance;

            GUILayout.Label(LangHelper.Get("ritualFlavor"), _flavorStyle);
            GUILayout.Space(6);
            DrawDivider();
            GUILayout.Space(6);

            // ── Corpse Selection ──
            GUILayout.Label(L("◆ 死体を選択", "◆ Select Corpse", "◆ 选择尸体"), _headerStyle);
            GUILayout.Space(4);

            var corpses = mgr.FindCorpses();

            // Sort by corpse level descending
            corpses.Sort((a, b) => GetCorpseLv(b).CompareTo(GetCorpseLv(a)));

            if (_selectedCorpse != null && !corpses.Contains(_selectedCorpse))
                _selectedCorpse = null;

            if (corpses.Count == 0)
            {
                GUILayout.Label(L("インベントリに死体がありません。",
                    "No corpses in inventory.",
                    "物品栏中没有尸体。"), _badStyle);
            }
            else
            {
                int totalCount = corpses.Count;
                bool hasMore = totalCount > CorpsePageSize && !_showAllCorpses;
                int displayCount = hasMore ? CorpsePageSize : totalCount;

                for (int i = 0; i < displayCount; i++)
                {
                    var corpse = corpses[i];
                    var captured = corpse;
                    bool isSelected = _selectedCorpse == corpse;
                    var style = isSelected ? _selectedButtonStyle : _buttonStyle;

                    var corpseStyle = isSelected
                        ? new GUIStyle(_corpseButtonStyle) { normal = _selectedButtonStyle.normal }
                        : _corpseButtonStyle;
                    int cLv = GetCorpseLv(corpse);
                    string corpseLabel = $"(Lv.{cLv}) {corpse.GetName(NameStyle.Full)}";
                    float btnHeight = Mathf.Max(CorpseIconSize, corpseStyle.CalcHeight(new GUIContent(corpseLabel), 366f));

                    GUILayout.BeginHorizontal(GUILayout.Height(btnHeight));
                    GUILayout.Label(GUIContent.none, GUIStyle.none,
                        GUILayout.Width(CorpseIconSize), GUILayout.Height(CorpseIconSize));
                    var corpseIconRect = GUILayoutUtility.GetLastRect();
                    DrawCharaIcon(corpseIconRect, sourceId: corpse.c_idRefCard);
                    if (GUILayout.Button(corpseLabel, corpseStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(btnHeight)))
                    {
                        _selectedCorpse = captured;
                    }
                    GUILayout.EndHorizontal();
                }

                if (hasMore)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(
                        string.Format(L("▼ もっと見る ({0}件)", "▼ More ({0} items)", "▼ 显示更多 ({0}个)"),
                            totalCount - CorpsePageSize),
                        _buttonStyle, GUILayout.Width(200)))
                    {
                        _showAllCorpses = true;
                    }
                    GUILayout.EndHorizontal();
                }
                else if (totalCount > CorpsePageSize && _showAllCorpses)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(L("▲ たたむ", "▲ Collapse", "▲ 收起"),
                        _buttonStyle, GUILayout.Width(200)))
                    {
                        _showAllCorpses = false;
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(8);

            // ── Soul Selection (Quantity Sliders) ──
            GUILayout.Label(L("◆ 魂を選択", "◆ Select Souls", "◆ 选择灵魂"), _headerStyle);
            GUILayout.Label(L("  SU = Soul Unit（魂の力の単位）", "  SU = Soul Unit", "  SU = Soul Unit"), _descStyle);
            GUILayout.Space(4);

            var soulsByType = mgr.FindSoulsByType();
            int totalSU = 0;
            bool hasSouls = false;

            foreach (var soulId in NecromancyManager.SoulIds)
            {
                int owned = soulsByType.TryGetValue(soulId, out int soulCount) ? soulCount : 0;
                if (owned == 0 && !_soulSliders.ContainsKey(soulId)) continue;
                hasSouls = true;

                if (!_soulSliders.ContainsKey(soulId)) _soulSliders[soulId] = 0;
                int current = _soulSliders[soulId];
                if (current > owned) { current = owned; _soulSliders[soulId] = current; }

                int su = NecromancyManager.SoulUnitTable[soulId];
                int lineSU = su * current;
                totalSU += lineSU;

                GUILayout.BeginHorizontal();
                GUILayout.Label(LangHelper.GetSoulName(soulId), _labelStyle, GUILayout.Width(115));
                GUILayout.Label($"x{current}", _labelStyle, GUILayout.Width(30));

                float sliderVal = GUILayout.HorizontalSlider(current, 0, owned, GUILayout.Width(185));
                _soulSliders[soulId] = Mathf.RoundToInt(sliderVal);

                GUILayout.Label($"({L("所持", "Own", "持有")}: {owned})", _descStyle, GUILayout.Width(70));
                GUILayout.Label($"SU: {lineSU}", lineSU > 0 ? _goodStyle : _descStyle, GUILayout.Width(60));
                GUILayout.EndHorizontal();
            }

            if (!hasSouls)
            {
                GUILayout.Label(L("インベントリに魂がありません。",
                    "No souls in inventory.",
                    "物品栏中没有灵魂。"), _badStyle);
            }

            // ── SU Summary ──
            GUILayout.Space(4);
            DrawDivider();
            GUILayout.Space(4);

            int levelCap = NecromancyManager.GetLevelCap();
            int rawLv = NecromancyManager.CalculateResurrectionLevel(totalSU);
            int revivedLv = Math.Min(rawLv, levelCap);
            bool capped = revivedLv >= levelCap && totalSU > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("合計SU:", "Total SU:", "合计SU:"), _summaryLabelStyle, GUILayout.Width(80));
            GUILayout.Label($"{totalSU}", totalSU > 0 ? _summaryValueStyle : _summaryMissingStyle, GUILayout.Width(60));
            GUILayout.Label(L("蘇生後Lv:", "Raised Lv:", "复苏后Lv:"), _summaryLabelStyle, GUILayout.Width(85));
            GUILayout.Label($"{revivedLv}", _summaryValueStyle, GUILayout.Width(40));
            GUILayout.Label("/", _descStyle, GUILayout.Width(10));
            GUILayout.Label(L("上限:", "Cap:", "上限:"), _summaryLabelStyle, GUILayout.Width(40));
            GUILayout.Label($"{levelCap}", capped ? _warningStyle : _descStyle, GUILayout.Width(40));
            if (capped)
                GUILayout.Label(L("↑上限到達", "↑Capped", "↑已达上限"), _warningStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_showRitualFormulaDetail ? "[−]" : "[?]", _buttonStyle, GUILayout.Width(28), GUILayout.Height(20)))
            {
                _showRitualFormulaDetail = !_showRitualFormulaDetail;
            }
            GUILayout.EndHorizontal();

            if (_showRitualFormulaDetail)
            {
                DrawRitualFormulaDetail(totalSU, levelCap, revivedLv);
            }

            // ── Ritual Preparation Summary ──
            GUILayout.Space(8);
            DrawDivider();
            GUILayout.Space(6);

            GUILayout.Label(L("◆ 儀式の準備", "◆ Ritual Preparation", "◆ 仪式准备"), _headerStyle);
            GUILayout.Space(4);

            // Corpse summary
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("死体:", "Corpse:", "尸体:"), _summaryLabelStyle, GUILayout.Width(80));
            if (_selectedCorpse != null)
            {
                GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Width(24), GUILayout.Height(24));
                var summaryIconRect = GUILayoutUtility.GetLastRect();
                DrawCharaIcon(summaryIconRect, sourceId: _selectedCorpse.c_idRefCard);
                GUILayout.Label(_selectedCorpse.GetName(NameStyle.Full), _summaryValueStyle);
            }
            else
            {
                GUILayout.Label(L("未選択", "Not selected", "未选择"), _summaryMissingStyle);
            }
            GUILayout.EndHorizontal();

            // Soul summary
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("魂:", "Souls:", "灵魂:"), _summaryLabelStyle, GUILayout.Width(80));
            if (totalSU > 0)
            {
                string soulSummary = string.Join(", ",
                    _soulSliders.Where(kv => kv.Value > 0)
                        .Select(kv => $"{LangHelper.GetSoulName(kv.Key)} x{kv.Value}"));
                GUILayout.Label(soulSummary, _summaryValueStyle);
            }
            else
            {
                GUILayout.Label(L("未選択", "Not selected", "未选择"), _summaryMissingStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // ── Perform Ritual Button ──
            bool canPerform = _selectedCorpse != null && totalSU > 0;
            GUI.enabled = canPerform && !_pendingConfirm;
            if (GUILayout.Button(L("儀式を行う", "Perform Ritual", "进行仪式"), _buttonStyle, GUILayout.Width(200)))
            {
                ShowRitualConfirmation(mgr, totalSU, revivedLv);
            }
            GUI.enabled = true;
        }

        private static void ShowRitualConfirmation(NecromancyManager mgr, int totalSU, int revivedLv)
        {
            if (_selectedCorpse == null || totalSU <= 0) return;

            var corpse = _selectedCorpse;
            var amounts = new Dictionary<string, int>(_soulSliders.Where(kv => kv.Value > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value));

            string confirmMsg = string.Format(
                "{0} → Lv.{1} {2}\n(SU: {3})",
                corpse.GetName(NameStyle.Full),
                revivedLv,
                L("の従者として蓋らせる", "servant", "的仆从"),
                totalSU);

            ShowConfirmDialog(confirmMsg, () =>
            {
                var servant = mgr.PerformRitual(corpse, amounts);
                if (servant != null)
                {
                    LangHelper.Say("ritualSuccess");
                    EClass.pc.pos.PlayEffect("revive");
                    EClass.pc.pos.PlaySound("revive");
                    _selectedCorpse = null;
                    _soulSliders.Clear();
                }
                else
                {
                    LangHelper.Say("ritualFailed");
                }
            });
        }

        private static void DrawRitualFormulaDetail(int totalSU, int levelCap, int revivedLv)
        {
            int deepest = EClass.player?.stats?.deepest ?? 1;
            double ratio = Math.Min(1.0, (double)totalSU / NecromancyCalculations.MaxResurrectionSU);
            double recoveryRate = NecromancyCalculations.BaseRecoveryRate
                + (1.0 - NecromancyCalculations.BaseRecoveryRate) * Math.Sqrt(ratio);
            int corpseLv = _selectedCorpse != null ? GetCorpseLv(_selectedCorpse) : 0;

            GUILayout.Space(4);
            GUILayout.Label(
                L("  深さで上限、魂で到達率を決めます。", "  Depth sets the cap; souls set how much of it you reach.", "  深度决定上限，灵魂决定你能到达几成。"),
                _summaryLabelStyle);
            GUILayout.Label(
                $"  {L("上限Lv", "Cap", "上限Lv")} = floor({L("最深階層", "Deepest", "最深层")} x 1.5) = floor({deepest} x 1.5) = {levelCap}",
                _descStyle);
            GUILayout.Label(
                $"  SU = {totalSU}    /    {L("到達率", "Reach", "到达率")} = 5% + 95% x sqrt(min(1, SU / {NecromancyCalculations.MaxResurrectionSU})) = {recoveryRate:P1}",
                _descStyle);
            GUILayout.Label(
                $"  {L("蘇生後Lv", "Raised Lv", "复苏后Lv")} = floor({levelCap} x {recoveryRate:F3}) = {revivedLv}",
                revivedLv >= levelCap && totalSU > 0 ? _warningStyle : _descStyle);
            GUILayout.Label(
                $"  {L("注", "Note", "注")}: {L("死体Lvは儀式Lv計算に使いません", "Corpse Lv is not used in ritual level calculation", "尸体Lv不参与仪式等级计算")}"
                + (_selectedCorpse != null ? $" ({L("選択死体Lv", "Selected corpse Lv", "所选尸体Lv")}: {corpseLv})" : string.Empty),
                _descStyle);
            GUILayout.Label(
                $"  {L("補足", "Tip", "补充")}: {NecromancyCalculations.MaxResurrectionSU} SU = 100% {L("到達", "reach", "到达")} ({ratio:P1})",
                _descStyle);
        }
    }
}
