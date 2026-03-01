using System.Reflection;
using System.Text;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// CWLコンソールコマンド（F7でアクセス）
    /// コマンド形式: /arena.dryrun
    /// </summary>
    [ConsoleCommandClassCustomizer("arena")]
    public class ArenaConsole
    {
        public static void Register()
        {
            ReflexCLI.CommandRegistry.assemblies.Add(Assembly.GetExecutingAssembly());
            ModLog.Log("[SukutsuArena] Console commands registered (arena.*)");
        }

        /// <summary>
        /// ドライラン: 周回とアンインストール時の削除内容を比較表示（実際には削除しない）
        /// 使用方法: /arena.dryrun
        /// </summary>
        [ConsoleCommand("dryrun")]
        public static string DryRun()
        {
            var ng = Reset.ArenaResetManager.DryRun(Reset.ResetLevel.NewGamePlus);
            var un = Reset.ArenaResetManager.DryRun(Reset.ResetLevel.Uninstall);

            var sb = new StringBuilder();
            sb.AppendLine("=== Reset Dry Run (No changes made) ===");
            sb.AppendLine("");
            sb.AppendLine("                      NewGame+  Uninstall  Diff");
            sb.AppendLine("                      --------  ---------  ----");
            sb.AppendLine($"Flags to remove:      {ng.FlagsRemoved,8}  {un.FlagsRemoved,9}  {un.FlagsRemoved - ng.FlagsRemoved,+4}");
            sb.AppendLine($"Pets to dismiss:      {ng.PetsRemoved,8}  {un.PetsRemoved,9}  {un.PetsRemoved - ng.PetsRemoved,+4}");
            sb.AppendLine($"Quests to remove:     {ng.QuestsRemoved,8}  {un.QuestsRemoved,9}  {un.QuestsRemoved - ng.QuestsRemoved,+4}");
            sb.AppendLine($"Feats to remove:      {"-",8}  {un.FeatsRemoved,9}");
            sb.AppendLine($"Conditions to remove: {"-",8}  {un.ConditionsRemoved,9}");
            sb.AppendLine($"Arena zone exists:    {"-",8}  {(un.ZoneRemoved ? "Yes" : "No"),9}");
            sb.AppendLine($"Extra zones:          {"-",8}  {un.ExtraZonesRemoved,9}");
            sb.AppendLine("");
            sb.AppendLine("NewGame+: Rank is preserved");
            sb.AppendLine("Uninstall: All data removed");

            return sb.ToString();
        }
    }
}

