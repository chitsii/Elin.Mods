# Elin FPS View - 3D単独プレイ化ロードマップ

## 目的

現行の実装計画を完了したあと、Elin の通常 2D タイルビューに頼らず、3D ビューだけを見ながらゲーム内プレイを継続できる状態までの残ギャップを整理する。

この文書でいう「3D 単独プレイ」は次を満たす状態を指す。

- 探索、移動、会話、拾う、開ける、攻撃、簡単な戦闘を 3D ビューだけで完結できる
- ワールド上の対象選択を hidden 2D cursor に依存しない
- 最低限の状況把握が 3D 側 HUD だけで成立する
- 2D ビューへ戻らないと詰む操作が常用範囲に残っていない

## セルフレビュー結論

現行の実装計画をすべて終えても、到達できるのは「3D 表示つきプレイ」であって「3D 単独プレイ」ではない。

最大の未解決点は描画ではなく、ワールド操作の依存先が依然として 2D マウス座標だから。

- `Scene.HitPoint` は 2D タイルマップ上のマウス位置から更新される
- `PointTarget` / `mouseTarget` は `Scene.HitPoint` を元に対象を決める
- `ActPlan` は `mouseTarget` を元に左クリック・右クリック行動を組み立てる
- `LayerInteraction` / `UIInspector` / `WidgetMouseover` も `Scene.HitPoint` と `mouseTarget` 前提

つまり、現行計画完了後の 3D ビューは「見ること」はできても、「何を対象に何をするか」の入力系がまだ 2D 側に依存している。

## 現行計画完了後に残る主要ギャップ

### 1. ワールドターゲット供給ギャップ

3D 中央視線やレティクルから、Elin のワールド操作系へ target を渡す経路がない。

残る不足:

- 3D 視線先からグリッドセルを解決する仕組み
- そのセルを `Scene.HitPoint` として反映する仕組み
- `mouseTarget.Update()` と `ActPlan.Update()` が自然に働く同期タイミング
- UI 上にポインタがある時の無効化ルール

これがないと次が全部成立しない。

- 会話
- 拾う
- 開ける
- 階段/出入口使用
- 左右クリック文脈行動
- インスペクト

### 2. 3D ネイティブ操作ギャップ

現行計画は「表示を壊さない」ことを優先しているため、移動や視線操作は本格的には 3D 化していない。

残る不足:

- free look と cursor lock の両立
- カメラ向き基準での 8 方向移動変換
- 近接行動を前方対象へ寄せる入力設計
- マウスルック中でも UI を触れるモード切替
- 自動移動と手動歩行の使い分け

これがないと、3D だけを見て遊ぶというより「3D を被せた 2D 操作」のままになる。

### 3. 3D 側 HUD / フィードバック不足

2D プレイでは、対象情報や行動候補は `WidgetMouseover`、`UIInspector`、HUD テキスト、ハイライトで補われている。

残る不足:

- 中央レティクル
- 現在ターゲット名
- 左右クリックで実行される行動表示
- 複数ターゲット時の切替表示
- インタラクト可能範囲のわかりやすい提示
- ダメージ方向や被弾方向の把握補助

描画だけ 3D でも、この情報がないと戦闘と探索の判断が難しい。

### 4. 地形忠実度ギャップ

現行計画でも高さや床再現は後段だが、3D 単独プレイには「誤認しない地形」が必要になる。

残る不足:

- `cell.height` の反映
- ramp / stairs / ladder / half-block
- low wall / pillar の視認性
- bridge / bridgeHeight
- 水辺、shore、autotile の視認性
- 室内/屋根/遮蔽時の見え方

この部分が弱いと、通れるか・撃てるか・登れるかが 3D 側で判断できない。

### 5. 行動モード対応ギャップ

通常 Adv 以外のモードは、2D の tile selector 前提がさらに強い。

残る不足:

- `Inspect`
- `Select`
- `Build`
- 範囲指定
- 高さ変更付き配置
- AoE 指定
- 採掘や農業などの位置依存行動

特に `Build` は `start` / 複数選択 / 高度 / bridge 判定に依存しているため別系統での 3D 化が必要。

### 6. 戦闘・ターゲティング高度化ギャップ

3D 単独プレイでは近接戦闘だけでなく、狙いと判定の整合が必要になる。

残る不足:

- 視線先ターゲットと Elin の対象選択の一致
- 遠隔攻撃 / 投擲 / 魔法の照準
- 視界外/遮蔽/高低差込みの誤認防止
- 敵が複数重なった時の cycle target
- 味方/敵/中立の見分けやすさ

### 7. ゾーン種別ギャップ

現行計画の主戦場はローカルマップだが、Elin には region/world 側もある。

残る不足:

- `IsRegion` 時の 3D 表示方針
- local zone への出入り
- border exit
- travel / embark / world 選択

ここを決めないと「ゲーム全体を 3D 単独で遊べる」とは言えない。

## ロードマップ

### Milestone 1: Adv モードを 3D 単独で成立させる

目標:

- ローカルマップでの探索、会話、拾う、開ける、近接戦闘を 3D ビューだけで行える

実装項目:

1. 3D レティクルヒットシステム
2. 3D 視線先セルを `Scene.HitPoint` に反映するブリッジ
3. `mouseTarget` / `ActPlan` を 3D 中央視線で更新
4. 3D ターゲット HUD
5. 近距離用の interact キー
6. 複数ターゲット cycle

出口条件:

- 町やダンジョンで NPC と話せる
- 床アイテムを拾える
- ドア/階段/出入口を使える
- 敵へ近接攻撃できる
- hidden 2D cursor を見なくても基本探索が可能

### Milestone 2: 3D ネイティブ移動とカメラ

目標:

- 3D を見るだけでなく、3D に合った操作で移動と視線制御ができる

実装項目:

1. free look / UI focus のモード切替
2. カメラ向き基準の移動変換
3. manual move と auto move の棲み分け
4. 壁際、角、近接対象での move/act 優先ルール
5. 被操作感を壊さない camera smoothing

出口条件:

- キーボード移動だけで狭い屋内を移動できる
- 3D 向きに応じて移動の前後左右が自然
- UI 操作時に誤って視線が暴れない

### Milestone 3: 3D で誤認しない地形・遮蔽

目標:

- 通行可否、遮蔽、上下差を 3D 側で判断できる

実装項目:

1. `cell.height` の反映
2. ramp / stairs / ladder
3. half-block / low wall / pillar
4. bridge / bridgeHeight
5. 室内・屋根・壁の occlusion ルール
6. 水辺・shore・雪面の最低限再現

出口条件:

- プレイヤーが「通れると思ったのに通れない」誤認が大幅に減る
- 高低差や階段が 3D で読める
- 室内戦や屋外水辺で進行判断ができる

### Milestone 4: 戦闘と能動行動の 3D 完結

目標:

- 戦闘と主なアクションを 3D 側だけで継続できる

実装項目:

1. 遠隔攻撃/投擲/魔法のターゲティング
2. 照準先と実際の `ActPlan` の整合
3. hostile/friendly の視認補助
4. 被弾/射線/有効距離の HUD
5. wheel やキーによる target cycle

出口条件:

- 近接だけでなく簡単な遠隔戦闘が 3D だけで可能
- 敵が重なっても対象切替できる
- 会話相手と攻撃対象を取り違えにくい

### Milestone 5: 選択系モードの 3D 化

目標:

- 2D tile selector を要求するモードを、順次 3D に移す

実装項目:

1. `Inspect` の 3D 対象選択
2. `Select` の 3D コンテキスト選択
3. 範囲指定の 3D gizmo
4. `Build` の single target 配置
5. `Build` の multi-tile / altitude / bridge 指定

出口条件:

- インスペクトや選択系 UI を 3D だけで開ける
- 単純設置や単一マス操作が 3D だけで可能

注記:

- `Build` を完全に 3D 化するのは大きな別テーマなので、単純配置と範囲指定を分けて進める。

### Milestone 6: Region / World 側の扱い決定

目標:

- 「3D 単独プレイ」の適用範囲をゲーム全体で定義し、必要箇所を埋める

分岐案:

1. ローカルマップのみ 3D 単独対応、region/world は既存 2D のまま
2. region/world も別系統の 3D 表示を実装

推奨:

- まずは案 1 で到達可能な品質を作る
- 案 2 は別ロードマップに切り出す

理由:

- `screenElona` / `elomap` 系は local map と操作モデルが違う
- 一括で 3D 化すると完了条件が大きく遅れる

出口条件:

- ユーザーに「どこまでが 3D 単独対象か」を明確に説明できる
- local map 内では 3D 単独プレイが成立する

## 推奨する優先順位

優先度順に並べると次の通り。

1. `Scene.HitPoint` / `mouseTarget` の 3D ブリッジ
2. 3D ターゲット HUD と interact 操作
3. free look と camera-relative move
4. 地形忠実度の最低限
5. 戦闘ターゲティング
6. 選択系モード
7. Build の本格対応
8. region/world 対応

## ここまで終われば「3D 単独プレイ」と呼べる最小ライン

最低ラインは Milestone 1 から 4 まで。

この時点でできること:

- 探索
- 会話
- 拾う
- 出入り
- 近接戦闘
- 簡単な遠隔戦闘

この時点でも残るもの:

- 建築の完全対応
- 範囲指定系の完全対応
- region/world 全面対応

## 参考依存点

- `Scene.HitPoint`
- `Scene.mouseTarget`
- `PointTarget.Update(Point)`
- `ActPlan.Update(PointTarget)`
- `AM_BaseGameMode.isMouseOnMap`
- `AM_Adv.SetPressedAction()`
- `LayerInteraction.TryShow()`
- `UIInspector.InspectUnderMouse()`
- `WidgetMouseover.Refresh()`
