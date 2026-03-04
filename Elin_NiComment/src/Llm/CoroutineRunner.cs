using System.Collections;
using UnityEngine;

namespace Elin_NiComment.Llm
{
    /// <summary>
    /// Lightweight MonoBehaviour for running coroutines from non-MonoBehaviour classes.
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("NiComment_CoroutineRunner");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
        }

        public static Coroutine Run(IEnumerator coroutine)
        {
            EnsureInstance();
            return _instance.StartCoroutine(coroutine);
        }

        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
