using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine.Windows;

namespace Elin_Mod
{
	[HarmonyPatch]
	/// <summary>
	/// Mod管理クラス.
	/// </summary>
	public class MyModManager : Singleton<MyModManager>
	{
		public enum eState {
			None,
			Initializing,
			Idle,
		}

		BaseUnityPlugin m_Plugin;
		string m_ModName;
		string m_ModFullName;
		string m_ModVersion;
		Harmony m_Harmony;

		eState m_State;
		ModConfigBase m_Config;

		System.Action m_OnLoadTable;
		System.Action m_OnLoadTableAfter;
		System.Action m_OnStartGame;
		


		/// <summary>
		/// 初期化.
		/// </summary>
		/// <typeparam name="ModConfigType"></typeparam>
		/// <param name="plugin"></param>
		/// <param name="logger"></param>
		/// <param name="modFullName"></param>
		/// <param name="modName"></param>
		/// <param name="modVersion"></param>
		public void Initialize<
			ModConfigType
		>(
			BaseUnityPlugin plugin, 
			BepInEx.Logging.ManualLogSource logger, 
			string modFullName, 
			string modName, 
			string modVersion
		) 
			where ModConfigType : ModConfigBase, new()
		{
			m_State = eState.Initializing;
			m_Plugin = plugin;
			m_ModName = modName;
			m_ModFullName = modFullName;
			m_ModVersion = modVersion;

			DebugUtil.Initialize(logger);
			CommonUtil.Initialize(plugin.Info);
			ModTextManager.Instance.Initialize();

			m_Config = new ModConfigType();
			m_Config.Initialize(plugin.Config);

			m_Harmony = new Harmony(modFullName);
			m_Harmony.PatchAll();
		}

		/// <summary>
		/// 終了処理.
		/// </summary>
		public void Terminate() {
			m_Harmony?.UnpatchSelf();
			m_Harmony = null;
			ModTextManager.Instance.Terminate();
			ModTextManager.DeleteInstance();
		}

		/// <summary>
		/// コンフィグ取得.
		/// </summary>
		/// <returns></returns>
		public ModConfigBase GetConfig() {
			return m_Config;
		}

		/// <summary>
		/// HarmonyPatch動的追加.
		/// </summary>
		/// <param name="info"></param>
		public void AddPatch( ModPatchInfo info ) {
			info.Patch(m_Harmony);
		}

		/// <summary>
		/// HarmonyPatch動的削除.
		/// </summary>
		/// <param name="info"></param>
		public void RemovePatch(ModPatchInfo info ) {
			info.Unpatch(m_Harmony);
		}


		/// <summary>
		/// ゲーム開始時コールバック登録.
		/// </summary>
		/// <param name="onStartGame"></param>
		public void RegisterOnStartGameAction( System.Action onStartGame ) {
			m_OnStartGame += onStartGame;
		}

		/// <summary>
		/// テーブル読み込み開始前コールバック登録.
		/// </summary>
		/// <param name="onLoadTable"></param>
		public void RegisterOnLoadTableAction(System.Action onLoadTable) {
			m_OnLoadTable += onLoadTable;
		}

		/// <summary>
		/// テーブル読み込み開始前コールバック登録.
		/// </summary>
		/// <param name="onLoadTable"></param>
		public void RegisterOnLoadTableAfterAction(System.Action onLoadTable) {
			m_OnLoadTableAfter += onLoadTable;
		}

		// 以下コールバック群....
		//------
		[HarmonyPatch(typeof(SourceManager), "Init")]
		[HarmonyPrefix]
		static void PreFix_SourceManagerInit() {
			switch (Instance.m_State) {
				case eState.Initializing:
					Instance.m_OnLoadTable?.Invoke();
					break;
			}
		}

		[HarmonyPatch(typeof(SourceManager), "Init")]
		[HarmonyPostfix]
		static void PostFix_SourceManagerInit() {
			switch (Instance.m_State) {
				case eState.Initializing:
					Instance.m_OnLoadTableAfter?.Invoke();
					break;
			}
		}

		[HarmonyPatch(typeof(Scene), "Init")]
		[HarmonyPostfix]
		static void PostFix_OnScebeInit( Scene.Mode newMode) {
			if (newMode != Scene.Mode.StartGame)
				return;
			Instance.m_OnStartGame?.Invoke();
			Instance.m_State = eState.Idle;
		}
	}
}
