using UnityEngine;

namespace Elin_Mod
{
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T s_Instance;

		public static T Instance {
			get {
				if (s_Instance == null) {
					CreateInstance();
				}
				return s_Instance;
			}
		}

		public static void CreateInstance() {
			if (s_Instance == null) {
				var go = new GameObject(typeof(T).Name);
				GameObject.DontDestroyOnLoad(go);
				s_Instance = go.AddComponent<T>();
			}
		}

		public static void DeleteInstance() {
			if (s_Instance != null) {
				GameObject.Destroy(s_Instance.gameObject);
			}
			s_Instance = null;
		}
	}
}
