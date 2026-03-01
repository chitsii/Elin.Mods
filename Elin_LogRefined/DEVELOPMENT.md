# 開発メモ

## パッチ戦略（API変更への耐性）

このModは、ゲーム更新でシグネチャが変わり得るメソッドに Harmony パッチを当てています。  
破損を減らしつつ誤パッチを避けるため、**2段階解決（strict → fallback）** を採用します。

1. **Strict一致を先に試す**
- 既知シグネチャ（メソッド名・戻り値・引数型）に完全一致させる。
- 通常運用ではこの経路を使い、誤マッチを最小化する。

2. **Fallback一致を次に試す**
- strict が見つからない場合のみ、緩めの構造条件で解決する。
- fallback 使用時は必ず Warning ログを出す。

両方失敗した場合は、そのパッチを無効化して Error ログを出す。  
実装の入口: `src/PatchResolver.cs`

## ログの取り決め

リゾルバのログは次の意味を持つ:

- `strict resolved ...` (Info): 想定シグネチャで解決できた。
- `strict target not found, fallback resolved ...` (Warning): API揺れを検出し、互換モードで動作中。
- `target not found (strict/fallback)` (Error): パッチ未適用。

更新後の不整合を、起動ログだけで即座に把握できるようにする。

## 現在の対象メソッド

- `PatchCard.DamageHP`
  - Strict: 既知のフルシグネチャ。
  - Fallback: `DamageHP` かつ `void` 戻り値、かつ `long` 引数を1つ以上含む。
  - `origin` / `ele` は動的に引数位置を解決して取得。

- `PatchCard.HealHP`
  - Strict: `void HealHP()`（引数なし）。
  - Fallback: 任意の `void HealHP`。

- `PatchChara.AddCondition`
  - Strict: `Condition AddCondition(Condition, bool)`。
  - Fallback: `Condition` 引数を持つ、または戻り値が `Condition` / `bool` の `AddCondition`。

- `PatchMsgBlock.Append`
  - Strict: `void Append(string, Color)`。
  - Fallback: 第1引数が `string`、かつ後続引数に `Color` または `Color32` を含む `Append`。

- `PatchPhaseMsgThrottle.PhaseMsg`
  - Strict: `void PhaseMsg(bool)`。
  - Fallback: `void` 戻り値かつ引数1つ以上の `PhaseMsg`。

- `PatchConditionKillThrottle.Kill`
  - Strict: `void Kill(bool)`。
  - Fallback: `void` 戻り値かつ `bool` 引数を含む `Kill`。
  - `silent` 引数位置は動的に解決。

## 新規パッチ追加時の安全ルール

パッチを追加・変更する際は以下を守る:

1. 現行ゲーム版から strict シグネチャを先に定義する。
2. fallback は「緩いが無制限ではない」条件にする。
3. postfix/prefix で動的引数を読む場合は必ず型と範囲を検証する。
4. fail-soft を優先する（機能出力をスキップし、ゲーム進行は止めない）。
5. ログは原因特定に十分な具体性を持たせる。

## 更新時チェックリスト（ゲーム版アップデート時）

1. ゲームを起動し、strict/fallback/error ログを確認する。
2. fallback に入っている機能は、ゲーム内表示が正しいか実機確認する。
3. 劣化があれば strict シグネチャを更新し、fallback 条件を引き締める。
4. リリース前に再ビルド・再確認する。
