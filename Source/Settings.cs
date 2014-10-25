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
			GameScenes.SPH,
		})
	]
	public class ExSettings : ScenarioModule
	{
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
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					enabled = true;
				}
			}
			if (!settings.HasValue ("ForceResourceUse")) {
				var val = force_resource_use;
				settings.AddValue ("ForceResourceUse", val);
			}
			if (!settings.HasValue ("TimedBuilds")) {
				var val = timed_builds;
				settings.AddValue ("TimedBuilds", val);
			}

			var frus = settings.GetValue ("ForceResourceUse");
			bool fru = false;
			bool.TryParse (frus, out fru);
			force_resource_use = fru;

			var tbs = settings.GetValue ("TimedBuilds");
			bool tb = true;;
			bool.TryParse (tbs, out tb);
			timed_builds = tb;

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

			bool fru = force_resource_use;
			settings.AddValue ("ForceResourceUse", fru);

			bool tb = timed_builds;
			settings.AddValue ("TimedBuilds", tb);

			config.AddNode (settings);

			ExShipInfo.SaveSettings (settings.AddNode ("ShipInfo"));
			ExBuildWindow.SaveSettings (settings.AddNode ("BuildWindow"));
		}

		void LoadGlobalSettings ()
		{
			HullRecycleTarget = "Metal";
			KerbalRecycleTarget = "Kethane";
			KerbalRecycleAmount = 150.0;
			AlwaysForceResourceUsage = false;
			var dbase = GameDatabase.Instance;
			var settings = dbase.GetConfigNode ("ELGlobalSettings");

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

			if (GUILayout.Button ("OK")) {
				enabled = false;
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
		}
	}
}
