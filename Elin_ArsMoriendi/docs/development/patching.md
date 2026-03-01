# パッチ戦略・Mod干渉

## パッチ戦略（API変更への耐性）

Elin はアクティブに開発中のため、ゲーム更新でメソッドシグネチャが変わる可能性があります。Harmony パッチを書く際は以下の方針を参考にしてください。

### 基本方針

1. **TargetMethod 解決は明示的に**
   - `[HarmonyPatch(typeof(Class), nameof(Class.Method))]` で対象を指定
   - オーバーロードがある場合は `[HarmonyPatch(new Type[] { ... })]` で引数型も指定

2. **Fail-soft を優先**
   - パッチ内で例外が発生してもゲーム進行を止めない
   - `try/catch` で囲み、問題時はログ出力してスキップ

3. **グローバルON/OFFは持たない**
   - `EnableMod` のような全機能トグルは運用上危険なため採用しない
   - 必要な場合は機能単位の設定（例: コンテキストメニュー表示）だけを設ける
   - 障害時の切り分けは fail-soft ログとパッチ失敗検知で行う

4. **ターン終了フックは実処理側を選ぶ**
   - `Player.EndTurn(bool)` は予約処理（`GoalEndTurn` セット）で、実ターン終端とは一致しないケースがある
   - プレイヤー1ターン終端に同期したい処理は `BaseGameScreen.OnEndPlayerTurn` を優先する
   - 参照値は `EClass.pc.turn` を優先（`TickConditions()` で進行）

### Stable/Nightly 両対応（2026-02-28）

1. **起動時に互換シンボルを warmup する**
   - `Plugin.Awake()` で `CompatBootstrap.Initialize()` を呼び、互換対象メソッドの解決可否を早期検知する

2. **改名/シグネチャ変更対象は直接呼び出しを避ける**
   - `Quest.Create`, `Chara.Die`, `ReleaseMinion/UnmakeMinion`, `SetMainElement` は `SafeInvoke` 経由で呼び出す
   - 具体的な候補名・述語は `src/Compat/CompatSymbol.cs` に集約する
   - `Point.GetNearestPoint` は `GetNearestPointCompat()` を使い、4/5引数差分を吸収する

3. **可変パッチターゲットは `PatchTargetResolver` を使う**
   - `ActPlan._Update` のように更新で破綻しやすいターゲットは `HarmonyPrepare + HarmonyTargetMethod` で resolver を使う
   - 解決失敗時はパッチをスキップし、エラーログを出して継続する（fail-soft）

4. **互換修正の意図は属性で残す**
   - 互換メソッドには `[CompatibilityPatch("<version>", "<what changed>")]` を付ける
   - どの更新差分向けの処置かをコード上で追跡できる形にする

### 高度なパッチ戦略（大規模Mod向け）

複数のゲームバージョンに対応する必要がある場合は、2段階解決パターンを検討:

1. **Strict一致**: 既知シグネチャに完全一致
2. **Fallback一致**: 緩い条件で解決（Warning ログ付き）

参考実装: `Elin_LogRefined/src/PatchResolver.cs`

## Prefix / Postfix パッチの選び方と Mod 干渉

### Prefix パッチ

```csharp
[HarmonyPrefix]
static bool Prefix(Chara __instance) { ... }
```

- 元メソッドの**前**に実行される
- `return false` で**元メソッドの実行をスキップ**できる
- 複数 Mod が同じメソッドに Prefix を貼った場合、**1つが `return false` すると他の Prefix も元メソッドも全てスキップ**される
- 干渉リスク: **高い**

### Postfix パッチ

```csharp
[HarmonyPostfix]
static void Postfix(Chara __instance) { ... }
```

- 元メソッドの**後**に実行される
- 元メソッドの実行を阻止できない
- `__result` 引数で戻り値を読み書きできる
- Prefix が `return false` した場合でも Postfix は実行される（Harmony 2.x）
- 干渉リスク: **低い**

### 比較

| | Prefix | Postfix |
|---|---|---|
| 実行タイミング | 元メソッドの前 | 元メソッドの後 |
| 元メソッドのスキップ | `return false` で可能 | 不可 |
| 他 Mod との干渉 | **高い** | **低い** |
| 推奨度 | 本当に必要な時だけ | **基本はこちら** |

### 避けるべきパターン

**1. Prefix で `return false`（最大のアンチパターン）**

```csharp
// 悪い例: 元メソッドを丸ごと置き換え → 他Modも元メソッドも殺す
static bool Prefix(Chara __instance) {
    // 自分の実装で全部やる
    return false;
}
```

代替: Postfix で結果を上書きするか、Transpiler で必要な箇所だけ書き換える。

**2. Prefix で引数を書き換えて副作用を起こす**

```csharp
// 危険: 他Modが期待する引数を変えてしまう
static void Prefix(ref int amount) {
    amount = 0;
}
```

**3. Postfix で `__result` を無条件に上書き**

```csharp
// 悪い例: 他Modの修正を無視して常に上書き
static void Postfix(ref bool __result) {
    __result = true;
}

// 良い例: 自分の条件に合う場合だけ変更
static void Postfix(Chara __instance, ref bool __result) {
    if (IsMyModTarget(__instance))
        __result = true;
}
```

**4. static フィールドで Prefix→Postfix 間の状態を共有**

```csharp
// 悪い例: 他Modが間で値を変更したら壊れる
static int _saved;
static void Prefix(Chara __instance) { _saved = __instance.hp; }
static void Postfix(Chara __instance) { int diff = __instance.hp - _saved; }

// 良い例: __state を使う（Harmony がスレッドセーフに管理）
static void Prefix(Chara __instance, out int __state) {
    __state = __instance.hp;
}
static void Postfix(Chara __instance, int __state) {
    int diff = __instance.hp - __state;
}
```

### 原則

**Postfix で済むなら Postfix を使う。** Prefix の `return false` は「他の全 Mod と元メソッドを殺す覚悟」がある場合のみ。
