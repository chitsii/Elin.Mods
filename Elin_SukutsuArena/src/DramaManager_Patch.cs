using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Commands;
using UnityEngine;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// DramaManagerへのHarmonyパッチ
    /// 独自アクション modInvoke / modFocusChara を Postfix で処理する。
    /// バニラのアクション名前空間には一切干渉しない。
    /// </summary>
    [HarmonyPatch(typeof(DramaManager))]
    public static class DramaManager_Patch
    {
        /// <summary>
        /// DramaManager.ParseLine の後にカスタムアクションを処理
        /// ArenaCommandRegistry にディスパッチ
        /// </summary>
        [GameDependency("Patch", "DramaManager.ParseLine", "High", "Method name/signature may change")]
        [HarmonyPostfix]
        [HarmonyPatch("ParseLine")]
        public static void ParseLine_Postfix(DramaManager __instance, Dictionary<string, string> item)
        {
            try
            {
                if (!item.ContainsKey("action"))
                    return;

                string[] actionParts = item["action"].Split('/');
                string action = actionParts[0];

                // modFocusChara: バニラ focusChara の安全版。
                // バニラの focusChara は FindChara() が null を返すと NullReferenceException でドラマが停止する。
                // バニラのアクションを Prefix で横取りすると他Mod/CWLに干渉するため、
                // modInvoke と同様に独自アクション名で Postfix 処理する。
                if (action == "modFocusChara")
                {
                    string charaId = item.ContainsKey("param") ? item["param"] : "";
                    __instance.AddEvent(delegate
                    {
                        var chara = EClass._map?.FindChara(charaId);
                        if (chara != null)
                        {
                            Point pos = chara.pos.Copy();
                            EClass.scene.screenElin.focusOption = new BaseGameScreen.FocusOption
                            {
                                pos = pos,
                                speed = 2f
                            };
                        }
                        else
                        {
                            ModLog.Log($"[modFocusChara] Character not found: {charaId}, skipping");
                        }
                    });
                    return;
                }

                if (action != "modInvoke")
                    return;

                string param = item.ContainsKey("param") ? item["param"] : "";
                if (string.IsNullOrEmpty(param))
                {
                    Debug.LogError("[ArenaModInvoke] Missing param for modInvoke action");
                    return;
                }

                // jumpカラムを取得
                string jump = item.ContainsKey("jump") ? item["jump"] : "";

                // PendingJumpTargetを使用するコマンド（switch_flag, if_flag）
                // jump列は使用せず、コマンド自身がジャンプ先を決定する
                if (param.StartsWith("switch_flag(") || param.StartsWith("if_flag("))
                {
                    var method = new DramaEventMethod(() =>
                    {
                        // action は空（jumpFuncで処理するため）
                    });
                    method.action = null;
                    method.jumpFunc = () =>
                    {
                        SwitchFlagCommand.PendingJumpTarget = null;
                        HandleModInvoke(__instance, param);
                        var target = SwitchFlagCommand.PendingJumpTarget;
                        ModLog.Log($"[ArenaModInvoke] jumpFunc returning: {target ?? "(null)"}");
                        return target ?? "";
                    };
                    __instance.AddEvent(method);
                }
                else
                {
                    // jumpがない場合は通常のイベント追加
                    __instance.AddEvent(delegate
                    {
                        HandleModInvoke(__instance, param);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArenaModInvoke] Error in ParseLine_Postfix: {ex}");
            }
        }

        /// <summary>
        /// modInvoke アクションのハンドラ
        /// ArenaCommandRegistry にディスパッチ
        /// </summary>
        private static void HandleModInvoke(DramaManager manager, string param)
        {
            try
            {
                ModLog.Log($"[ArenaModInvoke] HandleModInvoke: param='{param}'");

                var (methodName, args) = ParseMethodCall(param);

                if (string.IsNullOrEmpty(methodName))
                {
                    Debug.LogError($"[ArenaModInvoke] Invalid format: {param}");
                    return;
                }

                ModLog.Log($"[ArenaModInvoke] Method: {methodName}, Args: [{string.Join(", ", args)}]");

                // ArenaCommandRegistry にディスパッチ
                bool handled = ArenaCommandRegistry.TryExecute(methodName, manager, args);

                if (!handled)
                {
                    Debug.LogWarning($"[ArenaModInvoke] Unknown command: {methodName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArenaModInvoke] Error handling modInvoke: {ex}");
            }
        }

        /// <summary>
        /// メソッド呼び出し形式の文字列をパース
        /// 例: "methodName(arg1, arg2)" -> ("methodName", ["arg1", "arg2"])
        /// </summary>
        private static (string name, string[] args) ParseMethodCall(string param)
        {
            var openParen = param.IndexOf('(');
            if (openParen < 0)
            {
                return (null, Array.Empty<string>());
            }

            var name = param.Substring(0, openParen).Trim();
            var argsStr = param.Substring(openParen + 1, param.Length - openParen - 2);

            if (string.IsNullOrWhiteSpace(argsStr))
            {
                return (name, Array.Empty<string>());
            }

            var args = argsStr.Split(',').Select(s => s.Trim()).ToArray();
            return (name, args);
        }
    }

}
