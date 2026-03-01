# 簡体中国語(CN)翻訳追加計画

## Context

Ars Moriendi Modは現在JP/ENの2言語対応。中国語圏のElinプレイヤーにも届けるため、簡体中国語(CN)翻訳を追加する。SukutsuArenaで確立済みのCN対応パターン（Pythonデータに`_cn`フィールド → ビルドスクリプトでCN用Source TSV生成 → LangMod/CN/配置）を踏襲する。

## 前提: NPCごとの話し方の雰囲気ガイド

翻訳にあたり、各NPCのキャラクター性を中国語でも再現するための口調指針。

### 禁書 / ナレーター (The Tome)
- **JP**: 文語的・内省的。「～である」「～だろうか」。哲学的な問いかけ
- **EN**: Philosophical, questioning. "you" で読者に直接語りかける
- **CN方針**: **书面语/文言混用**。"——汝已……" "此非偶然" のように古雅な語感を維持。感情を抑え、観察者としての冷徹さを保つ。句読を「……」「——」で間を作る

### ヘカティア (Hecatia) - 初代継承者 / 商人
- **JP**: **関西弁**。「～やで」「～やねん」「うち」。軽口で深刻な話をする
- **EN**: Casual, witty, contractions多用。Dark humor
- **CN方針**: **市井口语/江湖气**。"来来来，第六代" "这买卖嘛" のような砕けた商人口調。深刻な話も軽く語る。方言的表現より「市場のおばちゃん」感を重視。"嘛" "呗" "啊" などの語気詞を活用

### カレン・グレイヴォーン (Karen) - 灰爵 / 聖騎士
- **JP**: 堅実・厳粛。「～だろう」「～している」。感情を抑えた宣言文
- **EN**: Formal, grave, weary. Direct observations
- **CN方針**: **军人书面语**。"住手，死灵术士！" "这不是承诺，而是事实" のように簡潔で力強い。感情を見せる際は語尾の微妙な変化で表現（"……大概吧"）。"我" 一人称

### エレノス・ヴェルデクト (Erenos) - 第五代継承者
- **JP**: 格式高い・哲学的。古語風。「～だろう」「～のか」。居候モードでは形式的だが情けない
- **EN**: Formal, philosophical. Passive-aggressive when flustered
- **CN方針 (真剣モード)**: **文人书卷气**。"汝已然……成为我" "结果才是一切" のように文語調。自己矛盾を内省する
- **CN方針 (居候モード)**: **酸腐书生**。"这是在下有生以来第一次受此等屈辱" のように大仰だが情けない。ヘカティアへの愚痴は丁寧語のまま

### 神殿騎士・偵察兵 (Temple soldiers)
- **CN方針**: **军令口吻**。短く、命令的。個性なし

## 翻訳対象と作業ステップ

### Step 0: NPC口調ガイドの作成
- 上記の口調指針を `docs/translation-guide-cn.md` に保存
- 翻訳作業時の参照用ドキュメント

### Step 1: Pythonデータファイルに CN フィールド追加

| ファイル | 追加フィールド | 対象数 |
|---|---|---|
| `tools/data/charas.py` | `name_cn`, `detail_cn` を `CharaDefinition` に追加 | ~8キャラ |
| `tools/data/elements.py` | `name_CN`, `detail_CN` を各spellに追加 | 17スペル |
| `tools/data/quests.py` | `name_cn`, `detail_cn` を `QuestPhase` に追加 | 8フェーズ |
| `tools/data/stats.py` | CN対応フィールド追加 | 要確認 |

### Step 2: ビルドスクリプトに CN TSV 出力を追加

SukutsuArenaパターン: `lang="cn"` 時に `name`/`detail` カラムにCN文字列を書き込む

| ファイル | 変更内容 |
|---|---|
| `tools/builder/create_chara_excel.py` | `OUTPUT_CN_TSV` 追加、`generate_rows(lang)` にCN分岐 |
| `tools/builder/create_quest_excel.py` | 同上 |
| その他の `create_*_excel.py` | 同上パターンで追加 |

### Step 3: ドラマシナリオに `text_cn` を追加

`drama_builder.py` は既に `text_cn` パラメータ対応済み（空文字列で出力）。各シナリオの `say()` 呼び出しに `text_cn=` を追加。

| ファイル | say()数 |
|---|---|
| `ars_hecatia_talk.py` | 14 |
| `ars_stigmata.py` | 8 |
| `ars_erenos_appear.py` | 5 |
| `ars_karen_encounter.py` | 4 |
| `ars_apotheosis.py` | 4 |
| `ars_successor_notes.py` | 4 |
| `ars_karen_shadow.py` | 3 |
| `ars_erenos_defeat.py` | 2 |
| `ars_scout_encounter.py` | 1 |
| **合計** | **45** |

`choice_block` のテキストも3箇所あり。

### Step 4: LangHelper.cs を3言語対応に拡張

現在: `Dictionary<string, (string jp, string en)>` + `Lang.isJP`
変更後: `Dictionary<string, (string jp, string en, string cn)>` + `Lang.langCode == "CN"` 判定

- タプルを3要素に拡張（95+メッセージ）
- `Get()` メソッド: `Lang.langCode == "CN"` → `msg.cn` を返す（SukutsuArenaパターン: `!Lang.isJP && Lang.langCode == "CN"`）
- `GetSoulName()` にもCN追加

### Step 5: 書籍テキストの CN 版作成

| ファイル | 内容 | 文字数(JP) |
|---|---|---|
| `LangMod/CN/Text/Book/ars_moriendi.txt` | 禁書本文（エレノスの手記風） | ~1,500字 |
| `LangMod/CN/Text/Book/ars_karen_journal.txt` | カレンの手帳（5代の記録） | ~1,200字 |

### Step 6: LangMod/CN/ ディレクトリ構造の整備

```
LangMod/CN/
├── Data/
│   └── stock_ars_hecatia.json  (JPからコピー、内容はID参照のみ)
├── Text/
│   └── Book/
│       ├── ars_moriendi.txt
│       └── ars_karen_journal.txt
├── Chara.tsv / .xlsx
├── Element.tsv / .xlsx
├── Quest.tsv / .xlsx
├── Stat.tsv / .xlsx
└── Thing.tsv / .xlsx
```

Drama xlsxは `LangMod/EN/Dialog/Drama/` に全言語含まれるため CN にコピー不要。

### Step 7: build.bat に CN ビルドステップ追加

各 `create_*_excel.py` の実行後に CN TSV → XLSX 変換を追加。SukutsuArenaの build.bat パターンを参考に:
```batch
:: CN Chara
LibreOffice --headless --infilter="CSV:9,34,76" --convert-to "xlsx:..." LangMod/CN/Chara.tsv
ren LangMod\CN\Chara.xlsx SourceChara.xlsx
```

## 作業順序

1. **Step 0** - 口調ガイド作成（翻訳品質の基盤）
2. **Step 1** - データファイルにCNフィールド追加（翻訳テキスト入力）
3. **Step 2** - ビルドスクリプトCN対応（CN TSV出力）
4. **Step 3** - ドラマシナリオにtext_cn追加（最大の作業量）
5. **Step 4** - LangHelper.cs 3言語化（C#側対応）
6. **Step 5** - 書籍テキストCN版作成
7. **Step 6** - LangMod/CN/ ディレクトリ構造整備
8. **Step 7** - build.bat CN統合

## 検証方法

1. `build.bat` でビルド → LangMod/CN/ 配下にSource*.xlsx が生成されることを確認
2. Drama xlsx を開いて text_CN 列にテキストが入っていることを確認
3. ゲーム言語を簡体中文に切り替えて動作確認:
   - NPC名・スペル名がCN表示
   - ドラマテキストがCN表示
   - 禁書・カレンの手帳がCN表示
   - ゲームログのキャストメッセージ等がCN表示

## 重要ファイル一覧

- `tools/data/charas.py` - キャラ定義
- `tools/data/elements.py` - スペル定義
- `tools/data/quests.py` - クエスト定義
- `tools/data/stats.py` - ステータス定義
- `tools/builder/create_chara_excel.py` - キャラTSV生成
- `tools/builder/create_quest_excel.py` - クエストTSV生成
- `tools/drama/drama_builder.py` - ドラマビルダー（CN対応済み）
- `tools/drama/scenarios/*.py` - 全9シナリオファイル
- `src/LangHelper.cs` - C#側ローカライズ
- `build.bat` - ビルドパイプライン
- `LangMod/JP/Text/Book/*.txt` - 書籍テキスト

## SukutsuArena参照パターン

- `Lang.langCode == "CN"` 判定: `Elin_SukutsuArena/src/RandomBattle/ArenaGimmick.cs:46`
- CN TSV生成パターン: `Elin_SukutsuArena/tools/builder/create_chara_excel.py`
- シナリオtext_cn: `Elin_SukutsuArena/tools/arena/scenarios/00_arena_master.py`
