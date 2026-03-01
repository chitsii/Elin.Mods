using System;
using System.Collections.Generic;
using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// アリーナコマンドの登録・実行を管理
    /// </summary>
    public static class ArenaCommandRegistry
    {
        private static readonly Dictionary<string, IArenaCommand> _commands = new Dictionary<string, IArenaCommand>();
        private static bool _initialized = false;

        /// <summary>
        /// 条件系コマンド（if_flag等）の最後の評価結果
        /// jumpFuncで使用される
        /// </summary>
        public static bool LastConditionResult { get; set; } = false;

        /// <summary>
        /// コマンドを登録
        /// </summary>
        public static void Register(IArenaCommand command)
        {
            if (command == null) return;
            _commands[command.Name] = command;
        }

        /// <summary>
        /// 全ての組み込みコマンドを登録
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            Register(new CheckQuestAvailableCommand());
            Register(new CheckAvailableQuestsCommand());
            Register(new CheckQuestsForDispatchCommand());
            Register(new StartQuestCommand());
            Register(new StartDramaCommand());
            Register(new CompleteQuestCommand());
            Register(new IfFlagCommand());
            Register(new SwitchFlagCommand());
            Register(new DebugLogFlagsCommand());
            Register(new DebugLogQuestsCommand());
            Register(new LeavePartyToArenaCommand());
            Register(new ApplyIrisTrainingBuffCommand());
            Register(new IrisTrainingPunishCommand());
            Register(new ApplyDeepEcstasyCommand());
            Register(new StartNewgameCommand());
            Register(new StartUninstallCommand());
            Register(new CheckResurrectionMaterialsCommand());
            Register(new ConsumeResurrectionMaterialsCommand());
            Register(new SpawnCainCommand());
            Register(new AddChickensToPartyCommand());
            Register(new UpdateLilyShopStockCommand());
            Register(new InvokeOheyaScrollCommand());
            Register(new HideNpcCommand());
            Register(new FullRecoveryCommand());
            Register(new CheckHasWrathCommand());
            Register(new PayWrathFeeCommand());

            _initialized = true;
            ModLog.Log($"[ArenaCommandRegistry] Initialized with {_commands.Count} commands");
        }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        /// <param name="name">コマンド名</param>
        /// <param name="drama">DramaManagerインスタンス</param>
        /// <param name="args">引数</param>
        /// <returns>コマンドが見つかって実行されたらtrue</returns>
        public static bool TryExecute(string name, DramaManager drama, string[] args)
        {
            // 初回呼び出し時に自動初期化
            if (!_initialized) Initialize();

            if (!_commands.TryGetValue(name, out var command))
            {
                return false;
            }

            try
            {
                command.Execute(ArenaContext.I, drama, args);
                ModLog.Log($"[ArenaCommand] Executed: {name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArenaCommand] Error executing {name}: {ex}");
                return false;
            }
        }

    }
}

