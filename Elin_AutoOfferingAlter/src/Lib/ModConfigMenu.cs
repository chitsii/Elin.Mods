using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace Elin_Mod
{
	[HarmonyPatch]
	public class ModConfigMenu : Singleton<ModConfigMenu>
	{
		const string c_ModConfigMenuTitle = "ItukiYu_ModConfigs";
		public class MenuInfo
		{
			public string m_TabName;
			public List<System.Action<UIContextMenu>> m_Menus;
		}

		
		bool m_IsInitialized;
		List<MenuInfo> m_Menues = new List<MenuInfo>();

		

		public void AddMenu(MenuInfo menu) {
			if (m_Menues == null)
				m_Menues = new List<MenuInfo>();

			if (m_Menues.Find(v => v.m_TabName == menu.m_TabName) != null)
				return;
			m_Menues.Add(menu);
		}

		
		public void OnCallback_AddMenu( object arg ) {
			if (m_Menues == null)
				return;
			var menu = arg as UIContextMenu;

			for ( int i = 0; i < m_Menues.Count; ++i) {
				var childInfo = m_Menues[i];
				var child = menu.AddChild();
				child.popper.textName.text = childInfo.m_TabName;
				for ( int j = 0; j < childInfo.m_Menus.Count; ++j ) {
					childInfo.m_Menus[j](child);
				}
				child.hideOnMouseLeave = false;
			}
		}



		void _Initialize(ActPlan actPlan) {
			if (m_IsInitialized)
				return;

			m_IsInitialized = true;

		}

		[HarmonyPatch(typeof(ActPlan), "ShowContextMenu")]
		[HarmonyPrefix]
		static void _Prefix_ShowContextMenu(ActPlan __instance) {
			if (!__instance.pos.Equals(EClass.pc.pos)) {
				return;
			}

			// 既にアクションが登録されていたらスキップ.
			var actRoot = __instance.list.Find(
				(v) => {
					var dAct = (v.act as DynamicAct);
					if (dAct == null)
						return false;
					return dAct.id == c_ModConfigMenuTitle;
				}
			)?.act as DynamicAct;


			if (actRoot != null)
				return;

			actRoot = new DynamicAct(c_ModConfigMenuTitle, () => {
				var menu = GameUtil.CreateContextMenu();
				menu.name = c_ModConfigMenuTitle;
				menu.hideOnMouseLeave = false;
				CommonUtil.SendMessageForAllMod("_ModConfigMenu_OnAddCallback", menu);
				menu.Show();
				menu.hideOnMouseLeave = false;
				return false;
			}, false);
			((List<ActPlan.Item>)(object)__instance.list).Add(new ActPlan.Item {
				act = (Act)(object)actRoot
			});
		}

		[HarmonyPatch(typeof(ActPlan.Item), "Perform")]
		[HarmonyPrefix]
		static bool _Prefix_Perform(ActPlan.Item __instance) {
			var val = __instance.act as DynamicAct;
			if (val != null && val.id == c_ModConfigMenuTitle) {
				((Act)val).Perform();
				return false;
			}
			return true;
		}


	}
}
