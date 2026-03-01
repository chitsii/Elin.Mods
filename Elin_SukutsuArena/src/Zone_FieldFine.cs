using System.IO;
using System.Reflection;

namespace Elin_SukutsuArena;

/// <summary>
/// 通常フィールド用カスタムゾーン
/// カスタムマップ chitsii_battle_field_fine.z を読み込む
/// </summary>
public class Zone_FieldFine : Zone_Field
{
    /// <summary>
    /// 敵の自然発生を無効化
    /// Zone_Field は PrespawnRate = 1.2f だが、アリーナ戦闘では不要
    /// </summary>
    public override float PrespawnRate => 0f;

    private const string MAP_FILE_NAME = "chitsii_battle_field_fine.z";

    /// <summary>
    /// カスタムマップファイルのパス
    /// LangMod/EN/Maps/chitsii_battle_field_fine.z を読み込む
    /// </summary>
    public override string pathExport
    {
        get
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string customMap = Path.Combine(modPath, "LangMod/EN/Maps", MAP_FILE_NAME);

            if (File.Exists(customMap))
            {
                ModLog.Log($"[SukutsuArena] Loading custom field map: {customMap}");
                return customMap;
            }

            ModLog.Log($"[SukutsuArena] Custom map not found: {customMap}, using default");
            return base.pathExport;
        }
    }
}

