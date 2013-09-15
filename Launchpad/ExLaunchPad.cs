using System;
using System.Collections.Generic;
using System.Linq;
//using System.IO;	  // needed for Path manipulation
//using Uri;
using UnityEngine;

using KSP.IO;

namespace ExLP {

/// <summary>
/// TODO
/// </summary>
public class ExLaunchPad : PartModule
{

	[KSPField]
	public bool debug = false;

	//public static bool kethane_present = CheckForKethane();
	public static bool kethane_present;

	public enum crafttype { SPH, VAB };

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
		public bool init = true;
		public bool linklfosliders = true;
		public bool showvab = true;
		public bool showsph = false;
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
		public Vessel vessel;
	}

	int padPartsCount;					// the number of parts in the pad vessel (for docking detection)
	VesselResources padResources;		// resources available to the pad

	[KSPField(isPersistant = false)]
	public float SpawnHeightOffset = 1.0f;	// amount of pad between origin and open space

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
	// =====================================================================================================================================================
	// UI Functions

	private void UseResources(Vessel craft)
	{
		VesselResources craftResources = new VesselResources(craft);
		craftResources.RemoveAllResources();

		// Solid Fuel is always full capacity, so put it all back
		craftResources.TransferResource("SolidFuel", craftResources.ResourceCapacity("SolidFuel"));

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
		uis.vessel.situation = Vessel.Situations.LANDED;
		uis.vessel.state = Vessel.State.ACTIVE;
		uis.vessel.Landed = false;
		uis.vessel.Splashed = false;
		uis.vessel.GoOnRails();
		uis.vessel.rigidbody.WakeUp();
		uis.vessel.ResumeStaging();
		uis.vessel.landedAt = "External Launchpad";
		InputLockManager.ClearControlLocks();
	}

	private void BuildAndLaunchCraft()
	{
		// build craft
		ShipConstruct nship = ShipConstruction.LoadShip(uis.craftfile);

		Vector3 offset = Vector3.up * SpawnHeightOffset;
		Transform t = this.part.transform;

		string landedAt = "External Launchpad";
		string flag = uis.flagname;
		Game state = FlightDriver.FlightStateCache;
		VesselCrewManifest crew = new VesselCrewManifest ();

		GameObject launchPos = new GameObject ();
		launchPos.transform.position = t.position;
		launchPos.transform.position += t.TransformDirection(offset);
		launchPos.transform.rotation = t.rotation;
		ShipConstruction.CreateBackup(nship);
		ShipConstruction.PutShipToGround(nship, launchPos.transform);
		Destroy(launchPos);

		ShipConstruction.AssembleForLaunch(nship, landedAt, flag, state, crew);

		Vessel vessel = FlightGlobals.ActiveVessel;
		vessel.Landed = false;

		if (kethane_present && !debug)
			UseResources(vessel);

		Staging.beginFlight();

		uis.timer = 3.0f;
		uis.vessel = vessel;
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
			uis.canbuildcraft = (!kethane_present || debug);
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
		 *
		 * Style declarations messy - how do I dupe them easily?
		 */
		if (uis.init)
		{
			uis.init = false;
		}

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
		if (GUILayout.Toggle(uis.showvab, "VAB", GUILayout.Width(80))) {
			uis.showvab = true;
			uis.showsph = false;
			uis.ct = crafttype.VAB;
		}
		if (GUILayout.Toggle(uis.showsph, "SPH")) {
			uis.showvab = false;
			uis.showsph = true;
			uis.ct = crafttype.SPH;
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		string strpath = HighLogic.CurrentGame.Title.Split(new string[] { " (Sandbox)" }, StringSplitOptions.None).First();

		if (GUILayout.Button("Select Craft", Styles.normal, GUILayout.ExpandWidth(true))) {
			//GUILayout.Button is "true" when clicked
			uis.craftlist = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), uis.ct.ToString(), strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
			uis.showcraftbrowser = true;
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

					// Close the UI
					HideBuildMenu();
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

	// Called each time the GUI is painted
	private void drawGUI()
	{
		GUI.skin = HighLogic.Skin;
		uis.windowpos = GUILayout.Window(1, uis.windowpos, WindowGUI, "Extraplanetary Launchpads", GUILayout.Width(600));
	}

	// Called ONCE at start
	private void Start()
	{
		// If "Show GUI on StartUp" ticked, show the GUI
		if (uis.showbuilduionload) {
			ShowBuildMenu();
		}
	}


	// Fired maybe multiple times a frame, maybe once every n frames
	public override void OnFixedUpdate()
	{
		// ToDo: Should not be checking this every frame - once per craft switch
		// OnVesselChange may be ideal but I cannot seem to get it to fire
		// Landed / Flying check should probably be with this code, but moved it elsewhere while this is firing so often

		// Does the UI want to be visible?
		if (uis.builduiactive) {
			// Decide if the build menu is allowed to be visible
			if (this.vessel == FlightGlobals.ActiveVessel) {
				// Yes - check if it is currently not visible
				if (!uis.builduivisible) {
					// Going from invisible to visible
					uis.builduivisible = true;
					RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI)); //start the GUI
				}
			} else {
				// No - check if it is currently visible
				if (uis.builduivisible) {
					// Going from visible to invisible
					uis.builduivisible = false;
					RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //stop the GUI
				}
			}
		}
	}

	/*
	// Called when you change vessel
	// ToDo: Cannot seem to get this code to fire...
	private void OnVesselChange()
	{
		if (this.vessel == FlightGlobals.ActiveVessel) {
			ShowBuildMenu();
		} else {
			HideBuildMenu();
		}
	}
	*/

	public void Update()
	{
		if (uis.vessel && uis.timer >= 0) {
			uis.timer -= Time.deltaTime;
			if (uis.timer <= 0) {
				FixCraftLock();
				uis.vessel = null;
			}
		}
	}

	// Fired ONCE per frame
	public override void OnUpdate()
	{
		// Update state of context buttons depending on state of UI
		// ToDo: Move to something fired when the GUI is updated?
		Events["ShowBuildMenu"].active = !uis.builduiactive;
		Events["HideBuildMenu"].active = uis.builduiactive;
	}

	// Fired multiple times per frame in response to GUI events
	private void OnGUI()
	{
		if (uis.showcraftbrowser) {
			uis.craftlist.OnGUI();
		}
	}

	/*
	// ToDo: What Does this Do?
	private void OnLoad()
	{
		bases = FlightGlobals.fetch.vessels;
		foreach (Vessel v in bases) {
			print(v.name);
		}
	}
	*/

	// Fired when KSP saves
	public override void OnSave(ConfigNode node)
	{
		PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
		config.SetValue("Window Position", uis.windowpos);
		config.SetValue("Show Build Menu on StartUp", uis.showbuilduionload);
		config.save();
	}


	// Fired when KSP loads
	public override void OnLoad(ConfigNode node)
	{
		kethane_present = CheckForKethane();
		LoadConfigFile();
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
		// Only allow enabling the menu if we are in a suitable place
		if (((this.vessel.situation == Vessel.Situations.LANDED) ||
				(this.vessel.situation == Vessel.Situations.PRELAUNCH) ||
				(this.vessel.situation == Vessel.Situations.SPLASHED))) {
			RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI)); //start the GUI
			uis.builduiactive = true;
		}
	}

	[KSPEvent(guiActive = true, guiName = "Hide Build Menu", active = false)]
	public void HideBuildMenu()
	{
		RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //stop the GUI
		uis.builduiactive = false;
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
				if (r.resourceName == "IntakeAir") {
					// Ignore intake Air
					continue;
				}

				if (!resources.ContainsKey(r.resourceName)) {
					resources[r.resourceName] = 0.0;
				}
				resources[r.resourceName] += r.maxAmount;
			}
		}
		if (missing_parts.Count > 0) {
			MissingPopup(missing_parts);
			return null;
		}

		// RocketParts for the hull is a separate entity to RocketParts in
		// storage containers
		PartResourceDefinition rpdef;
		rpdef = PartResourceLibrary.Instance.GetDefinition("RocketParts");
		uis.hullRocketParts = mass / rpdef.density;

		// If Solid Fuel is used, convert to RocketParts
		if (resources.ContainsKey("SolidFuel")) {
			PartResourceDefinition sfdef;
			sfdef = PartResourceLibrary.Instance.GetDefinition("SolidFuel");
			double sfmass = resources["SolidFuel"] * sfdef.density;
			double sfparts = sfmass / rpdef.density;
			uis.hullRocketParts += sfparts;
			resources.Remove("SolidFuel");
		}

		// If there is JetFuel (ie LF only tanks as well as LFO tanks - eg a SpacePlane) then split the Surplus LF off as "JetFuel"
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
}

public class Recycler : PartModule
{
	double busyTime;
	bool recyclerActive;
	[KSPField] public float RecycleRate = 1.0f;
	[KSPField (guiName = "State", guiActive = true)] public string status;

	public void OnTriggerStay(Collider col)
	{
		if (!recyclerActive
			|| Planetarium.GetUniversalTime() <= busyTime
			|| !col.CompareTag("Untagged")
			|| col.gameObject.name == "MapOverlay collider")	// kethane
			return;
		Part p = col.attachedRigidbody.GetComponent<Part>();
		Debug.Log(String.Format("[EL] {0}", p));
		if (p != null && p.vessel != null && p.vessel != vessel) {
			float mass;
			if (p.vessel.isEVA) {
				mass = RecycleKerbal(p.vessel);
			} else {
				mass = RecycleVessel(p.vessel);
			}
			busyTime = Planetarium.GetUniversalTime() + mass / RecycleRate;
		}
	}

	public float RecycleKerbal(Vessel v)
	{
		if (!v.isEVA)
			return 0;
		VesselResources recycler = new VesselResources(vessel);
		double remain;

		// idea and numbers taken from Kethane
		if (v.GetVesselCrew()[0].isBadass) {
			v.rootPart.explosionPotential = 10000;
		}
		FlightGlobals.ForceSetActiveVessel(this.vessel);
		float mass = v.rootPart.mass;
		v.rootPart.explode();

		remain = recycler.TransferResource("Kethane", 150);
		Debug.Log(String.Format("[EL] {0}-Kethane: {1} taken {2} reclaimed, {3} lost", v.name, 150, 150 - remain, remain));

		remain = recycler.TransferResource("Metal", 1);
		Debug.Log(String.Format("[EL] {0}-Metal: {1} taken {2} reclaimed, {3} lost", v.name, 1, 1 - remain, remain));
		return mass;
	}

	public float RecycleVessel(Vessel v)
	{
		float ConversionEfficiency = 0.8f;
		double amount, remain;
		VesselResources recycler = new VesselResources(vessel);
		VesselResources scrap = new VesselResources(v);
		PartResourceDefinition rpdef;
		rpdef = PartResourceLibrary.Instance.GetDefinition("RocketParts");

		foreach (string resource in scrap.resources.Keys) {
			remain = amount = scrap.ResourceAmount (resource);
			// Pull out solid fuel, but lose it.
			scrap.TransferResource(resource, -amount);
			if (resource != "SolidFuel") {
				// anything left over just evaporates
				remain = recycler.TransferResource(resource, amount);
			}
			Debug.Log(String.Format("[EL] {0}-{1}: {2} taken {3} reclaimed, {4} lost", v.name, resource, amount, amount - remain, remain));
		}
		float mass = v.GetTotalMass();
		amount = mass * ConversionEfficiency / rpdef.density;
		remain = recycler.TransferResource("RocketParts", amount);
		Debug.Log(String.Format("[EL] {0}: hull rocket parts {1} taken {2} reclaimed {3} lost", v.name, amount, amount - remain, remain));
		v.Die();
		return mass;
	}

	[KSPEvent(guiActive = true, guiName = "Activate Recycler", active = true)]
	public void Activate()
	{
		recyclerActive = true;
		Events["Activate"].active = false;
		Events["Deactivate"].active = true;
	}

	[KSPEvent(guiActive = true, guiName = "Deactivate Recycler",
	 active = false)]
	public void Deactivate()
	{
		recyclerActive = false;
		Events["Activate"].active = true;
		Events["Deactivate"].active = false;
	}

	public override void OnLoad(ConfigNode node)
	{
		Deactivate();
	}

	public override void OnUpdate()
	{
		if (Planetarium.GetUniversalTime() <= busyTime) {
			status = "Busy";
		} else if (recyclerActive) {
			status = "Active";
		} else {
			status = "Inactive";
		}
	}
}

}
