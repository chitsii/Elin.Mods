using UnityEngine;

namespace Elin_Mod
{

	public class ModTextManager : Singleton<ModTextManager>
	{
		enum eTag {
			UserCode,
			MAX
		}

		class Tag {
			public string text;
			public eTag tag;
			public int opt1;
		}

		static readonly Tag[] c_Tags = new Tag[] {
			new Tag() { text = "[0]", tag = eTag.UserCode, opt1 = 0 },
			new Tag() { text = "[1]", tag = eTag.UserCode, opt1 = 1 },
			new Tag() { text = "[2]", tag = eTag.UserCode, opt1 = 2 },
			new Tag() { text = "[3]", tag = eTag.UserCode, opt1 = 3 },
			new Tag() { text = "[4]", tag = eTag.UserCode, opt1 = 4 },
			new Tag() { text = "[5]", tag = eTag.UserCode, opt1 = 5 },
			new Tag() { text = "[6]", tag = eTag.UserCode, opt1 = 6 },
			new Tag() { text = "[7]", tag = eTag.UserCode, opt1 = 7 },
			new Tag() { text = "[8]", tag = eTag.UserCode, opt1 = 8 },
		};

		struct UserData {
			public string data;
			public bool isUse;

			public void Set(string data) {
				this.data = data;
				this.isUse = true;
			}

			public void Clear() {
				this.isUse = false;
				this.data = null;
			}
		}

		private ModText m_TextCore;
		private UserData[] m_UserDatas;
		private eLanguage m_Lang;




		public void Initialize()
		{
			// SourceDataはScriptableObjectなので本来はリソースとして保存しときたいのだが、.
			// とりあえず現状はランタイムで生成＆読み込みする.
			m_TextCore = ScriptableObject.CreateInstance<ModText>();
			m_TextCore.Setup();
			CommonUtil.LoadTable("mod_texts.xlsm", "texts", m_TextCore);

			m_UserDatas = new UserData[c_Tags.Length];//< TODO:適当.

			m_Lang = m_TextCore.GetLanguageCode();
		}

		public void Terminate() {

		}

		public string GetText( eTextID id )
		{
			var ret = m_TextCore.GetText( id );
			// タグ置き換え.
			for ( int i = 0; i < c_Tags.Length; ++i ) {
				switch (c_Tags[i].tag) {
				case eTag.UserCode:
					if (!m_UserDatas[c_Tags[i].opt1].isUse)
						continue;
					ret = ret.Replace(c_Tags[i].text, m_UserDatas[c_Tags[i].opt1].data);
					break;
				}
			}

			// ユーザーデータリセット.
			ClearUserData();

			return ret;
		}


		public void SetUserData( int index, string text ) {
			m_UserDatas[index].Set(text);
		}

		public void SetUserData<T>( int index, T dat ) where T : struct {
			m_UserDatas[index].Set( dat.ToString() );
		}

		public void ClearUserData() {
			for ( int i = 0; i < m_UserDatas.Length; ++i ) {
				m_UserDatas[i].Clear();
			}
		}

		public eLanguage GetLanguageCode() {
			return m_Lang;
		}
	}
}
