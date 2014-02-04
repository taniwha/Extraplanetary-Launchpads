using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class BuildWindow
	{
		public class Styles {
			public static GUIStyle normal;
			public static GUIStyle red;
			public static GUIStyle yellow;
			public static GUIStyle green;
			public static GUIStyle white;
			public static GUIStyle label;
			public static GUIStyle slider;
			public static GUIStyle sliderText;

			private static bool initialized;

			public static void Init ()
			{
				if (initialized)
					return;
				initialized = true;

				normal = new GUIStyle (GUI.skin.button);
				normal.normal.textColor = normal.focused.textColor = Color.white;
				normal.hover.textColor = normal.active.textColor = Color.yellow;
				normal.onNormal.textColor = normal.onFocused.textColor = normal.onHover.textColor = normal.onActive.textColor = Color.green;
				normal.padding = new RectOffset (8, 8, 8, 8);

				red = new GUIStyle (GUI.skin.box);
				red.padding = new RectOffset (8, 8, 8, 8);
				red.normal.textColor = red.focused.textColor = Color.red;

				yellow = new GUIStyle (GUI.skin.box);
				yellow.padding = new RectOffset (8, 8, 8, 8);
				yellow.normal.textColor = yellow.focused.textColor = Color.yellow;

				green = new GUIStyle (GUI.skin.box);
				green.padding = new RectOffset (8, 8, 8, 8);
				green.normal.textColor = green.focused.textColor = Color.green;

				white = new GUIStyle (GUI.skin.box);
				white.padding = new RectOffset (8, 8, 8, 8);
				white.normal.textColor = white.focused.textColor = Color.white;

				label = new GUIStyle (GUI.skin.label);
				label.normal.textColor = label.focused.textColor = Color.white;
				label.alignment = TextAnchor.MiddleCenter;

				slider = new GUIStyle (GUI.skin.horizontalSlider);
				slider.margin = new RectOffset (0, 0, 0, 0);

				sliderText = new GUIStyle (GUI.skin.label);
				sliderText.alignment = TextAnchor.MiddleCenter;
				sliderText.margin = new RectOffset (0, 0, 0, 0);
			}
		}

		static ExLaunchPad pad;
		static Rect windowpos;

		static bool linklfosliders = true;
		static CraftBrowser craftlist = null;
		static bool showcraftbrowser = false;
		static Vector2 resscroll;

		static Dictionary<string, float> resourcesliders = new Dictionary<string, float>();

		static float ResourceLine (string label, string resourceName, float fraction, double minAmount, double maxAmount, double available)
		{
			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120), GUILayout.Height (40));

			// Fill amount
			GUILayout.BeginVertical ();
			GUILayout.FlexibleSpace ();
			// limit slider to 0.5% increments
			if (minAmount == maxAmount) {
				GUILayout.Box ("Must be 100%", GUILayout.Width (300), GUILayout.Height (20));
				fraction = 1.0F;
			} else {
				fraction = (float)Math.Round (GUILayout.HorizontalSlider (fraction, 0.0F, 1.0F, Styles.slider, GUI.skin.horizontalSliderThumb, GUILayout.Width (300), GUILayout.Height (20)), 3);
				fraction = (Mathf.Floor (fraction * 200)) / 200;
				GUILayout.Box ((fraction * 100).ToString () + "%", Styles.sliderText, GUILayout.Width (300), GUILayout.Height (20));
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();

			double required = minAmount * (1 - fraction)  + maxAmount * fraction;

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (available >= 0 && available < required) {
				requiredStyle = Styles.red;
				// prevent building if using resources
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (), requiredStyle, GUILayout.Width (75), GUILayout.Height (40));
			if (available >= 0) {
				GUILayout.Box ((Math.Round (available, 2)).ToString (), Styles.white, GUILayout.Width (75), GUILayout.Height (40));
			} else {
				GUILayout.Box ("N/A", Styles.white, GUILayout.Width (75), GUILayout.Height (40));
			}

			// Flexi space to make sure any unused space is at the right-hand edge
			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();

			return fraction;
		}

		static void WindowGUI (int windowID)
		{
			Styles.Init ();

			EditorLogic editor = EditorLogic.fetch;
			if (editor) return;

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ("box");
			GUILayout.FlexibleSpace ();
			// VAB / SPH selection
			if (GUILayout.Toggle (pad.craftType == ExLaunchPad.CraftType.VAB, "VAB", GUILayout.Width (80))) {
				pad.craftType = ExLaunchPad.CraftType.VAB;
			}
			if (GUILayout.Toggle (pad.craftType == ExLaunchPad.CraftType.SPH, "SPH", GUILayout.Width (80))) {
				pad.craftType = ExLaunchPad.CraftType.SPH;
			}
			if (GUILayout.Toggle (pad.craftType == ExLaunchPad.CraftType.SUB, "SubAss", GUILayout.Width (160))) {
				pad.craftType = ExLaunchPad.CraftType.SUB;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			string strpath = HighLogic.SaveFolder;

			if (GUILayout.Button ("Select Craft", Styles.normal, GUILayout.ExpandWidth (true))) {
				string [] dir = new string[] {"SPH", "VAB", "../Subassemblies"};
				bool stock = HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels;
				if (pad.craftType == ExLaunchPad.CraftType.SUB)
					HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = false;
				//GUILayout.Button is "true" when clicked
				craftlist = new CraftBrowser (new Rect (Screen.width / 2, 100, 350, 500), dir[(int)pad.craftType], strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
				showcraftbrowser = true;
				HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = stock;
			}

			if (pad.craftConfig != null && pad.buildCost != null) {
				GUILayout.Box ("Selected Craft:	" + pad.craftConfig.GetValue ("ship"), Styles.white);

				// Resource requirements
				GUILayout.Label ("Resources required to build:", Styles.label, GUILayout.Width (600));

				// Link LFO toggle

				linklfosliders = GUILayout.Toggle (linklfosliders, "Link RocketFuel sliders for LiquidFuel and Oxidizer");

				resscroll = GUILayout.BeginScrollView (resscroll, GUILayout.Width (600), GUILayout.Height (300));

				GUILayout.BeginHorizontal ();

				// Headings
				GUILayout.Label ("Resource", Styles.label, GUILayout.Width (120));
				GUILayout.Label ("Fill Percentage", Styles.label, GUILayout.Width (300));
				GUILayout.Label ("Required", Styles.label, GUILayout.Width (75));
				GUILayout.Label ("Available", Styles.label, GUILayout.Width (75));
				GUILayout.EndHorizontal ();

				foreach (var br in pad.buildCost.required) {
					double a = br.amount;
					double available = -1;

					available = pad.padResources.ResourceAmount (br.name);
					ResourceLine (br.name, br.name, 1.0f, a, a, available);
				}
				foreach (var br in pad.buildCost.optional) {
					double available = pad.padResources.ResourceAmount (br.name);
					if (!resourcesliders.ContainsKey (br.name)) {
						resourcesliders.Add (br.name, 1);
					}
					resourcesliders[br.name] = ResourceLine (br.name, br.name, resourcesliders[br.name], 0, br.amount, available);
				}

				GUILayout.EndScrollView ();

				// Build button
				if (GUILayout.Button ("Build", Styles.normal, GUILayout.ExpandWidth (true))) {
					pad.BuildAndLaunchCraft ();

					pad.craftConfig = null;
					pad.buildCost = null;
					resourcesliders = new Dictionary<string, float>();;
				}
			} else {
				GUILayout.Box ("You must select a craft before you can build", Styles.red);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				pad.HideBuildMenu ();
			}

			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		// called when the user selects a craft the craft browser
		static void craftSelectComplete (string filename, string flagname)
		{
			showcraftbrowser = false;
			pad.flagname = flagname;
			ConfigNode craft = ConfigNode.Load (filename);

			// Get list of resources required to build vessel
			if ((pad.buildCost = pad.getBuildCost (craft)) != null)
				pad.craftConfig = craft;
		}

		// called when the user clicks cancel in the craft browser
		static void craftSelectCancel ()
		{
			showcraftbrowser = false;

			pad.buildCost = null;
			pad.craftConfig = null;
		}

		internal static void OnGUI (ExLaunchPad pad)
		{
			GUI.skin = HighLogic.Skin;
			string sit = pad.vessel.situation.ToString ();
			windowpos = GUILayout.Window (1, windowpos, WindowGUI,
										  "Extraplanetary Launchpad: " + sit,
										  GUILayout.Width (600));
			if (showcraftbrowser) {
				craftlist.OnGUI ();
			}
		}
	}

}
