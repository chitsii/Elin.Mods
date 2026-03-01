using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static partial class ArsMoriendiGUI
    {
        // ── Chapter Four Tab ──

        private static void DrawChapterFourTab()
        {
            var quest = NecromancyManager.Instance.QuestPath;
            var stage = quest.CurrentStage;

            // Author line
            string author = quest.GetTomeAuthor();
            GUILayout.Label(
                string.Format(L("── 著者: {0} ──", "── Author: {0} ──", "── 著者: {0} ──"), author),
                _flavorStyle);
            GUILayout.Space(6);
            DrawDivider();
            GUILayout.Space(6);

            // Journal entries (cumulative)
            var entries = ChapterFourContent.GetEntries(stage);
            bool useInkColors = UiTheme.Ars.UseInkColorsInChapterFour;
            foreach (var entry in entries)
            {
                if (entry.InkColor == ChapterFourContent.SystemInk)
                {
                    GUILayout.Space(8);
                    if (!useInkColors)
                    {
                        GUILayout.Label(entry.Text, _journalHeaderStyle);
                    }
                    else
                    {
                        GUILayout.Label(entry.Text, new GUIStyle(_journalHeaderStyle)
                        {
                            normal = { textColor = entry.InkColor }
                        });
                    }
                    GUILayout.Space(4);
                }
                else
                {
                    if (!string.IsNullOrEmpty(entry.AuthorLabel))
                    {
                        GUILayout.Space(4);
                        if (!useInkColors)
                        {
                            GUILayout.Label(entry.AuthorLabel, _journalAuthorStyle);
                        }
                        else
                        {
                            GUILayout.Label(entry.AuthorLabel, new GUIStyle(_journalAuthorStyle)
                            {
                                normal = { textColor = entry.InkColor }
                            });
                        }
                    }
                    if (!useInkColors)
                    {
                        GUILayout.Label(entry.Text, _journalStyle);
                    }
                    else
                    {
                        GUILayout.Label(entry.Text, new GUIStyle(_journalStyle)
                        {
                            normal = { textColor = entry.InkColor }
                        });
                    }
                    GUILayout.Space(4);
                }
            }

            GUILayout.Space(8);
            DrawDivider();
            GUILayout.Space(8);

            // ── Stage-specific action buttons ──
            DrawChapterFourActions(quest, stage);

            // Pursuit pause (encounter stages with pending encounters only)
            if (KnightEncounter.HasPendingEncounter())
                DrawPursuitPauseSection();
        }

        private static void DrawChapterFourActions(UnhallowedPath quest, UnhallowedStage stage)
        {
            switch (stage)
            {
                case UnhallowedStage.Stage7:
                    // "Face the trial" button — triggers Erenos boss
                    if (GUILayout.Button(
                        L("▶ 試練に臨む", "▶ Face the Trial", "▶ 迎接试炼"),
                        _buttonStyle, GUILayout.Width(200)))
                    {
                        ShowConfirmDialog(
                            L("エレノスの影が現れる。戦う覚悟はあるか？",
                              "The shadow of Erenos will appear. Are you prepared to fight?",
                              "艾雷诺斯之影将会出现。你做好战斗的觉悟了吗？"),
                            () =>
                            {
                                KnightEncounter.SetPursuitPaused(false);
                                Hide();
                                QuestDrama.PlayDeferred("ars_erenos_appear", onComplete: () =>
                                {
                                    ErenosBattle.SpawnBoss();
                                });
                            });
                    }
                    break;

                case UnhallowedStage.Stage8:
                    DrawApotheosisRitualSlots(quest);
                    break;

                default:
                    if (stage >= UnhallowedStage.Stage9)
                    {
                        GUILayout.Label(
                            L("✦ 昇華完了 ─ アルス・モリエンディの継承者 ✦",
                              "✦ Apotheosis Complete ─ Inheritor of Ars Moriendi ✦",
                              "✦ 升华完成 ─ Ars Moriendi 的继承者 ✦"),
                            _goodStyle);
                    }
                    break;
            }
        }

        // ── Apotheosis Ritual 6-Slot UI ──

        private static void DrawApotheosisRitualSlots(UnhallowedPath quest)
        {
            var mgr = NecromancyManager.Instance;

            GUILayout.Label(LangHelper.Get("ritualHeader"), _headerStyle);
            GUILayout.Space(6);

            // "Read Notes" button — gives/opens the ritual notes book
            if (GUILayout.Button(LangHelper.Get("ritualNotesButton"), _buttonStyle, GUILayout.Width(180)))
            {
                var pc = EClass.pc;
                if (pc != null && !pc.things.List(t => t.id == "ars_ritual_notes", onlyAccessible: true).Any())
                {
                    var notes = ThingGen.Create("ars_ritual_notes");
                    pc.AddThing(notes);
                    LangHelper.Say("ritualNotesGiven");
                }
                var book = pc?.things.List(t => t.id == "ars_ritual_notes", onlyAccessible: true).FirstOrDefault();
                if (book != null) book.trait.OnUse(pc);
            }
            GUILayout.Space(6);

            for (int i = 0; i < NecromancyManager.RitualSlotCount; i++)
            {
                int available = mgr.CountRitualMaterial(i);
                int required = NecromancyManager.RitualRequired[i];
                bool satisfied = available >= required;

                GUILayout.BeginHorizontal();

                // Slot status icon
                GUILayout.Label(satisfied ? "✓" : "✗",
                    satisfied ? _goodStyle : _badStyle, GUILayout.Width(20));

                // Slot name + count
                string slotName = LangHelper.Get(NecromancyManager.RitualSlotNameKeys[i]);
                GUILayout.Label(slotName, _labelStyle, GUILayout.Width(205));
                GUILayout.Label($"({available}/{required})",
                    satisfied ? _goodStyle : _badStyle, GUILayout.Width(74));

                // Show flavor text only when this material requirement is satisfied.
                if (satisfied)
                {
                    GUILayout.Space(14);
                    GUILayout.Label(
                        LangHelper.Get(NecromancyManager.RitualSlotCommentKeys[i]),
                        _flavorStyle);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            GUILayout.Space(8);

            bool allSatisfied = mgr.HasAllRitualMaterials();

            // Perform Ritual button
            GUI.enabled = allSatisfied && !_pendingConfirm;
            if (GUILayout.Button(
                L("▶ 儀式を行う", "▶ Perform the Ritual", "▶ 进行仪式"),
                _buttonStyle, GUILayout.Width(200)))
            {
                ShowConfirmDialog(
                    L("昇華の儀式を行う。全ての素材を消費する。",
                      "Perform the apotheosis ritual. All materials will be consumed.",
                      "进行升华仪式。将消耗所有素材。"),
                    () =>
                    {
                        if (quest.ApplyApotheosis())
                        {
                            LangHelper.Say("apotheosisComplete");
                            Hide();
                        }
                    });
            }
            GUI.enabled = true;
        }

        // ── Pursuit Pause Section ──

        private static void DrawPursuitPauseSection()
        {
            GUILayout.Space(8);
            DrawDivider();
            GUILayout.Space(4);

            bool paused = KnightEncounter.IsPursuitPaused();

            GUILayout.BeginVertical(_boxStyle);

            if (paused)
            {
                GUILayout.Label(LangHelper.Get("pursuitPauseActive"), _flavorStyle);
                GUILayout.Space(4);
                if (GUILayout.Button(LangHelper.Get("pursuitPauseOff"), _buttonStyle, GUILayout.Height(26)))
                {
                    KnightEncounter.SetPursuitPaused(false);
                }
            }
            else
            {
                GUILayout.Label(LangHelper.Get("pursuitPauseOffer"), _flavorStyle);
                GUILayout.Space(4);
                if (GUILayout.Button(LangHelper.Get("pursuitPauseOn"), _buttonStyle, GUILayout.Height(26)))
                {
                    KnightEncounter.SetPursuitPaused(true);
                }
            }

            GUILayout.EndVertical();
        }
    }
}
