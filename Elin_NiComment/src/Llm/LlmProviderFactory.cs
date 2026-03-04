using UnityEngine;

namespace Elin_NiComment.Llm
{
    public static class LlmProviderFactory
    {
        private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

        public static ILlmProvider Create()
        {
            var providerName = LlmConfig.Provider.Value?.Trim().ToLowerInvariant();
            switch (providerName)
            {
                case "openrouter":
                    Debug.Log("[NiComment] Using OpenRouter provider");
                    return new OpenAiCompatProvider(
                        LlmConfig.OpenRouterApiKey.Value,
                        LlmConfig.OpenRouterModel.Value,
                        OpenRouterEndpoint,
                        LlmConfig.OpenRouterTimeout.Value);

                case "local":
                case "openai_compat": // backward compat
                    Debug.Log("[NiComment] Using Local LLM provider");
                    return new OpenAiCompatProvider(
                        LlmConfig.LocalApiKey.Value,
                        LlmConfig.LocalModel.Value,
                        LlmConfig.LocalEndpoint.Value,
                        LlmConfig.LocalTimeout.Value);

                case "gemini":
                default:
                    Debug.Log("[NiComment] Using Gemini provider");
                    return new GeminiProvider(
                        LlmConfig.GeminiApiKey.Value,
                        LlmConfig.GeminiModel.Value,
                        LlmConfig.GeminiTimeout.Value);
            }
        }
    }
}
