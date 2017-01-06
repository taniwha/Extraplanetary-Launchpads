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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;
using KSP.UI.Screens;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

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
				green.wordWrap = false;

				white = new GUIStyle (GUI.skin.box);
				white.padding = new RectOffset (8, 8, 8, 8);
				white.normal.textColor = white.focused.textColor = Color.white;
				white.wordWrap = false;

				label = new GUIStyle (GUI.skin.label);
				label.normal.textColor = label.focused.textColor = Color.white;
				label.alignment = TextAnchor.MiddleCenter;
				label.wordWrap = false;

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

		static ELCraftBrowser craftlist = null;
		static Vector2 resscroll;

		static FlagBrowser flagBrowserPrefab;
		static FlagBrowser flagBrowser;
		static string flagURL;
		static Texture2D flagTexture;


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

			if (flagBrowserPrefab == null) {
				var fbObj = AssetBase.GetPrefab ("FlagBrowser");
				flagBrowserPrefab = fbObj.GetComponent<FlagBrowser> ();
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
			launchpads = null;
			pad_list = null;
			var pads = new List<ExBuildControl.IBuilder> ();

			for (int i = 0; i < v.Parts.Count; i++) {
				var p = v.Parts[i];
				pads.AddRange (p.Modules.OfType<ExBuildControl.IBuilder> ());
			}
			if (pads.Count < 1) {
				control = null;
			} else {
				launchpads = new List<ExBuildControl> ();
				int control_index = -1;
				for (int i = 0; i < pads.Count; i++) {
					launchpads.Add (pads[i].control);
					if (control == pads[i].control) {
						control_index = i;
					}
				}
				if (control_index < 0) {
					control_index = 0;
				}
				var pad_names = new List<string> ();
				for (int ind = 0; ind < launchpads.Count; ind++) {
					var p = launchpads[ind];
					if (p.builder.Name != "") {
						pad_names.Add (p.builder.Name);
					} else {
						pad_names.Add ("pad-" + ind);
					}
				}
				pad_list = new DropDownList (pad_names);

				Select_Pad (launchpads[control_index]);
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

		static public void updateCurrentPads() {
			instance.BuildPadList (FlightGlobals.ActiveVessel);
		}

		void UpdateGUIState ()
		{
			enabled = !hide_ui && launchpads != null && gui_enabled;
			if (control != null) {
				control.builder.Highlight (enabled && highlight_pad);
				if (enabled) {
					UpdateFlagTexture ();
				}
			}
			if (launchpads != null) {
				for (int i = 0; i < launchpads.Count; i++) {
					var p = launchpads[i];
					p.builder.UpdateMenus (enabled && p == control);
				}
			}
		}

		void UpdateFlagTexture ()
		{
			flagURL = control.flagname;

			if (String.IsNullOrEmpty (flagURL)) {
				flagURL = control.builder.part.flagURL;
			}

			flagTexture = GameDatabase.Instance.GetTexture (flagURL, false);
		}

		void CreateFlagBrowser ()
		{
			flagBrowser = Instantiate<FlagBrowser> (flagBrowserPrefab);

			flagBrowser.OnDismiss = OnFlagCancel;
			flagBrowser.OnFlagSelected = OnFlagSelected;
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
			GUILayout.Box (label, Styles.white, GUILayout.Width (125),
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
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box (displayAmount(required),
						   requiredStyle, GUILayout.Width (100),
						   GUILayout.Height (40));
			if (available >= 0) {
				GUILayout.Box (displayAmount(available),
							   Styles.white, GUILayout.Width (100),
							   GUILayout.Height (40));
			} else {
				GUILayout.Box ("N/A", Styles.white, GUILayout.Width (100),
							   GUILayout.Height (40));
			}

			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();

			return fraction;
		}

		string displayAmount(double amount) {
			if (amount > 1000000) {
				return Math.Round((amount / 1000000), 2).ToString() + " M";
			}
			else {
				return Math.Round(amount, 2).ToString();
			}
		}

		double BuildETA (BuildResource br, BuildResource req, bool forward)
		{
			double numberOfFramesLeft;
			if (br.deltaAmount <= 0) {
				return 0;
			}
			if (forward) {
				numberOfFramesLeft = (br.amount / br.deltaAmount);
			} else {
				numberOfFramesLeft = ((req.amount-br.amount) / br.deltaAmount);
			}
			return numberOfFramesLeft * TimeWarp.fixedDeltaTime;
		}

		double ResourceProgress (string label, BuildResource br,
			BuildResource req, bool forward)
		{
			double fraction = 1;
			if (req.amount > 0) {
				fraction = (req.amount - br.amount) / req.amount;
			}
			double required = br.amount;
			double available = control.padResources.ResourceAmount (br.name);
			double alarmTime;
			string percent = (fraction * 100).ToString ("G4") + "%";
			if (control.paused) {
				percent = percent + "[paused]";
				alarmTime = 0; // need assignment or compiler complains about use of unassigned variable
			} else {
				double eta = BuildETA (br, req, forward);
				alarmTime = Planetarium.GetUniversalTime () + eta;
				percent = percent + " " + EL_Utils.TimeSpanString (eta);

			}

			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (125),
						   GUILayout.Height (40));

			GUILayout.BeginVertical ();

			Styles.bar.Draw ((float) fraction, percent, 300);
			GUILayout.EndVertical ();

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (required > available) {
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box (displayAmount(required),
						   requiredStyle, GUILayout.Width (100),
						   GUILayout.Height (40));
			GUILayout.Box (displayAmount(available),
						   Styles.white, GUILayout.Width (100),
						   GUILayout.Height (40));
			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();
			return alarmTime;
		}

		void SelectPad_start ()
		{
			pad_list.styleListItem = Styles.listItem;
			pad_list.styleListBox = Styles.listBox;
			pad_list.DrawBlockingSelector ();
			control.builder.PadSelection_start ();
		}

		public static void SelectPad (ExBuildControl selected_pad)
		{
			instance.Select_Pad (selected_pad);
		}

		void Select_Pad (ExBuildControl selected_pad)
		{
			if (control != null && control != selected_pad) {
				control.builder.Highlight (false);
			}
			control = selected_pad;
			pad_list.SelectItem (launchpads.IndexOf (control));
			UpdateGUIState ();
		}

		void VesselName ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (control.builder.vessel.vesselName);
			GUILayout.EndHorizontal ();
		}

		void SelectPad ()
		{
			GUILayout.BeginHorizontal ();
			pad_list.DrawButton ();
			highlight_pad = GUILayout.Toggle (highlight_pad, "Highlight Pad");
			Select_Pad (launchpads[pad_list.SelectedIndex]);
			GUILayout.EndHorizontal ();
			control.builder.PadSelection ();
		}

		void SelectPad_end ()
		{
			if (pad_list != null) {
				pad_list.DrawDropDown();
				pad_list.CloseOnOutsideClick();
				control.builder.PadSelection_end ();
			}
		}

		void SelectCraft ()
		{
			string strpath = HighLogic.SaveFolder;

			GUILayout.BeginHorizontal ();
			GUI.enabled = craftlist == null;
			if (GUILayout.Button ("Select Craft", Styles.normal,
								  GUILayout.ExpandWidth (true))) {

				//GUILayout.Button is "true" when clicked
				craftlist = ELCraftBrowser.Spawn (control.craftType,
												  strpath,
												  craftSelectComplete,
												  craftSelectCancel,
												  false);
			}
			GUI.enabled = flagBrowser == null;
			if (GUILayout.Button (flagTexture, Styles.normal,
								  GUILayout.Width (48), GUILayout.Height (32),
								  GUILayout.ExpandWidth (false))) {
				CreateFlagBrowser ();
			}
			GUI.enabled = control.craftConfig != null;
			if (GUILayout.Button ("Reload", Styles.normal,
								  GUILayout.ExpandWidth (false))) {
				control.LoadCraft (control.filename, control.flagname);
			}
			if (GUILayout.Button ("Clear", Styles.normal,
								  GUILayout.ExpandWidth (false))) {
				control.UnloadCraft ();
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal ();
		}

		void SelectedCraft ()
		{
			if (control.craftConfig != null) {
				var ship_name = control.craftConfig.GetValue ("ship");
				GUILayout.Box ("Selected Craft:	" + ship_name, Styles.white);
			}
		}

		void LockedParts ()
		{
			GUILayout.Label ("Not all of the blueprints for this vessel can be found.");
		}

		void ResourceHeader ()
		{
			var width120 = GUILayout.Width (120);
			var width300 = GUILayout.Width (300);
			var width100 = GUILayout.Width (100);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resource", Styles.label, width120);
			GUILayout.Label ("Fill Percentage", Styles.label, width300);
			GUILayout.Label ("Required", Styles.label, width100);
			GUILayout.Label ("Available", Styles.label, width100);
			GUILayout.EndHorizontal ();
		}

		void ResourceScroll_begin ()
		{
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (680),
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

		void RequiredResources ()
		{
			GUILayout.Label ("Resources required to build:", Styles.label,
							 GUILayout.ExpandWidth (true));
			foreach (var br in control.buildCost.required) {
				double a = br.amount;
				double available = -1;

				available = control.padResources.ResourceAmount (br.name);
				ResourceLine (br.name, br.name, 1.0f, a, a, available);
			}
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
			GUILayout.BeginHorizontal ();
			GUI.enabled = control.builder.canBuild;
			if (GUILayout.Button ("Finalize Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				control.BuildAndLaunchCraft ();
			}
			GUI.enabled = true;
			if (GUILayout.Button ("Teardown Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				control.CancelBuild ();
			}
			GUILayout.EndHorizontal ();
		}

		internal static BuildResource FindResource (List<BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void UpdateAlarm (double mostFutureAlarmTime, bool forward)
		{
			if (KACWrapper.APIReady && ExSettings.use_KAC) {
				if (control.paused) {
					// It doesn't make sense to have an alarm for an event that will never happen
					if (control.KACalarmID != "") {
						try {
							KACWrapper.KAC.DeleteAlarm (control.KACalarmID);
						}
						catch {
							// Don't crash if there was some problem deleting the alarm
						}
						control.KACalarmID = "";
					}
				} else if (mostFutureAlarmTime>0) {
					// Find the existing alarm, if it exists
					// Note that we might have created an alarm, and then the user deleted it!
					KACWrapper.KACAPI.KACAlarmList alarmList = KACWrapper.KAC.Alarms;
					KACWrapper.KACAPI.KACAlarm a = null;
					if ((alarmList != null) && (control.KACalarmID != "")) {
						//Debug.Log ("Searching for alarm with ID [" + control.KACalarmID + "]");
						a = alarmList.FirstOrDefault (z => z.ID == control.KACalarmID);
					}

					// set up the strings for the alarm
					string builderShipName = FlightGlobals.ActiveVessel.vesselName;
					string newCraftName = control.craftConfig.GetValue ("ship");

					string alarmMessage = "[EL] build: \"" + newCraftName + "\"";
					string alarmNotes = "Completion of Extraplanetary Launchpad build of \"" + newCraftName + "\" on \"" + builderShipName + "\"";
					if (!forward) { // teardown messages
						alarmMessage = "[EL] teardown: \"" + newCraftName + "\"";
						alarmNotes = "Teardown of Extraplanetary Launchpad build of \"" + newCraftName + "\" on \"" + builderShipName + "\"";
					}

					if (a == null) {
						// no existing alarm, make a new alarm
						control.KACalarmID = KACWrapper.KAC.CreateAlarm (KACWrapper.KACAPI.AlarmTypeEnum.Raw, alarmMessage, mostFutureAlarmTime);
						//Debug.Log ("new alarm ID: [" + control.KACalarmID + "]");

						if (control.KACalarmID != "") {
							a = KACWrapper.KAC.Alarms.FirstOrDefault (z => z.ID == control.KACalarmID);
							if (a != null) {
								a.AlarmAction = ExSettings.KACAction;
								a.AlarmMargin = 0;
								a.VesselID = FlightGlobals.ActiveVessel.id.ToString ();
							}
						}
					}
					if (a != null) {
						// Whether we created an alarm or found an existing one, now update it
						a.AlarmTime = mostFutureAlarmTime;
						a.Notes = alarmNotes;
						a.Name = alarmMessage;
					}
				}
			}
		}

		void BuildProgress (bool forward)
		{
			double mostFutureAlarmTime = 0;
			foreach (var br in control.builtStuff.required) {
				var req = FindResource (control.buildCost.required, br.name);
				double alarmTime = ResourceProgress (br.name, br, req, forward);
				if (alarmTime > mostFutureAlarmTime) {
					mostFutureAlarmTime = alarmTime;
				}
			}
			UpdateAlarm (mostFutureAlarmTime, forward);
		}

		void OptionalResources ()
		{
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
			}
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
			VesselName ();
			SelectPad ();

			switch (control.state) {
			case ExBuildControl.State.Idle:
				SelectCraft ();
				break;
			case ExBuildControl.State.Planning:
				SelectCraft ();
				SelectedCraft ();
				if (control.lockedParts) {
					LockedParts ();
				} else {
					ResourceScroll_begin ();
					RequiredResources ();
					OptionalResources ();
					ResourceScroll_end ();
					BuildButton ();
				}
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

			GUILayout.EndVertical ();

			CloseButton ();

			SelectPad_end ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		void OnFlagCancel ()
		{
			flagBrowser = null;
		}

		void OnFlagSelected (FlagBrowser.FlagEntry selected)
		{
			control.flagname = selected.textureInfo.name;
			flagTexture = selected.textureInfo.texture;
			UpdateFlagTexture ();
			flagBrowser = null;
		}

		private void craftSelectComplete (string filename,
										  CraftBrowserDialog.LoadType lt)
		{
			control.LoadCraft (filename, flagURL);
			control.craftType = craftlist.craftType;
			craftlist = null;
		}

		private void craftSelectCancel ()
		{
			craftlist = null;
		}

		void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;
			string name = "Extraplanetary Launchpad";
			string ver = ExVersionReport.GetVersion ();
			string sit = control.builder.vessel.situation.ToString ();
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  name + " " + ver + ": " + sit,
										  GUILayout.Width (695));
		}
	}
}
