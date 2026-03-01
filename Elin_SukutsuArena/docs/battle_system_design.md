# 戦闘システム設計計画

> **注意**: 本ドキュメントは初期設計時の計画書です。現在のランダムバトル（通常戦闘）の仕様は [random_battle_spec.md](./random_battle_spec.md) を参照してください。

## 1. 現状分析

### 現在の実装
- `ArenaManager.cs`: ステージ設定、戦闘開始
- `ZonePreEnterArenaBattle.cs`: 敵スポーン処理
- `ZoneEventArenaBattle.cs`: 戦闘監視、勝敗判定
- `ZoneInstanceArenaBattle.cs`: 報酬付与、ゾーン帰還

### 現在の制限
- 敵は固定ID + レベルのみ
- マップは `SpatialGen.CreateInstance("field")` の汎用マップ
- ギミックなし
- BGMは固定（102: 戦闘、106: 勝利）

---

## 2. 機能実現可能性マトリクス

| 機能 | 実現可能性 | 実装方法 | 工数 | 優先度 |
|------|----------|---------|------|--------|
| **ステージサイズ** | ○ 可能 | ZoneType変更 or カスタムマップ | 低 | 高 |
| **カスタム敵（種族）** | ○ 可能 | SourceCharaシートで定義 | 中 | 高 |
| **カスタム敵（スキル）** | ○ 可能 | SourceElementシートで定義 | 中 | 高 |
| **カスタム敵（フィート）** | ○ 可能 | SourceElementシートで定義 | 中 | 中 |
| **カスタム敵（キャラチップ）** | ○ 可能 | Textureフォルダ + PCC設定 | 低 | 低 |
| **敵配置パターン** | ○ 可能 | ZonePreEnterで位置指定 | 低 | 中 |
| **敵AI/行動パターン** | △ 制限あり | Tacticsシート + カスタムAI | 高 | 低 |
| **BGM動的変更** | ○ 可能 | `_zone.SetBGM(id)` | 低 | 高 |
| **ギミック（落下物）** | △ 要C# | ZoneEvent.OnTickで実装 | 高 | 中 |
| **ステージ進行演出** | ○ 可能 | Drama + ZoneEvent連携 | 中 | 中 |
| **ボス専用デバフ** | ○ 可能 | カスタムAbility/Spell | 中 | 高 |
| **完全不可知化** | △ 要調査 | 既存の透明化拡張 or カスタム | 高 | 中 |

---

## 3. 推奨アーキテクチャ

### 3.1 ステージ定義システム

```
BattleStageConfig
├── stageId: string           # ユニークID
├── displayName: string       # 表示名
├── zoneType: string          # "field", "cave", "dungeon" etc.
├── mapSize: (w, h)           # マップサイズ（オプション）
├── bgmBattle: int            # 戦闘BGM ID
├── bgmVictory: int           # 勝利BGM ID
├── enemies: EnemySpawnConfig[]
├── gimmicks: GimmickConfig[] # オプション
└── victoryCondition: string  # "kill_all", "survive_turns", "kill_boss"
```

### 3.2 敵定義システム

```
EnemySpawnConfig
├── charaId: string           # SourceCharaのID
├── level: int                # レベル
├── rarity: Rarity            # Normal/Superior/Legendary
├── position: SpawnPosition   # Center/Random/Fixed(x,z)
├── equipment: string[]       # 装備ID（オプション）
├── abilities: string[]       # 付与するアビリティ（オプション）
└── isBoss: bool              # ボスフラグ
```

### 3.3 データ駆動設計

**Python側（ビルド時）**
```python
# battle_stages.py
STAGES = {
    "rank_g_trial": BattleStage(
        enemies=[
            Enemy("putty", level=5, count=5),
        ],
        bgm_battle=102,
    ),
    "rank_f_trial": BattleStage(
        enemies=[
            Enemy("sukutsu_frost_hound", level=15, is_boss=True),
        ],
        bgm_battle=103,  # 専用BGM
    ),
    # ...
}
```

**C#側（ランタイム）**
- JSONからステージ定義を読み込み
- `ArenaManager.StartBattleByStage(stageId, master)` で開始

---

## 4. カスタム敵の実装計画

### 4.1 シナリオで必要な敵

| 敵名 | ランク | 特殊能力 | 実装方法 |
|------|--------|---------|---------|
| 氷獄の猟犬 | F | 冷気攻撃、凍結付与 | SourceChara + 冷気スキル |
| カインの亡霊 | E | ゴースト、透過攻撃 | SourceChara (race: ghost) |
| ミノタウロス | D | 突進、範囲攻撃 | 既存 + スキル追加 |
| 執行者ヌル | B | **完全不可知化** | カスタムAbility必要 |
| バルガス（全盛期） | S | 多段攻撃、気迫 | SourceChara + カスタム |
| アスタロス | 最終 | **次元デバフ** | カスタムAbility必要 |

### 4.2 ヌルの「完全不可知化」

**案A: 既存の透明化拡張**
- `Condition.Invisible` を付与
- プレイヤーの探知を無効化

**案B: カスタム状態異常**
- `ConditionNullCloaking : Condition` を実装
- 攻撃命中時のみ一時的に可視化

**推奨: 案A**（工数削減、シナリオで「特殊な探知手段が必要」と説明を追加）

### 4.3 アスタロスのデバフ

**シナリオの設定**
1. 時の支配: 速度固定
2. 因果の拒絶: ダメージ上限
3. 削除命令: ステータス減少

**実装案**
```csharp
// カスタムAbility
public class ActAstarothTimeControl : Ability
{
    public override bool Perform()
    {
        // プレイヤーの速度を1に固定する状態異常を付与
        pc.AddCondition<ConditionTimeControl>(duration: 999);
        return true;
    }
}
```

**推奨: シナリオ変更で妥協**
- 複雑なデバフは実装工数が高い
- 「呪いのデバフを複数付与」として既存の状態異常を組み合わせ
- 仲間のセリフで「このデバフを解除してくれた」と演出

---

## 5. ギミック実装

### 5.1 落下物（上位存在の妨害）

**実装方法**
```csharp
public class ZoneEventAudienceInterference : ZoneEvent
{
    private float timer = 0f;
    private float interval = 5f; // 5秒ごと

    public override void OnTick()
    {
        timer += Core.delta;
        if (timer >= interval)
        {
            timer = 0f;
            SpawnFallingObject();
        }
    }

    private void SpawnFallingObject()
    {
        // プレイヤー周辺にランダムで障害物を落とす
        Point target = GetRandomPointNearPlayer();
        Thing rock = ThingGen.Create("rock");
        EClass._zone.AddCard(rock, target);

        // ダメージ判定
        if (target.HasChara && target.FirstChara == EClass.pc)
        {
            EClass.pc.DamageHP(10, AttackSource.Trap);
            Msg.Say("頭上から石が降ってきた！");
        }
    }
}
```

**工数評価: 中〜高**
- 演出の調整が必要
- バランス調整が難しい

**推奨: 後回し or シンプル化**
- 「落下物あり」のステージは特定ランクのみ
- ダメージではなく「速度低下エリア」として実装

---

## 6. 実装優先順位

### Phase 1: 基盤（必須）
1. ステージ定義JSONの設計と読み込み
2. カスタム敵の基本定義（SourceCharaシート）
3. BGM動的変更の確認

### Phase 2: シナリオ対応
4. ランクアップ試練用の敵定義
   - G: パティ×5
   - F: 氷獄の猟犬
   - E: カインの亡霊
   - D〜S: 順次追加
5. ボス戦BGMの追加

### Phase 3: 演出強化（オプション）
6. ギミック（落下物）の実装
7. ステージ進行中のドラマ挿入
8. 特殊デバフの実装

---

## 7. シナリオ調整の提案

### 7.1 簡略化すべき点

| 元の設定 | 問題 | 調整案 |
|---------|------|--------|
| ヌルの完全不可知化 | 実装困難 | 「高速移動による残像」として透明化+高回避 |
| アスタロスの3種デバフ | 複雑 | 既存デバフの組み合わせ（毒+混乱+減速） |
| 観客の落下物 | 工数大 | ランクD戦のみ、シンプルなダメージトラップ |

### 7.2 強化すべき点

| 要素 | 現状 | 強化案 |
|------|------|--------|
| ボス戦BGM | 共通 | ランク別に3種類用意 |
| 勝利演出 | テキストのみ | エフェクト + 専用SE |
| 敵登場演出 | なし | ボス戦前にセリフ表示 |

---

## 8. 次のステップ

1. **この設計書のレビュー** - シナリオ変更の許容範囲を確認
2. **ステージ定義JSONの仕様策定** - Pythonビルドシステムとの統合
3. **カスタム敵のSourceCharaシート作成** - 最低限の敵を定義
4. **プロトタイプ実装** - ランクG試練で動作確認

---

## 付録: CWL機能まとめ

### 使用可能なAPI
- `CharaGen.Create(id)` - キャラ生成
- `EClass._zone.SetBGM(id)` - BGM変更
- `EClass._zone.AddCard(card, pos)` - カード配置
- `Chara.AddCondition<T>()` - 状態異常付与
- `Chara.SetLv(level)` - レベル設定
- `Chara.ChangeRarity(rarity)` - レアリティ変更

### 使用可能なシート
- `SourceChara` - キャラ定義
- `SourceElement` - スキル/アビリティ定義
- `SourceTactics` - AI行動パターン

### 制限事項
- マップ自体のカスタム生成は困難（既存ZoneTypeを使用）
- AI行動の完全カスタムは工数大（Tacticsシートで対応可能な範囲で）
