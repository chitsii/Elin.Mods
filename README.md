# Elin.Mods

[Elin](https://store.steampowered.com/app/2135150/Elin/) 向け Mod のモノレポジトリです。

## 公開中の Mod

[SteamWorkshop](https://steamcommunity.com/profiles/76561198101671376/myworkshopfiles/?appid=2135150)

| ディレクトリ | Mod名 | 概要 |
|---|---|---|
| `Elin_ArsMoriendi` | Ars Moriendi | 死霊術とクエストライン追加。魂の捕獲、禁術の習得、アンデッド従者の使役 |
| `Elin_SukutsuArena` | 狭間の闘技場 | レシマス付近に闘技場とクエストライン追加。エンドコンテンツ向け難易度 |
| `Elin_LogRefined` | Log Refined | ダメージ・回復・バフ・デバフのログ強化 |
| `Elin_JustDoomIt` | Just Doom It | カジノのアーケード筐体から DOOM 互換プレイを起動。FreeDoom 同梱、外部 PWAD 対応 |
| `Elin_ItemRelocator` | Item Relocator | フィルタプリセット付きアイテム検索・一括移動ツール |
| `Elin_AutoOfferingAlter` | SleepOffer - 睡眠奉納 | 睡眠中に信仰ボックスから自動奉納。配分最適化付き |


## 未リリース / 今のところ個人用

| ディレクトリ | Mod名 | 概要 |
|---|---|---|
| `Elin_AutoEatSleep` | AutoEatSleep | 空腹・眠気の閾値に応じた自動食事・睡眠 |
| `Elin_RapidFireMagic` | Rapid Fire Magic | ホットバー魔法ボタン長押しで連射詠唱 |
| `Elin_BottleNeckFinder` | BottleNeckFinder | Mod環境のパフォーマンスプロファイラー |
| `Elin_JumpAndBop` | Dynamic Jump & Bop | 待機時のリズミカルなバウンスアニメーション |
| `Elin_NiComment` | NiComment | AIによるニコニコ風コメント表示 |

## 開発中

| ディレクトリ | Mod名 | 概要 |
|---|---|---|
| `Elin_QuestGoldenSoup` | ? | 推理クエスト（現場検証 → 聞き込み → 捜査 → 対決 → エンディング分岐） |


## テンプレート

| ディレクトリ | 用途 |
|---|---|
| `Elin_QuestMod` | クエストModテンプレート。ステートマシン進行、ドラマ生成パイプライン、ランタイムテスト付き |
| `Elin_ModTemplate` | 汎用 Mod テンプレート。最小構成のスケルトン |

## 共有ツール

| ディレクトリ | 用途 |
|---|---|
| `tools/elin_channel_tracker` | stable/nightly チャンネル間の API 互換性チェッカー（シグネチャ収集・差分検出・レポート生成） |
| `runtime-test-v2` | 共有ランタイムテストフレームワーク（smoke / drama テスト実行、結果集約、Player.log 差分追跡） |

## 開発環境

- Windows + Steam版Elin
- .NET SDK（`dotnet build` が実行できること）
- Python 3.11+（drama 生成、elin_channel_tracker）
- 各 Mod のビルド: 各ディレクトリ内の `build.bat` を実行

## ライセンス

[GPL-3.0](./LICENSE)
