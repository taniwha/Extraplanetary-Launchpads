using System;
using System.Collections.Generic;
using System.Linq;
//using System.IO;	  // needed for Path manipulation
//using Uri;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ExLaunchPad : PartModule, ExWorkSink
	{

		[KSPField]
		public bool DebugPad = false;

		public static bool timed_builds = false;
		public static bool kethane_checked;
		public static bool kethane_present;
		public static bool force_resource_use;
		public static bool use_resources;

		public enum CraftType { SPH, VAB, SUB };

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

		public Rect windowpos;
		public bool builduiactive = false;	// Whether the build menu is open or closed
		public bool builduivisible = true;	// Whether the build menu is allowed to be shown
		public bool showbuilduionload = false;
		public bool linklfosliders = true;
		public bool canbuildcraft = false;
		public CraftType craftType = CraftType.VAB;
		public string flagname = null;
		public CraftBrowser craftlist = null;
		public bool showcraftbrowser = false;
		public ConfigNode craftConfig = null;
		public Vector2 resscroll;
		public BuildCost.CostReport buildCost = null;
		public double hullRocketParts = 0.0;
		public Dictionary<string, float> resourcesliders = new Dictionary<string, float>();

		public PartResource KerbalMinutes;
		public bool autoRelease;
		public DockedVesselInfo vesselInfo;

		class Strut {
			GameObject gameObject;
			Vector3 pos;
			Vector3 dir;
			string targetName;
			float maxLength;
			public Part target;

			public Strut (Part part, string[] parms)
			{
				gameObject = part.gameObject;
				if (part is StrutConnector) {
					maxLength = ((StrutConnector)part).maxLength;
				} else if (part is FuelLine) {
					maxLength = ((FuelLine)part).maxLength;
				} else {
					// not expected to happen, but...
					maxLength = 10;
				}
				for (int i = 0; i < parms.Length; i++) {
					string[] keyval = parms[i].Split (':');
					string Key = keyval[0].Trim ();
					string Value = keyval[1].Trim ();
					if (Key == "tgt") {
						targetName = Value.Split ('_')[0];
					} else if (Key == "pos") {
						pos = KSPUtil.ParseVector3 (Value);
					} else if (Key == "dir") {
						dir = KSPUtil.ParseVector3 (Value);
					}
				}
				target = null;
				Transform xform = gameObject.transform;
				RaycastHit hitInfo;
				Vector3 castPos = xform.TransformPoint (pos);
				Vector3 castDir = xform.TransformDirection (dir);
				if (Physics.Raycast (castPos, castDir, out hitInfo, maxLength)) {
					GameObject hit = hitInfo.collider.gameObject;
					target = EditorLogic.GetComponentUpwards<Part>(hit);
				}
				Debug.Log (String.Format ("[EL] {0} {1} {2} {3}", target, targetName, xform.position, xform.rotation));
			}
		}

		int padPartsCount;					// the number of parts in the pad vessel (for docking detection)
		VesselResources padResources;		// resources available to the pad

		[KSPField (isPersistant = false)]
		public float SpawnHeightOffset = 0.0f;	// amount of pad between origin and open space

		[KSPField (isPersistant = false)]
		public string SpawnTransform;

		//private List<Vessel> bases;

		private static bool CheckForKethane ()
		{
			if (AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name == "MMI_Kethane" || a.assembly.GetName ().Name == "Kethane")) {
				Debug.Log ("[EL] Kethane found");
				return true;
			}
			Debug.Log ("[EL] Kethane not found");
			return false;
		}

		public bool isActive ()
		{
			return false;
		}

		public void DoWork (double kerbalHours)
		{
			//padResources.TransferResource (br.name, -br.amount);
			Debug.Log (String.Format ("[EL Launchpad] KerbalHours: {0}",
									  kerbalHours));
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (this);
		}

		private void UpdateGUIState ()
		{
			bool can_build = false;
			bool can_release = false;
			var situation = Vessel.Situations.LANDED;

			if (vessel) {
				situation = vessel.situation;
			}
			if (vesselInfo == null
				&& (situation == Vessel.Situations.LANDED
					|| situation == Vessel.Situations.ORBITING
					|| situation == Vessel.Situations.PRELAUNCH
					|| situation == Vessel.Situations.SPLASHED)) {
				can_build = true;
			}
			if (vesselInfo != null && CheckKerbalMinutes ()) {
				can_release = true;
			}
			enabled = can_build && builduiactive && builduivisible;
			Events["ShowBuildMenu"].active = can_build && !builduiactive;
			Events["HideBuildMenu"].active = can_build && builduiactive;
			Events["ReleaseVessel"].active = can_release;
		}
		// =====================================================================================================================================================
		// UI Functions

		private void UseResources (Vessel craft)
		{
			VesselResources craftResources = new VesselResources (craft);

			foreach (var br in buildCost.required) {
				if (br.name == "KerbalMinutes") {
					if (timed_builds && !DebugPad) {
						SetKerbalMinutes (br.amount);
					}
				} else {
					if (use_resources) {
						padResources.TransferResource (br.name, -br.amount);
					}
				}
			}
			foreach (var br in buildCost.optional) {
				craftResources.TransferResource (br.name, -br.amount);

				double tot = br.amount * resourcesliders[br.name];
				if (use_resources) {
					padResources.TransferResource (br.name, -tot);
				}
				craftResources.TransferResource (br.name, tot);
			}
		}

		private void SetKerbalMinutes (double kerbalMinutes)
		{
			ConfigNode khNode = new ConfigNode ("RESOURCE");
			khNode.AddValue ("name", "KerbalMinutes");
			khNode.AddValue ("amount", 0);
			khNode.AddValue ("maxAmount", kerbalMinutes);
			if (KerbalMinutes == null) {
				KerbalMinutes = part.AddResource (khNode);
			} else {
				part.SetResource (khNode);
			}
		}

		private void ClearKerbalMinutes ()
		{
			if (KerbalMinutes != null) {
				KerbalMinutes.amount = 0;
				KerbalMinutes.maxAmount = 0;
			}
		}

		private bool CheckKerbalMinutes ()
		{
			if (KerbalMinutes == null) {
				return true;
			}
			if (KerbalMinutes.maxAmount - KerbalMinutes.amount < 0.01) {
				return true;
			}
			return false;
		}

		private void HackStrutCData (ShipConstruct ship, Part p, int numParts)
		{
			Debug.Log (String.Format ("[EL] before {0}", p.customPartData));
			string[] Params = p.customPartData.Split (';');
			for (int i = 0; i < Params.Length; i++) {
				string[] keyval = Params[i].Split (':');
				string Key = keyval[0].Trim ();
				string Value = keyval[1].Trim ();
				if (Key == "tgt") {
					string[] pnameval = Value.Split ('_');
					string pname = pnameval[0];
					int val = int.Parse (pnameval[1]);
					if (val == -1) {
						Strut strut = new Strut (p, Params);
						if (strut.target != null) {
							val = ship.parts.IndexOf (strut.target);
						}
					}
					if (val != -1) {
						val += numParts;
					}
					Params[i] = "tgt: " + pname + "_" + val.ToString ();
					break;
				}
			}
			p.customPartData = String.Join ("; ", Params);
			Debug.Log (String.Format ("[EL] after {0}", p.customPartData));
		}

		private void HackStruts (ShipConstruct ship, bool addCount)
		{
			int numParts = vessel.parts.Count;
			if (!addCount)
				numParts = 0;

			var struts = ship.parts.OfType<StrutConnector>().Where (p => p.customPartData != "");
			foreach (Part part in struts) {
				HackStrutCData (ship, part, numParts);
			}
			var fuelLines = ship.parts.OfType<FuelLine>().Where (p => p.customPartData != "");
			foreach (Part part in fuelLines) {
				HackStrutCData (ship, part, numParts);
			}
		}

		private Transform GetLanchTransform ()
		{

			Transform launchTransform;

			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				Debug.Log (String.Format ("[EL] launchTransform:{0}:{1}", launchTransform, SpawnTransform));
			} else {
				Vector3 offset = Vector3.up * SpawnHeightOffset;
				Transform t = this.part.transform;
				GameObject launchPos = new GameObject ();
				launchPos.transform.position = t.position;
				launchPos.transform.position += t.TransformDirection (offset);
				launchPos.transform.rotation = t.rotation;
				launchTransform = launchPos.transform;
				Destroy (launchPos);
				Debug.Log (String.Format ("[EL] launchPos {0}", launchTransform));
			}
			return launchTransform;
		}

		private void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);
			HackStruts (nship, craftType != CraftType.SUB);

			Vector3 offset = nship.Parts[0].transform.localPosition;
			nship.Parts[0].transform.Translate (-offset);
			string landedAt = "External Launchpad";
			string flag = flagname;
			Game state = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			ShipConstruction.AssembleForLaunch (nship, landedAt, flag, state, crew);

			ShipConstruction.CreateBackup (nship);
			ShipConstruction.PutShipToGround (nship, GetLanchTransform ());

			Vessel vsl = FlightGlobals.Vessels[FlightGlobals.Vessels.Count - 1];
			FlightGlobals.ForceSetActiveVessel (vsl);
			vsl.Landed = false;

			UseResources (vsl);

			vesselInfo = new DockedVesselInfo ();
			vesselInfo.name = vsl.vesselName;
			vesselInfo.vesselType = vsl.vesselType;
			vesselInfo.rootPartUId = vsl.rootPart.flightID;
			vsl.rootPart.Couple (part);
			// For some reason a second attachJoint gets created by KSP later
			// on, so delete the one created by the above call to Couple.
			if (vsl.rootPart.attachJoint != null) {
				GameObject.Destroy (vsl.rootPart.attachJoint);
				vsl.rootPart.attachJoint = null;
			}
			autoRelease = false;

			FlightGlobals.ForceSetActiveVessel (vessel);
			if (vessel.situation != Vessel.Situations.ORBITING) {
				autoRelease = true;
			}

			Staging.beginFlight ();
		}

		private float ResourceLine (string label, string resourceName, float fraction, double minAmount, double maxAmount, double available)
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
				canbuildcraft = (!use_resources);
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

		private void WindowGUI (int windowID)
		{
			Styles.Init ();
			/*
			 * ToDo:
			 * can extend FileBrowser class to see currently highlighted file?
			 * rslashphish says: public myclass(arg1, arg2) : base(arg1, arg2);
			 * KSPUtil.ApplicationRootPath - gets KSPO root
			 * expose m_files and m_selectedFile?
			 * fileBrowser = new FileBrowser(new Rect(Screen.width / 2, 100, 350, 500), title, callback, true);
			 */

			EditorLogic editor = EditorLogic.fetch;
			if (editor) return;

			if (!builduiactive) return;

			if (padResources != null && padPartsCount != vessel.Parts.Count) {
				// something docked or undocked, so rebuild the pad's resouces info
				padResources = null;
			}
			if (padResources == null) {
				padPartsCount = vessel.Parts.Count;
				padResources = new VesselResources (vessel);
			}

			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ("box");
			GUILayout.FlexibleSpace ();
			// VAB / SPH selection
			if (GUILayout.Toggle (craftType == CraftType.VAB, "VAB", GUILayout.Width (80))) {
				craftType = CraftType.VAB;
			}
			if (GUILayout.Toggle (craftType == CraftType.SPH, "SPH", GUILayout.Width (80))) {
				craftType = CraftType.SPH;
			}
			if (GUILayout.Toggle (craftType == CraftType.SUB, "SubAss", GUILayout.Width (160))) {
				craftType = CraftType.SUB;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			string strpath = HighLogic.SaveFolder;

			if (GUILayout.Button ("Select Craft", Styles.normal, GUILayout.ExpandWidth (true))) {
				string [] dir = new string[] {"SPH", "VAB", "../Subassemblies"};
				bool stock = HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels;
				if (craftType == CraftType.SUB)
					HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = false;
				//GUILayout.Button is "true" when clicked
				craftlist = new CraftBrowser (new Rect (Screen.width / 2, 100, 350, 500), dir[(int)craftType], strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
				showcraftbrowser = true;
				HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = stock;
			}

			if (craftConfig != null && buildCost != null) {
				GUILayout.Box ("Selected Craft:	" + craftConfig.GetValue ("ship"), Styles.white);

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

				canbuildcraft = true;	   // default to can build - if something is stopping us from building, we will set to false later

				foreach (var br in buildCost.required) {
					double a = br.amount;
					double available = -1;

					if (br.name != "KerbalMinutes") {
						available = padResources.ResourceAmount (br.name);
					}
					ResourceLine (br.name, br.name, 1.0f, a, a, available);
				}
				foreach (var br in buildCost.optional) {
					double available = padResources.ResourceAmount (br.name);
					if (!resourcesliders.ContainsKey (br.name)) {
						resourcesliders.Add (br.name, 1);
					}
					resourcesliders[br.name] = ResourceLine (br.name, br.name, resourcesliders[br.name], 0, br.amount, available);
				}

				GUILayout.EndScrollView ();

				// Build button
				if (canbuildcraft) {
					if (GUILayout.Button ("Build", Styles.normal, GUILayout.ExpandWidth (true))) {
						BuildAndLaunchCraft ();
						// Reset the UI
						craftConfig = null;
						buildCost = null;
						resourcesliders = new Dictionary<string, float>();;
						builduiactive = false;
						UpdateGUIState ();
					}
				} else {
					GUILayout.Box ("You do not have the resources to build this craft", Styles.red);
				}
			} else {
				GUILayout.Box ("You must select a craft before you can build", Styles.red);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				HideBuildMenu ();
			}

			showbuilduionload = GUILayout.Toggle (showbuilduionload, "Show on StartUp");

			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			//DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is
			//clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
			//dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
			//it may "cover up" your controls and make them stop responding to the mouse.
			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		// called when the user selects a craft the craft browser
		private void craftSelectComplete (string filename, string flagname)
		{
			showcraftbrowser = false;
			this.flagname = flagname;
			ConfigNode craft = ConfigNode.Load (filename);

			// Get list of resources required to build vessel
			if ((buildCost = getBuildCost (craft)) != null)
				craftConfig = craft;
		}

		// called when the user clicks cancel in the craft browser
		private void craftSelectCancel ()
		{
			showcraftbrowser = false;

			buildCost = null;
			craftConfig = null;
		}

		// =====================================================================================================================================================
		// Event Hooks
		// See http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html for some help on what fires when

		private void Start ()
		{
			// If "Show GUI on StartUp" ticked, show the GUI
			if (showbuilduionload) {
				ShowBuildMenu ();
			}
		}

		public override void OnFixedUpdate ()
		{
			if (vesselInfo != null && !vessel.packed) {
				if (CheckKerbalMinutes ()) {
					ClearKerbalMinutes ();
					if (autoRelease) {
						ReleaseVessel ();
					}
					UpdateGUIState ();
				}
			}
		}

		private void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;
			windowpos = GUILayout.Window (1, windowpos, WindowGUI, "Extraplanetary Launchpad: " + vessel.situation.ToString (), GUILayout.Width (600));
			if (showcraftbrowser) {
				craftlist.OnGUI ();
			}
		}

		public override void OnSave (ConfigNode node)
		{
			if (vesselInfo != null) {
				ConfigNode vi = node.AddNode ("DockedVesselInfo");
				vesselInfo.Save (vi);
				vi.AddValue ("autoRelease", autoRelease);
			}
		}

		private void dumpxform (Transform t, string n = "")
		{
			Debug.Log (String.Format ("[EL] {0}", n + t.name));
			foreach (Transform c in t)
				dumpxform (c, n + t.name + ".");
		}

		public override void OnLoad (ConfigNode node)
		{
			dumpxform (part.transform);

			enabled = false;
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);

			GameEvents.onVesselSituationChange.Add (onVesselSituationChange);
			GameEvents.onVesselChange.Add (onVesselChange);

			if (node.HasNode ("DockedVesselInfo")) {
				ConfigNode vi = node.GetNode ("DockedVesselInfo");
				vesselInfo = new DockedVesselInfo ();
				vesselInfo.Load (vi);
				bool.TryParse (vi.GetValue ("autoRelease"), out autoRelease);
			}
			UpdateGUIState ();
		}

		public override void OnAwake ()
		{
			if (!kethane_checked) {
				kethane_present = CheckForKethane ();
				kethane_checked = true;
			}
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (force_resource_use || (kethane_present && !DebugPad)) {
				use_resources = true;
			}
			foreach (PartResource res in part.Resources) {
				if (res.resourceName == "KerbalMinutes") {
					KerbalMinutes = res;
					break;
				}
			}
			part.force_activate ();
		}

		void OnDestroy ()
		{
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);

			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			GameEvents.onVesselChange.Remove (onVesselChange);
		}

		// =====================================================================================================================================================
		// Flight UI and Action Group Hooks

		[KSPEvent (guiActive = true, guiName = "Show Build Menu", active = true)]
		public void ShowBuildMenu ()
		{
			builduiactive = true;
			UpdateGUIState ();
		}

		[KSPEvent (guiActive = true, guiName = "Hide Build Menu", active = false)]
		public void HideBuildMenu ()
		{
			builduiactive = false;
			UpdateGUIState ();
		}

		[KSPEvent (guiActive = true, guiName = "Release", active = false)]
		public void ReleaseVessel ()
		{
			vessel[vesselInfo.rootPartUId].Undock (vesselInfo);
			vesselInfo = null;
			UpdateGUIState ();
		}

		[KSPAction ("Show Build Menu")]
		public void EnableBuildMenuAction (KSPActionParam param)
		{
			ShowBuildMenu ();
		}

		[KSPAction ("Hide Build Menu")]
		public void DisableBuildMenuAction (KSPActionParam param)
		{
			HideBuildMenu ();
		}

		[KSPAction ("Toggle Build Menu")]
		public void ToggleBuildMenuAction (KSPActionParam param)
		{
			if (builduiactive) {
				HideBuildMenu ();
			} else {
				ShowBuildMenu ();
			}
		}

		[KSPAction ("Release Vessel")]
		public void ReleaseVesselAction (KSPActionParam param)
		{
			if (vesselInfo != null && CheckKerbalMinutes ()) {
				ReleaseVessel ();
			}
		}

		// =====================================================================================================================================================
		// Build Helper Functions

		public BuildCost.CostReport getBuildCost (ConfigNode craft)
		{
			ShipConstruct ship = new ShipConstruct ();
			ship.LoadShip (craft);
			GameObject ro = ship.parts[0].localRoot.gameObject;
			Vessel dummy = ro.AddComponent<Vessel>();
			dummy.Initialize (true);

			BuildCost resources = new BuildCost ();

			foreach (Part p in ship.parts) {
				resources.addPart (p);
			}
			dummy.Die ();

			return resources.cost;
		}

		private void onVesselSituationChange (GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
		{
			if (vs.host != vessel)
				return;
			UpdateGUIState ();
		}

		void onVesselChange (Vessel v)
		{
			builduivisible = (v == vessel);
			UpdateGUIState ();
		}

		void onHideUI ()
		{
			builduivisible = false;
			UpdateGUIState ();
		}

		void onShowUI ()
		{
			builduivisible = true;
			UpdateGUIState ();
		}
	}

}
