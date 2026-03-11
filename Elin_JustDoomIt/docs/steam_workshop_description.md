### Just Doom It

『初代DOOM』のアーケード筐体をフォーチュンベル受付横に追加します。

各マップ開始時に `LOW / MID / HIGH` の RATE を選択。
`LOW` は安定、`MID` は標準、`HIGH` は長マップ無被弾で大きく狙う夢枠です。
撃破報酬は未確定プールに貯まり、被弾で一部ロスト + 連続ボーナスリセット、クリア時だけ `CASH OUT`。
シークレット発見でも未確定プールへ固定 `+500` が加算されます。
死亡時はそのマップの未確定プールを精算し、`ESC` 退出では失います。
ちょっと物騒なパチスロです。
あなたはカジノに通ってプレイしてもいいし、盗んでもいいし、牧場や寝室に置いてもいい。

JP / EN / CN 対応

#### 操作方法

- 移動: `W / A / S / D`（矢印キーでも可）
- エイム / 射撃: `マウス移動` / `左クリック`
- 使う/開ける: `Space` / `E`
- ダッシュ: `Shift`
- 武器切替: `1 - 7` / `マウスホイール`
- 終了: `ESC`

#### よくある質問

Q. 安全にアンインストールできますか？
A. はい。セーブデータへの影響はカスタムアイテムのみです。Modを抜くとアーケード筐体は錬金灰になります。

Q. なぜ？
A. ブラックチャックでのリロード疲れに効く

Q. スローライフに戻れる？
A. BFGを撃った後に鍬を持つと、少しだけ物足りなくなります。

#### 構成要素

- C#製DOOM互換エンジン: [DoomNetFrameworkEngine](https://github.com/mahach666/DoomNetFrameworkEngine)
- ゲームデータ: [FreeDoom](https://freedoom.github.io/) の `freedoom1.wad`
- 筐体の追加は、CWL（Custom Whatever Loader）経由のカスタムアイテム

#### ライセンスについて

- DoomNetFrameworkEngine: MIT License.
- FreeDoom (`freedoom1.wad`): BSD 3-Clause License.

配布物には `LICENSES/` フォルダを同梱し、以下を収録しています。

- LICENSES/FreeDoom-BSD-3-Clause.txt
- LICENSES/FreeDoom-CREDITS.txt
- DoomNetFrameworkEngine-MIT.txt

上記はいずれもライセンス条件の範囲で再配布可能なリソースです。
素晴らしい資産を公開してくださっている各配布元・開発者の皆さまに深く感謝申し上げます。
詳細は同梱のREADME.mdを参照してください。
