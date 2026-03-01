using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


namespace Elin_Mod.Lib
{
	/// <summary>
	/// キー入力.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	class ModInput : MonoSingleton<ModInput>
	{
		// リピートキー入力のためだけに作成.
		
		const float c_Time_RepeatStart = 0.5f;
		const float c_Time_RepeatInterval = 0.1f;

		class Key {
			public enum eType {
				Key,
				Mouse,
			}

			public enum eState {
				None,
				Push,
				Press,
				Release,
			}

			eState m_State;
			eType m_Type;
			KeyCode m_KeyCode;
			int m_MouseButtonIndex;

			float m_PressTime;
			bool m_IsRepeat;
			float m_RepeatTime;
			bool m_IsPush;

			bool m_IsTrgRepeat;

			public Key( KeyCode code ) {
				m_KeyCode = code;
				m_Type = eType.Key;
			}

			public Key(int mouseButtonIndex) {
				m_MouseButtonIndex = mouseButtonIndex;
				m_Type = eType.Mouse;
			}


			public void Update( float delta ) {
				bool isPrevPush = m_IsPush;
				switch ( m_Type) {
					case eType.Key:
						m_IsPush = Input.GetKey(m_KeyCode);
						break;
					case eType.Mouse:
						m_IsPush = Input.GetMouseButton(m_MouseButtonIndex);
						break;
				}
				
				if ( m_IsPush ) {
					if (isPrevPush)
						m_State = eState.Push;
					else
						m_State = eState.Press;
				}
				else {
					if (isPrevPush)
						m_State = eState.Release;
					else
						m_State = eState.None;
				}

				if ( m_IsPush )
					m_PressTime += delta;
				else 
					m_PressTime = 0;


				if ( m_IsRepeat) {
					m_IsTrgRepeat = false;
					if (m_PressTime <= 0.0f ) {
						m_IsRepeat = false;
						m_RepeatTime = 0.0f;
					} else {
						m_RepeatTime += delta;
						if (m_RepeatTime > c_Time_RepeatInterval ) {
							m_RepeatTime = 0.0f;
							m_IsTrgRepeat = true;
						}
					}
				} else {
					if ( m_PressTime > c_Time_RepeatStart ) {
						m_IsRepeat = true;
						m_RepeatTime = 0.0f;
					}
				}
			}

			public bool IsTriggerRepeat() {
				return m_IsTrgRepeat || m_State == eState.Press;
			}

			public eState GetState() {
				return m_State;
			}

			public eType GetKeyType() {
				return m_Type;
			}

			public bool IsEqual( int mouseButton ) {
				if (m_Type != eType.Mouse)
					return false;
				if (m_MouseButtonIndex != mouseButton)
					return false;
				return true;
			}

			public bool IsEqual(KeyCode key) {
				if (m_Type != eType.Key)
					return false;
				if (m_KeyCode != key)
					return false;
				return true;
			}
		}


		List<Key> m_Keys = new List<Key>();

		void Awake() {
			m_Keys = new List<Key>();
		}


		void  Update() {
			float delta = Time.deltaTime;
			foreach ( var itr in m_Keys ) {
				itr.Update(delta);
			}
		}


		Key _GetOrCreateKey(int mouseButton) {
			foreach (var item in m_Keys) {
				if (!item.IsEqual(mouseButton))
					continue;
				return item;
			}
			Key ret = new Key(mouseButton);
			m_Keys.Add(ret);
			return ret;
		}

		Key _GetOrCreateKey(KeyCode key) {
			foreach (var item in m_Keys) {
				if (!item.IsEqual(key))
					continue;
				return item;
			}
			Key ret = new Key(key);
			m_Keys.Add(ret);
			return ret;
		}


		public bool GetMouseDown(int button) { return _GetOrCreateKey(button).GetState() == Key.eState.Press; }
		public bool GetMouseUp(int button) { return _GetOrCreateKey(button).GetState() == Key.eState.Release; }
		public bool GetMouse(int button) { return _GetOrCreateKey(button).GetState() == Key.eState.Push; }
		public bool GetMouseRepeat(int button) { return _GetOrCreateKey(button).IsTriggerRepeat(); }

		public bool GetKeyDown(KeyCode keyCode) { return _GetOrCreateKey(keyCode).GetState() == Key.eState.Press; }

		public bool GetKeyUp(KeyCode keyCode) { return _GetOrCreateKey(keyCode).GetState() == Key.eState.Release; }

		public bool GetKey(KeyCode keyCode) { return _GetOrCreateKey(keyCode).GetState() == Key.eState.Push; }
		
		public bool GetKeyRepeat(KeyCode keyCode) { return _GetOrCreateKey(keyCode).IsTriggerRepeat(); }

		public bool GetKeyAny() {
			return Input.anyKey;
		}
		public bool GetKeyAnyDown() {
			return Input.anyKeyDown;
		}

	}
}
