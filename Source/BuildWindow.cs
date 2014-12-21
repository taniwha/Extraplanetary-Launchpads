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
using System.Linq;
using UnityEngine;

using KSP.IO;

using ExLP_KACWrapper;

namespace ExLP {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExBuildWindow : MonoBehaviour
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

			public static GUIStyle listItem;
			public static GUIStyle listBox;

			public static ProgressBar bar;

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

				listItem = new GUIStyle ();
				listItem.normal.textColor = Color.white;
				Texture2D texInit = new Texture2D(1, 1);
				texInit.SetPixel(0, 0, Color.white);
				texInit.Apply();
				listItem.hover.background = texInit;
				listItem.onHover.background = texInit;
				listItem.hover.textColor = Color.black;
				listItem.onHover.textColor = Color.black;
				listItem.padding = new RectOffset(4, 4, 4, 4);

				listBox = new GUIStyle(GUI.skin.box);

				bar = new ProgressBar (XKCDColors.Azure,
									   XKCDColors.ElectricBlue,
									   new Color(255, 255, 255, 0.8f));
			}
		}

		static ExBuildWindow instance;
		static bool hide_ui = false;
		static bool gui_enabled = true;
		static Rect windowpos;
		static bool highlight_pad = true;
		static bool link_lfo_sliders = true;

		static CraftBrowser craftlist = null;
		static Vector2 resscroll;

		List<ExBuildControl> launchpads;
		DropDownList pad_list;
		ExBuildControl control;

		internal void Start()
		{
			KACWrapper.InitKACWrapper();
			if (KACWrapper.APIReady)
			{
				//All good to go
				Debug.Log ("KACWrapper initialized");
			}
		}

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void ShowGUI ()
		{
			gui_enabled = true;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				windowpos.x = pos.x;
				windowpos.y = pos.y;
				windowpos.width = pos.z;
				windowpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out gui_enabled);
			}
			val = node.GetValue ("link_lfo_sliders");
			if (val != null) {
				bool.TryParse (val, out link_lfo_sliders);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			Quaternion pos;
			pos.x = windowpos.x;
			pos.y = windowpos.y;
			pos.z = windowpos.width;
			pos.w = windowpos.height;
			node.AddValue ("rect", KSPUtil.WriteQuaternion (pos));
			node.AddValue ("visible", gui_enabled);
			node.AddValue ("link_lfo_sliders", link_lfo_sliders);
		}

		void BuildPadList (Vessel v)
		{
			if (control != null) {
				//control.builder.part.SetHighlightDefault ();
			}
			launchpads = null;
			pad_list = null;
			control = null;	//FIXME would be nice to not lose the active pad
			var pads = new List<ExBuildControl.IBuilder> ();

			foreach (var p in v.Parts) {
				pads.AddRange (p.Modules.OfType<ExBuildControl.IBuilder> ());
			}
			if (pads.Count > 0) {
				launchpads = new List<ExBuildControl> ();
				foreach (var p in pads) {
					launchpads.Add (p.control);
				}
				control = launchpads[0];
				var pad_names = new List<string> ();
				int ind = 0;
				foreach (var p in launchpads) {
					if (p.builder.Name != "") {
						pad_names.Add (p.builder.Name);
					} else {
						pad_names.Add ("pad-" + ind);
					}
					ind++;
				}
				pad_list = new DropDownList (pad_names);
			}
		}

		void onVesselChange (Vessel v)
		{
			BuildPadList (v);
			UpdateGUIState ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (FlightGlobals.ActiveVessel == v) {
				BuildPadList (v);
			}
		}

		void UpdateGUIState ()
		{
			enabled = !hide_ui && launchpads != null && gui_enabled;
			if (!enabled) {
				InputLockManager.RemoveControlLock ("EL_Build_window_lock");
			}
			if (control != null) {
				if (enabled && highlight_pad) {
					//control.builder.part.SetHighlightColor (XKCDColors.LightSeaGreen);
					//control.builder.part.SetHighlight (true, false);
				} else {
					//control.builder.part.SetHighlightDefault ();
				}
			}
			if (launchpads != null) {
				foreach (var p in launchpads) {
					p.builder.UpdateMenus (enabled && p == control);
				}
			}
		}

		void onHideUI ()
		{
			hide_ui = true;
			UpdateGUIState ();
		}

		void onShowUI ()
		{
			hide_ui = false;
			UpdateGUIState ();
		}

		void Awake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				enabled = false;
				return;
			}
			instance = this;
			GameEvents.onVesselChange.Add (onVesselChange);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);
			enabled = false;
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onVesselChange.Remove (onVesselChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
		}

		float ResourceLine (string label, string resourceName, float fraction,
							double minAmount, double maxAmount,
							double available)
		{
			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			// Fill amount
			// limit slider to 0.5% increments
			GUILayout.BeginVertical ();
			if (minAmount == maxAmount) {
				GUILayout.Box ("Must be 100%", GUILayout.Width (300),
							   GUILayout.Height (20));
				fraction = 1.0F;
			} else {
				fraction = GUILayout.HorizontalSlider (fraction, 0.0F, 1.0F,
													   Styles.slider,
													   GUI.skin.horizontalSliderThumb,
													   GUILayout.Width (300),
													   GUILayout.Height (20));
				fraction = (float)Math.Round (fraction, 3);
				fraction = (Mathf.Floor (fraction * 200)) / 200;
				GUILayout.Box ((fraction * 100).ToString () + "%",
							   Styles.sliderText, GUILayout.Width (300),
							   GUILayout.Height (20));
			}
			GUILayout.EndVertical ();

			double required = minAmount + (maxAmount - minAmount) * fraction;

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (available >= 0 && available < required) {
				if (ExSettings.timed_builds) {
					requiredStyle = Styles.yellow;
				} else {
					requiredStyle = Styles.red;
				}
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			if (available >= 0) {
				GUILayout.Box ((Math.Round (available, 2)).ToString (),
							   Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			} else {
				GUILayout.Box ("N/A", Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			}

			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();

			return fraction;
		}

		void ResourceProgress (string label, BuildCost.BuildResource br,
			BuildCost.BuildResource req, bool forward)
		{
			double fraction = (req.amount - br.amount) / req.amount;
			double required = br.amount;
			double available = control.padResources.ResourceAmount (br.name);
			double alarmTime;
			string percent = (fraction * 100).ToString ("G4") + "% ";
			if (control.paused) {
				percent = percent + "[paused]";
				alarmTime = 0; // need assignment or compiler complains about use of unassigned variable
			} else {
				double numberOfFramesLeft;
				if (forward) {
					numberOfFramesLeft = (br.amount / br.deltaAmount);
				} else {
					numberOfFramesLeft = ((req.amount-br.amount) / br.deltaAmount);
				}
				double numberOfSecondsLeft = numberOfFramesLeft * TimeWarp.fixedDeltaTime;
				TimeSpan timeLeft = TimeSpan.FromSeconds (numberOfSecondsLeft);
				percent = percent + String.Format ("{0:D2}:{1:D2}:{2:D2}", timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);
				alarmTime = Planetarium.GetUniversalTime () + timeLeft.TotalSeconds;
			}

			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			GUILayout.BeginVertical ();

			Styles.bar.Draw ((float) fraction, percent, 300);
			GUILayout.EndVertical ();

			if (KACWrapper.APIReady) {
				if (control.paused) {
					// It doesn't make sense to have an alarm for an event that will never happen
					if (control.KACalarmID != "") {
						KACWrapper.KAC.DeleteAlarm (control.KACalarmID);
						control.KACalarmID = "";
					}
				} else {
					// Find the existing alarm, if it exists
					KACWrapper.KACAPI.KACAlarmList alarmList = KACWrapper.KAC.Alarms;
					KACWrapper.KACAPI.KACAlarm a = null;
					if ((alarmList != null) && (control.KACalarmID!="")) {
						//Debug.Log ("Searching for alarm with ID [" + control.KACalarmID + "]");
						a = alarmList.First(z=>z.ID==control.KACalarmID);
					}

					// set up the strings for the alarm
					string builderShipName = FlightGlobals.ActiveVessel.vesselName;
					string newCraftName = control.craftConfig.GetValue ("ship");

					string alarmMessage = "[EPL] build: \"" + newCraftName + "\"";
					string alarmNotes = "Completion of Extraplanetary Launchpad build of \"" + newCraftName + "\" on \"" + builderShipName + "\"";
					if (!forward) { // teardown messages
						alarmMessage = "[EPL] teardown: \"" + newCraftName + "\"";
						alarmNotes = "Teardown of Extraplanetary Launchpad build of \"" + newCraftName + "\" on \"" + builderShipName + "\"";
					}

					if (a == null) {
						// no existing alarm, make a new alarm
						control.KACalarmID = KACWrapper.KAC.CreateAlarm (KACWrapper.KACAPI.AlarmTypeEnum.Raw, alarmMessage, alarmTime);
						//Debug.Log ("new alarm ID: [" + control.KACalarmID + "]");

						if (control.KACalarmID != "") {
							a = KACWrapper.KAC.Alarms.First (z => z.ID == control.KACalarmID);
							if (a != null) {
								a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp; //FIXME: should be configurable in EPL options
								a.AlarmMargin = 0;
								a.VesselID = FlightGlobals.ActiveVessel.id.ToString ();
							}
						}
					}
					if (a != null) {
						// Whether we created an alarm or found an existing one, now update it
						a.AlarmTime = alarmTime;
						a.Notes = alarmNotes;
						a.Name = alarmMessage;
					}
				}

			}

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (required > available) {
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.Box ((Math.Round (available, 2)).ToString (),
						   Styles.white, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();
		}

		void SelectPad_start ()
		{
			pad_list.styleListItem = Styles.listItem;
			pad_list.styleListBox = Styles.listBox;
			pad_list.DrawBlockingSelector ();
		}

		public static void SelectPad (ExBuildControl selected_pad)
		{
			instance.Select_Pad (selected_pad);
		}

		void Select_Pad (ExBuildControl selected_pad)
		{
			if (control != null) {
				//control.builder.part.SetHighlightDefault ();
			}
			control = selected_pad;
			pad_list.SelectItem (launchpads.IndexOf (control));
			UpdateGUIState ();
		}

		void SelectPad ()
		{
			GUILayout.BeginHorizontal ();
			pad_list.DrawButton ();
			highlight_pad = GUILayout.Toggle (highlight_pad, "Highlight Pad");
			Select_Pad (launchpads[pad_list.SelectedIndex]);
			GUILayout.EndHorizontal ();
		}

		void SelectPad_end ()
		{
			if (pad_list != null) {
				pad_list.DrawDropDown();
				pad_list.CloseOnOutsideClick();
			}
		}

		void SelectCraft ()
		{
			GUILayout.BeginHorizontal ("box");
			GUILayout.FlexibleSpace ();
			// VAB / SPH selection
			for (var t = ExBuildControl.CraftType.VAB;
				 t <= ExBuildControl.CraftType.SubAss;
				 t++) {
				if (GUILayout.Toggle (control.craftType == t, t.ToString (),
									  GUILayout.Width (80))) {
					control.craftType = t;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			string strpath = HighLogic.SaveFolder;

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Select Craft", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				//string []dir = new string[] {"VAB", "SPH", "../Subassemblies"};
				var diff = HighLogic.CurrentGame.Parameters.Difficulty;
				bool stock = diff.AllowStockVessels;
				if (control.craftType == ExBuildControl.CraftType.SubAss) {
					diff.AllowStockVessels = false;
				}
				//GUILayout.Button is "true" when clicked
				var clrect = new Rect (Screen.width / 2, 100, 350, 500);
				craftlist = new CraftBrowser (clrect, EditorFacility.None,
											  strpath,
											  "Select a ship to load",
											  craftSelectComplete,
											  craftSelectCancel,
											  HighLogic.Skin,
											  EditorLogic.ShipFileImage, true);
				diff.AllowStockVessels = stock;
			}
			GUI.enabled = control.craftConfig != null;
			if (GUILayout.Button ("Clear", Styles.normal,
								  GUILayout.ExpandWidth (false))) {
				control.UnloadCraft ();
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal ();
		}

		void SelectedCraft ()
		{
			var ship_name = control.craftConfig.GetValue ("ship");
			GUILayout.Box ("Selected Craft:	" + ship_name, Styles.white);
		}

		void ResourceHeader ()
		{
			var width120 = GUILayout.Width (120);
			var width300 = GUILayout.Width (300);
			var width75 = GUILayout.Width (75);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resource", Styles.label, width120);
			GUILayout.Label ("Fill Percentage", Styles.label, width300);
			GUILayout.Label ("Required", Styles.label, width75);
			GUILayout.Label ("Available", Styles.label, width75);
			GUILayout.EndHorizontal ();
		}

		void ResourceScroll_begin ()
		{
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (625),
												   GUILayout.Height (300));
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();
		}

		void ResourceScroll_end ()
		{
			GUILayout.EndVertical ();
			GUILayout.Label ("", Styles.label, GUILayout.Width (15));
			GUILayout.EndHorizontal ();
			GUILayout.EndScrollView ();
		}

		bool RequiredResources ()
		{
			bool can_build = true;
			GUILayout.Label ("Resources required to build:", Styles.label,
							 GUILayout.ExpandWidth (true));
			foreach (var br in control.buildCost.required) {
				double a = br.amount;
				double available = -1;

				available = control.padResources.ResourceAmount (br.name);
				ResourceLine (br.name, br.name, 1.0f, a, a, available);
				if (br.amount > available) {
					can_build = false;
				}
			}
			return can_build;
		}

		void BuildButton ()
		{
			if (GUILayout.Button ("Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				control.BuildCraft ();
			}
		}

		void SpawnOffset ()
		{
			/*if (control.vessel.situation == Vessel.Situations.LANDED) {
				GUILayout.BeginVertical();
				GUILayout.Space(10.0f);
				GUILayout.BeginHorizontal();
				GUILayout.Box("Spawn Height Offset", Styles.white,
							  GUILayout.Width(180), GUILayout.Height(40));
				control.spawnOffset = GUILayout.HorizontalSlider(control.spawnOffset,
					0.0F, 10.0F, Styles.slider, GUI.skin.horizontalSliderThumb,
					GUILayout.Width(300), GUILayout.Height(40));
				control.spawnOffset = (float)Math.Round(control.spawnOffset, 1);
				GUILayout.Box(control.spawnOffset.ToString() + "m",
							  Styles.white, GUILayout.Width(75),
							  GUILayout.Height(40));
				GUILayout.FlexibleSpace();

				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			} else {
				control.SpawnHeightOffset = 0.0f;
			}*/
		}

		void FinalizeButton ()
		{
			if (GUILayout.Button ("Finalize Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				control.BuildAndLaunchCraft ();
			}
		}

		internal static BuildCost.BuildResource FindResource (List<BuildCost.BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void BuildProgress (bool forward)
		{
			foreach (var br in control.builtStuff.required) {
				var req = FindResource (control.buildCost.required, br.name);
				ResourceProgress (br.name, br, req, forward);
			}
		}

		bool OptionalResources ()
		{
			bool can_build = true;

			link_lfo_sliders = GUILayout.Toggle (link_lfo_sliders,
												 "Link LiquidFuel and "
												 + "Oxidizer sliders");
			foreach (var br in control.buildCost.optional) {
				double available = control.padResources.ResourceAmount (br.name);
				double maximum = control.craftResources.ResourceCapacity(br.name);
				float frac = (float) (br.amount / maximum);
				frac = ResourceLine (br.name, br.name, frac, 0,
									 maximum, available);
				if (link_lfo_sliders
					&& (br.name == "LiquidFuel" || br.name == "Oxidizer")) {
					string other;
					if (br.name == "LiquidFuel") {
						other = "Oxidizer";
					} else {
						other = "LiquidFuel";
					}
					var or = FindResource (control.buildCost.optional, other);
					if (or != null) {
						double om = control.craftResources.ResourceCapacity (other);
						or.amount = om * frac;
					}
				}
				br.amount = maximum * frac;
				if (br.amount > available) {
					can_build = false;
				}
			}
			return can_build;
		}

		static string[] state_str = {
			"Build", "Teardown",
		};
		void PauseButton ()
		{
			int ind = control.state == ExBuildControl.State.Building ? 0 : 1;
			GUILayout.BeginHorizontal ();
			if (control.paused) {
				if (GUILayout.Button ("Resume " + state_str[ind], Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					control.ResumeBuild ();
				}
			} else {
				if (GUILayout.Button ("Pause " + state_str[ind], Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					control.PauseBuild ();
				}
			}
			if (control.state == ExBuildControl.State.Building) {
				if (GUILayout.Button ("Cancel Build", Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					control.CancelBuild ();
				}
			} else {
				if (GUILayout.Button ("Restart Build", Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					control.UnCancelBuild ();
				}
			}
			GUILayout.EndHorizontal ();
		}

		void ReleaseButton ()
		{
			if (GUILayout.Button ("Release", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				control.ReleaseVessel ();
			}
		}

		void CloseButton ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				HideGUI ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void WindowGUI (int windowID)
		{
			Styles.Init ();

			SelectPad_start ();

			GUILayout.BeginVertical ();
			SelectPad ();

			if (ExSettings.timed_builds) {
				switch (control.state) {
				case ExBuildControl.State.Idle:
					SelectCraft ();
					break;
				case ExBuildControl.State.Planning:
					SelectCraft ();
					SelectedCraft ();
					ResourceScroll_begin ();
					RequiredResources ();
					ResourceScroll_end ();
					BuildButton ();
					break;
				case ExBuildControl.State.Building:
					SelectedCraft ();
					ResourceScroll_begin ();
					BuildProgress (true);
					ResourceScroll_end ();
					PauseButton ();
					break;
				case ExBuildControl.State.Canceling:
					SelectedCraft ();
					ResourceScroll_begin ();
					BuildProgress (false);
					ResourceScroll_end ();
					PauseButton ();
					break;
				case ExBuildControl.State.Complete:
					SpawnOffset ();
					FinalizeButton ();
					break;
				case ExBuildControl.State.Transfer:
					SelectedCraft ();
					ResourceScroll_begin ();
					OptionalResources ();
					ResourceScroll_end ();
					ReleaseButton ();
					break;
				}
			} else {
				switch (control.state) {
				case ExBuildControl.State.Idle:
					SelectCraft ();
					break;
				case ExBuildControl.State.Planning:
					SelectCraft ();
					SelectedCraft ();
					ResourceScroll_begin ();
					bool have_required = RequiredResources ();
					bool have_optional = OptionalResources ();
					ResourceScroll_end ();
					SpawnOffset ();
					if (!ExBuildControl.useResources
						|| (have_required && have_optional)) {
						BuildButton ();
					}
					break;
				case ExBuildControl.State.Building:
					// shouldn't happen
					break;
				case ExBuildControl.State.Complete:
					SelectedCraft ();
					ReleaseButton ();
					break;
				}
			}

			GUILayout.EndVertical ();

			CloseButton ();

			SelectPad_end ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		private void craftSelectComplete (string filename, string flagname)
		{
			craftlist = null;
			control.LoadCraft (filename, flagname);
		}

		private void craftSelectCancel ()
		{
			craftlist = null;
		}

		void OnGUI ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			GUI.skin = HighLogic.Skin;
			string name = "Extraplanetary Launchpad";
			string ver = ExSettings.GetVersion ();
			string sit = control.builder.vessel.situation.ToString ();
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  name + " " + ver + ": " + sit,
										  GUILayout.Width (640));
			if (enabled && windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
				InputLockManager.SetControlLock ("EL_Build_window_lock");
			} else {
				InputLockManager.RemoveControlLock ("EL_Build_window_lock");
			}
			if (craftlist != null) {
				craftlist.OnGUI ();
			}
		}
	}
}
