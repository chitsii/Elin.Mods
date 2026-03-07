# Ars Moriendi AGENT

Elin用の死霊術Mod。ここには「毎回の実装判断に必要な運用ルール」だけを置く。

## 必須フロー

1. `build.bat` でビルド。
2. ゲーム内で動作確認。
3. 公開前に `build.bat` で Release ビルド。

## 実装ルール（要点）

- 生成物は通常 `build.bat`（`--check`）で差分検出し、差分があれば失敗扱い。
- 生成物を更新する明示操作は `build.bat regen` のみ。
- Harmony パッチは TargetMethod を明示し、fail-soft（例外時はログ出しして継続）を優先。
- `EnableMod` のようなグローバルON/OFFは持たず、機能単位トグルのみ許可する。
- Prefix で済ませず Postfix で実現できるなら Postfix を優先。
- 外部PWADの開発用サンプルは `dev_samples/external_mods/` にのみ置き、git追跡は許可するが配布物には混ぜない。
- Thing をマップ上でも mod sprite で描かせたい場合、`render_data` は pass 付きの `obj` ではなく `@obj` 系を使って sprite actor 描画へ寄せる。
- プレイヤー1ターン終端に同期する処理は `BaseGameScreen.OnEndPlayerTurn` を優先し、`Player.EndTurn` を基準にしない。
- 従者の生存・所属チェックは `IsPCFactionOrMinion` を使う。
- `homeBranch.members` に残したキャラはロード時に `currentZone==null/somewhere` だとバニラが拠点へ自動補正するため、スタッシュ設計ではこの条件を避ける。
- 実装変更で運用ルール/手順に影響が出る場合、同一作業内で関連ドキュメントを更新する。

## AGENT.md運用ルール

### 1) Elin API知見の追加フロー

1. トリガー: 実装・調査中に再利用価値の高い API 挙動を確認した。
2. 判定: 以下のいずれかを満たす場合は記録対象。
   - 今後の誤用リスクが高い
   - 今後2回以上参照する見込みがある
3. 更新:
   - 詳細は `docs/development/elin-api.md` に追記（前提、挙動、注意点）
   - 毎回の実装判断で使う要点のみ `AGENT.md` の「実装ルール（要点）」へ1行で追加
4. 検証: 可能な範囲で該当コードかテストで事実確認する。
5. 記録: 変更理由をコミットメッセージまたは `docs/release_notes/YYYY-MM-DD.md` に残す。

### 2) ドキュメント誤り修正フロー

1. トリガー: 手順不整合、古い API 名、仕様誤認などの誤りを発見した。
2. 判定: 「事実誤り」か「表現改善」かを区別する。
3. 更新:
   - 事実誤りは即時修正（該当ドキュメント + 必要なら `AGENT.md`）
   - 表現改善は関連変更と同時対応でも可
4. 検証: 実コマンド、実ファイルパス、現行コードと突合する。
5. 記録: 修正箇所に確認日を残す（例: `最終確認: 2026-02-17`）。

### 3) ユーザ方針修正の永続化フロー

1. トリガー: ユーザから汎用的な運用方針の修正指示を受けた。
2. 判定: 一時要望か恒久ルールかを確認し、恒久ルールは永続化対象とする。
3. 更新:
   - 毎回の実装判断に必要なルールは `AGENT.md` へ
   - 詳細手順や背景は `docs/development/*.md` へ
   - 両方必要な場合は「AGENTに要点、docsに詳細」を徹底する
4. 反映報告: どのファイルのどの節に反映したかを作業報告で明示する。
5. 適用開始: 反映後の次タスクから新方針をデフォルト適用する。

### 4) 記載基準（AGENT / docs）

- `AGENT.md` に書く: 毎回の作業判断を変える短い運用ルール、参照頻度の高いパス・資材。
- `docs` に書く: 背景説明、詳細仕様、長い手順、毎回は参照しない情報。

### 5) ドキュメント同期トリガー

- `build.bat` / 生成・検証手順を変更したら `docs/development/build-and-validation.md` を同時更新する。
- Harmony パッチ方針・干渉方針を変更したら `docs/development/patching.md` を同時更新する。
- API運用知見を追加・修正したら `docs/development/elin-api.md` を同時更新する。
- ログレベルやログ方針を変更したら `docs/development/logging.md` を同時更新する。
- ゲーム更新時の確認項目を変更したら `docs/development/update-checklist.md` を同時更新する。

### 6) 反映報告フォーマット

- 作業報告では次の形式を使う: `反映: <file path> (<section or line>) - <what changed>`
- 例: `反映: AGENT.md (ドキュメント同期トリガー) - build/patch/api更新時の同時更新ルールを追加`

## 詳細ドキュメント

- ビルド/バリデーション: [docs/development/build-and-validation.md](./docs/development/build-and-validation.md)
- 呪文追加ガイド: [docs/development/spell-authoring.md](./docs/development/spell-authoring.md)
- パッチ戦略: [docs/development/patching.md](./docs/development/patching.md)
- Elin APIメモ: [docs/development/elin-api.md](./docs/development/elin-api.md)
- ログ方針: [docs/development/logging.md](./docs/development/logging.md)
- 更新時チェック（ゲーム更新時のみ）: [docs/development/update-checklist.md](./docs/development/update-checklist.md)
- Unity FX導入: [docs/development/unity-fx-first-effect-guide.md](./docs/development/unity-fx-first-effect-guide.md)
- パーティクル設計Tips: [docs/development/unity-particle-design-tips.md](./docs/development/unity-particle-design-tips.md)

## 高頻度参照（パス/資材）

### DLL逆コンパイル

```bash
DOTNET_ROLL_FORWARD=LatestMajor ilspycmd "path/to/target.dll"
DOTNET_ROLL_FORWARD=LatestMajor ilspycmd "path/to/target.dll" -o "output_directory"
```

### 外部参照

- 親ディレクトリ: `C:\Users\tishi\programming\elin_modding\`

- CWLソース（git pull可）: `Elin.Plugins\CustomWhateverLoader`
- CWLドキュメント（git pull可）: `Elin.Docs/articles/100_Mod Documentation/Custom Whatever Loader/JP/`
- ゲーム本体ソース（git pull可）: `Elin-Decompiled/`

- Modお手本（pull不可）: `Elin.Mods\Elin_LogRefined/`
- SukutsuArenaパイプライン参考: `Elin.Mods\Elin_SukutsuArena/tools/`
- YK Framework参照: `ykeyjp/ElinMod/YKFramework/`

### SourceExcel（ゲームデータ検索）

- ソース: `Elin.Mods\SourceExcels\` （xlsx）
- CSV変換済み: `tools/data/csv/` （grep で即検索可能）
- 再生成: `python tools/data/convert_source_excel.py`
- 詳細: [docs/development/elin-api.md](./docs/development/elin-api.md) の「SourceExcel データの探索」節

### デバッグログ

- Player.log: `/c/Users/tishi/AppData/LocalLow/Lafrontier/Elin/Player.log`
- 開発用外部PWAD置き場（非デプロイ・追跡可）: `dev_samples/external_mods/`
- パス/資材情報は定期検証で十分とし、リリース前チェック時に存在確認する。

## 設計・仕様

- 設計: [docs/plans/2026-02-11-ars-moriendi-design.md](./docs/plans/2026-02-11-ars-moriendi-design.md)
- 仕様: [docs/specs/servant-system.md](./docs/specs/servant-system.md), [docs/specs/enhancement-system.md](./docs/specs/enhancement-system.md), [docs/UI_DESIGN.md](./docs/UI_DESIGN.md)
