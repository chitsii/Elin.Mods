using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Elin_NiComment.Llm
{
    public class GeminiProvider : ILlmProvider
    {
        // Gemini response_schema uses OpenAPI-style types (uppercase)
        private static readonly JObject ResponseSchema = new JObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JObject
            {
                ["hype"] = new JObject { ["type"] = "INTEGER" },
                ["comments"] = new JObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JObject { ["type"] = "STRING" }
                },
                ["kotehan"] = new JObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JObject
                    {
                        ["type"] = "OBJECT",
                        ["properties"] = new JObject
                        {
                            ["n"] = new JObject { ["type"] = "STRING" },
                            ["t"] = new JObject { ["type"] = "STRING" }
                        },
                        ["required"] = new JArray("n", "t")
                    }
                },
                ["barrage"] = new JObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JObject { ["type"] = "STRING" }
                }
            },
            ["required"] = new JArray("hype", "comments", "kotehan", "barrage")
        };

        private readonly string _apiKey;
        private readonly string _model;
        private readonly float _timeout;

        public string Name => "Gemini";
        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

        public GeminiProvider(string apiKey, string model, float timeout)
        {
            _apiKey = apiKey ?? "";
            _model = string.IsNullOrEmpty(model) ? "gemini-2.5-flash-lite" : model;
            _timeout = timeout;
        }

        public void SendAsync(string systemPrompt, string userMessage, Action<string, string> onComplete)
        {
            if (!IsAvailable)
            {
                onComplete?.Invoke(null, "API key not set");
                return;
            }

            CoroutineRunner.Run(SendCoroutine(systemPrompt, userMessage, onComplete));
        }

        private IEnumerator SendCoroutine(string systemPrompt, string userMessage, Action<string, string> onComplete)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var body = new JObject
            {
                ["system_instruction"] = new JObject
                {
                    ["parts"] = new JArray { new JObject { ["text"] = systemPrompt } }
                },
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JArray { new JObject { ["text"] = userMessage } }
                    }
                },
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = 1.2,
                    ["maxOutputTokens"] = 256,
                    ["response_mime_type"] = "application/json",
                    ["response_schema"] = ResponseSchema
                }
            };

            var bodyJson = body.ToString(Newtonsoft.Json.Formatting.None);
            var request = new UnityWebRequest(url, "POST");
            var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)_timeout;

#if DEBUG
            Debug.Log($"[NiComment] Gemini request: model={_model}, bodyLen={bodyBytes.Length}");
#endif

            yield return request.SendWebRequest();

#if DEBUG
            Debug.Log($"[NiComment] Gemini HTTP {request.responseCode} result={request.result}");
#endif

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errMsg = $"[{request.responseCode}] {request.error}";
#if DEBUG
                Debug.LogWarning($"[NiComment] Gemini HTTP error: {errMsg}\nBody: {request.downloadHandler?.text}");
#endif
                onComplete?.Invoke(null, errMsg);
                request.Dispose();
                yield break;
            }

            string result = null;
            string error = null;
            var rawText = request.downloadHandler.text;
#if DEBUG
            Debug.Log($"[NiComment] Gemini raw response ({rawText?.Length ?? 0} chars):\n{rawText}");
#endif
            try
            {
                var json = JObject.Parse(rawText);
                result = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                if (string.IsNullOrEmpty(result))
                    error = "Empty response from Gemini";
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
