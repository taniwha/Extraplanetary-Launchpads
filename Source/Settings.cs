/*
This file is part of Extraplanetary Launchpads.

Extraplanetary Launchpads is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Extraplanetary Launchpads is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Extraplanetary Launchpads.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSPAPIExtensions;

using KSP.IO;

namespace ExLP {
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] {
			GameScenes.SPACECENTER,
			GameScenes.EDITOR,
			GameScenes.FLIGHT,
			GameScenes.TRACKSTATION,
		})
	]
	public class ExSettings : ScenarioModule
	{
		static bool settings_loaded;
		static bool kethane_checked;
		public static bool kethane_present
		{
			get;
			private set;
		}
		public static bool force_resource_use
		{
			get;
			private set;
		}
		public static bool timed_builds
		{
			get;
			private set;
		}
		public static bool use_KAC
		{
			get;
			private set;
		}
		public static ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum KACAction
		{
			get;
			private set;
		}
		public static string HullRecycleTarget
		{
			get;
			private set;
		}
		public static string KerbalRecycleTarget
		{
			get;
			private set;
		}
		public static double KerbalRecycleAmount
		{
			get;
			private set;
		}
		public static bool AlwaysForceResourceUsage
		{
			get;
			private set;
		}

		static string version = null;
		static Rect windowpos;
		private static bool gui_enabled;
		private static string[] alarmactions = new string[] {"Kill Warp+Message", "Kill Warp only", "Message Only", "Pause Game"};
		public static string GetVersion ()
		{
			if (version != null) {
				return version;
			}

			var asm = Assembly.GetCallingAssembly ();
			version =  SystemUtils.GetAssemblyVersionString (asm);

			return version;
		}

		internal static bool CheckForKethane ()
		{
			if (AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name == "Kethane")) {
				Debug.Log ("[EL] Kethane found");
				return true;
			}
			Debug.Log ("[EL] Kethane not found");
			return false;
		}

		public static ExSettings current
		{
			get {
				var game = HighLogic.CurrentGame;
				return game.scenarios.Select (s => s.moduleRef).OfType<ExSettings> ().SingleOrDefault ();

			}
		}

		public override void OnLoad (ConfigNode config)
		{
			//Debug.Log (String.Format ("[EL] Settings load"));
			var settings = config.GetNode ("Settings");
			if (settings == null) {
				settings = new ConfigNode ("Settings");
				gui_enabled = true; // Show settings window on first startup
			}
			if (!settings.HasValue ("ForceResourceUse")) {
				var val = force_resource_use;
				settings.AddValue ("ForceResourceUse", val);
			}
			if (!settings.HasValue ("TimedBuilds")) {
				var val = timed_builds;
				settings.AddValue ("TimedBuilds", val);
			}
			if (!settings.HasValue ("UseKAC")) {
				var val = use_KAC;
				settings.AddValue ("UseKAC", val);
			}
			if (!settings.HasValue ("KACAction")) {
				var val = KACAction.ToString();
				settings.AddValue ("KACAction", val);
			}

			var frus = settings.GetValue ("ForceResourceUse");
			bool fru = false;
			bool.TryParse (frus, out fru);
			force_resource_use = fru;

			var tbs = settings.GetValue ("TimedBuilds");
			bool tb = true;
			bool.TryParse (tbs, out tb);
			timed_builds = tb;

			var uks = settings.GetValue ("UseKAC");
			bool uk = true;
			bool.TryParse (uks, out uk);
			use_KAC = uk;

			string str = settings.GetValue ("KACAction");
			switch (str){
			case ("KillWarp"):
				KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
				break;
			case ("KillWarpOnly"):
				KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
				break;
			case ("MessageOnly"):
				KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
				break;
			case ("PauseGame"):
				KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
				break;
			default:
				KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
				break;
			};

			if (settings.HasNode ("ShipInfo")) {
				var node = settings.GetNode ("ShipInfo");
				ExShipInfo.LoadSettings (node);
			}

			if (settings.HasNode ("BuildWindow")) {
				var node = settings.GetNode ("BuildWindow");
				ExBuildWindow.LoadSettings (node);
			}

			if (CompatibilityChecker.IsWin64 ()) {
				enabled = false;
			} else {
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					enabled = true;
				}
			}
		}

		public override void OnSave(ConfigNode config)
		{
			//Debug.Log (String.Format ("[EL] Settings save: {0}", config));
			var settings = new ConfigNode ("Settings");

			bool fru = force_resource_use;
			settings.AddValue ("ForceResourceUse", fru);

			bool tb = timed_builds;
			settings.AddValue ("TimedBuilds", tb);

			bool uk = use_KAC;
			settings.AddValue ("UseKAC", uk);

			string ka = KACAction.ToString ();
			settings.AddValue ("KACAction", ka);

			config.AddNode (settings);

			ExShipInfo.SaveSettings (settings.AddNode ("ShipInfo"));
			ExBuildWindow.SaveSettings (settings.AddNode ("BuildWindow"));
		}

		void LoadGlobalSettings ()
		{
			if (settings_loaded) {
				return;
			}
			settings_loaded = true;
			HullRecycleTarget = "Metal";
			KerbalRecycleTarget = "Kethane";
			KerbalRecycleAmount = 150.0;
			AlwaysForceResourceUsage = false;
			force_resource_use = true;
			timed_builds = true;
			use_KAC = true;
			KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
			var dbase = GameDatabase.Instance;
			var settings = dbase.GetConfigNodes ("ELGlobalSettings").LastOrDefault ();

			if (settings == null) {
				return;
			}
			if (settings.HasValue ("HullRecycleTarget")) {
				string val = settings.GetValue ("HullRecycleTarget");
				HullRecycleTarget = val;
			}
			if (settings.HasValue ("KerbalRecycleTarget")) {
				string val = settings.GetValue ("KerbalRecycleTarget");
				KerbalRecycleTarget = val;
			}
			if (settings.HasValue ("KerbalRecycleAmount")) {
				string val = settings.GetValue ("KerbalRecycleAmount");
				double kra;
				double.TryParse (val, out kra);
				KerbalRecycleAmount = kra;
			}
			if (settings.HasValue ("AlwaysForceResourceUsage")) {
				string val = settings.GetValue ("AlwaysForceResourceUsage");
				bool afru;
				bool.TryParse (val, out afru);
				AlwaysForceResourceUsage = afru;
			}
			if (settings.HasValue ("ForceResourceUse")) {
				string str = settings.GetValue ("ForceResourceUse");
				bool val;
				if (bool.TryParse (str, out val)) {
					force_resource_use = val;
				}
			}
			if (settings.HasValue ("TimedBuilds")) {
				string str = settings.GetValue ("TimedBuilds");
				bool val;
				if (bool.TryParse (str, out val)) {
					timed_builds = val;
				}
			}
			if (settings.HasValue ("UseKAC")) {
				string str = settings.GetValue ("UseKAC");
				bool val;
				if (bool.TryParse (str, out val)) {
					use_KAC = val;
				}
			}
			if (settings.HasValue ("KACAction")) {
				string str = settings.GetValue ("KACAction");
				switch (str){
				case ("KillWarp"):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				case ("KillWarpOnly"):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
					break;
				case ("MessageOnly"):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
					break;
				case ("PauseGame"):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
					break;
				default:
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				};
			}
		}
		
		public override void OnAwake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				enabled = false;
				return;
			}
			LoadGlobalSettings ();
			if (!kethane_checked) {
				kethane_present = CheckForKethane ();
				kethane_checked = true;
			}

			enabled = false;
		}

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
		}

		void WindowGUI (int windowID)
		{
			GUILayout.BeginVertical ();

			if (!AlwaysForceResourceUsage && !kethane_present
				&& HighLogic.CurrentGame.Mode != Game.Modes.CAREER) {
				bool fru = force_resource_use;
				fru = GUILayout.Toggle (fru, "Always use resources");
				force_resource_use = fru;
			}

			bool tb = timed_builds;
			tb = GUILayout.Toggle (tb, "Allow progressive builds");
			timed_builds = tb;

			bool uk = use_KAC;
			uk = GUILayout.Toggle (uk, "Create alarms in Kerbal Alarm Clock");
			use_KAC = uk;

			if (uk) {
				int actionint;
				switch (KACAction){
				case (ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp):
					actionint = 0;
					break;
				case (ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly):
					actionint = 1;
					break;
				case (ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.MessageOnly):
					actionint = 2;
					break;
				case (ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.PauseGame):
					actionint = 3;
					break;
				default:
					actionint = 0;
					break;
				};

				//GUIStyle gridStyle = new GUIStyle ();

				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Alarm type: ");
				actionint = GUILayout.SelectionGrid (actionint, alarmactions, 2);
				GUILayout.EndHorizontal ();

				switch (actionint){
				case (0):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				case (1):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
					break;
				case (2):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
					break;
				case (3):
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
					break;
				default:
					KACAction = ExLP_KACWrapper.KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				};

			}

			if (GUILayout.Button ("OK")) {
				gui_enabled = false;
				InputLockManager.RemoveControlLock ("EL_Settings_window_lock");
			}
			GUILayout.EndVertical ();
			GUI.DragWindow (new Rect (0, 0, 10000, 20));
		}

		void OnGUI ()
		{
			if (enabled) { // don't do any work at all unless we're enabled
				if (gui_enabled) { // don't create windows unless we're going to show them
					GUI.skin = HighLogic.Skin;
					if (windowpos.x == 0) {
						windowpos = new Rect (Screen.width / 2 - 250,
							Screen.height / 2 - 30, 0, 0);
					}
					string name = "Extraplanetary Launchpad";
					string ver = ExSettings.GetVersion ();
					windowpos = GUILayout.Window (GetInstanceID (),
						windowpos, WindowGUI,
						name + " " + ver,
						GUILayout.Width (500));
					if (windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
						InputLockManager.SetControlLock ("EL_Settings_window_lock");
					} else {
						InputLockManager.RemoveControlLock ("EL_Settings_window_lock");
					}
				} else {
					InputLockManager.RemoveControlLock ("EL_Settings_window_lock");
				}
			}
		}
	}
}
