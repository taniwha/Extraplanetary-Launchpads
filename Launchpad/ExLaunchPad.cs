using System;
using System.Collections.Generic;
using System.Linq;
//using System.IO;	  // needed for Path manipulation
//using Uri;
using UnityEngine;

using KSP.IO;

namespace ExLP {

public class ExLaunchPad : PartModule
{

	[KSPField]
	public bool DebugPad = false;

	//public static bool kethane_present = CheckForKethane();
	public static bool kethane_present;

	public enum crafttype { SPH, VAB, SUB };

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

		public static void Init()
		{
			if (initialized)
				return;
			initialized = true;

			normal = new GUIStyle(GUI.skin.button);
			normal.normal.textColor = normal.focused.textColor = Color.white;
			normal.hover.textColor = normal.active.textColor = Color.yellow;
			normal.onNormal.textColor = normal.onFocused.textColor = normal.onHover.textColor = normal.onActive.textColor = Color.green;
			normal.padding = new RectOffset(8, 8, 8, 8);

			red = new GUIStyle(GUI.skin.box);
			red.padding = new RectOffset(8, 8, 8, 8);
			red.normal.textColor = red.focused.textColor = Color.red;

			yellow = new GUIStyle(GUI.skin.box);
			yellow.padding = new RectOffset(8, 8, 8, 8);
			yellow.normal.textColor = yellow.focused.textColor = Color.yellow;

			green = new GUIStyle(GUI.skin.box);
			green.padding = new RectOffset(8, 8, 8, 8);
			green.normal.textColor = green.focused.textColor = Color.green;

			white = new GUIStyle(GUI.skin.box);
			white.padding = new RectOffset(8, 8, 8, 8);
			white.normal.textColor = white.focused.textColor = Color.white;

			label = new GUIStyle(GUI.skin.label);
			label.normal.textColor = label.focused.textColor = Color.white;
			label.alignment = TextAnchor.MiddleCenter;

			slider = new GUIStyle(GUI.skin.horizontalSlider);
			slider.margin = new RectOffset(0, 0, 0, 0);

			sliderText = new GUIStyle(GUI.skin.label);
			sliderText.alignment = TextAnchor.MiddleCenter;
			sliderText.margin = new RectOffset(0, 0, 0, 0);
		}
	}

	public class UIStatus
	{
		public Rect windowpos;
		public bool builduiactive = false;	// Whether the build menu is open or closed
		public bool builduivisible = true;	// Whether the build menu is allowed to be shown
		public bool showbuilduionload = false;
		public bool linklfosliders = true;
		public bool canbuildcraft = false;
		public crafttype ct = crafttype.VAB;
		public string craftfile = null;
		public string flagname = null;
		public CraftBrowser craftlist = null;
		public bool showcraftbrowser = false;
		public ConfigNode craftnode = null;
		public bool craftselected = false;
		public Vector2 resscroll;
		public Dictionary<string, double> requiredresources = null;
		public double hullRocketParts = 0.0;
		public Dictionary<string, float> resourcesliders = new Dictionary<string, float>();

		public float timer;
		public Vessel launchee;
		public DockedVesselInfo vesselInfo;
	}

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
				string[] keyval = parms[i].Split(':');
				string Key = keyval[0].Trim();
				string Value = keyval[1].Trim();
				if (Key == "tgt") {
					targetName = Value.Split('_')[0];
				} else if (Key == "pos") {
					pos = KSPUtil.ParseVector3 (Value);
				} else if (Key == "dir") {
					dir = KSPUtil.ParseVector3 (Value);
				}
			}
			target = null;
			Transform xform = gameObject.transform;
			RaycastHit hitInfo;
			Vector3 castPos = xform.TransformPoint(pos);
			Vector3 castDir = xform.TransformDirection(dir);
			if (Physics.Raycast (castPos, castDir, out hitInfo, maxLength)) {
				GameObject hit = hitInfo.collider.gameObject;
				target = EditorLogic.GetComponentUpwards<Part>(hit);
			}
			Debug.Log(String.Format("[EL] {0} {1} {2} {3}", target, targetName, xform.position, xform.rotation));
		}
	}

	int padPartsCount;					// the number of parts in the pad vessel (for docking detection)
	VesselResources padResources;		// resources available to the pad

	[KSPField(isPersistant = false)]
	public float SpawnHeightOffset = 0.0f;	// amount of pad between origin and open space

	[KSPField(isPersistant = false)]
	public string SpawnTransform;

	private UIStatus uis = new UIStatus();

	//private List<Vessel> bases;

	private static bool CheckForKethane()
	{
		if (AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "MMI_Kethane")) {
			Debug.Log("[EL] Kethane found");
			return true;
		}
		Debug.Log("[EL] Kethane not found");
		return false;
	}

	private void UpdateGUIState()
	{
		bool can_build = false;
		bool can_release = false;
		var situation = Vessel.Situations.LANDED;

		if (vessel) {
			situation = vessel.situation;
		}
		if (uis.vesselInfo == null
			&& (situation == Vessel.Situations.LANDED
				|| situation == Vessel.Situations.ORBITING
				|| situation == Vessel.Situations.PRELAUNCH
				|| situation == Vessel.Situations.SPLASHED)) {
			can_build = true;
		}
		if (uis.vesselInfo != null) {
			can_release = true;
		}
		enabled = can_build && uis.builduiactive && uis.builduivisible;
		Events["ShowBuildMenu"].active = can_build && !uis.builduiactive;
		Events["HideBuildMenu"].active = can_build && uis.builduiactive;
		Events["ReleaseVessel"].active = can_release;
	}
	// =====================================================================================================================================================
	// UI Functions

	private void UseResources(Vessel craft)
	{
		VesselResources craftResources = new VesselResources(craft);

		// Remove all resources that we might later fill (hull resources will not be touched)
		HashSet<string> resources_to_remove = new HashSet<string>(uis.requiredresources.Keys);
		craftResources.RemoveAllResources(resources_to_remove);

		// remove rocket parts required for the hull and solid fuel
		padResources.TransferResource("RocketParts", -uis.hullRocketParts);

		// use resources
		foreach (KeyValuePair<string, double> pair in uis.requiredresources) {
			// If resource is "JetFuel", rename to "LiquidFuel"
			string res = pair.Key;
			if (pair.Key == "JetFuel") {
				res = "LiquidFuel";
				if (pair.Value == 0)
					continue;
			}
			if (!uis.resourcesliders.ContainsKey(pair.Key)) {
				Debug.Log(String.Format("[EL] missing slider {0}", pair.Key));
				continue;
			}
			// Calculate resource cost based on slider position - note use pair.Key NOT res! we need to use the position of the dedicated LF slider not the LF component of LFO slider
			double tot = pair.Value * uis.resourcesliders[pair.Key];
			// Transfer the resource from the vessel doing the building to the vessel being built
			padResources.TransferResource(res, -tot);
			craftResources.TransferResource(res, tot);
		}
	}

	private void FixCraftLock()
	{
		// Many thanks to Snjo (firespitter)
		uis.launchee.situation = Vessel.Situations.LANDED;
		uis.launchee.state = Vessel.State.ACTIVE;
		uis.launchee.Landed = false;
		uis.launchee.Splashed = false;

		uis.launchee.GoOnRails();
		uis.launchee.rigidbody.WakeUp();
		uis.launchee.ResumeStaging();
		uis.launchee.landedAt = "External Launchpad";
		InputLockManager.ClearControlLocks();
	}

	private void HackStrutCData(ShipConstruct ship, Part p, int numParts)
	{
		Debug.Log(String.Format("[EL] before {0}", p.customPartData));
		string[] Params = p.customPartData.Split(';');
		for (int i = 0; i < Params.Length; i++) {
			string[] keyval = Params[i].Split(':');
			string Key = keyval[0].Trim();
			string Value = keyval[1].Trim();
			if (Key == "tgt") {
				string[] pnameval = Value.Split('_');
				string pname = pnameval[0];
				int val = int.Parse(pnameval[1]);
				if (val == -1) {
					Strut strut = new Strut(p, Params);
					if (strut.target != null) {
						val = ship.parts.IndexOf(strut.target);
					}
				}
				if (val != -1) {
					val += numParts;
				}
				Params[i] = "tgt: " + pname + "_" + val.ToString();
				break;
			}
		}
		p.customPartData = String.Join("; ", Params);
		Debug.Log(String.Format("[EL] after {0}", p.customPartData));
	}

	private void HackStruts(ShipConstruct ship, bool addCount)
	{
		int numParts = vessel.parts.Count;
		if (!addCount)
			numParts = 0;

		var struts = ship.parts.OfType<StrutConnector>().Where(p => p.customPartData != "");
		foreach (Part part in struts) {
			HackStrutCData(ship, part, numParts);
		}
		var fuelLines = ship.parts.OfType<FuelLine>().Where(p => p.customPartData != "");
		foreach (Part part in fuelLines) {
			HackStrutCData(ship, part, numParts);
		}
	}

	private Transform GetLanchTransform()
	{

		Transform launchTransform;

		if (SpawnTransform != "") {
			launchTransform = part.FindModelTransform (SpawnTransform);
			Debug.Log(String.Format("[EL] launchTransform:{0}:{1}", launchTransform, SpawnTransform));
		} else {
			Vector3 offset = Vector3.up * SpawnHeightOffset;
			Transform t = this.part.transform;
			GameObject launchPos = new GameObject ();
			launchPos.transform.position = t.position;
			launchPos.transform.position += t.TransformDirection(offset);
			launchPos.transform.rotation = t.rotation;
			launchTransform = launchPos.transform;
			Destroy(launchPos);
			Debug.Log(String.Format("[EL] launchPos {0}", launchTransform));
		}
		return launchTransform;
	}

	private void BuildAndLaunchCraft()
	{
		// build craft
		ShipConstruct nship = ShipConstruction.LoadShip(uis.craftfile);
		HackStruts(nship, uis.ct == crafttype.SUB);

		Vector3 offset = nship.Parts[0].transform.localPosition;
		nship.Parts[0].transform.Translate(-offset);
		string landedAt = "External Launchpad";
		string flag = uis.flagname;
		Game state = FlightDriver.FlightStateCache;
		VesselCrewManifest crew = new VesselCrewManifest ();

		ShipConstruction.AssembleForLaunch(nship, landedAt, flag, state, crew);

		ShipConstruction.CreateBackup(nship);
		ShipConstruction.PutShipToGround(nship, GetLanchTransform());

		Vessel vsl = FlightGlobals.Vessels[FlightGlobals.Vessels.Count - 1];
		FlightGlobals.ForceSetActiveVessel(vsl);
		vsl.Landed = false;

		if (kethane_present && !DebugPad)
			UseResources(vsl);

		if (vessel.situation == Vessel.Situations.ORBITING) {
			uis.vesselInfo = new DockedVesselInfo();
			uis.vesselInfo.name = vsl.vesselName;
			uis.vesselInfo.vesselType = vsl.vesselType;
			uis.vesselInfo.rootPartUId = vsl.rootPart.flightID;
			vsl.rootPart.Couple(part);
			// For some reason a second attachJoint gets created by KSP later
			// on, so delete the one created by the above call to Couple.
			if (vsl.rootPart.attachJoint != null) {
				GameObject.Destroy(vsl.rootPart.attachJoint);
				vsl.rootPart.attachJoint = null;
			}
			FlightGlobals.ForceSetActiveVessel (vessel);
		} else {
			uis.timer = 3.0f;
			uis.launchee = vsl;
		}

		Staging.beginFlight();
	}

	private float ResourceLine(string label, string resourceName, float fraction, double minAmount, double maxAmount, double available)
	{
		GUILayout.BeginHorizontal();

		// Resource name
		GUILayout.Box(label, Styles.white, GUILayout.Width(120), GUILayout.Height(40));

		// Fill amount
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		// limit slider to 0.5% increments
		if (minAmount == maxAmount) {
			GUILayout.Box("Must be 100%", GUILayout.Width(300), GUILayout.Height(20));
			fraction = 1.0F;
		} else {
			fraction = (float)Math.Round(GUILayout.HorizontalSlider(fraction, 0.0F, 1.0F, Styles.slider, GUI.skin.horizontalSliderThumb, GUILayout.Width(300), GUILayout.Height(20)), 3);
			fraction = (Mathf.Floor(fraction * 200)) / 200;
			GUILayout.Box((fraction * 100).ToString() + "%", Styles.sliderText, GUILayout.Width(300), GUILayout.Height(20));
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();

		double required = minAmount * (1 - fraction)  + maxAmount * fraction;

		// Calculate if we have enough resources to build
		GUIStyle requiredStyle = Styles.green;
		if (available < required) {
			requiredStyle = Styles.red;
			// prevent building unless debug mode is on, or kethane is not
			// installed (kethane is required for resource production)
			uis.canbuildcraft = (!kethane_present || DebugPad);
		}
		// Required and Available
		GUILayout.Box((Math.Round(required, 2)).ToString(), requiredStyle, GUILayout.Width(75), GUILayout.Height(40));
		GUILayout.Box((Math.Round(available, 2)).ToString(), Styles.white, GUILayout.Width(75), GUILayout.Height(40));

		// Flexi space to make sure any unused space is at the right-hand edge
		GUILayout.FlexibleSpace();

		GUILayout.EndHorizontal();

		return fraction;
	}

	private void WindowGUI(int windowID)
	{
		Styles.Init();
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

		if (!uis.builduiactive) return;

		if (padResources != null && padPartsCount != vessel.Parts.Count) {
			// something docked or undocked, so rebuild the pad's resouces info
			padResources = null;
		}
		if (padResources == null) {
			padPartsCount = vessel.Parts.Count;
			padResources = new VesselResources(vessel);
		}

		GUILayout.BeginVertical();

		GUILayout.BeginHorizontal("box");
		GUILayout.FlexibleSpace();
		// VAB / SPH selection
		if (GUILayout.Toggle(uis.ct == crafttype.VAB, "VAB", GUILayout.Width(80))) {
			uis.ct = crafttype.VAB;
		}
		if (GUILayout.Toggle(uis.ct == crafttype.SPH, "SPH", GUILayout.Width(80))) {
			uis.ct = crafttype.SPH;
		}
		if (GUILayout.Toggle(uis.ct == crafttype.SUB, "SubAss", GUILayout.Width(160))) {
			uis.ct = crafttype.SUB;
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		string strpath = HighLogic.SaveFolder;

		if (GUILayout.Button("Select Craft", Styles.normal, GUILayout.ExpandWidth(true))) {
			string [] dir = new string[] {"SPH", "VAB", "../Subassemblies"};
			bool stock = HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels;
			if (uis.ct == crafttype.SUB)
				HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = false;
			//GUILayout.Button is "true" when clicked
			uis.craftlist = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), dir[(int)uis.ct], strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
			uis.showcraftbrowser = true;
			HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = stock;
		}

		if (uis.craftselected) {
			GUILayout.Box("Selected Craft:	" + uis.craftnode.GetValue("ship"), Styles.white);

			// Resource requirements
			GUILayout.Label("Resources required to build:", Styles.label, GUILayout.Width(600));

			// Link LFO toggle

			uis.linklfosliders = GUILayout.Toggle(uis.linklfosliders, "Link RocketFuel sliders for LiquidFuel and Oxidizer");

			uis.resscroll = GUILayout.BeginScrollView(uis.resscroll, GUILayout.Width(600), GUILayout.Height(300));

			GUILayout.BeginHorizontal();

			// Headings
			GUILayout.Label("Resource", Styles.label, GUILayout.Width(120));
			GUILayout.Label("Fill Percentage", Styles.label, GUILayout.Width(300));
			GUILayout.Label("Required", Styles.label, GUILayout.Width(75));
			GUILayout.Label("Available", Styles.label, GUILayout.Width(75));
			GUILayout.EndHorizontal();

			uis.canbuildcraft = true;	   // default to can build - if something is stopping us from building, we will set to false later

			if (!uis.requiredresources.ContainsKey("RocketParts")) {
				// if the craft to be built has no rocket parts storage, then the amount to use is not adjustable
				string resname = "RocketParts";
				double available = padResources.ResourceAmount(resname);
				ResourceLine(resname, resname, 1.0F, uis.hullRocketParts, uis.hullRocketParts, available);
			}

			// Cycle through required resources
			foreach (KeyValuePair<string, double> pair in uis.requiredresources) {
				string resname = pair.Key;	// Holds REAL resource name. May need to translate from "JetFuel" back to "LiquidFuel"
				string reslabel = resname;	 // Resource name for DISPLAY purposes only. Internally the app uses pair.Key
				if (reslabel == "JetFuel") {
					if (pair.Value == 0f) {
						// Do not show JetFuel line if not being used
						continue;
					}
					//resname = "JetFuel";
					resname = "LiquidFuel";
				}
				if (!uis.resourcesliders.ContainsKey(pair.Key)) {
					uis.resourcesliders.Add(pair.Key, 1);
				}

				// If in link LFO sliders mode, rename Oxidizer to LFO (Oxidizer) and LiquidFuel to LFO (LiquidFuel)
				if (reslabel == "Oxidizer") {
					reslabel = "RocketFuel (Ox)";
				}
				if (reslabel == "LiquidFuel") {
					reslabel = "RocketFuel (LF)";
				}

				double minAmount = 0.0;
				double maxAmount = uis.requiredresources[resname];
				if (resname == "RocketParts") {
					minAmount += uis.hullRocketParts;
					maxAmount += uis.hullRocketParts;
				}

				double available = padResources.ResourceAmount(resname);
				// If LFO LiquidFuel exists and we are on LiquidFuel (Non-LFO), then subtract the amount used by LFO(LiquidFuel) from the available amount
				if (pair.Key == "JetFuel") {
					available -= uis.requiredresources["LiquidFuel"] * uis.resourcesliders["LiquidFuel"];
					if (available < 0.0)
						available = 0.0;
				}

				uis.resourcesliders[pair.Key] = ResourceLine(reslabel, pair.Key, uis.resourcesliders[pair.Key], minAmount, maxAmount, available);
				if (uis.linklfosliders) {
					float tmp = uis.resourcesliders[pair.Key];
					if (pair.Key == "Oxidizer") {
						uis.resourcesliders["LiquidFuel"] = tmp;
					} else if (pair.Key == "LiquidFuel") {
						uis.resourcesliders["Oxidizer"] = tmp;
					}
				}
			}

			GUILayout.EndScrollView();

			// Build button
			if (uis.canbuildcraft) {
				if (GUILayout.Button("Build", Styles.normal, GUILayout.ExpandWidth(true))) {
					BuildAndLaunchCraft();
					// Reset the UI
					uis.craftselected = false;
					uis.requiredresources = null;
					uis.resourcesliders = new Dictionary<string, float>();;
					uis.builduiactive = false;
					UpdateGUIState();
				}
			} else {
				GUILayout.Box("You do not have the resources to build this craft", Styles.red);
			}
		} else {
			GUILayout.Box("You must select a craft before you can build", Styles.red);
		}
		GUILayout.EndVertical();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close")) {
			HideBuildMenu();
		}

		uis.showbuilduionload = GUILayout.Toggle(uis.showbuilduionload, "Show on StartUp");

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		//DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is
		//clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
		//dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
		//it may "cover up" your controls and make them stop responding to the mouse.
		GUI.DragWindow(new Rect(0, 0, 10000, 20));

	}

	// called when the user selects a craft the craft browser
	private void craftSelectComplete(string filename, string flagname)
	{
		uis.showcraftbrowser = false;
		uis.craftfile = filename;
		uis.flagname = flagname;
		uis.craftnode = ConfigNode.Load(filename);
		ConfigNode[] nodes = uis.craftnode.GetNodes("PART");

		// Get list of resources required to build vessel
		if ((uis.requiredresources = getBuildCost(nodes)) != null)
			uis.craftselected = true;
	}

	// called when the user clicks cancel in the craft browser
	private void craftSelectCancel()
	{
		uis.showcraftbrowser = false;

		uis.requiredresources = null;
		uis.craftselected = false;
	}

	// =====================================================================================================================================================
	// Event Hooks
	// See http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html for some help on what fires when

	private void Start()
	{
		// If "Show GUI on StartUp" ticked, show the GUI
		if (uis.showbuilduionload) {
			ShowBuildMenu();
		}
	}

	public override void OnUpdate()
	{
		if (uis.launchee && uis.timer >= 0) {
			uis.timer -= Time.deltaTime;
			if (uis.timer <= 0) {
				FixCraftLock();
				uis.launchee = null;
			}
		}
	}

	private void OnGUI()
	{
		GUI.skin = HighLogic.Skin;
		uis.windowpos = GUILayout.Window(1, uis.windowpos, WindowGUI, "Extraplanetary Launchpad: " + vessel.situation.ToString(), GUILayout.Width(600));
		if (uis.showcraftbrowser) {
			uis.craftlist.OnGUI();
		}
	}

	public override void OnSave(ConfigNode node)
	{
		if (uis.vesselInfo != null) {
			uis.vesselInfo.Save(node.AddNode("DockedVesselInfo"));
		}

		PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
		config.SetValue("Window Position", uis.windowpos);
		config.SetValue("Show Build Menu on StartUp", uis.showbuilduionload);
		config.save();
	}

	private void dumpxform(Transform t, string n = "")
	{
		Debug.Log(String.Format("[EL] {0}", n + t.name));
		foreach (Transform c in t)
			dumpxform(c, n + t.name + ".");
	}

	public override void OnLoad(ConfigNode node)
	{
		dumpxform(part.transform);
		kethane_present = CheckForKethane();
		LoadConfigFile();

		enabled = false;
		GameEvents.onHideUI.Add(onHideUI);
		GameEvents.onShowUI.Add(onShowUI);

		GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
		GameEvents.onVesselChange.Add(onVesselChange);

		if (node.HasNode("DockedVesselInfo")) {
			uis.vesselInfo = new DockedVesselInfo();
			uis.vesselInfo.Load(node.GetNode("DockedVesselInfo"));
		}
		UpdateGUIState();
	}

	void OnDestroy()
	{
		GameEvents.onHideUI.Remove(onHideUI);
		GameEvents.onShowUI.Remove(onShowUI);

		GameEvents.onVesselSituationChange.Remove(onVesselSituationChange);
		GameEvents.onVesselChange.Remove(onVesselChange);
	}

	private void LoadConfigFile()
	{
		PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
		config.load();
		uis.windowpos = config.GetValue<Rect>("Window Position");
		uis.showbuilduionload = config.GetValue<bool>("Show Build Menu on StartUp");
	}

	// =====================================================================================================================================================
	// Flight UI and Action Group Hooks

	[KSPEvent(guiActive = true, guiName = "Show Build Menu", active = true)]
	public void ShowBuildMenu()
	{
		uis.builduiactive = true;
		UpdateGUIState();
	}

	[KSPEvent(guiActive = true, guiName = "Hide Build Menu", active = false)]
	public void HideBuildMenu()
	{
		uis.builduiactive = false;
		UpdateGUIState();
	}

	[KSPEvent(guiActive = true, guiName = "Release", active = false)]
	public void ReleaseVessel()
	{
		vessel[uis.vesselInfo.rootPartUId].Undock(uis.vesselInfo);
		uis.vesselInfo = null;
		UpdateGUIState ();
	}

	[KSPAction("Show Build Menu")]
	public void EnableBuildMenuAction(KSPActionParam param)
	{
		ShowBuildMenu();
	}

	[KSPAction("Hide Build Menu")]
	public void DisableBuildMenuAction(KSPActionParam param)
	{
		HideBuildMenu();
	}

	[KSPAction("Toggle Build Menu")]
	public void ToggleBuildMenuAction(KSPActionParam param)
	{
		if (uis.builduiactive) {
			HideBuildMenu();
		} else {
			ShowBuildMenu();
		}
	}

	[KSPAction("Release Vessel")]
	public void ReleaseVesselAction(KSPActionParam param)
	{
		if (uis.vesselInfo != null) {
			ReleaseVessel();
		}
	}

	// =====================================================================================================================================================
	// Build Helper Functions

	private void MissingPopup(Dictionary<string, bool> missing_parts)
	{
		string text = "";
		foreach (string mp in missing_parts.Keys)
			text += mp + "\n";
		int ind = uis.craftfile.LastIndexOf("/") + 1;
		string craft = uis.craftfile.Substring (ind);
		craft = craft.Remove (craft.LastIndexOf("."));
		PopupDialog.SpawnPopupDialog("Sorry", "Can't build " + craft + " due to the following missing parts\n\n" + text, "OK", false, HighLogic.Skin);
	}

	public Dictionary<string, double> getBuildCost(ConfigNode[] nodes)
	{
		float mass = 0.0f;
		Dictionary<string, double> resources = new Dictionary<string, double>();
		Dictionary<string, double> hull_resources = new Dictionary<string, double>();
		Dictionary<string, bool> missing_parts = new Dictionary<string, bool>();

		foreach (ConfigNode node in nodes) {
			string part_name = node.GetValue("part");
			part_name = part_name.Remove(part_name.LastIndexOf("_"));
			AvailablePart ap = PartLoader.getPartInfoByName(part_name);
			if (ap == null) {
				missing_parts[part_name] = true;
				continue;
			}
			Part p = ap.partPrefab;
			mass += p.mass;
			foreach (PartResource r in p.Resources) {
				if (r.resourceName == "IntakeAir" || r.resourceName == "KIntakeAir") {
					// Ignore intake Air
					continue;
				}

				Dictionary<string, double> res_dict = resources;

				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition(r.resourceName);
				if (res_def.resourceTransferMode == ResourceTransferMode.NONE
					|| res_def.resourceFlowMode == ResourceFlowMode.NO_FLOW) {
					res_dict = hull_resources;
				}

				if (!res_dict.ContainsKey(r.resourceName)) {
					res_dict[r.resourceName] = 0.0;
				}
				res_dict[r.resourceName] += r.maxAmount;
			}
		}
		if (missing_parts.Count > 0) {
			MissingPopup(missing_parts);
			return null;
		}

		// RocketParts for the hull is a separate entity to RocketParts in
		// storage containers
		PartResourceDefinition rp_def;
		rp_def = PartResourceLibrary.Instance.GetDefinition("RocketParts");
		uis.hullRocketParts = mass / rp_def.density;

		// If non pumpable resources are used, convert to RocketParts
		foreach (KeyValuePair<string, double> pair in hull_resources) {
			PartResourceDefinition res_def;
			res_def = PartResourceLibrary.Instance.GetDefinition(pair.Key);
			double hull_mass = pair.Value * res_def.density;
			double hull_parts = hull_mass / rp_def.density;
			uis.hullRocketParts += hull_parts;
		}

		// If there is JetFuel (ie LF only tanks as well as LFO tanks - eg a SpacePlane) then split off the Surplus LF as "JetFuel"
		if (resources.ContainsKey("Oxidizer") && resources.ContainsKey("LiquidFuel")) {
			double jetFuel = 0.0;
			// The LiquidFuel:Oxidizer ratio is 9:11. Try to minimize rounding effects.
			jetFuel = (11 * resources["LiquidFuel"] - 9 * resources["Oxidizer"]) / 11;
			if (jetFuel < 0.01)	{
				// Forget it. not getting far on that. Any discrepency this
				// small will be due to precision losses.
				jetFuel = 0.0;
			}
			resources["LiquidFuel"] -= jetFuel;
			resources["JetFuel"] = jetFuel;
		}

		return resources;
	}

	private void onVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
	{
		if (vs.host != vessel)
			return;
		UpdateGUIState ();
	}

	void onVesselChange(Vessel v)
	{
		uis.builduivisible = (v == vessel);
		UpdateGUIState();
	}

	void onHideUI()
	{
		uis.builduivisible = false;
		UpdateGUIState();
	}

	void onShowUI()
	{
		uis.builduivisible = true;
		UpdateGUIState();
	}
}

}
