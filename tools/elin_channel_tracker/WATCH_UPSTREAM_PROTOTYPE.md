# Local upstream watch prototype

Game更新時または週次の差分確認用。Python標準libraryとGitで動き、fetch・常駐・queue・DB・自動修正を行わない。JSONとMarkdownの両方を必ず指定する。

```powershell
python tools/elin_channel_tracker/run.py watch-upstream `
  --repo C:/path/to/Elin-Decompiled `
  --watchlist upstream-watchlist.prototype.json `
  --accepted-ref 35fac67c8cd3adbc145f37a55b0f04b1347dbb73 `
  --candidate-ref 38a69bd9e976e46d5a512bb4811d586b96e70b3c `
  --report-json tools/elin_channel_tracker/reports/watch.json `
  --report-md tools/elin_channel_tracker/reports/watch.md
```

例のSHAはStable 23.338.2 / Nightly 23.351.2の実在commit。架空のstable branchや欠落refからHEADへのfallbackは使用しない。2 refは同じ履歴上で基準→候補のancestor関係が必要。異なる履歴間のchannel比較は既存`verify-compat`と実DLL署名catalogを使う。

| Exit | JSON status | 人が読む判定 | 意味 |
| --- | --- | --- | --- |
| 0 | `clear` | 監視範囲に関連差分なし | 明示されたファイル内でtoken差分なし、選択API契約一致 |
| 1 | `needs_maintenance` | 要メンテ | 関連ファイル変更、または読めたAPI契約の破断／risky |
| 2 | `unknown` | 判定不能 | ref／設定／入力欠落、対応外署名syntax等。既知の関連変更理由も残す |

`clear`はMod動作合格ではない。`needs_maintenance`のファイル変更も動作破断の証明ではない。通常のquoted string／charを読める時だけコメント・通常空白を除き、文字列・演算子tokenを保持する。interpolated、verbatim、raw string、その他の対応外syntaxを見たら字句比較を中止し、Gitで監視fileの変更が分かったことを根拠に保守的に要メンテとする（`comparison=raw_fallback`）。この場合はコメントだけの変更も要メンテになる。C#全体を構文検証するcompilerではなく、本文の意味・正当性は保証しない。

## 設定を維持する場所

`upstream-watchlist.prototype.json`の9 Mod entry、73ファイル行（重複を除く40ファイル）、API選択1件が初期範囲。既存target scannerで得たクラスに、直接呼出し等の広めのクラスを手で加えた試作一覧であり、repo全Mod／全依存の完全な一覧ではない。`files`は正確なGit相対POSIXパスで、globや暗黙の型名推定はしない。クラス追加時はそのクラスのファイルを追加する。

選択APIは既存`Elin_ArsMoriendi/tools/elin_channel_tracker/config/compat_targets.json`の`candidate_signatures`を参照する。これはOR契約で、各endpointでいずれか1署名が一致すればよい。新しい署名を見つけただけで候補に追加せず、ModのCompat実装と契約testで確認してから更新する。`targets_file`はwatchlistの所在からの相対パスで、`..`は受け付けない。

`api_checks`は既存Evaluatorの形式を再利用しているため、`stable_signatures`が基準endpoint、`nightly_signatures`が候補endpointを表す。このCLIの中では実際のchannel名や実DLL由来を意味せず、SHAと`provenance`を合わせて読む。

署名readerの範囲は、1つのglobal classで宣言されたpublic ordinary method、明示static/instance、通常の単純型／修飾型、by-value引数、単純な既定値、通常bodyまたはexpression body。戻り値と全引数型を既存collectorの表記へ正規化する。内部helper class/local functionは対象APIと混同しない。namespace、generic、ref/out、nullable/array/tuple、attribute付き対象宣言、preprocessor、interpolated／verbatim／raw string等は対応外。対象class全体を安全に字句分割できない時は署名を推測せず`unknown`になる。`@ref`等の通常escaped identifierが他のmethod本文にあるだけでは遮断しない。DLLの継承・metadata解決、accessor、field、ABIを保証しない。

手で維持する追加設定fileはこのwatchlist 1つ。署名の値は既存targets 1 fileを再利用する。R0の合格済みSHAと候補SHAは実行時に渡す。Stable/Nightlyを別々に保守するなら、channelごとに合格済みSHAをメモする。

## 通常手順と待機理由

1. 必要時だけ通常のGit操作でupstream HEAD／差分を取得し、対象commitを確定する。CLI自体はローカル読み取りのみ。
2. 前回人が合格確認を終えたR0を`--accepted-ref`にして実行する。R1に関連変更がありR2が無関係でも、R0→R2の集約差分にR1の理由が残る。
3. `needs_maintenance`なら表示されたファイル差分とAPI破断を調べ、必要な既存Game不要contract/unit testを行う。実DLL確認は既存net8 collector、機能確認は既存CWL runnerを必要時に別実行する。
4. 人によるメンテと検証が完了してから合格済みSHAを候補へ進める。CLIはSHAを進めない。各channelの合格は別々に判断する。

評価はendpointの差分を集約する。途中で発生して最終的に完全revertされた変更は残らない。Mod sourceやwatchlistの変更、CWL/Unity等の別repo変更、監視外の呼出し先、SourceExcel/asset変更は自動で追跡しない。Mod自身を変更した時はwatchlistを見直し、Game更新時または週次の手動確認を続ける。

## 検証

```powershell
cd tools/elin_channel_tracker
python -m pytest
```

新規`test_watch_upstream.py`はreal local Git historyで検証し、ネットワークやGameは不要。既存trackerには`test_detect_target_gaps_from_game_dependency_and_compat_symbol`の既知baseline失敗がある（fixtureの`id =`とscannerの`id:`の不一致）。試作のためにこの別件は修正していない。

142 commit評価で`clear`だった92件はすべて監視file自体が無変更だった。これは部分watchlistの差分頻度評価であり、tokenizerの精度評価ではない。監視範囲は9 Mod／40 unique file／API契約1件に限定される。
