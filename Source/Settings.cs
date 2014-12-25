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
		public static string GetVersion ()
		{
			if (version != null) {
				return version;
			}

			var asm = Assembly.GetCallingAssembly ();
			version =  SystemUtils.GetAssemblyVersionString (asm);

			return version;
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
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					//XXX enable again when there are settings to tweak.
					//enabled = true;
				}
			}

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
			}
		}

		public override void OnSave(ConfigNode config)
		{
			//Debug.Log (String.Format ("[EL] Settings save: {0}", config));
			var settings = new ConfigNode ("Settings");

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
		}
		
		public override void OnAwake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				enabled = false;
				return;
			}
			LoadGlobalSettings ();

			enabled = false;
		}

		void WindowGUI (int windowID)
		{
			GUILayout.BeginVertical ();

			if (GUILayout.Button ("OK")) {
				enabled = false;
				InputLockManager.RemoveControlLock ("EL_Settings_window_lock");
			}
			GUILayout.EndVertical ();
			GUI.DragWindow (new Rect (0, 0, 10000, 20));
		}

		void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;

			string name = "Extraplanetary Launchpad";
			string ver = ExSettings.GetVersion ();
			if (windowpos.x == 0) {
				windowpos = new Rect (Screen.width / 2 - 250,
								  Screen.height / 2 - 30, 0, 0);
			}
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  name + " " + ver,
										  GUILayout.Width (500));
			if (enabled && windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
				InputLockManager.SetControlLock ("EL_Settings_window_lock");
			} else {
				InputLockManager.RemoveControlLock ("EL_Settings_window_lock");
			}
		}
	}
}
