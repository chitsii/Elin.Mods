using UnityEngine;

namespace Elin_Mod
{
	/// <summary>
	/// Reset()を潰すためだけの哀れな存在.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SourceNoReset<T> : SourceData 
		where T : SourceData
	{
		T m_T;

		public static SourceNoReset<T> Create() {
			var ret = new SourceNoReset<T>();
			ret.m_T = ScriptableObject.CreateInstance<T>();
			return ret;
		}

	

		public T GetData() {
			return m_T;
		}


		public override void Reset() {
			base.Reset();
		}
	}
}
