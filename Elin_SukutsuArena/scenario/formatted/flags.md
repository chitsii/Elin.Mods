# 宿敵アリーナ：フラグ管理仕様書（簡略版）

## 【フラグ構造の概要】

このドキュメントは、シナリオ分岐に必要な最小限のフラグを管理します。

### フラグの命名規則

**全フラグは `chitsii.arena.` プレフィクスを使用**

```
chitsii.arena.<category>.<name>
```

#### カテゴリ一覧

- `player` - プレイヤー状態・選択
- `rel` - NPC関係値（Relationship）

### フラグ型

- **enum**: 相互排他的選択（例: `"returned"`, `"sold"`）
- **number**: 数値（好感度、カルマなど）
- **string**: 文字列（ランク名など）
- **boolean**: `true` / `false`

### 設計方針

- **報酬/バフは管理しない** - アクション系フラグは除外
- **ランクは単一フラグで管理** - `rank_X_clear`フラグは廃止し、`player.rank`で統一
- **コンテンツ解禁フラグは最小限** - 実装負荷軽減のため
- **「不信」状態は削除** - 意味が薄いため、関係値または明確な状態で管理

---

## 【時系列フラグ遷移表】

### scenario/01_opening.md - オープニング

```json
{
  "event": "アリーナ到着・動機選択",
  "flags_set": {
    "chitsii.arena.player.motivation": "<CHOICE>",
    "chitsii.arena.player.rank": "unranked",
    "chitsii.arena.rel.lily": 30,
    "chitsii.arena.rel.balgas": 20,
    "chitsii.arena.rel.zek": 0
  },
  "choices": [
    {
      "choice_id": "motivation_select",
      "flag_key": "chitsii.arena.player.motivation",
      "options": [
        "greed",
        "battle_lust",
        "nihilism",
        "arrogance"
      ]
    }
  ]
}
```

---

### scenario/02_rank_up_01.md - Rank G昇格試合

```json
{
  "event": "Rank G昇格試合クリア",
  "flags_set": {
    "chitsii.arena.player.rank": "G"
  }
}
```

---

### scenario/03_zek.md - ゼク初登場

```json
{
  "event": "商人ゼクとの邂逅",
  "required": {
    "chitsii.arena.player.rank": ">= G"
  },
  "flags_set": {
    "chitsii.arena.rel.zek": 10
  }
}
```

---

### scenario/04_rank_up_02.md - Rank F昇格試合

```json
{
  "event": "Rank F昇格試合クリア（凍土の魔犬）",
  "required": {
    "chitsii.arena.player.rank": "G"
  },
  "flags_set": {
    "chitsii.arena.player.rank": "F"
  }
}
```

---

### scenario/05_1_lily_experiment.md - リリィの共鳴瓶製作依頼

```json
{
  "event": "リリィの器製作クエスト完了",
  "required": {
    "chitsii.arena.player.rank": ">= F"
  },
  "flags_set": {
    "chitsii.arena.rel.lily": "+5"
  }
}
```

---

### scenario/05_2_zek_steal_lily.md - ゼクの器すり替え提案【重要分岐】

```json
{
  "event": "ゼクの器すり替え提案",
  "required": {
    "chitsii.arena.player.rank": ">= F"
  },
  "choices": [
    {
      "choice_id": "bottle_swap",
      "flag_key": "chitsii.arena.player.bottle_choice",
      "options": [
        {
          "value": "refused",
          "sets": { "chitsii.arena.rel.lily": "+10" }
        },
        {
          "value": "swapped",
          "sets": {
            "chitsii.arena.player.karma": "-20",
            "chitsii.arena.rel.zek": "+15"
          },
          "consequences": "scenario/13でbottle_malfunction発生"
        }
      ]
    }
  ]
}
```

---

### scenario/06_1_rank_up_03.md - Rank E昇格試合（カイン戦）

```json
{
  "event": "Rank E昇格試合クリア（錆びついた英雄カイン）",
  "required": {
    "chitsii.arena.player.rank": "F"
  },
  "flags_set": {
    "chitsii.arena.player.rank": "E",
    "chitsii.arena.rel.balgas": "+15"
  }
}
```

---

### scenario/06_2_zek_steal_soulgem.md - カインの魂選択【重要分岐】

```json
{
  "event": "カインの魂の選択",
  "required": {
    "chitsii.arena.player.rank": "E"
  },
  "choices": [
    {
      "choice_id": "kain_soul",
      "flag_key": "chitsii.arena.player.kain_soul_choice",
      "context_monologue_ref": "chitsii.arena.player.motivation",
      "options": [
        {
          "value": "returned",
          "sets": {
            "chitsii.arena.player.karma": "+10",
            "chitsii.arena.rel.balgas": "+30"
          }
        },
        {
          "value": "sold",
          "sets": {
            "chitsii.arena.player.karma": "-15",
            "chitsii.arena.rel.zek": "+20"
          },
          "consequences": "scenario/13でbalgas_confrontation発生"
        }
      ]
    }
  ],
  "flags_set": {
    "chitsii.arena.player.rank": "D"
  }
}
```

---

### scenario/07_upper_existence.md - Rank D初戦

```json
{
  "event": "Rank D初戦（観客のヤジ）",
  "required": {
    "chitsii.arena.player.rank": "D"
  }
}
```

---

### scenario/08_lily_private.md - リリィの私室招待

```json
{
  "event": "リリィの私室イベント",
  "required": {
    "chitsii.arena.player.rank": "D",
    "chitsii.arena.rel.lily": ">= 40"
  },
  "flags_set": {
    "chitsii.arena.rel.lily": "+10"
  }
}
```

---

### scenario/09_bulgas_training.md - Rank C昇格試合（バルガス修行）

```json
{
  "event": "Rank C昇格試合クリア（バルガスの哲学修行）",
  "required": {
    "chitsii.arena.player.rank": "D"
  },
  "flags_set": {
    "chitsii.arena.player.rank": "C",
    "chitsii.arena.rel.balgas": "+20"
  }
}
```

---

### scenario/10_rank_up_04.md - Rank C初戦（英雄の残党）

```json
{
  "event": "Rank C初戦（英雄の残党戦）",
  "required": {
    "chitsii.arena.player.rank": "C"
  }
}
```

---

### scenario/11_rank_up_05.md - Rank B昇格試合（ヌル戦）

```json
{
  "event": "Rank B昇格試合クリア（暗殺者ヌル）",
  "required": {
    "chitsii.arena.player.rank": "C"
  },
  "flags_set": {
    "chitsii.arena.player.rank": "B"
  }
}
```

---

### scenario/12_makuma.md - ヌルの記憶チップとリリィの衣装

```json
{
  "event": "ヌルの記憶チップ入手、リリィから衣装授与",
  "required": {
    "chitsii.arena.player.rank": "B"
  },
  "flags_set": {
    "chitsii.arena.player.null_chip_obtained": true,
    "chitsii.arena.rel.lily": "+10"
  },
  "consequences": {
    "triggers_in": "scenario/14_rank_up_06.md",
    "event": "null_memory_integration_in_rank_a_trial"
  }
}
```

---

### scenario/13_makuma2.md - 虚空の心臓製作【複数分岐統合】

```json
{
  "event": "虚空の心臓製作依頼",
  "required": {
    "chitsii.arena.player.rank": "B"
  },
  "sub_events": [
    {
      "condition": "chitsii.arena.player.bottle_choice == 'swapped'",
      "event": "bottle_malfunction",
      "description": "偽物の共鳴瓶が暴走",
      "choices": [
        {
          "choice_id": "bottle_confession",
          "flag_key": "chitsii.arena.player.lily_bottle_confession",
          "options": [
            {
              "value": "confessed",
              "sets": {
                "chitsii.arena.rel.lily": "20",
                "chitsii.arena.player.lily_trust_rebuilding": true,
                "chitsii.arena.player.karma": "+5"
              },
              "consequences": "scenario/16で和解イベント発生"
            },
            {
              "value": "blamed_zek",
              "sets": {
                "chitsii.arena.rel.lily": "50 (表面維持)"
              },
              "consequences": "scenario/16真名開示不発生"
            },
            {
              "value": "denied",
              "sets": {
                "chitsii.arena.rel.lily": "0",
                "chitsii.arena.player.lily_hostile": true
              },
              "consequences": "scenario/16不発生、scenario/18リリィ離反可能性"
            }
          ]
        }
      ]
    },
    {
      "condition": "chitsii.arena.player.kain_soul_choice == 'sold'",
      "event": "balgas_confrontation",
      "description": "バルガスがカインの魂について問い詰める",
      "choices": [
        {
          "choice_id": "kain_soul_confession",
          "flag_key": "chitsii.arena.player.kain_soul_confession",
          "options": [
            {
              "value": "confessed",
              "sets": {
                "chitsii.arena.rel.balgas": "0",
                "chitsii.arena.player.balgas_trust_broken": true
              },
              "consequences": "scenario/15でバルガス本気、scenario/18協力不可"
            },
            {
              "value": "lied",
              "sets": {
                "chitsii.arena.rel.balgas": "30 (表面維持)"
              },
              "consequences": "scenario/15特別台詞、scenario/18支援弱体化"
            }
          ]
        }
      ]
    }
  ]
}
```

---

### scenario/14_rank_up_06.md - Rank A昇格試合（影との戦い）

```json
{
  "event": "Rank A昇格試合クリア（自分の影=第二のヌル）",
  "required": {
    "chitsii.arena.player.rank": "B"
  },
  "pre_battle_event": {
    "condition": "chitsii.arena.player.null_chip_obtained == true",
    "description": "ゼクがヌルの記憶を再生し、アリーナの真実を明かす"
  },
  "flags_set": {
    "chitsii.arena.player.rank": "A"
  }
}
```

---

### scenario/15_vs_bulgas.md - Rank S昇格試合（バルガス全盛期戦）

```json
{
  "event": "Rank S昇格試合（バルガス全盛期との一騎打ち）",
  "required": {
    "chitsii.arena.player.rank": "A"
  },
  "climax_choice": {
    "choice_id": "spare_balgas",
    "flag_key": "chitsii.arena.player.balgas_choice",
    "context_dependent_monologue": "chitsii.arena.player.motivation",
    "options": [
      {
        "value": "spared",
        "sets": {
          "chitsii.arena.player.rank": "S",
          "chitsii.arena.rel.balgas": "100"
        }
      },
      {
        "value": "killed",
        "sets": {
          "chitsii.arena.player.rank": "S",
          "chitsii.arena.rel.balgas": "0"
        },
        "consequences": "scenario/18でバルガス不在"
      }
    ],
    "special_effect": {
      "condition": "chitsii.arena.player.balgas_trust_broken == true",
      "override": "バルガスが本気で殺しにかかり、赦しを得られない"
    }
  }
}
```

---

### scenario/16_lily_real_name.md - リリィの真名告白

```json
{
  "event": "リリィの真名開示",
  "required": {
    "chitsii.arena.player.rank": "S",
    "chitsii.arena.player.balgas_choice": "spared",
    "chitsii.arena.rel.lily": ">= 50"
  },
  "blocking_conditions": [
    "chitsii.arena.player.lily_bottle_confession == 'blamed_zek'",
    "chitsii.arena.player.lily_bottle_confession == 'denied'",
    "chitsii.arena.player.lily_hostile == true"
  ],
  "special_variations": {
    "condition": "chitsii.arena.player.lily_trust_rebuilding == true",
    "description": "瓶すり替えを告白した場合の和解イベント発生"
  },
  "flags_set": {
    "chitsii.arena.player.lily_true_name": "Liliaris",
    "chitsii.arena.rel.lily": "100"
  }
}
```

---

### scenario/17_vs_grandmaster_1.md - グランドマスター戦（失敗とゼクの介入）

```json
{
  "event": "アスタロト初遭遇、ゼクによる救出",
  "required": {
    "chitsii.arena.player.rank": "S"
  },
  "flags_set": {
    "chitsii.arena.player.fugitive_status": true
  }
}
```

---

### scenario/18_last_battle.md - 最終決戦

```json
{
  "event": "最終決戦：アスタロト撃破",
  "required": {
    "chitsii.arena.player.fugitive_status": true
  },
  "battle_mechanics": {
    "party_support": [
      {
        "character": "zek",
        "condition": "always"
      },
      {
        "character": "balgas",
        "condition": "chitsii.arena.player.balgas_trust_broken != true && chitsii.arena.player.balgas_choice == 'spared'"
      },
      {
        "character": "lily",
        "condition": "chitsii.arena.player.lily_hostile != true"
      }
    ]
  },
  "ending_choice": {
    "choice_id": "post_victory_path",
    "flag_key": "chitsii.arena.player.ending",
    "options": [
      "return_to_surface",
      "rebuild_arena",
      "challenge_observers"
    ]
  }
}
```

---

## 【グローバルフラグ一覧】

全フラグのデフォルト値と型定義（簡略版）

```json
{
  // === プレイヤー状態（8フラグ） ===
  "chitsii.arena.player.motivation": null,  // enum: "greed" | "battle_lust" | "nihilism" | "arrogance"
  "chitsii.arena.player.rank": "unranked",  // enum: "unranked" | "G" | "F" | "E" | "D" | "C" | "B" | "A" | "S"
  "chitsii.arena.player.karma": 0,          // number: -100 ~ +100
  "chitsii.arena.player.fugitive_status": false,    // boolean
  "chitsii.arena.player.lily_true_name": null,      // string | null: 真名を知っている場合 "Liliaris"
  "chitsii.arena.player.null_chip_obtained": false, // boolean: ヌルの記憶チップ入手済み
  "chitsii.arena.player.lily_trust_rebuilding": false,  // boolean: リリィとの信頼再構築中
  "chitsii.arena.player.ending": null,              // enum: "return_to_surface" | "rebuild_arena" | "challenge_observers"

  // === 重要選択（5フラグ） ===
  "chitsii.arena.player.bottle_choice": null,           // enum: null | "refused" | "swapped"
  "chitsii.arena.player.kain_soul_choice": null,        // enum: null | "returned" | "sold"
  "chitsii.arena.player.balgas_choice": null,           // enum: null | "spared" | "killed"
  "chitsii.arena.player.lily_bottle_confession": null,  // enum: null | "confessed" | "blamed_zek" | "denied"
  "chitsii.arena.player.kain_soul_confession": null,    // enum: null | "confessed" | "lied"

  // === NPC関係値（3フラグ） ===
  "chitsii.arena.rel.lily": 30,      // number: 0-100
  "chitsii.arena.rel.balgas": 20,    // number: 0-100
  "chitsii.arena.rel.zek": 0,        // number: 0-100

  // === 重要状態フラグ（2フラグ） ===
  "chitsii.arena.player.lily_hostile": false,        // boolean: リリィ敵対状態
  "chitsii.arena.player.balgas_trust_broken": false  // boolean: バルガスとの信頼崩壊
}
```

**合計: 18フラグ**（従来の50+フラグから大幅削減）

---

## 【フラグ依存関係マップ】

### 主要分岐ポイント

#### scenario/13 bottle_malfunction（瓶すり替えの発覚）

```
IF player.bottle_choice == "swapped"
  THEN bottle_malfunction発生
    CHOICE player.lily_bottle_confession:
      "confessed"   → rel.lily = 20, player.lily_trust_rebuilding = true
      "blamed_zek"  → rel.lily = 50 (表面維持), scenario/16ブロック
      "denied"      → rel.lily = 0, player.lily_hostile = true
```

#### scenario/13 balgas_confrontation（カイン魂売却の発覚）

```
IF player.kain_soul_choice == "sold"
  THEN balgas_confrontation発生
    CHOICE player.kain_soul_confession:
      "confessed" → rel.balgas = 0, player.balgas_trust_broken = true
      "lied"      → rel.balgas = 30 (表面維持)
```

#### scenario/16 lily_true_name（真名開示の条件）

```
REQUIRE:
  player.rank == "S"
  player.balgas_choice == "spared"
  rel.lily >= 50

BLOCK IF:
  player.lily_bottle_confession IN ["blamed_zek", "denied"]
  OR player.lily_hostile == true
```

#### scenario/18 final_battle（最終決戦の協力者）

```
zek:    常にサポート
balgas: player.balgas_trust_broken != true AND player.balgas_choice == "spared"
lily:   player.lily_hostile != true
```

---
