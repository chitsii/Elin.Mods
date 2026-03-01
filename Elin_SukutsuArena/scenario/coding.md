# Elin_SukutsuArena シナリオ実装ガイド

このドキュメントは、Elin Mod「Sukutsu Arena」のドラマ（会話イベント）パートを実装するAIエージェントおよび開発者のためのリファレンスです。

## 🏗 アーキテクチャ概要

このModでは、Elinのドラマデータ（Excelファイル）を直接編集するのではなく、**PythonスクリプトによるDSL (Domain Specific Language)** を使用して生成しています。

1.  **Python DSL**: `DramaBuilder` クラスを使用して、コードベースで会話フローを定義。
2.  **Excel生成**: ビルド時にPythonスクリプトが実行され、定義されたフローをExcel形式 (`.xlsx`) に変換。
3.  **ゲーム読み込み**: Modロード時にElinがExcelを読み込み、ゲーム内に反映。

**開発者は Pythonスクリプト (`tools/common/scenarios/*.py`) のみを編集します。Excelファイルは成果物であり、編集対象ではありません。**

---

## 📂 ディレクトリ構造と役割

重要なファイルとディレクトリのみを抜粋しています。

```hierarchy
Elin_SukutsuArena/
├── build.bat                   # ビルドスクリプト（これを実行すると全工程が走る）
├── tools/
│   ├── common/
│   │   ├── drama_builder.py    # 【重要】DSLコアロジック (API定義はここを見る)
│   │   ├── scenarios/          # 【作業場所】シナリオ定義ファイルの置き場
│   │   │   ├── arena_master.py # 参考例1: メイン分岐ロジック
│   │   │   └── rank_up/        # 参考例2: ランク昇格試合のシナリオ群
│   │       └── rank_g.py
│   ├── create_drama_excel.py   # エントリーポイント (作成したシナリオをここで登録)
│   └── flag_definitions.py     # フラグ名の定義定数
└── LangMod/JP/Dialog/Drama/    # 生成されたExcelが出力される場所 (確認用)
```

---

## 🚀 実装ワークフロー

新しいシナリオ（例: `my_new_scenario`）を追加する場合の手順です。

### 1. シナリオファイルの作成
`tools/common/scenarios/my_new_scenario.py` を作成し、関数を定義します。

```python
from drama_builder import DramaBuilder

def define_my_scenario(builder: DramaBuilder):
    # アクターの登録 (ID, 表示名, 英語名)
    pc = builder.register_actor("pc", "あなた", "You")
    lily = builder.register_actor("sukutsu_receptionist", "リリィ", "Lily")

    # ラベルの定義
    start = builder.label("start")
    end = builder.label("end")

    # フローの構築
    builder.step(start) \
        .focus_chara("sukutsu_receptionist") \
        .say("hello", "こんにちは、新しいシナリオです。", "Hello.", actor=lily) \
        .wait(0.5) \
        .jump(end)

    builder.step(end).end()
```

### 2. エントリーポイントへの登録
`tools/create_drama_excel.py` を開き、作成した関数をインポートして登録します。

```python
# tools/create_drama_excel.py

from common.scenarios.my_new_scenario import define_my_scenario  # インポート

def main():
    # ... (他コード)
    
    # シート名と定義関数を紐付け
    process_sheet("my_new_drama_id", define_my_scenario)
```

### 3. ビルドと確認
プロジェクトルートで `build.bat` を実行します。

```bash
> build.bat
```

成功すると `[INFO] Drama Excel generation successful.` と表示され、`LangMod/.../drama_my_new_drama_id.xlsx` が生成されます。

---

## 📚 DramaBuilder API クイックリファレンス

詳細は `tools/common/drama_builder.py` の docstring を参照してください。

### 基本操作
- `step(label)`: 指定したラベルの処理ステップを開始。
- `label("name")`: ラベルオブジェクトを作成（文字列でも指定可能だが変数推奨）。
- `jump(label)`: 指定ラベルへジャンプ。
- `end()`: ドラマ終了。

### 会話・演出
- `say(id, text_jp, text_en, actor)`: 台詞を表示。`actor` は登録した変数を渡す。
- `focus_chara(chara_id)`: 指定キャラにカメラ/フォーカスを向ける。
- `wait(seconds)`: 指定秒数待機。
- `play_bgm("BGM/Title")`: BGM再生。
- `play_sound("sound_id")`: SE再生。

### 選択肢
- `choice(jump_label, text_jp, text_en)`: 選択肢を表示。
- **注意**: 選択肢は `builder.step(label)` の直後に連続して記述し、その後に分岐先のステップ定義を書くのが基本パターンです。

```python
    # 選択肢の提示
    builder.step(question) \
        .say("q1", "どちらにしますか？", "", actor=lily) \
        .choice(route_a, "Aにする") \
        .choice(route_b, "Bにする")

    # 分岐先A
    builder.step(route_a).say("ans_a", "Aですね。", "", actor=lily).end()
    # 分岐先B
    builder.step(route_b).say("ans_b", "Bですね。", "", actor=lily).end()
```

### フラグ分岐
- `branch_if(flag_name, operator, value, jump_label)`: 条件を満たせばジャンプ。
- `set_flag(flag_name, value)`: フラグ値を設定。

```python
    builder.step(check) \
        .branch_if("my_flag", ">=", 10, high_route) \
        .jump(low_route) # 条件を満たさない場合
```

---

## 🔍 トラブルシューティング・調査ガイド

問題が発生した場合の調査の取っ掛かりです。

### Q1. ビルドが通らない (Pythonエラー)
- **確認場所**: コンソールのトレースバック。
- **よくある原因**:
    - インポートパスミス (`from common.scenarios...` が正しいか)
    - 未定義のラベルへのジャンプ
    - `DramaBuilder` メソッドの引数不足
    - cd tools; uv run python ... で実行する

### Q2. ゲーム内でドラマが始まらない
- **確認場所**:
    1. 生成されたExcelファイル (`LangMod/JP/Dialog/Drama/*.xlsx`) が存在するか。
    2. ゲーム内の呼び出しID（`drama_rank_up_G`など）と、`create_drama_excel.py` で指定したシートIDが一致しているか。
    3. `package.xml` の設定（通常は自動コピーされるので問題ないはず）。

### Q3. ロジックが複雑で書き方がわからない
- **参考ファイル**:
    - **分岐・ループ**: `tools/common/scenarios/arena_master.py` (アリーナマスターのメイン分岐)
    - **戦闘開始・結果分岐**: `tools/common/scenarios/rank_up/rank_g.py`

### Q4. C#のエラーが出る (CS1501など)
- `eval` アクションや `set_flag_eval` を使っている場合、生成されるC#コードが正しいか確認してください。
- `DramaManager.cs` や `ArenaFlagManager.cs` の定義を確認する必要があります（デコンパイルコード参照）。

---

## ✅ 実装の掟 (Rules)

1.  **既存コードの破壊禁止**: `arena_master.py` などの基盤ロジックを修正する場合は、必ずバックアップを取るか、影響範囲を確認すること。
2.  **IDのユニーク性**: `say` や `step` のIDはExcel内でユニークである必要があります（Builderが自動で連番を振る機能もあるが、明示的なID指定を推奨）。
3.  **日本語コメント**: コード内には意図を説明する日本語コメントを積極的に残すこと。
