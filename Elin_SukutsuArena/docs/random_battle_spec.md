# ランダムバトル（通常戦闘）仕様書

本ドキュメントは、アリーナの通常戦闘（ランダムバトル）システムの現行仕様を記述する。

## 概要

ランダムバトルは、ランクアップ試練とは別に毎日挑戦できる通常戦闘。
ゲーム内1日ごとに新しい戦闘が生成され、プラチナコイン10枚でリロール可能。

## スケーリング基準値

**唯一の入力**: `EClass.player.stats.deepest`（プレイヤーのネフィア最深層到達記録）

```
取得経路:
  RandomBattleDebug.GetEffectiveDeepest()
    → _overrideDeepest ?? EClass.player.stats.deepest
```

DEBUGビルドではオーバーライド可能（`src/Debug/RandomBattleDebug.cs:129-131`）。

**アリーナランク（G-S）は敵レベル選定に直接影響しない。** ランクは報酬基本値とギミック最大数にのみ影響する。

## 敵選定・レベルスケーリング

### 敵種の選定

バニラの `SpawnList` からプレイヤー深度に適合したレベル帯の敵を**選定**する。

```csharp
// src/RandomBattle/RandomEnemyGenerator.cs:418-424
SpawnList spawnList = SpawnList.Get("chara");
var selected = spawnList.Select(genLv, 10);  // genLv = deepest
```

- `SpawnList.Select(genLv, lv)` はバニラのスポーンシステムで、`genLv` に適合するキャラを確率的に返す
- ランクアップ試練の敵（`sukutsu_` prefix）は `battle.py` で固定定義、SourceChara のLVを使用

### レベルスケーリング (SetLv)

選定された敵は `CharaGen.Create(id)` で生成後、`SetLv(deepest)` でプレイヤー深度に合わせてレベルスケーリングされる。

```csharp
// src/ZonePreEnterArenaBattle.cs (SpawnSingleEnemy)
EClass._zone.AddCard(enemy, pos);  // Zone追加後に実行（Feat.Applyのため）
if (config.Level > 0 && config.Level > enemy.LV)
{
    enemy.SetLv(config.Level);
}
```

- `EnemyConfig.Level = 0` → SetLv を呼ばない（ランクアップ試練: SourceChara既定LV使用）
- `EnemyConfig.Level > 0` → SetLv でスケーリング（ランダムバトル: deepest に合わせる）
- `config.Level > enemy.LV` のガード: SetLv でレベルダウンすると NullReferenceException が発生するため
- BossWithMinions の取り巻きは `deepest / 2` でスケーリング（ボスより弱め）

## 敵数スケーリング

```
敵数 = min(100, max(5, 5 + deepest / 5))
```

`src/RandomBattle/RandomEnemyGenerator.cs:38-47`

| deepest | 敵数 |
|---------|------|
| 1-24    | 5    |
| 25      | 10   |
| 50      | 15   |
| 100     | 25   |
| 250     | 55   |
| 475+    | 100  |

## スポーンパターン

`src/RandomBattle/RandomEnemyGenerator.cs:54-132`

深度に応じて危険なパターンが解放される。抽選は上から順に評価され、最初に当選したパターンが使用される。

### パターン解放条件

| パターン | 解放深度 | 確率 | 備考 |
|---------|---------|------|------|
| Horde | 常時 | - | deepest < 20 では確定 |
| Mixed | 20+ | 50% | Horde と択一（deepest 20-49） |
| UndeadArmy | 20+ | 10% | アンデッド種族のみ |
| SeaCreatures | 20+ | 10% | 水棲生物のみ |
| Kamikazes | 30+ | 7% | 自爆持ちのみ |
| CosmicHorror | 50+ | 7% | イス系/ホラー種族のみ |
| BossRush | 80+ | 10% | 3-5体のボス級 |
| BossWithMinions | 50+ | 25% | ボス1体 + 取り巻き |
| Random | 50+ | 25% | 完全ランダム |

deepest 50+ の基本パターン（特殊パターン非当選時）は Random / Horde / Mixed / BossWithMinions の均等抽選。

### 各パターンの敵構成

| パターン | 構成 |
|---------|------|
| **Horde** | 同種 x totalCount |
| **Mixed** | 2-4種類（`min(4, 2 + deepest/50)`）を均等配分 |
| **BossWithMinions** | ボス1体(Legendary, center) + 取り巻き(`deepest/2` で選定, Normal) |
| **BossRush** | 3-5体(Superior, 最後の1体はLegendary) |
| **種族系** | フィルタ条件で2-3種を混成、フォールバックID付き |
| **Random** | 1体ずつ個別に `SelectEnemy(deepest)` |

### 種族フィルタ条件

| パターン | フィルタ | フォールバック |
|---------|---------|-------------|
| UndeadArmy | `race.IsUndead` or `CTAG.undead` | lich, zombie |
| SeaCreatures | `race.IsFish` or `race.tag=water` or `race=octopus` | fish, crab |
| CosmicHorror | `race.IsHorror` or `race=yeek` | yeek, shub_niggurath |
| Kamikazes | `CTAG.suicide` or `CTAG.kamikaze` or id含む"putty"/"puti" | putty, putty_ |

地獄門ギミック（Hellish）発動時: `race.IsDragon` or `race.tag=demon/giant` or 特定raceId。フォールバック: imp。

## レアリティ

`src/RandomBattle/RandomEnemyGenerator.cs:814-827`

通常パターンの敵（`DetermineRarity()`）:

| レアリティ | 確率 |
|-----------|------|
| Normal | 94% |
| Superior | 5% |
| Legendary | 1% |

ボス系パターンではレアリティが固定:
- BossWithMinions: ボス=Legendary, 取り巻き=Normal
- BossRush: Superior (最後の1体はLegendary)

## ボス強化

`src/ZonePreEnterArenaBattle.cs:331-335, 542-558`

`isBoss=true` のキャラに対して以下の強化が適用される:

1. **耐久(END) x3**: `boss.elements.SetBase(71, currentEnd * 3)` → HP大幅増加
2. **ダメージ倍率**: `ArenaBalance.ApplyBossDamageRate()` で `SKILL.dmgDealt`(ID=94) を補正
   - デフォルト: 100%
   - 範囲: 1-200%
   - 設定キー: `ArenaFlagKeys.BossDamageRate`

## ギミックシステム

`src/RandomBattle/ArenaGimmick.cs`

### ランク別ギミック最大数

| ランク | 最大ギミック数 |
|--------|--------------|
| G | 0 |
| F, E, D | 1 |
| C, B, A | 2 |
| S | 3 |

実際の数は `0 ~ 最大数` のランダム。

### ギミック一覧

| ギミック | 効果 | 報酬補正 | 排他 |
|---------|------|---------|------|
| 無法地帯 (AntiMagic) | 魔法ダメージ50%軽減 | +20% | MagicAffinity |
| 臨死の闘技場 (Critical) | 物理ダメージ2倍 | +30% | - |
| 地獄門 (Hellish) | ドラゴン/デーモン/ジャイアントのみ出現 | +25% | - |
| 共感の場 (Empathetic) | 全員にテレパシー&透視付与 | +10% | - |
| 混沌の爆発 (Chaos) | 観客からの妨害（落下物） | +15% | - |
| 属性傷 (ElementalScar) | 周期的属性ダメージ | +15% | - |
| 禁忌の癒し (NoHealing) | 回復アイテム・魔法使用不可 | +35% | - |
| 魔法親和 (MagicAffinity) | 物理ダメージ0、魔法ダメージ2倍 | +40% | AntiMagic |

報酬補正は加算方式（例: Critical+NoHealing = 1.0 + 0.3 + 0.35 = 1.65）。

## 報酬スケーリング

`src/RandomBattle/TodaysBattleCache.cs:119-148`

### 計算式

```
basePlat, basePotion = GetRankBaseReward(rank)
totalMod = gimmickModifier * patternModifier

プラチナ = (basePlat + floor(sqrt(deepest) * 1)) * totalMod
媚薬     = (basePotion + floor(sqrt(deepest) * 0.75)) * totalMod
```

### ランク基本報酬

| ランク | プラチナ | 媚薬 |
|--------|---------|------|
| G | 1 | 1 |
| F | 1 | 2 |
| E | 2 | 3 |
| D | 2 | 4 |
| C | 3 | 5 |
| B | 3 | 7 |
| A | 4 | 8 |
| S | 5 | 10 |

### パターン報酬補正

| パターン | 補正 |
|---------|------|
| FreeForAll | x0.7 |
| Standard (Random/Horde/Mixed) | x1.0 |
| SeaCreatures | x1.0 |
| BossWithMinions | x1.1 |
| UndeadArmy | x1.1 |
| Kamikazes | x1.2 |
| BossRush | x1.2 |
| CosmicHorror | x1.3 |
| Apocalypse | x1.5 |

### 報酬例（ギミックなし、標準パターン）

| deepest | ランクS時 プラチナ | ランクS時 媚薬 |
|---------|-------------------|---------------|
| 10 | 8 | 12 |
| 50 | 12 | 15 |
| 100 | 15 | 17 |
| 400 | 25 | 25 |

## バトルキャッシュ

`src/RandomBattle/TodaysBattleCache.cs`

- ゲーム内1日ごとにバトルが生成される（`EClass.world.date.day` ベース）
- メモリキャッシュされ、同日中は同一バトルが返される
- リロール: プラチナ10枚消費で再生成
- セーブ/ロードでキャッシュはクリアされるが、同日なら同じシードで再生成される

## 未実装パターン

以下のパターンはコード上定義されているが、現在無効化されている:

- **FreeForAll**: 敵同士も敵対するバトルロイヤル。敵対設定の実装が未完了。
- **Apocalypse**: 5Wave耐久戦。Wave制の実装が未完了。

## 関連ソースコード

| ファイル | 内容 |
|---------|------|
| `src/RandomBattle/RandomEnemyGenerator.cs` | 敵選定・数計算・パターン選択・報酬計算 |
| `src/RandomBattle/TodaysBattleCache.cs` | 日次キャッシュ・報酬最終計算・リロール |
| `src/RandomBattle/ArenaGimmick.cs` | ギミック定義・選択・報酬補正 |
| `src/ZonePreEnterArenaBattle.cs` | 敵スポーン・ボス強化・レアリティ適用 |
| `src/Arena/ArenaBalance.cs` | ボスダメージ倍率 |
| `src/Debug/RandomBattleDebug.cs` | deepest/パターン/ギミックのオーバーライド |
