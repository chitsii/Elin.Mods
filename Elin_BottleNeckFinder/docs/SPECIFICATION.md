# BottleNeckFinder 仕様書

## 概要

BottleNeckFinder は Elin 用のパフォーマンス分析Modです。
他のModがゲームのフレームレートに与える影響をリアルタイムで計測し、
ボトルネックとなっているModやメソッドを特定します。

## 機能一覧

### 1. FPSカウンター（常時）

- 0.5秒間隔でFPSとフレーム時間（ms）を計算
- ミニオーバーレイ（画面左上）に常時表示可能
- 閾値以下で赤色表示（デフォルト: 30FPS）
- プロファイラOFF時でも独立して動作

### 2. Harmonyパッチプロファイラ（手動起動）

他Modが適用したHarmonyパッチ（Prefix/Postfix）のパフォーマンス影響を計測します。

#### 計測方式

各パッチ済みメソッドに対して、2層のタイミングパッチを適用します:

```
TimingPrefix (Priority.First)     ← 全体計測開始
  [他Modの Prefix パッチ群]
  BaseTimingPrefix (Priority.Last) ← ベース計測開始
    [メソッド本体]
  BaseTimingPostfix (Priority.First) ← ベース計測終了
  [他Modの Postfix パッチ群]
TimingPostfix (Priority.Last)      ← 全体計測終了
```

- **Total時間** = メソッド全体（ベース + 全Modパッチ）の実行時間
- **Base時間** = メソッド本体のみ（他Modのパッチを除く）の実行時間
- **Mod Overhead** = Total - Base = 他Modのパッチが追加したコスト

#### 集計方式

- **累計平均（avg）**: プロファイル開始からの総計時間 / 総フレーム数
  - 低頻度で発火する重い処理（`OnAdvanceHour` 等）も平均に反映される
- **ピーク値（peak）**: プロファイル開始以降の単一フレーム最大負荷
  - 一瞬のカクつき（スパイク）を検出するために使用
  - Mod単位・メソッド単位の両方で追跡
- `[P]` でプロファイラをOFFにすると累計データ・ピーク値ともにクリアされる

### 3. Update/LateUpdate/FixedUpdate プロファイラ（手動起動）

各BepInExプラグインの `Update()`, `LateUpdate()`, `FixedUpdate()` メソッドの実行時間を計測します。
Harmonyパッチを持たないModでも、毎フレームの処理コストを把握できます。

### 4. エラーモニター（常時）

- Unityのログ出力を監視し、Error/Exception を捕捉
- スタックトレースからMod名を自動解決
- Harmonyパッチ適用失敗を専用検出（`[PATCH]` タグ付き）

### 5. オーバーレイ表示

ModOptionsの「オーバーレイ表示」をONにすると表示されます。

| 状態 | 内容 |
|---|---|
| プロファイラ停止中 | FPS / フレーム時間 + パッチ状況 + Start Profiler ボタン |
| プロファイラ稼働中 | 上記 + Modランキング + メソッド内訳 + エラー一覧 + Export Report ボタン |

- FPSの色はゲームのターゲットFPS（`Application.targetFrameRate` / VSync）から自動判定
  - 緑: 90%以上、黄: 50%以上、赤: 50%未満
- Modランキングはバーグラフ付きで負荷を視覚化
- メソッド内訳は上位3件を表示（ワードラップ対応）
- パッチオーナー（どのModが適用したか）を表示

### 6. レポートエクスポート

Markdownフォーマットのレポートを `Player.log` と同じディレクトリに出力します。

レポート構成:

| セクション | 内容 |
|---|---|
| **Environment** | ゲームバージョン、Unity バージョン、プラットフォーム、ロード済みMod数 |
| **Profiling Session** | 計測時間、フレーム数、サンプル間隔、計測対象メソッド数、Transpilerスキップ数 |
| **Mod Performance** | avg ms/frame ソートの全Modランキング（メソッド内訳付き） |
| **Spike Ranking** | peak ms ソートのModランキング（Peak/Avg比率付き） |
| **High-Frequency Methods** | calls/frame が多いメソッド上位10件（1回あたりコスト表示） |
| **Skipped Methods (Transpiler)** | Transpilerパッチによりスキップされたメソッド一覧 |
| **Multi-Mod Patched Methods** | 2つ以上のModがパッチしているメソッドの競合リスク表示 |
| **Recent Errors** | エラー履歴 |
| **Patch Registry** | どのModがどのメソッドをパッチしているか |

#### Spike Ranking

avgソートでは見えないスパイク性のボトルネックを発見するためのビューです。
Peak/Avg比率が高いModは、普段は軽いが特定条件で大きなラグスパイクを発生させます。

#### High-Frequency Methods

全Mod横断で calls/frame が多いメソッドの上位10件を表示します。
「1回あたりコスト × 呼び出し回数 = 合計コスト」の観点で、
呼び出し回数の多さがボトルネックになっているケースを発見できます。

#### Skipped Methods (Transpiler)

Transpilerパッチが適用されたメソッドは計測対象外です（安全性のため）。
このセクションではスキップされたメソッドを一覧表示し、
レポートの盲点を読者に明示します。

## 操作方法

| 操作 | 動作 | 条件 |
|---|---|---|
| ModOptions: オーバーレイ表示 | オーバーレイの表示/非表示 | Mod有効時 |
| `Start/Stop Profiler` ボタン | プロファイラのON/OFF | オーバーレイ表示中 |
| `Export Report` ボタン | レポートファイル出力 | プロファイラ稼働中 |

- プロファイラON: Harmonyプロファイラ + Updateプロファイラが同時に起動
- プロファイラOFF: 全計測データがクリアされる

## 設定項目

| 設定 | デフォルト | 説明 |
|---|---|---|
| EnableMod | true | Mod全体の有効/無効 |
| ShowOverlay | false | オーバーレイの表示 |
| TopModCount | 5 | ランキングに表示するMod数（3-20） |
| MaxErrorHistory | 5 | 表示するエラー件数（1-20） |
| SampleInterval | 1 | N フレームごとに計測（1-10） |

## 計測の限界

### Transpilerパッチは計測対象外

Harmonyの **Transpiler** パッチ（メソッドのILコードを直接書き換えるパッチ）が適用されたメソッドは、
プロファイリング対象から**スキップ**されます。

理由:
1. **安全性**: Transpiler済みメソッドに追加のHarmonyパッチを適用すると、
   Harmonyがパッチチェーン全体のILを再生成する際に既存のIL書き換えが破損する可能性がある
   （ゲーム内テキストの変数評価が壊れる等の実害を確認済み）
2. **計測の意味**: 仮に安全に計測できたとしても、Transpilerによる変更は
   メソッド本体のILに組み込まれるため「ベース時間」に含まれてしまい、
   Mod Overhead として分離できない

| パッチ種別 | Total時間 | Mod Overhead分離 | 安全な計測 |
|---|---|---|---|
| Prefix / Postfix | 可能 | 可能 | 可能 |
| Transpiler | 理論上可能 | **不可能** | **不可能** |

### UnpatchSelf の非使用

プロファイラ停止時に `Harmony.UnpatchSelf()` を呼ぶと、MonoMod/CWL のフックチェーンが
破損する（`NotSupportedException`）ため、パッチは適用したまま `_enabled` フラグで
無効化しています。パッチ自体は残るが、`if (!_enabled) return;` により即座にリターンするため
パフォーマンス影響は最小限です。

### パッチ収集のタイミング

`PatchRegistry.Build()` は起動時に1回だけ実行されます。
起動後に動的に追加されたHarmonyパッチは検出されません。

### エラーのMod帰属

エラーのMod帰属はスタックトレースのアセンブリ名から推定しています。
ゲーム本体のメソッド内で発生したエラーや、アセンブリ名がBepInExプラグインと
一致しないModのエラーは「Unknown」と表示されます。

## アーキテクチャ

```
Plugin.cs           … エントリポイント、ライフサイクル管理
├── ModConfig.cs        … BepInEx設定の定義
├── ModOptionsBridge.cs … ModOptions UI連携
├── PatchRegistry.cs    … 起動時にHarmonyパッチ情報を収集
├── HarmonyProfiler.cs  … Prefix/Postfixパッチの2層タイミング計測
├── UpdateProfiler.cs   … Update/LateUpdate/FixedUpdate計測
├── ProfilingData.cs    … 累計データの蓄積とランキング生成
├── ErrorMonitor.cs     … ログ監視によるエラー捕捉
├── OverlayRenderer.cs  … IMGUI によるオーバーレイ描画
└── ProfileExporter.cs  … Markdownレポート出力
```
