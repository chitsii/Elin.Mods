# DOOM IWAD/PWAD Switching Design (Elin In-Game UX)

## Goal
- Elin内の筐体操作だけで、DOOMの起動構成を切り替える。
- 切り替え対象:
  - IWAD (`freedoom1.wad` / `freedoom2.wad`)
  - PWAD (追加Mod、複数同時ロード)
- 外部ファイル依存を保ちつつ、ゲーム内では「基板/カートリッジ」として自然に表現する。

## Player UX
`justdoomit_arcade` を使用した時に、直接起動ではなく筐体メニューを表示する。

### Menu Items
1. `プレイ開始`
2. `IWADを選ぶ`
3. `Modセットを選ぶ`
4. `現在の構成を保存`
5. `閉じる`

### IWAD Selection
- `FreeDoom Phase 1 (freedoom1)`
- `FreeDoom Phase 2 (freedoom2)`

### Mod Set Selection
- `なし`
- `所持カートリッジから選択`
- 複数選択可（読み込み順はUI表示順）
- Modスロット制（最大5スロット）

## In-Game Representation
外部WADを直接見せず、以下のアイテム表現を使う。

- IWAD: `DOOM基板`
  - 例: `DOOM基板: FREEDOOM1`, `DOOM基板: FREEDOOM2`
- PWAD: `DOOM拡張カートリッジ`
  - 例: `地獄工業区カートリッジ`, `高難度アリーナカートリッジ`

### Optional Progression Hook
- 強敵ドロップに `DOOM拡張カートリッジ` を低確率で追加。
- 「討伐 -> カートリッジ入手 -> 筐体へ装着」という収集導線を作る。

## File Layout
Mod直下にWAD管理ディレクトリを置く。

```text
wad/
  iwads/
    freedoom1.wad
    freedoom2.wad
  mods/
    *.wad
  profiles/
    default.json
    last_used.json
```

## Runtime Profile Model
最小プロファイル仕様:

```json
{
  "name": "default",
  "iwadId": "freedoom1",
  "pwadIds": [],
  "lastUsedAt": "2026-03-05T00:00:00Z"
}
```

- `iwadId`: 1つ必須
- `pwadIds`: 0..N
- `lastUsedAt`: デバッグとトラブル時の追跡用
- `unlockedSlots`: 解放済みModスロット数（1..5）

## Mod Slot Unlock Rules
- 初期: 1スロット
- 最大: 5スロット
- 解放コスト:
  - 2スロット目: 10,000 チップ
  - 3スロット目: 20,000 チップ
  - 4スロット目: 30,000 チップ
  - 5スロット目: 40,000 チップ
- 一般式:
  - `nextUnlockCost = unlockedSlots * 10000`

## Launch Argument Build
DOOM起動時に引数を組み立てる:

1. `-iwad <resolved_path>`
2. PWADがある場合 `-file <pwad1> <pwad2> ...`
3. 既存フラグ (`-warp`, `-skill`, `-nomusic`) は従来ロジックを維持

## Validation and Error Handling
- 起動前に実ファイル存在チェック:
  - IWAD欠落: 起動しない + ログ通知
  - PWAD欠落: そのPWADのみ除外して起動、警告ログ
- ログは `Player.log` / BepInEx ログへ明示:
  - 使用IWAD
  - 使用PWAD一覧
  - 欠落ファイル

## Compatibility
- 既存セーブ互換: 未設定時は `freedoom1` をデフォルト選択
- 既存筐体アイテムはそのまま利用可能
- 既存のDOOM起動導線を壊さない（メニュー導入後も最短で1操作起動）

## Implementation Scope (Phased)
### Phase 1 (Minimum)
- IWAD切替 (FreeDoom1/2)
- プロファイル1本保存 (`default.json`)

### Phase 2
- PWAD複数ロード
- UIでロード順変更

### Phase 3
- カートリッジアイテム連携（所持チェック）
- 強敵ドロップ連動

## Non-Goals (Current)
- WADブラウザのフルファイルマネージャ
- UI内でのWADメタデータ編集
- ネットワーク配布/自動DL
