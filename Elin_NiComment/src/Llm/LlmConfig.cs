using System;
using BepInEx.Configuration;

namespace Elin_NiComment.Llm
{
    public static class LlmConfig
    {
        /// <summary>
        /// Fired when any provider-related setting changes (Provider, ApiKey, ModelName, etc.).
        /// </summary>
        public static event Action OnProviderSettingsChanged;

        // --- Shared ---
        public static ConfigEntry<bool> EnableLlm;
        public static ConfigEntry<string> Provider;
        public static ConfigEntry<float> BatchInterval;
        public static ConfigEntry<int> MaxBatchSize;
        public static ConfigEntry<float> DripInterval;
        public static ConfigEntry<float> EventMaxAge;

        // --- Gemini ---
        public static ConfigEntry<string> GeminiApiKey;
        public static ConfigEntry<string> GeminiModel;
        public static ConfigEntry<float> GeminiTimeout;

        // --- OpenRouter ---
        public static ConfigEntry<string> OpenRouterApiKey;
        public static ConfigEntry<string> OpenRouterModel;
        public static ConfigEntry<float> OpenRouterTimeout;

        // --- Local LLM ---
        public static ConfigEntry<string> LocalApiKey;
        public static ConfigEntry<string> LocalModel;
        public static ConfigEntry<string> LocalEndpoint;
        public static ConfigEntry<float> LocalTimeout;

        public static void LoadConfig(ConfigFile config)
        {
            // === Shared ===
            EnableLlm = config.Bind("LLM", "EnableLlm", false,
                "Enable LLM-based comment reactions.");

            Provider = config.Bind("LLM", "Provider", "gemini",
                "Active LLM provider: \"gemini\", \"openrouter\", or \"local\".");

            BatchInterval = config.Bind("LLM", "BatchInterval", 12f,
                new ConfigDescription("Seconds to accumulate events before sending to LLM.",
                    new AcceptableValueRange<float>(1f, 30f)));

            MaxBatchSize = config.Bind("LLM", "MaxBatchSize", 10,
                new ConfigDescription("Max events per LLM request.",
                    new AcceptableValueRange<int>(1, 20)));

            DripInterval = config.Bind("LLM", "DripInterval", 1.5f,
                new ConfigDescription("Seconds between drip-fed comments.",
                    new AcceptableValueRange<float>(0.5f, 5f)));

            EventMaxAge = config.Bind("LLM", "EventMaxAge", 20f,
                new ConfigDescription("Max age in seconds before events are discarded.",
                    new AcceptableValueRange<float>(5f, 60f)));

            // === Gemini ===
            GeminiApiKey = config.Bind("LLM.Gemini", "ApiKey", "",
                "Google AI Studio API key. Get one at https://aistudio.google.com/apikey");

            GeminiModel = config.Bind("LLM.Gemini", "ModelName", "gemini-2.5-flash-lite",
                "Gemini model name.");

            GeminiTimeout = config.Bind("LLM.Gemini", "TimeoutSeconds", 10f,
                new ConfigDescription("HTTP request timeout in seconds.",
                    new AcceptableValueRange<float>(3f, 60f)));

            // === OpenRouter ===
            OpenRouterApiKey = config.Bind("LLM.OpenRouter", "ApiKey", "",
                "OpenRouter API key (sk-or-v1-...). Get one at https://openrouter.ai/keys");

            OpenRouterModel = config.Bind("LLM.OpenRouter", "ModelName", "qwen/qwen3-next-80b-a3b-instruct:free",
                "OpenRouter model ID. Free models end with :free. See https://openrouter.ai/collections/free-models");

            OpenRouterTimeout = config.Bind("LLM.OpenRouter", "TimeoutSeconds", 15f,
                new ConfigDescription("HTTP request timeout in seconds.",
                    new AcceptableValueRange<float>(3f, 60f)));

            // === Local LLM ===
            LocalApiKey = config.Bind("LLM.LocalLLM", "ApiKey", "",
                "API key for local LLM server (optional, leave empty for Ollama).");

            LocalModel = config.Bind("LLM.LocalLLM", "ModelName", "llama3",
                "Model name for local LLM server.");

            LocalEndpoint = config.Bind("LLM.LocalLLM", "EndpointUrl",
                "http://localhost:11434/v1/chat/completions",
                "OpenAI-compatible endpoint URL.");

            LocalTimeout = config.Bind("LLM.LocalLLM", "TimeoutSeconds", 10f,
                new ConfigDescription("HTTP request timeout in seconds.",
                    new AcceptableValueRange<float>(3f, 60f)));

            // Hook SettingChanged on all provider-affecting entries
            Provider.SettingChanged += OnSettingChanged;
            GeminiApiKey.SettingChanged += OnSettingChanged;
            GeminiModel.SettingChanged += OnSettingChanged;
            OpenRouterApiKey.SettingChanged += OnSettingChanged;
            OpenRouterModel.SettingChanged += OnSettingChanged;
            LocalApiKey.SettingChanged += OnSettingChanged;
            LocalModel.SettingChanged += OnSettingChanged;
            LocalEndpoint.SettingChanged += OnSettingChanged;
        }

        private static void OnSettingChanged(object sender, EventArgs e)
        {
            OnProviderSettingsChanged?.Invoke();
        }
    }
}
