namespace Elin_Mod
{

	public class Singleton<T> where T : class, new()
	{
		private static T s_Instance;

		public static T Instance
		{
			get
			{
				if (s_Instance == null)
				{
					CreateInstance();
				}
				return s_Instance;
			}
		}

		public static void CreateInstance()
		{
			if (s_Instance == null)
			{
				s_Instance = new T();
			}
		}

		public static void DeleteInstance()
		{
			s_Instance = null;
		}
	}
}
