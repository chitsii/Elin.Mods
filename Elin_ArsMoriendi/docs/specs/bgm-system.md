# BGM System Specification

## Overview

全17ドラマシーンにシーンの雰囲気に合ったBGMを冒頭から再生する。
自前3曲を基本とし、SukutsuArena のBGMが利用可能ならそちらを優先する（なければ自前にフォールバック）。

## BGM Assets

### 自前BGM（常に利用可能）

| 定数名 | パス | 用途 | 雰囲気 |
|---|---|---|---|
| `BATTLE` | `BGM/AshAndHolyLance` | 戦闘曲 | 激しい、緊迫 |
| `REQUIEM` | `BGM/TheFadingSignature` | 鎮魂/別れ | 静かな悲哀、余韻 |
| `REVELATION` | `BGM/ManuscriptByCandlelight` | 啓示/内省 | 神秘的、思索的 |

### SukutsuArena BGM（インストール時のみ）

| 定数名 | パス | 用途 | フォールバック先 |
|---|---|---|---|
| `OMINOUS` | `BGM/Ominous_Suspense_01` | 緊張感 | REVELATION |
| `SORROW` | `BGM/Emotional_Sorrow` | 悲哀 | REQUIEM |
| `RITUAL` | `BGM/Mystical_Ritual` | 儀式的 | REVELATION |

## Implementation

### 定数定義

`tools/drama/data.py` の `BGM` クラスで全定数を管理。

### 再生メソッド

`DramaBuilder` に2つのBGM再生メソッドがある:

- **`play_bgm(bgm_id)`** — 自前BGM用。見つからなければ警告ログを出力。
- **`play_bgm_with_fallback(primary, fallback)`** — SukutsuArena BGM用。primary が見つからなければ fallback を再生。ログなし（フォールバックは想定内の動作）。

どちらも内部では `eval` アクションで C# コードを生成し、`SoundManager.current.GetData()` → `BGMData` チェック → `SoundManager.current.PlayBGM()` を実行する。`LayerDrama.haltPlaylist = true` と `LayerDrama.maxBGMVolume = true` を設定し、ドラマ中のBGM制御を有効にする。

## Drama BGM Mapping

### メインストーリー (S1-S10)

| # | Drama | 開始BGM | 途中切替 | 切替タイミング |
|---|---|---|---|---|
| S1 | `ars_first_soul` | REVELATION | -- | -- |
| S2 | `ars_tome_awakening` | REVELATION | -- | -- |
| S3 | `ars_karen_encounter` | OMINOUS->REVELATION | BATTLE | `step(merge)` |
| S4 | `ars_karen_retreat` | SORROW->REQUIEM | -- | -- |
| S5 | `ars_cinder_records` | REVELATION | -- | -- |
| S6 | `ars_scout_encounter` | REVELATION | -- | -- |
| S7 | `ars_stigmata` | RITUAL->REVELATION | -- | -- |
| S8 | `ars_erenos_appear` | REVELATION | BATTLE | `step(shadow_emerge)` |
| S9 | `ars_erenos_defeat` | REQUIEM | -- | -- |
| S10 | `ars_apotheosis` | REQUIEM | -- | -- |

### サブストーリー A (従者関連)

| # | Drama | 開始BGM | 途中切替 | 切替タイミング |
|---|---|---|---|---|
| A-1 | `ars_first_servant` | REVELATION | -- | -- |
| A-2 | `ars_servant_rampage` | BATTLE | -- | -- |
| A-3 | `ars_servant_lost` | SORROW->REQUIEM | -- | -- |
| A-4 | `ars_dormant_flavor` | REVELATION | -- | -- |

### サブストーリー B (ポストゲーム)

| # | Drama | 開始BGM | 途中切替 | 切替タイミング |
|---|---|---|---|---|
| B-1 | `ars_successor_notes` | REVELATION | -- | -- |
| B-2 | `ars_karen_shadow` | SORROW->REQUIEM | -- | -- |
| B-3 | `ars_seventh_sign` | REVELATION | -- | -- |

凡例:
- `OMINOUS->REVELATION` = `play_bgm_with_fallback(OMINOUS, REVELATION)`
- 単独名 = `play_bgm(BGM_NAME)` （自前BGM、フォールバック不要）

## Design Rationale

### BGM選定方針

- **REVELATION** がデフォルト。禁書との対話、知識の発見、内省的な場面に使用。全17ドラマ中10シーンで使用。
- **REQUIEM** は別れや喪失の場面。エレノス撃破後、昇華の儀式、従者の喪失で使用。
- **BATTLE** は戦闘直前のシーンのみ。カレン戦、エレノスの影戦、暴走イベント。
- **SORROW/OMINOUS/RITUAL** は雰囲気の差別化用。SukutsuArena 未導入環境でも自前BGMで同系統の雰囲気を再現。

### フォールバック設計

SukutsuArena BGM は別Modのアセットのため、インストールされていない環境では `SoundManager.current.GetData()` が `null` を返す。フォールバック先は雰囲気が近い自前BGMを選定:
- OMINOUS (緊張) -> REVELATION (神秘) : 緊張感は弱まるが神秘的な雰囲気は維持
- SORROW (悲哀) -> REQUIEM (鎮魂) : 悲哀の方向性は一致
- RITUAL (儀式) -> REVELATION (啓示) : 超自然的な雰囲気は共通

### 途中切替

戦闘開始直前にBATTLEに切り替えるのは2シーン（S3, S8）のみ。切替は `step()` の直後に配置し、プレイヤーが戦闘BGMを認識してから実際の戦闘に入るようにしている。
