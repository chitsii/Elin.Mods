# Ars Moriendi UI Design Principles

## Visual Theme: "Forbidden Grimoire"

古い革装丁の魔導書を開いている感覚を再現する。冷たいテック感を排し、温かみのある暗色で統一。

## Color Palette

### Surfaces (背景・ボタン)

| Name | Usage | RGBA |
|------|-------|------|
| Obsidian | ウィンドウ背景 | `(0.07, 0.06, 0.05, 0.94)` |
| Charred Vellum | スクロール領域 | `(0.10, 0.08, 0.06, 0.70)` |
| Worn Leather | ボタン通常 | `(0.18, 0.13, 0.08, 0.90)` |
| Warm Leather | ボタンhover | `(0.25, 0.18, 0.10, 0.92)` |
| Pressed Hide | ボタンactive | `(0.12, 0.09, 0.05, 0.95)` |
| Blood Seal | 選択状態 | `(0.30, 0.10, 0.08, 0.92)` |
| Ashen Tab | 非アクティブタブ | `(0.13, 0.10, 0.07, 0.90)` |
| Ember Tab | アクティブタブ | `(0.28, 0.12, 0.08, 0.95)` |
| Confirm Overlay | 確認ダイアログ背景 | `(0.03, 0.02, 0.02, 0.85)` |

### Text (文字色)

| Name | Usage | RGB |
|------|-------|-----|
| Parchment | 本文 | `(0.82, 0.76, 0.65)` |
| Faded Ink | 説明文 | `(0.60, 0.55, 0.47)` |
| Gilded Gold | メインタイトル | `(0.85, 0.72, 0.35)` |
| Tarnished Gold | セクションヘッダ | `(0.75, 0.62, 0.30)` |
| Bone White | 非アクティブタブ文字 | `(0.65, 0.60, 0.52)` |
| Spectral Green | 習得済/生存/ポジティブ | `(0.40, 0.78, 0.35)` |
| Blood Crimson | 未習得/死亡/ネガティブ | `(0.82, 0.22, 0.18)` |
| Amber Warning | マナ警告 | `(0.90, 0.72, 0.20)` |
| Ghostly Violet | フレーバーテキスト | `(0.62, 0.55, 0.70)` |

### Utility

| Name | Usage | RGBA |
|------|-------|------|
| Divider Gold | 区切り線 | `(0.55, 0.45, 0.20, 0.50)` |
| Bar Background | ストックバー背景 | `(0.15, 0.12, 0.08, 0.80)` |

### 色の使い分けルール

- **ポジティブ/成功**: Spectral Green のみ
- **ネガティブ/不足/エラー**: Blood Crimson のみ
- **注意/コスト**: Amber Warning のみ
- **上記以外の通常テキスト**: Parchment
- 新しい色を追加する前に既存パレットで表現できないか検討すること

## Typography

### Font

**Yu Mincho (游明朝)** — 明朝体のセリフが魔導書に適合。

フォールバックチェーン: `"Yu Mincho"` → `"MS PMincho"` → `"Meiryo"`

### Size Hierarchy

| Size | Usage |
|------|-------|
| 20px | タイトル (`✦ Ars Moriendi ✦`) |
| 16px | セクションヘッダ (`◆ 呪文一覧`) |
| 15px | タブ名・確認ダイアログメッセージ |
| 14px | 本文・ボタン |
| 13px | 説明文・フレーバーテキスト |

新しいサイズを追加しない。既存の階層で収まらない場合は fontStyle (Bold/Italic) で差別化する。

## Decorative Elements

### Title
`✦ Ars Moriendi ✦` (U+2726 Four Pointed Star)

### Tab Names (章立てスタイル)
- JP: `─ 第壱章 知識 ─` / `─ 第弐章 儀式 ─` / `─ 第参章 従者 ─`
- EN: `─ I · Knowledge ─` / `─ II · Ritual ─` / `─ III · Servants ─`

新しいタブを追加する場合: `─ 第肆章 XXXX ─` / `─ IV · XXXX ─`

### Section Headers
`◆` (U+25C6 Black Diamond) を接頭辞として使用。

### Status Icons
| Icon | Meaning |
|------|---------|
| `✓` (U+2713) + Spectral Green | 習得済・成功 |
| `✗` (U+2717) + Blood Crimson | 未習得・失敗 |
| `●` (U+25CF) + Spectral Green | 生存 |
| `○` (U+25CB) + Blood Crimson | 死亡 |

### Dividers
`DrawDivider()` — Divider Gold (金色半透明) の 1px 水平線。セクション間に配置。

### Stock Bar
`DrawStockBar(int stock)` — MaxStockDisplay=50 基準の水平バー。

色分け:
- ≥50%: Spectral Green
- 20-49%: Amber Warning
- <20%: Blood Crimson

## Layout Rules

### Window
- Size: **720 x 580**
- 画面中央に配置
- padding: 14px horizontal, 10px vertical
- タイトルバー (30px) でドラッグ可能

### Scroll Area
- Charred Vellum 背景
- padding: 8px horizontal, 6px vertical

### Spacing Convention
- セクション間: `DrawDivider()` + `GUILayout.Space(6-8)`
- 要素間: `GUILayout.Space(4)`
- ヘッダ後: `GUILayout.Space(4)`

## UX Patterns

### Confirmation Dialog
ゲームUIに遷移せず、IMGUI ウィンドウ内にモーダルオーバーレイを描画する。

```
State:
  _pendingConfirm: bool
  _confirmMessage: string
  _confirmAction: Action?

Flow:
  1. アクションボタン押下 → ShowConfirmDialog(message, action)
  2. DrawWindow末尾で半透明オーバーレイ + 中央ダイアログ描画
  3. 確認中: タブ切替・タブ内容は GUI.enabled = false
  4. 実行ボタン → action実行 + reset
  5. キャンセル / Escape → reset のみ
  6. ウィンドウ外クリックでは閉じない (確認中のみ)
```

新しい確認が必要な操作には `ShowConfirmDialog()` を使う。`CreateContextMenuInteraction` は使用禁止。

### Multi-Select (チェックボックス式)
- `HashSet<Thing>` で選択状態を管理
- `[✓]` / `[  ]` のトグルボタンで個別選択
- 下部にサマリ (選択数 + 合計値) + 実行ボタン
- 実行前に確認ダイアログを挟む

### Text Filter
- `GUILayout.TextField` + クリアボタン (`✗`)
- `IndexOf(filter, OrdinalIgnoreCase)` で部分一致
- フィルター適用時は件数表示: `"3 / 25件"`

### Quantity Slider (数量スライダー)
- `GUILayout.HorizontalSlider` で 0 ~ 所持数の範囲
- `Mathf.RoundToInt` で整数化
- 左にラベル名、右に所持数と合計値を表示
- 用途: 儀式タブの魂選択、改造タブの魂注入数量

### Selection Summary
重要な操作 (儀式等) の前に、選択内容を下部にまとめて表示する。
- 選択済: Parchment で名前表示
- 未選択: Blood Crimson + Italic で "未選択" 表示

### Sub-Panel (サブパネル)
リスト内の要素を選択すると、リスト下部に展開される詳細パネル。
- トグル動作: 選択中の要素を再クリックで閉じる
- Escape キーでも閉じる (3段階: 確認→サブパネル→UI全体)
- 用途: 従者タブの改造サブパネル

### Risk Display (リスク表示)
パネルヘッダーに常時表示する数値リスク指標。
- 安全: Spectral Green
- 注意: Amber Warning
- 危険: Blood Crimson + `⚠` プレフィックス
- 用途: 暴走率表示

## Implementation Notes

### MakeTex Pattern
1px×1px の単色テクスチャを `MakeTex()` で生成し、GUIStyle の background に設定。
`hideFlags = HideAndDontSave` で GC から保護。

### Lazy Texture Initialization
頻繁に使うテクスチャ (divider, bar background) は static field にキャッシュし、null チェックで遅延生成。
`DrawConfirmOverlay` 内のボーダーなど一時的なものは都度生成で問題ない。

### Style Initialization Guard
`InitStyles()` は `_stylesInitialized && _font != null` でガード。
`GUI.skin.textField` を参照するスタイル (filter等) は OnGUI 内でのみ初期化可能。

### Bilingual Support
`L(jp, en)` ヘルパーで `Lang.isJP` に基づいて切り替え。
定型メッセージは `LangHelper.Get(key)` を使用。
