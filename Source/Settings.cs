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

using KSP.IO;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {
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
		public static bool KIS_Present
		{
			get;
			private set;
		}
		public static bool B9Wings_Present
		{
			get;
			private set;
		}
		public static bool FAR_Present
		{
			get;
			private set;
		}
		public static bool use_KAC
		{
			get;
			private set;
		}
		public static KACWrapper.KACAPI.AlarmActionEnum KACAction
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

		static string version = null;
		static Rect windowpos;
		private static bool gui_enabled;
		private static string[] alarmactions = new string[] {
			"Kill Warp+Message",
			"Kill Warp only",
			"Message Only",
			"Pause Game"
		};

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
			if (!settings.HasValue ("UseKAC")) {
				var val = use_KAC;
				settings.AddValue ("UseKAC", val);
			}
			if (!settings.HasValue ("KACAction")) {
				var val = KACAction.ToString();
				settings.AddValue ("KACAction", val);
			}

			var uks = settings.GetValue ("UseKAC");
			bool uk = true;
			bool.TryParse (uks, out uk);
			use_KAC = uk;

			string str = settings.GetValue ("KACAction");
			switch (str){
			case ("KillWarp"):
				KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
				break;
			case ("KillWarpOnly"):
				KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
				break;
			case ("MessageOnly"):
				KACAction = KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
				break;
			case ("PauseGame"):
				KACAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
				break;
			default:
				KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
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

			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				enabled = true;
			}
		}

		public override void OnSave(ConfigNode config)
		{
			//Debug.Log (String.Format ("[EL] Settings save: {0}", config));
			var settings = new ConfigNode ("Settings");

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
			use_KAC = true;
			KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
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
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				case ("KillWarpOnly"):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
					break;
				case ("MessageOnly"):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
					break;
				case ("PauseGame"):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
					break;
				default:
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				};
			}
		}
		
		public override void OnAwake ()
		{
			KIS_Present = KIS.KISWrapper.Initialize ();
			B9Wings_Present = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("B9_Aerospace_WingStuff", StringComparison.InvariantCultureIgnoreCase));
			FAR_Present = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("FerramAerospaceResearch", StringComparison.InvariantCultureIgnoreCase));
			LoadGlobalSettings ();

			enabled = false;
		}

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
		}

		void WindowGUI (int windowID)
		{
			GUILayout.BeginVertical ();

			bool uk = use_KAC;
			uk = GUILayout.Toggle (uk, "Create alarms in Kerbal Alarm Clock");
			use_KAC = uk;

			if (uk) {
				int actionint;
				switch (KACAction){
				case (KACWrapper.KACAPI.AlarmActionEnum.KillWarp):
					actionint = 0;
					break;
				case (KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly):
					actionint = 1;
					break;
				case (KACWrapper.KACAPI.AlarmActionEnum.MessageOnly):
					actionint = 2;
					break;
				case (KACWrapper.KACAPI.AlarmActionEnum.PauseGame):
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
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				case (1):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
					break;
				case (2):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
					break;
				case (3):
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
					break;
				default:
					KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
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
					string ver = ExtraplanetaryLaunchpadsVersionReport.GetVersion ();
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
