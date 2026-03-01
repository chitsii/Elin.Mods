# Elin_SukutsuArena 開発メモ

## バリデーション
```bash
cd tools
uv run python validation.py
```

## フラグ検証
```bash
cd tools
uv run python builder/validate_scenario_flags.py
```

## 型/APIバリデーション（ゲーム更新対策）

### 目的
- ゲーム本体更新で **パッチ対象/呼び出し対象の型やメソッドが変わった** 場合に機械的に検知する。
- Harmonyパッチは **メソッド名 + 引数型の一致**まで検証する。

### 仕組み
- `GameDependency` を抽出し、ゲーム側アセンブリ内に型/メンバーが存在するか確認する。
- HarmonyPatch の `new[]{ typeof(...) }` から **署名**を取得し、検証に使用する。
- 参照アセンブリは原則 `Elin_Data/Managed` を対象にスキャンする。
- `Assembly-CSharp.dll` が極小（スタブ）の場合は **自動的に `Elin.dll` を使用**する。

### 実行コマンド
```bash
cd tools
uv run python builder/verify_game_api.py --ci
```

### 必須環境
- **.NET 8 Runtime (x64)**
- ゲームの Managed パス指定（どちらか）

```bash
set ELIN_GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Elin
```

または

```bash
set ELIN_MANAGED_DIR=C:\Program Files (x86)\Steam\steamapps\common\Elin\Elin_Data\Managed
```

### CWL の扱い
- CWL 未導入環境では `Cwl.*` 型が存在しないため、**INFO として表示**する。
- CWL を検証対象にしたい場合は `BepInEx/plugins` に CWL 本体が存在する環境で実行する。

## Channel Tracker / Compat運用

stable/nightly の差分監視は monorepo 共通の
`..\tools\elin_channel_tracker\run.py` を直接使う。

### 実行コマンド
```bash
python ..\tools\elin_channel_tracker\run.py track-channels --report-json tools/elin_channel_tracker/reports/channel_snapshot.json --report-md tools/elin_channel_tracker/reports/channel_snapshot.md --no-legacy-shim
python ..\tools\elin_channel_tracker\run.py verify-compat --targets tools/elin_channel_tracker/config/compat_targets.json --stable-signatures tools/elin_channel_tracker/config/stable_signatures.json --nightly-signatures tools/elin_channel_tracker/config/nightly_signatures.json --report-json tools/elin_channel_tracker/reports/compat_report.json --report-md tools/elin_channel_tracker/reports/compat_report.md --no-legacy-shim
```

- `origin/stable` / `origin/nightly` が無い環境では `track-channels` は自動で `HEAD` にフォールバックする。

### 出力
- `tools/elin_channel_tracker/reports/channel_snapshot.json`
- `tools/elin_channel_tracker/reports/compat_report.json`
- `tools/elin_channel_tracker/reports/compat_report.md`

### 失敗条件
- `verify-compat` は `summary.broken > 0` のとき終了コード `1`
- `risky` は通知（観測）扱い

### 月次手順
- 詳細は `docs/development/update-checklist.md` を参照

## Prefix / Postfix パッチの選び方と Mod 干渉

### Prefix パッチ

```csharp
[HarmonyPrefix]
static bool Prefix(Chara __instance) { ... }
```

- 元メソッドの**前**に実行される
- `return false` で**元メソッドの実行をスキップ**できる
- 複数 Mod が同じメソッドに Prefix を貼った場合、**1つが `return false` すると他の Prefix も元メソッドも全てスキップ**される
- 干渉リスク: **高い**

### Postfix パッチ

```csharp
[HarmonyPostfix]
static void Postfix(Chara __instance) { ... }
```

- 元メソッドの**後**に実行される
- 元メソッドの実行を阻止できない
- `__result` 引数で戻り値を読み書きできる
- Prefix が `return false` した場合でも Postfix は実行される（Harmony 2.x）
- 干渉リスク: **低い**

### 比較

| | Prefix | Postfix |
|---|---|---|
| 実行タイミング | 元メソッドの前 | 元メソッドの後 |
| 元メソッドのスキップ | `return false` で可能 | 不可 |
| 他 Mod との干渉 | **高い** | **低い** |
| 推奨度 | 本当に必要な時だけ | **基本はこちら** |

### 避けるべきパターン

**1. Prefix で `return false`（最大のアンチパターン）**

```csharp
// 悪い例: 元メソッドを丸ごと置き換え → 他Modも元メソッドも殺す
static bool Prefix(Chara __instance) {
    // 自分の実装で全部やる
    return false;
}
```

代替: Postfix で結果を上書きするか、Transpiler で必要な箇所だけ書き換える。

**2. Prefix で引数を書き換えて副作用を起こす**

```csharp
// 危険: 他Modが期待する引数を変えてしまう
static void Prefix(ref int amount) {
    amount = 0;
}
```

**3. Postfix で `__result` を無条件に上書き**

```csharp
// 悪い例: 他Modの修正を無視して常に上書き
static void Postfix(ref bool __result) {
    __result = true;
}

// 良い例: 自分の条件に合う場合だけ変更
static void Postfix(Chara __instance, ref bool __result) {
    if (IsMyModTarget(__instance))
        __result = true;
}
```

**4. static フィールドで Prefix→Postfix 間の状態を共有**

```csharp
// 悪い例: 他Modが間で値を変更したら壊れる
static int _saved;
static void Prefix(Chara __instance) { _saved = __instance.hp; }
static void Postfix(Chara __instance) { int diff = __instance.hp - _saved; }

// 良い例: __state を使う（Harmony がスレッドセーフに管理）
static void Prefix(Chara __instance, out int __state) {
    __state = __instance.hp;
}
static void Postfix(Chara __instance, int __state) {
    int diff = __instance.hp - __state;
}
```

### 原則

**Postfix で済むなら Postfix を使う。** Prefix の `return false` は「他の全 Mod と元メソッドを殺す覚悟」がある場合のみ。

## 商人在庫の操作方法（CWL）

このModでは商人在庫を **CWLのカスタム在庫API** で制御する。

### 在庫ファイル
- 在庫JSON: `LangMod/**/Data/stock_{ID}.json`
- 生成元: `tools/builder/create_merchant_stock.py`
- 例:
  - `stock_sukutsu_receptionist.json`（リリィの基本在庫）
  - `stock_sukutsu_scroll_showcase.json`（大部屋の巻物の追加在庫）

### ゲーム内での動的切り替え（CWL API）
以下のAPI/コマンドで在庫を切り替えられる。

- 追加: `CustomMerchant.AddStock(cardId, stockId = "")`
  - `stockId` を省略すると `cardId` と同じ在庫IDが使われる
  - 例: `cwl.stock.add sukutsu_receptionist sukutsu_scroll_showcase`
- クリア: `CustomChara.ClearStock(cardId)`
  - 例: `cwl.stock.clear sukutsu_receptionist`

### Mod内の実装（参考）
`src/Commands/UpdateLilyShopStockCommand.cs` で在庫を切り替えている。
クエスト完了後に `sukutsu_scroll_showcase` を追加し、未完了時は一度クリアして基本在庫を再登録する。

### 失敗時の判断
- `High` はビルド失敗（必須修正）。
- `Medium/Low` は状況に応じて確認。
- `INFO` は未導入依存（CWL不在など）。

## 多言語対応（Excel生成）

### CWLの言語切り替え仕様

CWLは `LangMod/{言語コード}/` 配下のファイルを、ユーザーの言語設定に応じて読み込む。
JP/EN以外の言語では、**サフィックスなしのカラム（`name`, `text`, `detail` 等）にその言語のテキストを入れる**必要がある。

- `LangMod/EN/` — `name`=英語, `name_JP`=日本語（ENがマスター）
- `LangMod/CN/` — `name`=**中国語**, `name_JP`=日本語（`text_CN` カラムは効果なし）

`text_CN` のような言語サフィックス付きカラムは **CWLでは無視される**。言語フォルダごとにサフィックスなしカラムの内容を切り替えたファイルを生成すること。

### 各生成スクリプトの対応方法

全ての `tools/builder/create_*_excel.py` は `lang` パラメータで EN/CN を切り替える:

```python
# EN版: name=English
create_tsv(OUTPUT_EN, lang="en")

# CN版: name=中国語（別途生成、コピーではない）
create_tsv(OUTPUT_CN, lang="cn")
```

**やってはいけないこと:**
- EN版を `shutil.copy2` でCNフォルダにコピーする（`name` が英語のままになる）
- `text_CN` カラムを追加して翻訳を入れる（CWLに無視される）

### 新しい言語を追加する場合

1. `LangMod/{言語コード}/` ディレクトリを作成
2. 各 `create_*_excel.py` に `lang="{言語コード}"` 対応を追加
3. サフィックスなしカラム（`name`, `text`, `detail` 等）にその言語のテキストを出力
4. `name_JP` 等のサフィックス付きカラムはENと同じ内容でOK

### 新しいテキストカラムを追加する場合

Source系Excelに新しいテキストカラム（例: `textFlavor`）を追加した場合:
1. データ定義に `_CN` サフィックス付きの翻訳を追加（例: `textFlavor_CN`）
2. `lang="cn"` 時にサフィックスなしカラムへCN翻訳を出力するロジックを追加
3. CN翻訳が未定義の場合はEN値にフォールバック

### ドラマExcel

ドラマExcelは全言語を1ファイルに出力する方式（`text_JP`, `text_EN`, `text_CN` カラム）。
CWLのドラマシステムは `text_CN` カラムを認識するため、Source系とは異なりコピーで問題ない。

## BGM追加・差し替え

### 音声フォーマット仕様
- 形式: OGG Vorbis
- サンプルレート: 48kHz
- チャンネル: ステレオ
- 品質: `-q:a 2`（nominal 96kbps）

### 手順

#### 1. OGGに変換
```bash
ffmpeg -y -i input.mp3 -map 0:a -map_metadata -1 -codec:a libvorbis -ar 48000 -ac 2 -q:a 2 Sound/BGM/OutputName.ogg
```

- `-map 0:a`: 音声ストリームのみ抽出（カバーアート等を除外）
- `-map_metadata -1`: メタデータを全て削除

#### 2. ファイル配置
- `Sound/BGM/` に配置（ファイル名はPascalCase、拡張子なしのパスで参照される）

#### 3. 参照設定
BGMの使用箇所に応じて以下を更新:
- **バトルステージ**: `Package/battle_stages.json` の `bgmBattle` / `bgmVictory`
- **プレイリスト**: `Sound/BGM/Playlist/Zone_*.json` の `list` 配列
- **ドラマ**: シナリオ定義の `bgm` フィールド

参照パスは `BGM/FileName`（`Sound/` プレフィックスと `.ogg` 拡張子は不要）。

## リリースノート

リリース時に `docs/release_notes/YYYY-MM-DD.md` を作成する。

### フォーマット
- ファイル名: `YYYY-MM-DD.md`（リリース日）
- 1行目: `YYYY/M/D Update (vX.XX.XXX)` （package.xmlのバージョン）
- 内容: JP → EN → CN の順で、`---` 区切り
- セクション: ストーリー / 新機能 / バランス調整 / バグ修正 / その他（該当するもののみ）

### 手順
1. `docs/release_notes/YYYY-MM-DD.md` を作成
2. Steam Workshop の更新情報欄に転記（必要に応じて簡略化）
3. コミット
