using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Elin_NiComment.Llm
{
    public class OpenAiCompatProvider : ILlmProvider
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _endpoint;
        private readonly float _timeout;
        private readonly bool _isOpenRouter;

        public string Name => _isOpenRouter ? "OpenRouter" : "OpenAI Compatible";
        public bool IsAvailable => !string.IsNullOrEmpty(_endpoint);

        public OpenAiCompatProvider(string apiKey, string model, string endpoint, float timeout)
        {
            _apiKey = apiKey ?? "";
            _endpoint = endpoint ?? "http://localhost:11434/v1/chat/completions";
            _isOpenRouter = _endpoint.Contains("openrouter.ai");
            _model = string.IsNullOrEmpty(model)
                ? (_isOpenRouter ? "qwen/qwen3-next-80b-a3b-instruct:free" : "llama3")
                : model;
            _timeout = timeout;
        }

        public void SendAsync(string systemPrompt, string userMessage, Action<string, string> onComplete)
        {
            if (!IsAvailable)
            {
                onComplete?.Invoke(null, "Endpoint not set");
                return;
            }

            CoroutineRunner.Run(SendCoroutine(systemPrompt, userMessage, onComplete));
        }

        private IEnumerator SendCoroutine(string systemPrompt, string userMessage, Action<string, string> onComplete)
        {
            var body = new JObject
            {
                ["model"] = _model,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JObject { ["role"] = "user", ["content"] = userMessage }
                },
                ["temperature"] = 1.2,
                ["max_tokens"] = 256,
                ["response_format"] = new JObject { ["type"] = "json_object" }
            };

            var request = new UnityWebRequest(_endpoint, "POST");
            var bodyBytes = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_apiKey))
                request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            if (_isOpenRouter)
            {
                request.SetRequestHeader("HTTP-Referer", "https://github.com/Elin-NiComment");
                request.SetRequestHeader("X-Title", "Elin NiComment");
            }
            request.timeout = (int)_timeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errMsg = $"[{request.responseCode}] {request.error}";
#if DEBUG
                Debug.LogWarning($"[NiComment] {Name} HTTP error: {errMsg}\nBody: {request.downloadHandler?.text}");
#endif
                onComplete?.Invoke(null, errMsg);
                request.Dispose();
                yield break;
            }

            string result = null;
            string error = null;
            try
            {
                var json = JObject.Parse(request.downloadHandler.text);
                result = json["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrEmpty(result))
                    error = "Empty response";
            }
            catch (Exception ex)
            {
                error = $"Parse error: {ex.Message}";
            }

            request.Dispose();
            onComplete?.Invoke(result, error);
        }
    }
}
