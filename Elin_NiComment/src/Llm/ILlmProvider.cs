using System;

namespace Elin_NiComment.Llm
{
    public interface ILlmProvider
    {
        string Name { get; }
        bool IsAvailable { get; }
        void SendAsync(string systemPrompt, string userMessage, Action<string, string> onComplete);
    }
}
