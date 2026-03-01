using Elin_Mod.Lib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static UIList;

namespace Elin_Mod
{
	/// <summary>
	/// スライダーをキー操作するやつ.
	/// </summary>
	class UISliderKeyMover : MonoBehaviour
	{
		Slider m_Slider;
		bool m_IsEntered;
		float m_IncrementsValue;

		public void Setup( Slider owner, float incrementValue ) {
			m_Slider = owner;
			m_IncrementsValue = incrementValue;
			var evTrigger = owner.gameObject.GetComponent<EventTrigger>();
			if (evTrigger == null) {
				evTrigger = owner.GetOrCreate<EventTrigger>();
			}
			var evEntry = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
			evEntry.callback.AddListener((v) => { m_IsEntered = true; });

			var evExit = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
			evExit.callback.AddListener((v) => { m_IsEntered = false; });

			var evCncel = new EventTrigger.Entry() { eventID = EventTriggerType.Cancel };
			evCncel.callback.AddListener((v) => { m_IsEntered = false; });

			var evEnd = new EventTrigger.Entry() { eventID = EventTriggerType.Submit };
			evEnd.callback.AddListener((v) => { m_IsEntered = false; });

			evTrigger.triggers.Add(evEntry);
			evTrigger.triggers.Add(evExit);
			evTrigger.triggers.Add(evCncel);
			evTrigger.triggers.Add(evEnd);
		}


		void Update() {
			if (!m_IsEntered)
				return;

			float addV = 0.0f;
			float v = m_Slider.value;
			var input = ModInput.Instance;
			if (input.GetKeyRepeat(KeyCode.LeftArrow) || input.GetKeyRepeat(KeyCode.A))
				addV -= m_IncrementsValue;
			if (input.GetKeyRepeat(KeyCode.RightArrow) || input.GetKeyRepeat(KeyCode.D))
				addV += m_IncrementsValue;
			if (input.GetKey(KeyCode.LeftShift) || input.GetKey(KeyCode.RightShift))
				addV *= 10.0f;
			v += addV;
			m_Slider.value = Mathf.Clamp(v, m_Slider.minValue, m_Slider.maxValue);
		}
	}
}
