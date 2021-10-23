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
using System.IO;
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
	public class ELSettings : ScenarioModule
	{
		public static bool KAS_Present
		{
			get;
			private set;
		}
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
			internal set;
		}
		public static KACWrapper.KACAPI.AlarmActionEnum KACAction
		{
			get;
			internal set;
		}
		public static bool PreferBlizzy
		{
			get;
			internal set;
		}
		public static bool ShowCraftHull
		{
			get;
			internal set;
		}
		public static bool DebugCraftHull
		{
			get;
			internal set;
		}

		public static ELSettings current
		{
			get {
				var game = HighLogic.CurrentGame;
				return game.scenarios.Select (s => s.moduleRef).OfType<ELSettings> ().SingleOrDefault ();
			}
		}

		static void ParseUseKAC (ConfigNode settings)
		{
			if (!settings.HasValue ("UseKAC")) {
				var val = use_KAC;
				settings.AddValue ("UseKAC", val);
			}

			var uks = settings.GetValue ("UseKAC");
			bool uk = true;
			bool.TryParse (uks, out uk);
			use_KAC = uk;
		}

		static void ParseKACAction (ConfigNode settings)
		{
			if (!settings.HasValue ("KACAction")) {
				var val = KACAction.ToString();
				settings.AddValue ("KACAction", val);
			}

			string str = settings.GetValue ("KACAction");
			switch (str) {
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

		static void UpdateToolbarButton ()
		{
			ELAppButton.UpdateVisibility ();
			if (ELToolbar_SettingsWindow.Instance != null) {
				ELToolbar_SettingsWindow.Instance.UpdateVisibility ();
			}
		}

		static void ParsePreferBlizzy (ConfigNode settings)
		{
			if (!settings.HasValue ("PreferBlizzy")) {
				var val = PreferBlizzy.ToString();
				settings.AddValue ("PreferBlizzy", val);
			}

			string str = settings.GetValue ("PreferBlizzy");
			bool bval;
			if (bool.TryParse (str, out bval)) {
				PreferBlizzy = bval;
			}
			UpdateToolbarButton ();
		}

		static void ParseShowCraftHull (ConfigNode settings)
		{
			if (!settings.HasValue ("ShowCraftHull")) {
				var val = ShowCraftHull.ToString();
				settings.AddValue ("ShowCraftHull", val);
			}

			string str = settings.GetValue ("ShowCraftHull");
			bool bval;
			if (bool.TryParse (str, out bval)) {
				ShowCraftHull = bval;
			}
			UpdateToolbarButton ();
		}

		static void ParseDebugCraftHull (ConfigNode settings)
		{
			if (!settings.HasValue ("DebugCraftHull")) {
				var val = DebugCraftHull.ToString();
				settings.AddValue ("DebugCraftHull", val);
			}

			string str = settings.GetValue ("DebugCraftHull");
			bool bval;
			if (bool.TryParse (str, out bval)) {
				DebugCraftHull = bval;
			}
			UpdateToolbarButton ();
		}

		static void ParseWindowManager (ConfigNode settings)
		{
			if (settings.HasNode ("WindowManager")) {
				var node = settings.GetNode ("WindowManager");
				ELWindowManager.LoadSettings (node);
			}
		}

		public static void Save ()
		{
			//Debug.Log (String.Format ("[EL] Settings save: {0}", config));
			var settings = new ConfigNode ("Settings");

			settings.AddValue ("UseKAC", use_KAC);
			settings.AddValue ("KACAction", KACAction.ToString ());
			settings.AddValue ("PreferBlizzy", PreferBlizzy);
			settings.AddValue ("ShowCraftHull", ShowCraftHull);
			settings.AddValue ("DebugCraftHull", DebugCraftHull);

			ELWindowManager.SaveSettings (settings.AddNode ("WindowManager"));

			string path = DataPath + "/" + "Settings.cfg";
			Directory.CreateDirectory (DataPath);
			settings.Save (path);
		}

		public override void OnSave(ConfigNode config)
		{
			Save ();
		}

		public static string DataPath { get; private set; }

		public static void Load ()
		{
			use_KAC = true;
			KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;

			string path = DataPath + "/" + "Settings.cfg";
			ConfigNode settings = ConfigNode.Load (path);

			if (settings == null) {
				Save ();
				return;
			}
			ParseUseKAC (settings);
			ParseKACAction (settings);
			ParsePreferBlizzy (settings);
			ParseShowCraftHull (settings);
			ParseDebugCraftHull (settings);
			ParseWindowManager (settings);
		}
		
		public override void OnAwake ()
		{
			DataPath = AssemblyLoader.loadedAssemblies.GetPathByType (typeof (ELSettings));
			GameObject.DontDestroyOnLoad(this);

			KAS_Present = KAS.KASWrapper.Initialize ();
			KIS_Present = KIS.KISWrapper.Initialize ();
			B9Wings_Present = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("B9_Aerospace_WingStuff", StringComparison.InvariantCultureIgnoreCase));
			FAR_Present = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("FerramAerospaceResearch", StringComparison.InvariantCultureIgnoreCase));
			Load ();

			enabled = false;
		}

		public static void ToggleGUI ()
		{
			ELWindowManager.ToggleSettingsWindow ();
		}
	}
}
