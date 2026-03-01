# アイテム（Thing）スプライト仕様

## ファイル名

Modの `Texture/` フォルダに **`{id}.png`** を配置する。

- `id` = SourceThingシートの `id` カラムの値（例: `ars_tome` → `ars_tome.png`）
- 雪バージョン: `{id}_snow.png`（任意）
- 拡張子は **`.png`**（`.PNG` は不可）

CWLが `SpriteReplacer.dictModItems` にModの `Texture/` フォルダ内のファイルを登録し、ゲーム本体の `SpriteReplacer.HasSprite()` がそれを参照する。

## `_idRenderData` の設定

SourceThingシートの `_idRenderData` に **`@obj`** プレフィックス付きの値を指定する。

- `@obj` → カスタムテクスチャモード（スプライトシート置換ではなく、個別PNG読み込み）
- `_idRenderData` が `@` で始まると、`RenderRow.SetRenderData()` で `pass = null` になり、Actor-based rendering（SpriteReplacer経由）が使われる

### 代表的な `_idRenderData` の種類

| 値 | 用途 |
|---|---|
| `@obj` | 標準的な地面配置オブジェクト |
| `@obj tall` | やや上にオフセット |
| `@obj flat` | 平置き（じゅうたん等） |
| `@obj ceil` | 天井吊り下げ |
| `@obj eq` | 装備品（左16pxオフセット） |
| `@obj door` | ドア（壁が必要） |
| `@obj hangboard` | 壁掛け看板 |
| `@obj hangroof` | 屋根吊り下げ |
| `@obj vine` | つる植物 |

詳細な配置挙動は [Sam's _idRenderData Notes](https://elin-modding-resources.github.io/Elin.Docs/articles/15_Texture%20Mods/sam_id_render_data) を参照。

## サイズ（ピクセル）

PPUは一律 **100**（`SpriteData.cs:118`）。`@obj` 方式では `pass = null` → `ThingActor` 経由の描画になり、**スプライトの自動縮小は行われない**（`CardActor.cs:151-153`）。PNGのピクセルサイズがそのまま表示サイズに直結する。

### 推奨サイズ

| 用途 | 推奨サイズ | ワールドユニット |
|---|---|---|
| 1タイルアイテム | **48x48** | 0.48（1タイル相当） |
| 大きめアイテム | **96x96** | 0.96（2タイル相当、`size` 指定と合わせる） |

### なぜ 128x128 ではダメか

128x128px / PPU100 = 1.28ワールドユニット。1タイルは約0.48ワールドユニットなので、**約2.67倍** の大きさになりタイルからはみ出る。高解像度の絵を使いたい場合は、48x48にリサイズしたPNGを配置すること。

### バニラのスプライトシート参考

| シート | 用途 |
|---|---|
| `objs_SS` | 極小アイテム（弾丸など） |
| `objs_S` | 小型アイテム |
| `objs` | 標準アイテム（48x48相当） |
| `objs_L` | 大型家具（96x96相当、2タイル分） |

`@obj` 方式ではこれらのシートを使わず個別PNGから直接Spriteを生成するが、**サイズの目安はこれらに合わせる**。

## アニメーション

複数フレームのアニメーションは、横に並べた1枚のPNGで対応する。

- `.ini` ファイルで `frame=N` を指定（デフォルト1）
- 画像の幅 / frame数 = 1フレームの幅（`SpriteData.cs:113`）
- `time` で切り替え間隔を指定（デフォルト0.2秒）

### `.ini` ファイルの例

```ini
frame=4
scale=50
time=0.2
```

## ディレクトリ構造

```
YourMod/
├── Texture/
│   ├── your_item_id.png        ← アイテムスプライト
│   ├── your_item_id_snow.png   ← 雪バージョン（任意）
│   └── your_item_id.ini        ← アニメ/スケール設定（任意）
```

## ソースコード参照

- `SpriteReplacer.cs` - `HasSprite()`: `dictModItems` → `CorePath.packageCore + "Texture/Item/" + id` の順で検索
- `SpriteData.cs:118` - `Sprite.Create` with pivot `(0.5, 0.5)`, PPU=100
- `RenderRow.cs:263` - `replacer.HasSprite(idSprite, renderData)` でカスタムスプライト判定
- `CardRow.cs:64` - `idSprite => id`（ThingのスプライトIDはそのままSourceThingの`id`）
- `CardRow.cs:62` - `idRenderData`: `_idRenderData` が空なら `"Thing/" + id` にフォールバック
