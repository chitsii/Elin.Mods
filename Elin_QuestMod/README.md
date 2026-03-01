# Elin_QuestMod (Quest Mod Skeleton)

Elin 向けクエスト Mod の最小テンプレートです。  
「stable / nightly 差分に耐える実装」と「ゲーム内 smoke テスト運用」を最初から含めることを目的にしています。

## 概要

- 役割
  - クエスト進行フラグ管理の土台を提供する
  - Harmony パッチの最小構成を提供する
  - runtime smoke test の標準ケースを提供する
- 実装の芯
  - `EClass.player.dialogFlags` ベースで状態管理（キー prefix は既定で `Plugin.ModGuid`）
  - forward-only `QuestStateMachine` による宣言的な進行管理
  - `Zone.Activate` の Postfix で定期的にクエスト判定 (`Pulse`) を実行
  - API 変更リスクがある箇所は Compat ラッパーで吸収

## 構成の全体像

### 実行時フロー

1. `Plugin.Awake()` で `Harmony.PatchAll()` を実行
2. `Patch_Zone_Activate_QuestPulse.Postfix()` が `QuestFlow.Pulse()` を呼ぶ
3. `QuestFlow.Pulse()` が以下を実行
   - `EnsureBootstrap()` で初期状態を一度だけセット
   - `QuestStateMachine` でフェーズを前進（forward-only）
   - `QuestStateService.CheckQuestsForDispatch()` で分岐先を決定
4. クエスト状態は `dialogFlags` に保存

### ディレクトリ

```text
Elin_QuestMod/
  src/
    Plugin.cs
    ModLog.cs
    Compat/
      PointCompat.cs
    Patches/
      Patch_Zone_Activate_QuestPulse.cs
    Quest/
      QuestFlow.cs
      QuestStateService.cs
    CommonQuest/
      Conditions/
        QuestCondition.cs
      StateMachine/
        QuestStateMachine.cs
        QuestTriggeredStateMachine.cs
  tests/runtime/
    run.ps1
    README.md
    src/cases/
    src/drama/
  build.bat
  config.bat
  package.xml
```

### 主要クラス

- `src/Plugin.cs`
  - Mod GUID 定義と Harmony 初期化
- `src/Quest/QuestStateService.cs`
  - フラグ読み書き
  - `StartQuest` / `CompleteQuest`
  - dispatch 判定
- `src/Quest/QuestFlow.cs`
  - ブートストラップ
  - フェーズ遷移のステートマシン定義
  - `Pulse` の実行順序
- `src/CommonQuest/StateMachine/*.cs`
  - Vile由来の汎用ステートマシン（重複遷移/後退遷移を拒否）
- `src/Patches/Patch_Zone_Activate_QuestPulse.cs`
  - `Zone.Activate` への Postfix
- `src/Compat/PointCompat.cs`
  - `Point.GetNearestPoint` の 4/5 引数差異吸収

## 運用方法

### 前提

- Windows + Steam 版 Elin
- .NET SDK (`dotnet build` が実行できること)
- `config.bat` の Steam パスが正しいこと

### `build.bat` の仕様と更新方法

#### 仕様

`build.bat` は次の順序で実行されます。

1. `python tools/drama/schema/generate_keys.py --write`
2. `python -m unittest discover -s tools/drama/tests -t . -v`
3. `python tools/drama/create_drama_excel.py`
4. `dotnet build Elin_QuestMod.csproj -c <Debug/Release>`
5. `elin_link\Package\<MOD_FOLDER>\` へ DLL / `package.xml` / `LangMod` をコピー（存在すれば `Texture` / `Portrait` / `Sound` もコピー）
6. `STEAM_PACKAGE_DIR`（Steam 実ゲーム側）へ DLL / `package.xml` / `LangMod` をコピー（存在すれば `Texture` / `Portrait` / `Sound` もコピー）

引数は次の4パターンです。

- `build.bat` : `Debug` ビルド
- `build.bat debug` : `Debug` ビルド
- `build.bat release` : `Release` ビルド
- `build.bat drama` : drama 生成 + `LangMod` 配布（存在すれば `Texture` / `Portrait` / `Sound` も配布、DLL再ビルドなし）

失敗時の挙動:

- drama キー生成・drama test・drama Excel 生成のいずれかが失敗したら即終了（exit code 1）
- ビルド失敗時は即終了（exit code 1）
- Steam 側コピー失敗時も終了（ゲーム起動中の DLL ロックが主因）

`build.bat drama` の用途:
- drama シナリオ修正を高速反映したいとき
- DLL更新を伴わず `LangMod/EN/Dialog/Drama/*.xlsx` を再配布したいとき（必要に応じて `Texture` / `Portrait` / `Sound` の同梱更新も可能）

#### 設定ファイル（`config.bat`）

`build.bat` は `config.bat` を読み、次の変数を使います。

- `MOD_FOLDER` : 配置先フォルダ名（例: `Elin_QuestMod`）
- `MOD_DLL` : 出力 DLL 名（例: `Elin_QuestMod.dll`）
- `STEAM_ELIN_DIR` : Steam の Elin ルート
- `STEAM_PACKAGE_DIR` : 実コピー先（通常は `STEAM_ELIN_DIR\Package\MOD_FOLDER`）

#### 更新方法（Mod 名や配置先を変えるとき）

1. `config.bat` を更新する
   - `MOD_FOLDER`, `MOD_DLL`, `STEAM_ELIN_DIR`
2. `package.xml` のメタ情報を更新する
   - `<id>`, `<title>`, `<author>`, `<version>`
3. `src/Plugin.cs` の `ModGuid` を更新する
4. 必要なら `Elin_QuestMod.csproj` の `AssemblyName` / `RootNamespace` を更新する
5. `build.bat` 実行でコピー先と DLL 名が意図どおりか確認する

#### `build.bat` 自体を更新するとき

1. 6段階フローを維持する
   - drama key generate -> drama test -> drama excel generate -> build -> linked copy -> steam copy
2. 失敗時に `exit /b 1` を返す動作を維持する
3. 変数は `config.bat` に寄せ、`build.bat` に直書きしない
4. 変更後は `build.bat` と `build.bat release` の両方を実行して確認する
5. README の本セクションを同時更新する

### 日常開発フロー

1. ビルド
   - `build.bat`
2. ゲーム起動・対象セーブ（プレイヤー名に"RUNTIME_TEST"文字列が含まれることがテスト起動条件）読み込み
3. smoke 実行
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
4. drama 通常 smoke 実行（成功必須ケース）
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama`
5. 重要系のみ確認
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`

### リリース前フロー

1. `build.bat release`
2. `-Suite smoke` を完走
3. `-Suite drama` を完走（placeholder + feature showcase + followup + branch_a + branch_b + merge）
4. 必要に応じて単体 drama ケースを実行
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_showcase`
5. 必要に応じて能動 compat チェックを単発実行
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.patch.compat.cwl_incompatible_scan`
   - このケースにパスするとメソッドに（INCOMPATIBLE）マークがつかなくなります。
   - 落ちたとしてもLINQとチェッカーとの相性問題の可能性もあり、実際の挙動は問題ないことがあります。テスト中にゲームが不安定になる可能性があります。


### 定期更新フロー

目的は「stable / nightly の両対応を維持し、破壊的な API 差分を早期に検知する」ことです。
stableがリリースされる頻度、月1回ぐらいで回すことを想定しています。

前提（パス運用）:
- 実行ディレクトリは `Elin_QuestMod/`（この README がある階層）
- tracker 実行本体は monorepo 共通の `..\tools\elin_channel_tracker\run.py`
- QuestMod 側の入力/出力は `.\tools\elin_channel_tracker\config\` と `.\tools\elin_channel_tracker\reports\` を使う

#### 0. 事前準備（初回のみ）

1. 専用セーブを2つ作る（通常プレイ用と混ぜない）
   - `RUNTIME_TEST_STABLE`
   - `RUNTIME_TEST_NIGHTLY`
2. 2つのセーブは同じ検証条件に揃える
   - 同じマップ（拠点や野原）
   - 可能な範囲で、同じ所持品/仲間状態
3. tracker 用の mod ローカル設定フォルダを作る（未作成なら）
   - `tools/elin_channel_tracker/config/`
   - `tools/elin_channel_tracker/reports/`
4. `tools/elin_channel_tracker/config/compat_targets.json` を用意する
   - 初回は他 Mod の既存ファイルをコピーして最小化しても良い

#### 1. stable チャンネル検証

1. Steam で stable に切り替える
   - Steam -> Elin -> プロパティ -> ベータ
   - `なし`（または stable 相当）を選択
2. 更新完了後、ゲームを起動せず Mod をビルドして反映
   - `build.bat`
3. ゲームを起動し、stable 用セーブをロード
4. smoke テストを実行
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`
5. ゲームを終了し、stable API 署名を収集

```powershell
python ..\tools\elin_channel_tracker\run.py collect-signatures `
  --game-dir "C:\Program Files (x86)\Steam\steamapps\common\Elin" `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --output ".\tools\elin_channel_tracker\config\stable_signatures.json"
```

#### 2. nightly チャンネル検証

1. Steam で nightly に切り替える
   - Steam -> Elin -> プロパティ -> ベータ
   - `nightly`（名称が近い preview/beta 枝でも可）を選択
2. 更新完了後、ゲームを起動せず Mod をビルドして反映
   - `build.bat`
3. ゲームを起動し、nightly 用セーブをロード
4. smoke テストを実行
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`
5. ゲームを終了し、nightly API 署名を収集

```powershell
python ..\tools\elin_channel_tracker\run.py collect-signatures `
  --game-dir "C:\Program Files (x86)\Steam\steamapps\common\Elin" `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --output ".\tools\elin_channel_tracker\config\nightly_signatures.json"
```

#### 3. 互換性レポート作成と解析（必須）

1. verify-compat を実行（broken は exit code 1）

```powershell
python ..\tools\elin_channel_tracker\run.py verify-compat `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --stable-signatures ".\tools\elin_channel_tracker\config\stable_signatures.json" `
  --nightly-signatures ".\tools\elin_channel_tracker\config\nightly_signatures.json" `
  --report-json ".\tools\elin_channel_tracker\reports\compat_report.questmod.json" `
  --report-md ".\tools\elin_channel_tracker\reports\compat_report.questmod.md"
```

2. detect-target-gaps を実行（`compat_targets.json` の棚卸し）
   - `--fail-on-missing` で未登録ターゲットをゲート化
   - `--fail-on-undetected` で設定だけ残った古いターゲットをゲート化

```powershell
python ..\tools\elin_channel_tracker\run.py detect-target-gaps `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --src-root ".\src" `
  --include-heuristics `
  --fail-on-missing `
  --fail-on-undetected `
  --report-json ".\tools\elin_channel_tracker\reports\target_gap_report.questmod.json" `
  --report-md ".\tools\elin_channel_tracker\reports\target_gap_report.questmod.md"
```

3. `target_gap_report.questmod.md` を読み、次を確認
   - `missing_targets = 0`
   - `configured_not_detected = 0`
   - `check_kind_mismatches = 0`
4. 生成レポートの読み方（古い Compat の棚卸しに使う）
   - `compat_report.questmod.md` / `.json`
   - 主な項目
     - `broken`: 修正必須（`missing_symbol` / `signature_mismatch`）
     - `risky`: 要確認（`renamed_candidate` / `unresolved_dynamic`）
     - `ok`: 両チャネルで解決成功
   - `target_gap_report.questmod.md` / `.json`
   - 主な項目
     - `missing_targets`: コードで使っているが `compat_targets.json` 未登録
     - `configured_not_detected`: 設定にあるがコードから検出されない（古い Compat 候補）
     - `check_kind_mismatches`: `check_kind` の設定ミス候補

#### 4. 修正と再検証（必須）

1. レポートの差分種類を切り分ける
   - 引数変化（追加/削除/型変更/optional化）
   - メソッド改名（旧名が消えて新名へ移動）
2. 引数変化のとき（最優先でこの形に寄せる）
   - `compat_targets.json` に旧/新の両シグネチャを並記（両チャネルの共通部分を作る）
   - Compat ラッパーを追加し、`MethodInfo` 解決を「新シグネチャ優先 -> 旧シグネチャ fallback」にする
   - optional 引数は「バニラ既定値」を明示して渡す（例: `allowInstalled=true` を維持）
   - 呼び出し側を直接呼び出しから Compat ラッパー経由に統一する
3. 改名のとき（旧名/新名の二段解決）
   - Compat ラッパーで `GetMethod("新名", ...)` を先に試し、失敗時に `GetMethod("旧名", ...)` を試す
   - `compat_targets.json` は旧名・新名の両方を登録し、検知漏れを防ぐ
   - パッチ対象メソッド（`TargetMethod`）も同じ順で二段解決にする
4. 修正後の再検証（省略不可）
   - stable・nightlyの両方で `build.bat` -> smoke 実行
   - `verify-compat` と `detect-target-gaps` を再実行
   - 合格基準: `broken=0` / `missing_targets=0` / `configured_not_detected=0` / `check_kind_mismatches=0`
5. 古くなった Compat のメンテ（削除）手順
   - 対象は `target_gap_report.questmod.md` の `Configured but Not Detected`
   - まず参照を確認（0件なら削除候補）
     - `rg -n "<Type.Member>" .\\src .\\tests`
   - stable/nightly 署名カタログで残存確認（両方に無ければ削除候補）
     - `rg -n "\"<Type.Member>\"" "<stable_signatures.json>" "<nightly_signatures.json>"`
   - 改名移行の掃除
     - `compat_report.questmod.md` で対象が `ok` かつ `reason_code=matched` を確認
     - 旧 `candidate_names` / 旧 `candidate_signatures` が両署名カタログに存在しないことを確認
     - 確認できたら `compat_targets.json` と Compat ラッパーの旧 fallback を削除
   - 削除条件（原則）
     - コード参照なし
     - 両チャネル署名カタログにも存在しない
   - 削除対象
     - `compat_targets.json` の対象エントリ
     - Compat ラッパー内の旧 fallback 分岐（旧メソッド名/旧シグネチャ）
     - 旧ターゲット向け patch 解決分岐
   - 例外（削除しない）
     - 文字列連結などで自動検出できない意図的 dynamic 呼び出し
     - この場合は `compat_targets.json` の `auto_detect_ignore_targets` に理由付きで追加

#### 5. 失敗時の対処

- `verify-compat` は通るが runtime で落ちる
  - そのまま出荷せず、runtime を優先して原因を潰す

## クエストの作り方

### 1. まず ID を置き換える

- `src/Plugin.cs` の `ModGuid`
- `package.xml` の `<id>`, `<title>`, `<author>`, `<version>`

### 2. クエスト状態キーを設計する

- `QuestStateService` のキー命名を基準にする
  - 進行段階: `<mod_guid>.quest.current_phase`
  - active: `<mod_guid>.quest.active.<quest_id>`
  - done: `<mod_guid>.quest.done.<quest_id>`
  - `<mod_guid>` は未指定時 `Plugin.ModGuid` が自動採用される

### 3. 初期化と分岐を定義する

- `QuestFlow` を編集して以下を定義
  - `IntroQuestId`, `FollowupQuestId` など実 ID
  - `EnsureBootstrap()` の初期状態
  - `DispatchQuestIdsCsv` の優先順位

### 4. 進行ロジックを追加する

- イベント発火箇所から `QuestStateService` を呼ぶ
  - 開始: `StartQuest("quest_id")`
  - 完了: `CompleteQuest("quest_id", nextPhase)`
- 条件分岐で `IsQuestActive` / `IsQuestCompleted` / `GetCurrentPhase` を使う

### 5. トリガー (パッチ) を拡張する

- `Patch_Zone_Activate_QuestPulse` を起点に、必要なら別パッチを追加
- 推奨ルール
  - `TargetMethod()` を明示
  - 例外は握りつぶさずログ化して継続 (fail-soft)
  - Prefix でなく Postfix で済むなら Postfix を優先

### 6. Compat を先に整備する

- API 変更リスクがある呼び出しは Compat ヘルパー経由に統一
- 例: `PointCompat.GetNearestPointCompat(...)`
  - `allowInstalled=true` のデフォルトを維持して挙動退行を防ぐ

### 7. runtime テストを先に作る

- `tests/runtime/src/cases` にケース追加
- 最低でも以下を維持
  - plugin/package 契約
  - patch target 契約
  - quest flow の bootstrap
  - quest lifecycle と dispatch

## Runtime Test / Drama Test

詳細仕様は `tests/runtime/README.md` を参照してください。  
ここでは運用上の要点のみ記載します。

- 共通実行条件
  - セーブ名に `RUNTIME_TEST` を含める（既定のセーブガード）
  - ゲーム起動前に `build.bat` を実行して DLL を最新化する
- smoke（機能回帰チェック）
  - 既定実行: `run.ps1` は `Suite=smoke` かつ `Tag=smoke`
  - 重要系のみ: `-Suite smoke -Tag critical`
  - 単体ケース: `-Suite smoke -CaseId <case_id>`
  - 能動互換チェック: `-CaseId questmod.patch.compat.cwl_incompatible_scan`（通常 smoke 外）
- drama（ドラマ分岐チェック）
  - `-Suite drama` で `IDramaCaseProvider` 実装を実行
  - `-CaseId` は `DramaId` を指定
  - 既定カタログは `quest_drama_replace_me` / `quest_drama_feature_showcase` / `quest_drama_feature_followup` / `quest_drama_feature_branch_a` / `quest_drama_feature_branch_b` / `quest_drama_feature_merge`
  - 必要に応じて `-CaseId` で単体実行
  - `-Tag` は空または `drama` のみ有効（他タグだと対象0件になり得る）
  - テンプレートの `quest_drama_replace_me` は実 ID へ置換して使う
- 追加済みの最小 smoke
  - `questmod.drama.feature_showcase.activate_smoke`
  - `questmod.drama.feature_showcase_followup.activate_smoke`
  - 生成済み xlsx の存在確認 + `LayerDrama.Activate` 成功確認

## Drama Builder (tools/drama)

`tools/drama` はクエストドラマの Excel 生成ツールです。  
テンプレートでは次のサンプルを同梱しています。

- `quest_drama_replace_me`: 最小シナリオ
- `quest_drama_feature_showcase`: 会話形式の機能ショーケース（クエスト進行・分岐・BGM/背景/NPC演出）
- `quest_drama_feature_followup`: feature showcase ルートAからの遷移先ドラマ

- キー定義を更新して再生成
  - 定義編集: `tools/drama/schema/key_spec.py`
  - 生成実行: `python tools/drama/schema/generate_keys.py --write`
  - 生成物:
    - `tools/drama/data_generated.py`
    - `src/Drama/Generated/DramaKeys.g.cs`
- ドラマ Excel を生成
  - `python tools/drama/create_drama_excel.py`
  - 出力先:
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_replace_me.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_showcase.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_followup.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_branch_a.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_branch_b.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_merge.xlsx`
- 一時フラグ操作（ドラマ側）
  - `set_flag("<tmp_flag>", <value>)`
  - `mod_flag("<tmp_flag>", "+/-", <value>)`
  - `branch_if(...)` / `switch_on_flag(...)` で分岐
  - `switch_on_flag(..., fallback=...)` の fallback は未一致時の default jump（`==0` 固定ではない）
  - 例は `tools/drama/scenarios/quest_drama_feature_showcase.py` を参照
  - ルートAは同一ドラマ再起動ではなく、`quest_drama_feature_followup` へ明示遷移する
- CWL-only ポリシー（既定）
  - `DramaBuilder` は既定で `modInvoke` 依存APIを禁止（`mod_invoke` / `switch_flag` など）
  - 可能な限り CWL/vanilla 標準 action を使用（例: `start_quest` -> `startQuest`, `complete_quest` -> `completeQuest`）
  - 互換目的でどうしても `modInvoke` が必要な場合のみ `DramaBuilder(allow_mod_invoke=True)` を明示する
- DEBUG での showcase 手動起動
  - コンソールコマンド: `questmod.debug.showcase`
  - ヘルプ: `questmod.debug.showcase.help`
  - `cwl.cs.eval` 直呼び出し:
    - `cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.StartShowcase();`
    - `cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.EvalShowcase();`
- テンプレートをコピーした後に必ず置換
  - `yourname.elin_quest_mod.*` プレフィックス
  - `quest_drama_replace_me` ID

注意:
- `build.bat` は drama キー生成・drama test・drama Excel 生成を自動実行します。
- ゲーム起動中は DLL ロックで Steam 側コピーに失敗するため、先にゲームを終了してください。

## よくある問題

- `Elin.dll がロックされている`
  - ゲームが DLL を掴んでいるため。ゲーム終了後に `build.bat` を再実行
- Runtime 実行時に型未解決 (`CS0246`) が出る
  - Mod DLL が読み込まれていない可能性が高い。再ビルド + ゲーム再起動で確認
- `drama` suite が失敗する
  - プレースホルダ ID (`quest_drama_replace_me`) のまま

## 最小チェックリスト

- [ ] `build.bat` が通る
- [ ] `build.bat release` が通る
- [ ] `smoke` が通る
- [ ] `critical` が通る
- [ ] `package.xml` の ID と `Plugin.ModGuid` が一致
- [ ] 新しい patch / compat 追加時に対応ケースを追加
