using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {
	public class ExSettings : ScenarioModule
	{
		public static ExSettings current
		{
			get {
				var game = HighLogic.CurrentGame;
				return game.scenarios.Select (s => s.moduleRef).OfType<ExSettings> ().SingleOrDefault ();
				
			}
		}

		public static void CreateSettings (Game game)
		{
			if (!game.scenarios.Any (p => p.moduleName == typeof (ExSettings).Name)) {
				//Debug.Log (String.Format ("[EL] Settings create"));
				var proto = game.AddProtoScenarioModule (typeof (ExSettings), GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.SPH, GameScenes.TRACKSTATION, GameScenes.FLIGHT);
				proto.Load (ScenarioRunner.fetch);
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
				var val = ExLaunchPad.force_resource_use;
				settings.AddValue ("ForceResourceUse", val);
			}
			if (!settings.HasValue ("TimedBuilds")) {
				var val = ExLaunchPad.timed_builds;
				settings.AddValue ("TimedBuilds", val);
			}

			ExLaunchPad.force_resource_use = false;
			var fru = settings.GetValue ("ForceResourceUse");
			bool.TryParse (fru, out ExLaunchPad.force_resource_use);

			ExLaunchPad.timed_builds = true;
			var tb = settings.GetValue ("TimedBuilds");
			bool.TryParse (tb, out ExLaunchPad.timed_builds);

			if (settings.HasNode ("ShipInfo")) {
				var node = settings.GetNode ("ShipInfo");
				ExShipInfo.LoadSettings (node);
			}

			if (settings.HasNode ("BuildWindow")) {
				var node = settings.GetNode ("BuildWindow");
				ExBuildWindow.LoadSettings (node);
			}
		}

		public override void OnSave(ConfigNode config)
		{
			//Debug.Log (String.Format ("[EL] Settings save: {0}", config));
			var settings = new ConfigNode ("Settings");

			bool fru = ExLaunchPad.force_resource_use;
			settings.AddValue ("ForceResourceUse", fru);

			bool tb = ExLaunchPad.timed_builds;
			settings.AddValue ("TimedBuilds", tb);

			config.AddNode (settings);

			ExShipInfo.SaveSettings (settings.AddNode ("ShipInfo"));
			ExBuildWindow.SaveSettings (settings.AddNode ("BuildWindow"));
		}
		
		public override void OnAwake ()
		{
			enabled = false;
		}

		void OnGUI ()
		{
			var rect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 30,
								500, 100);

			GUI.skin = AssetBase.GetGUISkin("KSP window 2");

			GUILayout.BeginArea(rect, "Extraplanetary Launchpads Settings",
								GUI.skin.window);
			GUILayout.BeginVertical ();

			if (!ExLaunchPad.kethane_present) {
				bool fru = ExLaunchPad.force_resource_use;
				fru = GUILayout.Toggle (fru, "Always use resources");
				ExLaunchPad.force_resource_use = fru;
			}

			bool tb = ExLaunchPad.timed_builds;
			tb = GUILayout.Toggle (tb, "Allow progressive builds");
			ExLaunchPad.timed_builds = tb;

			if (GUILayout.Button ("OK")) {
				enabled = false;
			}
			GUILayout.EndVertical ();
			GUILayout.EndArea();
		}
	}

	// Fun magic to get a custom scenario into a game automatically.

	public class ExSettingsCreator
	{
		public static ExSettingsCreator me;
		void onGameStateCreated (Game game)
		{
			//Debug.Log (String.Format ("[EL] onGameStateCreated"));
			ExSettings.CreateSettings (game);
		}

		public ExSettingsCreator ()
		{
			GameEvents.onGameStateCreated.Add (onGameStateCreated);
		}
	}

	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class ExSettingsCreatorSpawn : MonoBehaviour
	{

		void Start ()
		{
			//Debug.Log (String.Format ("[EL] ExSettingsCreatorSpawn.Start"));
			ExSettingsCreator.me = new ExSettingsCreator ();
			enabled = false;
		}
	}
}
