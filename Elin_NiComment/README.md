# NiComment

Elin にニコニコ風のコメントオーバーレイを追加するMod。
ゲーム内イベント（戦闘、NPC発言、天候変化など）に対してコメントが流れます。

LLM（大規模言語モデル）を接続すると、ゲームログの内容に応じてAIが文脈を読んだコメントを生成します。

## LLM機能のセットアップ

各プロバイダの設定は独立したセクションに保存されます。`[LLM]` の `Provider` を切り替えるだけで即座にプロバイダを変更できます。

| Provider値 | プロバイダ | 特徴 |
|---|---|---|
| `gemini` | Gemini (Google) | 構造化出力でJSON安定。無料枠あり |
| `openrouter` | OpenRouter | 多数の無料モデルに対応。APIキーのみで利用可 |
| `local` | ローカルLLM (Ollama等) | オフライン動作。OpenAI互換API |

### 設定例

`BepInEx/config/tishi.elin_nicomment.cfg`:

```ini
[LLM]
## Active LLM provider: "gemini", "openrouter", or "local"
Provider = openrouter
EnableLlm = true

[LLM.Gemini]
## Google AI Studio API key. Get one at https://aistudio.google.com/apikey
ApiKey =
ModelName = gemini-2.5-flash-lite

[LLM.OpenRouter]
## OpenRouter API key (sk-or-v1-...). Get one at https://openrouter.ai/keys
ApiKey = sk-or-v1-ここにAPIキーを貼り付け
ModelName = google/gemma-3-27b-it:free

[LLM.LocalLLM]
## API key for local LLM server (optional, leave empty for Ollama)
ApiKey =
ModelName = llama3
EndpointUrl = http://localhost:11434/v1/chat/completions
```

### Gemini

1. https://aistudio.google.com/apikey でAPIキーを作成
2. `[LLM.Gemini]` の `ApiKey` に貼り付け
3. `[LLM]` の `Provider = gemini` に設定

無料枠: 15 RPM / 1,000リクエスト/日

### OpenRouter

1. https://openrouter.ai でアカウント作成
2. https://openrouter.ai/keys でAPIキーを作成
3. `[LLM.OpenRouter]` の `ApiKey` に貼り付け
4. `[LLM]` の `Provider = openrouter` に設定

無料枠: 20 RPM / 200リクエスト/日

おすすめ無料モデル（`ModelName` に指定）：

| モデルID | 特徴 |
|---|---|
| `google/gemma-3-27b-it:free` | デフォルト。日本語品質良好 |
| `qwen/qwen3-30b-a3b:free` | MoE構造で高速 |
| `meta-llama/llama-3.3-70b-instruct:free` | 大型、高品質 |
| `qwen/qwen3-4b:free` | 最速。低スペックPC向き |

利用可能な無料モデル一覧: https://openrouter.ai/collections/free-models

### ローカルLLM（Ollama等）

1. Ollama等でモデルを起動
2. `[LLM.LocalLLM]` の `EndpointUrl` と `ModelName` を設定
3. `[LLM]` の `Provider = local` に設定
