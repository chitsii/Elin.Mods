# 新規呪文開発ガイド（ゼロから作成）

このドキュメントは、Ars Moriendi に新しい呪文を1つ追加する最短手順をまとめたものです。
対象は以下です。

- C#実装（`Spell` クラス）
- データ定義（Element / 必要なら Stat）
- 習得導線（禁書UIのアンロック一覧）
- 魔法書アイテム導線（必要時）

## 0. 追加先の全体像

- 呪文ロジック: `src/Spells/Act*.cs`
- 呪文データ: `tools/data/elements.py` -> `tools/builder/create_element_excel.py` -> `LangMod/*/Element.tsv`
- 状態異常データ（必要時）: `tools/data/stats.py` -> `tools/builder/create_stat_excel.py` -> `LangMod/*/Stat.tsv`
- UI習得一覧: `src/NecromancyManager.cs` (`SpellUnlocks`)
- 魔法書アイテム: `tools/data/items.py`（`_SPELLBOOK_DEFS`）
- 文言ログ: `src/LangHelper.cs`

## 1. C#の呪文クラスを作る

1. `src/Spells/ActYourSpell.cs` を作成し、`Spell` 継承で `Perform()` を実装する。
2. 成功時は `return true;` を返す（経験値付与フローに乗るため）。
3. 基本は `var power = GetPower(Act.CC);` を取得し、効果量計算に使う。
4. 攻撃系は `NecroSpellUtil.CheckHostile(Act.TC, Act.CC)` を先頭で使う。
5. 例外は握って `ModLog.Warn(...)` を出す（fail-soft）。

最小テンプレート:

```csharp
using System;

namespace Elin_ArsMoriendi
{
    public class ActYourSpell : Spell
    {
        public override bool IsHostileAct => true; // 必要なら

        public override bool Perform()
        {
            if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;

            try
            {
                var power = GetPower(Act.CC);
                var caster = Act.CC;
                var target = Act.TC?.Chara;

                // TODO: 効果実装
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ActYourSpell.Perform failed: {ex.Message}");
            }

            return true;
        }
    }
}
```

## 2. Element定義を追加する

`tools/data/elements.py` の `CUSTOM_SPELLS` に1エントリ追加する。

必須項目:

- `id`: 10000超のユニークID
- `alias`: 例 `actYourSpell`
- `type`: 例 `Elin_ArsMoriendi.ActYourSpell`
- `group`: `SPELL`
- `category`: `ability`
- `categorySub`: `attack` / `defense` / `util`
- `target`: `Enemy` / `Self` など
- `cost`: 例 `"12,6"`
- `charge`, `cooldown`, `max`
- `detail_JP`, `detail`, `detail_CN`

重要:

- `lvFactor` は未指定でも、`create_element_excel.py` 側で `SPELL` なら自動的に `100` が入る。
- バニラ呪文も `lvFactor=100` で統一されているため、この値を基準にする。

## 2.5 動的説明文（バニラ式）の書き方

バニラの呪文ツールチップは、主に `textExtra_JP` / `textExtra` で動的情報を出している。
`detail_JP` / `detail` は短い概要文、`textExtra_*` は実効果の表示に使い分ける。

実コードの参照箇所（ゲーム本体）:

- `Elin-Decompiled/Elin/ELEMENT.cs`
  - `Element._WriteNote(UINote n, Chara c, Act act)` が `textExtra` を処理する中核。
  - `@ConAlias` は `Condition.Create(alias, p)` + `EvaluateTurn(p)` で展開される。
  - `#calc` は `Dice.Create(e, c)` の結果で置換される。
- `Elin-Decompiled/Elin/Plugins.basecore/SourceData.cs`
  - `SourceData.BaseRow.GetText(id)` が `detail` / `textExtra` の言語文字列を返す。

基本トークン:

- `@ConAlias`:
  - Condition定義（`tools/data/stats.py` -> `LangMod/*/Stat.tsv`）を参照して表示する。
  - `duration` や `elements` の `p/..` 式を含めて説明されるため、バフ/デバフ呪文で最優先。
- `#calc`:
  - 一部のバニラ `proc` で計算結果を表示できる（例: Heal/Meteor）。
  - `proc` が独自実装（`Elin_ArsMoriendi.Act...`）の呪文では期待通りに解決されない場合がある。
- `#ele`:
  - 属性攻撃系の一部 `proc` で属性名を表示する補助トークン。

実装時の注意:

- `create_element_excel.py` は、`SPELL` で `textExtra_*` が空だと `detail_*` を自動コピーする。
  - 動的表示したい呪文は、`elements.py` で `textExtra_JP` / `textExtra` / `textExtra_CN` を明示すること。
- `@Con` 参照を使う場合、実際の効果量式は `stats.py` 側（`duration`, `elements`）を正とする。
  - C# 側で `SetOwner()` により別式で上書きすると、ツールチップと実挙動が乖離する。
- C#主導で追記する場合は、まず各 `Act*.cs` の `GetDetail()` override を優先する。
  - `Element._WriteNote` へのHarmonyは全呪文UIに波及するため、最後の手段にする。

推奨テンプレート:

- 単体デバフ/バフ: `textExtra_JP: "@ConYourCondition,補足説明"`
- 継続回復/継続ダメージ: `textExtra_JP: "@ConYourCondition"`
- 固有ロジック（死体数依存・蘇生数依存など）:
  - `@Con` で表現できる部分だけ動的化し、残りは短い補足文を併記する。

## 3. 状態異常（Condition）が必要なら追加

DoT・デバフ・バフ持続などを行う場合:

1. `src/Conditions/ConYourCondition.cs` を追加。
2. `tools/data/stats.py` の `CONDITIONS` に対応行を追加（`type` は完全修飾名）。
3. `duration` は `p/5` など `power` 連動を基本にする。

## 4. UIの習得一覧に出す

`src/NecromancyManager.cs` の `SpellUnlocks` に1件追加する。

- `alias` は `elements.py` と一致させる。
- 魂コスト・初期解放・説明文（JP/EN/CN）をここで設定。
- この説明文が禁書UI（Knowledgeタブ）に出る。

## 5. 魔法書導線が必要なら追加

`tools/data/items.py` の `_SPELLBOOK_DEFS` に alias を追加すると、
`ars_book_<alias>` 形式の魔法書が自動生成される。

- Traitは `NecroSpellbook,<alias>`
- 実際の習得可否は `TraitNecroSpellbook` + `SpellUnlocks` 側で制御される。

## 6. キャスト文言を追加（推奨）

`src/LangHelper.cs` の辞書に `castYourSpell` を追加し、
`Perform()` 内で `LangHelper.Say("castYourSpell", ...)` を呼ぶ。

## 7. 生成とビルド

個別生成:

```bat
python tools\builder\create_element_excel.py
python tools\builder\create_stat_excel.py
python tools\builder\create_thing_excel.py
```

通常は `build.bat` で一括実行:

```bat
build.bat
```

## 8. 動作確認チェック

- 禁書UIに呪文名・説明・習得コストが表示される。
- 習得後にアビリティとして使用できる。
- ストック（`vPotential`）が正しく減る。
- 成功キャスト時に経験値が入る（`cost > 0` かつ `lvFactor > 0`）。
- `GetPower` が効果量/持続に反映され、レベルアップが意味を持つ。
- `Player.log` に例外が出ない（`[ArsMoriendi]` で確認）。

## 9. よくあるハマりどころ

- `type` の名前空間違いでクラス解決できない。
- `SpellUnlocks` 未登録でUIから解放できない。
- `GetPower` を使っておらず、レベルが上がっても体感が変わらない。
- `target` と `Perform()` 内の想定（`Act.TC` 前提など）が一致していない。
- `detail` だけ更新して `SpellUnlocks` 側説明を更新していない。

## 10. 既存実装の参照先

- 直線的な単体デバフ: `src/Spells/ActCurseWeakness.cs`
- 召喚: `src/Spells/ActSummonUndead.cs`
- 継続効果（Condition）: `src/Conditions/ConDeathZone.cs`
- GUI説明の出力元: `src/NecromancyManager.cs` (`SpellUnlocks`)
