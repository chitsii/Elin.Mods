# 従者強化システム仕様書

## 概要

蘇生レベルの制御、蘇生後のアトリビュート強化・部位増設、暴走リスクの3本柱で構成される。

---

## 1. ソウルユニット (SU) と蘇生レベル

### SU テーブル

| 魂ランク | アイテムID | SU/個 |
|---------|-----------|-------|
| 弱い魂 | `ars_soul_weak` | 5 |
| 普通の魂 | `ars_soul_normal` | 15 |
| 強い魂 | `ars_soul_strong` | 40 |
| 伝説の魂 | `ars_soul_legendary` | 100 |

### レベル算出（原Lv復元モデル）

SUが死体の元Lvに対する「復元率」を決定する。30強い魂(1200SU)で100%復元。

```
ratio        = min(1.0, 総SU / MaxSU)
recoveryRate = BaseFraction + (1 - BaseFraction) × √ratio
蘇生Lv      = max(1, floor(corpseLv × recoveryRate))
上限         = floor(deepest × 1.5)
最終Lv       = min(蘇生Lv, 上限)
```

- **MaxSU = 1200** (`NecromancyManager.MaxResurrectionSU`) — 強い魂30個相当
- **BaseFraction = 0.05** (`NecromancyManager.BaseRecoveryRate`) — 最低復元率5%
- **corpseLv** = 死体に記録された実Lv (`GetInt(92710)`)、未記録時は `SourceChara.Row.LV` フォールバック
- `deepest` = `EClass.player.stats.deepest` (プレイヤーの最高到達 DangerLv)

#### Lv50死体での投資カーブ

| 投資 | SU | 復元率 | 蘇生Lv |
|---|---|---|---|
| 弱い魂 1個 | 5 | 11% | 5 |
| 弱い魂 3個 | 15 | 16% | 7 |
| 強い魂 1個 | 40 | 22% | 11 |
| 強い魂 3個 | 120 | 35% | 17 |
| 強い魂 5個 | 200 | 44% | 21 |
| 強い魂 10個 | 400 | 60% | 29 |
| 強い魂 20個 | 800 | 83% | 41 |
| 強い魂 30個 | 1200 | 100% | 50 |

### 死体LVの保存

- `Chara.Die` の Prefix で `__instance.LV` をキャッシュ
- Postfix で死亡位置の死体 (`TraitFoodMeat`) に `SetInt(92710, lv)` でスタンプ
- Mod未適用時に入手した死体は `GetInt(92710) == 0` → `SourceChara.Row.LV` にフォールバック

### 死体Lvの影響

高Lv死体ほど投資リターンの絶対値が大きい（同じSUでもリターンが異なる）:
- チキン(Lv1) + SU40 → floor(1 × 0.22) = 1（元が弱いため低い）
- ドラゴン(Lv50) + SU40 → floor(50 × 0.22) = 11
- ドラゴン(Lv100) + SU40 → floor(100 × 0.22) = 22
- ドラゴン(Lv50) + SU1200 → floor(50 × 1.0) = 50（完全復元）

### 調整定数

| 定数 | 現在値 | 場所 | 調整の指針 |
|------|--------|------|-----------|
| MaxSU | 1200 | `NecromancyManager.MaxResurrectionSU` | 下げると少ない魂で100%に到達 |
| BaseFraction | 0.05 | `NecromancyManager.BaseRecoveryRate` | 上げると最低保証Lvが上がる |
| 上限倍率 | 1.5 | `NecromancyManager.GetLevelCap()` | プレイヤー進行度に対する制限 |

### PerformRitual シグネチャ

```csharp
public Chara? PerformRitual(Thing corpse, Dictionary<string, int> soulAmounts)
```

- `soulAmounts`: 魂アイテムID → 投入数
- 魂はPC のインベントリから消費される (`ConsumeItems`)
- 蘇生Lvは `ServantEnhancement.ResurrectionLevel` に保存される (強化閾値計算用)

---

## 2. アトリビュート強化 (魂注入)

### 対象属性

| 属性 | Element ID |
|------|-----------|
| STR | 70 |
| END | 71 |
| DEX | 72 |
| PER | 73 |
| LER | 74 |
| WIL | 75 |
| MAG | 76 |
| CHA | 77 |
| SPD | 79 |

### 効果

```
加算値 = max(1, floor(√(SU/個 × 数量) × 効率 × 倍率))
実効回数 = 累積注入魂数 + (数量 - 1) × 0.35
効率    = 1.0 / (1 + 実効回数 × 0.08)
倍率   = 1.0 (通常) / 2.0 (闇の覚醒時)
```

- 平方根スケーリングにより大量投資の逓減を実現（蘇生システムと同じパターン）
- 1回の大量注入は効率が落ちる（数量も効率に反映）
- `servant.elements.ModBase(attrId, 加算値)` で適用
- 各注入で **強化レベル + 注入魂数**
- 暴走結果が「闇の覚醒」の場合、効果が2倍になる
- 1回注入あたりの魂投入数は **最大10個** (`NecromancyManager.MaxSoulsPerInjection`)

### 設計意図（2026-02-22）

- 課題: 「未強化状態で最大投入」が最適行動になり、反復強化の楽しみが薄れる。
- 対応: 1回の注入で使った魂数ぶん、`AttrInjections` と `EnhancementLevel` を進める。
- 対応: 暴走率は「今回の注入量を加味した予測値」で確認/判定する。
- 期待効果: 大量一括投入は短期火力を得られるが、進行コストも同時に支払うため、反復注入とのトレードオフが成立する。

### 投資カーブ（初回注入、効率100%）

| 投資 | 総SU | 加算値 |
|---|---|---|
| 弱い魂 1個 | 5 | 2 |
| 普通の魂 1個 | 15 | 3 |
| 強い魂 1個 | 40 | 6 |
| 伝説の魂 1個 | 100 | 10 |
| 強い魂 10個 | 400 | 20 |
| 伝説の魂 10個 | 1,000 | 31 |
| 伝説の魂 100個 | 10,000 | 100 |

### 効率逓減カーブ

| 累積注入魂数 | 効率 |
|---------|------|
| 1回目 | 100% |
| 2回目 | 93% |
| 3回目 | 86% |
| 4回目 | 81% |
| 5回目 | 76% |
| 6回目 | 71% |
| 10回目 | 58% |

### 調整定数

| 定数 | 現在値 | 場所 | 調整の指針 |
|------|--------|------|-----------|
| スケーリング | √(SU×数量) | `NecromancyCalculations.CalculateInjectionBoost()` | 平方根で逓減 |
| 回数逓減係数 | 0.08 | `NecromancyCalculations.InjectionDecayPerStep` | 上げると回数逓減が急になる |
| 数量重み | 0.35 | `NecromancyCalculations.InjectionBatchWeight` | 上げると一括注入の効率が下がる |
| 1回注入上限 | 10 | `NecromancyManager.MaxSoulsPerInjection` | 下げると反復性が増える |
| 最低加算値 | 1 | 同上 | SUが低くても最低1は加算される |
| 闇の覚醒倍率 | 2.0 | GUI呼び出し時に設定 | DarkAwakeningの報酬度 |

---

## 3. 部位増設

### 増設可能部位

| 部位 | SLOT定数 | figure名 | 増設上限 |
|------|---------|---------|---------|
| 腕 | 34 (`SLOT.arm`) | `"腕"` | +2 |
| 手 | 35 (`SLOT.hand`) | `"手"` | +2 |
| 頭 | 30 (`SLOT.head`) | `"頭"` | +1 |
| 指 | 36 (`SLOT.finger`) | `"指"` | +2 |
| 足 | 39 (`SLOT.foot`) | `"足"` | +1 |

> ゲームの `ParseBodySlot` は `"足"→39(foot)` にマッピングする。`"脚"→38(leg)` はゲーム内に存在しない。

### 素材判定

```
corpse.c_idRefCard → SourceChara.Row → row.race → SourceRace.Row → raceRow.figure
```

- `race.figure` は `"頭|体|腕|手|手|指|指|腰|足"` のようなパイプ区切り文字列
- 素材死体の種族の `figure` に対象部位の名前 (e.g. `"手"`) が含まれている必要がある

### 成功率 (ピティタイマー付き)

```
実効成功率 = min(95%, 基本率 × (1 + (投入数 - 1) × 0.5) + 共鳴度 × 5%)
```

- **共鳴度**: 失敗するたびに +1 (その部位 × その従者に紐づく)
- **成功時**: 共鳴度リセット (その部位のみ)
- **失敗時**: 素材消費 + 共鳴度 +1

| 投入数 | 共鳴0 | 共鳴1 (+5%) | 共鳴4 (+20%) | 共鳴9 (+45%) |
|--------|-------|-------------|-------------|-------------|
| 1個 | 10% | 15% | 30% | 55% |
| 3個 | 20% | 25% | 40% | 65% |
| 5個 | 30% | 35% | 50% | 75% |
| 10個 | 55% | 60% | 75% | 95% |

- 各成功で **強化レベル +2**

### 調整定数

| 定数 | 現在値 | 場所 | 調整の指針 |
|------|--------|------|-----------|
| 基本成功率 | 10% | `NecromancyManager.AugmentBaseRate` | テストプレイで調整 |
| 投入数ボーナス係数 | 0.5 | `NecromancyManager.AugmentBodyPart()` | 上げると少数投入でも成功しやすい |
| 共鳴度ボーナス | 5%/回 | 同上 | 上げるとピティの効果が大きい |
| 成功率上限 | 95% | 同上 | 100%にすると確定成功 |
| 増設上限 | 各部位定義 | `AugmentableSlots[]` | 部位ごとに設定可能 |

### 永続化

- `servant.body.AddBodyPart()` は `rawSlots` に追加される
- `rawSlots` はバニラが `[JsonProperty("3")]` でシリアライズするため、**永続化は自動**
- ModConfig 側では追跡情報のみ保存 (何回増設したか + 共鳴度)

---

## 4. 暴走システム

### 強化レベル

```
強化レベル = アトリビュート注入魂数 × 1 + 部位増設成功回数 × 2
```

### 暴走閾値

```csharp
int threshold = MAG / 5 + (int)Math.Sqrt(kills);
```

- `MAG` = `EClass.pc.MAG` (= `elements.Value(76)`)
- `kills` = `EClass.player.stats.kills` (累計殺害数)

### 判定

- `強化レベル ≤ 暴走閾値` → **安全** (暴走率 0%)
- 超過時、強化実行時に判定:
  ```
  超過量 = 強化レベル - 暴走閾値
  暴走率 = min(超過量 × 10%, 90%)
  ```

### 暴走結果テーブル (リスク-リワード)

50%がポジティブ結果 — 「あえて閾値を超える」判断に価値がある。永久喪失なし。

| ロール (1-100) | 結果 | 性質 | 実装 |
|---------------|------|------|------|
| 1-35 | ★ 闇の覚醒 | ポジティブ | 強化効果2倍 + 永続MAG+5 (屍気の恩寵) |
| 36-70 | △ 狂戦士化 | ニュートラル | `hostility = Enemy` + `ConBerserk(300)` |
| 71-85 | × 自壊 | ネガティブ | `ActEffect.ProcAt(EffectId.Suicide, ...)` + `Die()` (回復可) |
| 86-100 | ★ 変異覚醒 | ポジティブ | `AddRandomBodyPart(true)` + `ConBerserk(200)` + 強化Lv+2 |

### 調整定数

| 定数 | 現在値 | 場所 | 調整の指針 |
|------|--------|------|-----------|
| MAG除数 | 5 | `GetRampageThreshold()` | 下げるとMAGの影響が大きくなる |
| 超過量→暴走率 | ×10% | `GetRampageChance()` | 下げるとリスクが緩和される |
| 暴走率上限 | 90% | 同上 | 100%にすると確定暴走 |
| ConBerserk パワー | 300/200 | `ExecuteRampage()` | 上げると狂戦士化が長い |
| 自壊パワー | `servant.LV × 5` | 同上 | AoE爆発ダメージに影響 |
| 闇の覚醒MAGボーナス | +5 | 同上 | 屍気の恩寵の強さ |

---

## 5. データ永続化

### ConfigEntry

| キー | セクション | 形式 |
|-----|-----------|------|
| `UnlockedSpells` | Necromancy | CSV (エイリアス名) |
| `ServantUIDs` | Necromancy | CSV (UID) |
| `EnhancementData` | Necromancy | カスタム形式 (下記) |

### EnhancementData シリアライズ形式

```
<uid>:<enhLv>:<bodyParts>:<slotAdditions>:<attrInjections>:<resLv>:<slotResonance>;<uid2>:...
```

- `slotAdditions` = `slotId=count,slotId=count` (空も可)
- `attrInjections` = `attrId=count,attrId=count` (空も可)
- `resLv` = 蘇生レベル (整数)
- `slotResonance` = `slotId=resonance,slotId=resonance` (空も可)
- 従者間のセパレータ: `;`

例:
```
12345:7:2:35=1,36=1:70=3,76=2:45:35=2;67890:3:0::72=3:20:
```

### ServantEnhancement データモデル

```csharp
public class ServantEnhancement
{
    public int EnhancementLevel;               // 総強化レベル
    public int AddedBodyParts;                 // 追加した部位数 (tracking only)
    public int ResurrectionLevel;              // 蘇生時のLv (強化閾値計算用)
    public Dictionary<int, int> SlotAdditions; // slotId → 追加回数
    public Dictionary<int, int> AttrInjections; // attrId → 累積注入魂数
    public Dictionary<int, int> SlotResonance; // slotId → 共鳴度 (ピティタイマー)
}
```

---

## 6. UI

### 儀式タブ: 魂スライダー + 死体Lv表示

- 各魂種類ごとに `HorizontalSlider` (0 ~ 所持数)
- 死体一覧に `(Lv.X)` を表示
- 合計SU・死体Lv・予測Lv・上限をリアルタイム表示
- 上限到達時は Amber Warning 色

### 従者タブ: 改造サブパネル

従者一覧に **[改造]** ボタンを追加。

#### アトリビュート強化セクション
- 8属性をボタンで選択 (現在値・累積注入量・効率を表示)
- 魂種類をボタンで選択 (所持数表示)
- 数量スライダー（1回上限10）
- プレビュー表示 (加算値)
- 暴走判定は強化実行前に行い、DarkAwakeningなら倍率2.0を適用
- [注入する] ボタン (確認ダイアログ付き)

#### 部位増設セクション
- 増設可能部位をボタンで選択 (現在数・追加数/上限表示)
- 素材死体チェックボックス (適合する種族のみ表示)
- 成功率プレビュー (共鳴度ボーナスを含む)
- 共鳴度 > 0 なら `(共鳴: +X%)` を表示
- [増設する] ボタン (確認ダイアログ付き)

#### 暴走リスク表示
- ヘッダーに常時表示: `強化Lv: X / 閾値: Y`
- 安全: 緑, 閾値付近: 黄, 超過: 赤 + `暴走率: Z%`
- 強化実行前の確認ダイアログにも暴走率を表示

### Escape キー挙動 (3段階)
1. 確認ダイアログ表示中 → ダイアログを閉じる
2. 改造サブパネル表示中 → サブパネルを閉じる
3. いずれでもない → UI全体を閉じる

---

## 7. 未決事項・要テストプレイ

- [ ] MaxSU (1200) の妥当性 — 強い魂30個が投資上限として適切か
- [ ] BaseFraction (0.05) の妥当性 — 最低保証5%が低すぎないか
- [ ] 基本成功率 (10%) — ギャンブル感の調整
- [ ] 共鳴度ボーナス (5%) — ピティが効きすぎないか
- [ ] 狂戦士化後の hostility 回復 — ConBerserk 解除後にAllyに戻るか
- [ ] 死亡中の従者への改造 — 現在は改造可能 (死体改造のフレーバー)
