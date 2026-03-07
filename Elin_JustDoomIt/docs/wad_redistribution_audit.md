# WAD再配布 監査リスト

更新日: 2026-03-07  
対象: `Elin_JustDoomIt` 外部PWAD監査メモ  
注記: 法的助言ではなく、配布同梱テキストの監査メモ。

## 判定基準
- `同梱可`: 再配布条件が明示され、実運用で満たせる。
- `条件付き可`: 再配布可能だが、厳守条件あり（テキスト同梱、無改変など）。
- `要確認`: 同梱テキストだけでは条件が不明確。手動導入推奨。

## 監査表
| WAD/パック | 判定 | 根拠（一次ソース） | 同梱時の必須対応 |
|---|---|---|---|
| FreeDoom Phase 1/2 | 同梱可 | `COPYING.adoc` (BSD 3-Clause) | ライセンス文面と著作権表示を同梱 |
| FreeDM | 同梱可 | Freedoomプロジェクト配布物（BSD 3-Clause） | 上記同様 |
| Alien Vendetta (AV) | 条件付き可 | `AV.TXT` の `LEGAL STUFF` / `You may distribute ... with no modifications ... include this textfile intact.` | `AV.WAD` 無改変、`AV.TXT` 同梱 |
| Memento Mori (MM) | 条件付き可 | `MM.TXT` の `Copyright / Permissions` / `You MAY distribute ... include this file ... no modifications.` | 無改変、`MM.TXT` 同梱 |
| No End In Sight (NEIS) | 条件付き可 | `NEIS.txt` の `Copyright / Permissions` / `You MAY distribute ... include this text file ... no modifications.` | 無改変、`NEIS.txt` 同梱 |
| Memento Mori II (MM2) | 要確認 | `MM2.TXT` で明確な再配布条項を確認できず | Mod同梱は避け、ユーザー手動導入 |
| Back to Saturn X (BTSX) | 要確認 | 本監査時点で一次配布テキスト未取得 | 同梱前に配布同梱文書を再監査 |

## 実務ルール（このMod向け）
1. 現行方針では `FreeDoom/FreeDM` 以外は同梱しない。
2. 第三者 PWAD はユーザー手動導入のみとし、案内テキストと配布元 URL のみ提供する。
3. 将来同梱を再検討する場合のみ、この監査メモと一次ソースを再確認する。
4. `要確認` は引き続き非同梱とする。

## 参照URL
- Freedoom license: https://raw.githubusercontent.com/freedoom/freedoom/master/COPYING.adoc
- Alien Vendetta package: https://doom2.net/av/av.zip
- Memento Mori package: https://ftp.fu-berlin.de/pc/games/idgames/themes/mm/mm_allup.zip
- No End In Sight package: https://www.dukeworld.com/idgames/levels/doom/Ports/megawads/neis.zip
- Memento Mori II package: https://ftp.fu-berlin.de/pc/games/idgames/themes/mm/mm2.zip
