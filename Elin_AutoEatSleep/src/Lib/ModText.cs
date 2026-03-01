using System;

namespace Elin_Mod
{
	public class ModText : SourceLang<ModText.Row>
	{
		[Serializable]
		public class Row : LangRow
		{
			public eTextID textID;

			public string text_CN;

			public string text_ZHTW;

			public string text_KR;
		}

		private (int, eTextID)[] m_TextIDs;

		private (int, eLanguage)[] m_Languages;

		private Row[] m_Rows;

		public void Setup()
		{
			Array langs = Enum.GetValues( typeof( eLanguage ) );
			m_Languages = new (int, eLanguage)[ langs.Length ];
			for (int i = 0; i < langs.Length; i++)
			{
				eLanguage lang = (eLanguage)langs.GetValue( i );
				m_Languages[ i ] = (lang.ToString().GetHashCode(), lang);
			}
			Array textIDs = Enum.GetValues( typeof( eTextID ) );
			m_TextIDs = new (int, eTextID)[ textIDs.Length ];
			for (int j = 0; j < textIDs.Length; j++)
			{
				eTextID id = (eTextID)textIDs.GetValue( j );
				m_TextIDs[ j ] = (id.ToString().GetHashCode(), id);
			}
			m_Rows = new Row[ m_TextIDs.Length ];

		
		}

		public override Row CreateRow()
		{
			Row ret = new Row
			{
				id = SourceData.GetString( 0 ),
				text_JP = SourceData.GetString( 1 ),
				text = SourceData.GetString( 2 ),
				text_CN = SourceData.GetString( 3 ),
				text_ZHTW = SourceData.GetString( 4 ),
				text_KR = SourceData.GetString(5),
			};
			int hash = ret.id.GetHashCode();
			int idx = Array.FindIndex( m_TextIDs, ( v ) => v.Item1 == hash );
			if (idx < 0)
			{
				DebugUtil.LogError( "[ModText] error!!! invalid id --> " + ret.id );
			} else
			{
				ret.textID = m_TextIDs[ idx ].Item2;
			}
			return ret;
		}

		public override void SetRow( Row r )
		{
			m_Rows[ (int)r.textID ] = r;

#if false
			// Generalにもつっこむ.
			// ただし日本語と英語のみ.
			var general = EClass.sources.langGeneral;
			var generalRow = new LangGeneral.Row();
			generalRow.id = r.id;
			generalRow.text = r.text;
			generalRow.text_JP = r.text_JP;
			generalRow.text_L = r.text_L;
			generalRow._index = general.rows.Count;
			general.rows.Add(generalRow);
			general.SetRow(generalRow);
#endif
		}


		public string GetText( eTextID id )
		{
			string ret = null;
			Row row = m_Rows[ (int)id ];
			switch ( GetLanguageCode() )
			{
			case eLanguage.JP: ret = row.text_JP; break;
			case eLanguage.CN: ret = row.text_CN; break;
			case eLanguage.ZHTW: ret = row.text_ZHTW; break;
			case eLanguage.KR: ret = row.text_KR; break;
			default: ret = row.text; break;
			};
			if (string.IsNullOrEmpty(ret))
				ret = row.text;
			return ret;
		}

		public eLanguage GetLanguageCode()
		{
			eLanguage code = eLanguage.EN;
			int langHash = EClass.core.config.lang.GetHashCode();
			for (int i = 0; i < m_Languages.Length; i++)
			{
				if (m_Languages[ i ].Item1 != langHash)
					continue;
				code = m_Languages[ i ].Item2;
				break;
			}
			return code;
		}

		public Row[] GetRows()
		{
			return m_Rows;
		}
	}

}