# Build And Validation

最終確認: 2026-03-07

## 通常手順

1. `build.bat` を実行する。
2. `LangMod/*/Thing.tsv` から `Thing.xlsx` を再生成する。
3. `dotnet build Elin_JustDoomIt.csproj -c Release` を実行する。
4. 生成物を `elin_link/Package/Elin_JustDoomIt/` と Steam の `Package/Elin_JustDoomIt/` にコピーする。

## build.bat が配布する主なフォルダ

- `LangMod/`
- `LICENSES/`
- `Sound/`
- `Texture/`
- `wad/`

## 補足

- `Texture/Item/justdoomit_arcade.png` のような custom item texture も `build.bat` で配布対象になる。
- 実装変更後は `build.bat` に加えてゲーム内確認を行う。
