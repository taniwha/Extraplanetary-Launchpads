using System;
using System.Collections.Generic;
using System.Linq;
//using System.IO;    // needed for Path manipulation
//using Uri;
using UnityEngine;

using KSP.IO;


/// <summary>
/// TODO
/// </summary>
public class ExLaunchPad : PartModule
{
    public enum crafttype { SPH, VAB };

    public class UIStatus
    {
        public Rect windowpos;
        public bool builduiactive = false;
        public bool showbuilduionload = false;
        public bool init = true;
        public bool linklfosliders = true;
        public bool showvab = true;
        public bool showsph = false;
        public bool canbuildcraft = false;
        public crafttype ct = crafttype.VAB;
        public string craftfile = null;
        public CraftBrowser craftlist = null;
        public bool showcraftbrowser = false;
        public ConfigNode craftnode = null;
        public bool craftselected = false;
        public Vector2 resscroll;
        public Dictionary<string, float> requiredresources = null;
        public Dictionary<string, float> resourcesliders = new Dictionary<string, float>();
    }

    private UIStatus uis = new UIStatus();

	//private List<Vessel> bases;

    // =====================================================================================================================================================
    // UI Functions

    private void WindowGUI(int windowID)
    {
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

        GUIStyle mySty = new GUIStyle(GUI.skin.button);
        mySty.normal.textColor = mySty.focused.textColor = Color.white;
        mySty.hover.textColor = mySty.active.textColor = Color.yellow;
        mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
        mySty.padding = new RectOffset(8, 8, 8, 8);

        GUIStyle redSty = new GUIStyle(GUI.skin.box);
        redSty.padding = new RectOffset(8, 8, 8, 8);
        redSty.normal.textColor = redSty.focused.textColor = Color.red;

        GUIStyle yelSty = new GUIStyle(GUI.skin.box);
        yelSty.padding = new RectOffset(8, 8, 8, 8);
        yelSty.normal.textColor = yelSty.focused.textColor = Color.yellow;

        GUIStyle grnSty = new GUIStyle(GUI.skin.box);
        grnSty.padding = new RectOffset(8, 8, 8, 8);
        grnSty.normal.textColor = grnSty.focused.textColor = Color.green;

        GUIStyle whiSty = new GUIStyle(GUI.skin.box);
        whiSty.padding = new RectOffset(8, 8, 8, 8);
        whiSty.normal.textColor = whiSty.focused.textColor = Color.white;

        GUIStyle labSty = new GUIStyle(GUI.skin.label);
        labSty.normal.textColor = labSty.focused.textColor = Color.white;
        labSty.alignment = TextAnchor.MiddleCenter;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal("box");
        GUILayout.FlexibleSpace();
        // VAB / SPH selection
        if (GUILayout.Toggle(uis.showvab, "VAB", GUILayout.Width(80)))
        {
            uis.showvab = true;
            uis.showsph = false;
            uis.ct = crafttype.VAB;
        }
        if (GUILayout.Toggle(uis.showsph, "SPH"))
        {
            uis.showvab = false;
            uis.showsph = true;
            uis.ct = crafttype.SPH;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        string strpath = HighLogic.CurrentGame.Title.Split(new string[] { " (Sandbox)" }, StringSplitOptions.None).First();

        if (GUILayout.Button("Select Craft", mySty, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
        {
            uis.craftlist = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), uis.ct.ToString(), strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
            uis.showcraftbrowser = true;
        }

        if (uis.craftselected)
        {
            GUILayout.Box("Selected Craft:  " + uis.craftnode.GetValue("ship"), whiSty);

            // Resource requirements
            GUILayout.Label("Resources required to build:", labSty, GUILayout.Width(600));

            // Link LFO toggle

            uis.linklfosliders = GUILayout.Toggle(uis.linklfosliders, "Link RocketFuel sliders for LiquidFuel and Oxidizer");

            uis.resscroll = GUILayout.BeginScrollView(uis.resscroll, GUILayout.Width(600), GUILayout.Height(300));

            GUILayout.BeginHorizontal();
			
            // Headings
            GUILayout.Label("Resource", labSty, GUILayout.Width(120));
            GUILayout.Label("Fill Percentage", labSty, GUILayout.Width(300));
            GUILayout.Label("Required", labSty, GUILayout.Width(60));
            GUILayout.Label("Available", labSty, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            uis.canbuildcraft = true;      // default to can build - if something is stopping us from building, we will set to false later

            // LFO = 55% oxidizer

            // Cycle through required resources
            foreach (KeyValuePair<string, float> pair in uis.requiredresources)
            {
                string resname = pair.Key;  // Holds REAL resource name. May neet to translate from "JetFuel" back to "LiquidFuel"
                string reslabel = resname;   // Resource name for DISPLAY purposes only. Internally the app uses pair.Key
                if (reslabel == "JetFuel")
                {
                    // Do not show JetFuel line if not being used
                    if (pair.Value == 0f)
                    {
                        continue;
                    }
                    //resname = "JetFuel";
                    resname = "LiquidFuel";
                }

                // ToDo: If you request an unknown resource type, does this crash the UI?
                List<PartResource> connectedresources = GetConnectedResources(this.part, resname);
                // If in link LFO sliders mode, rename Oxidizer to LFO (Oxidizer) and LiquidFuel to LFO (LiquidFuel)
                if (reslabel == "Oxidizer")
                {
                    reslabel = "RocketFuel (Ox)";
                }
                if (reslabel == "LiquidFuel")
                {
                    reslabel = "RocketFuel (LF)";
                }

                GUILayout.BeginHorizontal();

                // Resource name
                GUILayout.Box(reslabel, whiSty, GUILayout.Width(120), GUILayout.Height(40));
                
                // Add resource to Dictionary if it does not exist
				if (!uis.resourcesliders.ContainsKey(pair.Key)) {
                   uis.resourcesliders.Add(pair.Key, 1);
                }

                GUIStyle tmpSty = new GUIStyle(GUI.skin.label);
                tmpSty.alignment = TextAnchor.MiddleCenter;
                tmpSty.margin = new RectOffset(0, 0, 0, 0);

                GUIStyle sliSty = new GUIStyle(GUI.skin.horizontalSlider);
                sliSty.margin = new RectOffset(0, 0, 0, 0);

                // Fill amount
                GUILayout.BeginVertical();
                {
                    if (pair.Key == "RocketParts")
                    {
                        // Partial Fill for RocketParts not allowed - Instead of creating a slider, hard-wire slider position to 100%
                        uis.resourcesliders[pair.Key] = 1;
                        GUILayout.FlexibleSpace();
                        GUILayout.Box("Must be 100%", GUILayout.Width(300), GUILayout.Height(20));
                        GUILayout.FlexibleSpace();

                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        // limit slider to 0.5% increments
                        float tmp = (float)Math.Round(GUILayout.HorizontalSlider(uis.resourcesliders[pair.Key], 0.0F, 1.0F, sliSty, new GUIStyle(GUI.skin.horizontalSliderThumb), GUILayout.Width(300), GUILayout.Height(20)), 3);
                        tmp = (Mathf.Floor(tmp * 200)) / 200;

                        // Are we in link LFO mode?
                        if (uis.linklfosliders)
                        {

                            if (pair.Key == "Oxidizer")
                            {
                                uis.resourcesliders["LiquidFuel"] = tmp;
                            }
                            else if (pair.Key == "LiquidFuel")
                            {
                                uis.resourcesliders["Oxidizer"] = tmp;
                            }
                        }
                        // Assign slider value to variable
                        uis.resourcesliders[pair.Key] = tmp;
                        GUILayout.Box((tmp * 100).ToString() + "%", tmpSty, GUILayout.Width(300), GUILayout.Height(20));
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.EndVertical();


                // Calculate if we have enough resources to build
                double tot = 0;
                foreach (PartResource pr in connectedresources)
                {
                    tot += pr.amount;
                }

                // If LFO LiquidFuel exists and we are on LiquidFuel (Non-LFO), then subtract the amount used by LFO(LiquidFuel) from the available amount

                if (pair.Key == "JetFuel")
                {
                    tot -= uis.requiredresources["LiquidFuel"] * uis.resourcesliders["LiquidFuel"];
                }
				GUIStyle avail = new GUIStyle();
                if (tot < pair.Value*uis.resourcesliders[pair.Key])
                {
                    avail = redSty;
                    uis.canbuildcraft = false; // prevent building
                }
                else
                {
                    avail = grnSty;
                }

                // Required
                GUILayout.Box((Math.Round(pair.Value * uis.resourcesliders[pair.Key],2)).ToString(), avail, GUILayout.Width(60), GUILayout.Height(40));
                // Available
                GUILayout.Box(((int)tot).ToString(), whiSty, GUILayout.Width(60), GUILayout.Height(40));

                // Flexi space to make sure any unused space is at the right-hand edge
                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // Build button
            if (uis.canbuildcraft)
            {
                if(GUILayout.Button("Build", mySty, GUILayout.ExpandWidth(true)))
                {

                    // build craft
                    FlightState state = new FlightState();
                    ShipConstruct nship = ShipConstruction.LoadShip(uis.craftfile);
                    ShipConstruction.PutShipToGround(nship, this.part.transform);
                    // is this line causing bug #11 ?
                    ShipConstruction.AssembleForLaunch(nship, "External Launchpad", HighLogic.CurrentGame.flagURL, state);
                    Staging.beginFlight();
                    nship.parts[0].vessel.ResumeStaging();
                    Staging.GenerateStagingSequence(nship.parts[0].localRoot);
                    Staging.RecalculateVesselStaging(nship.parts[0].vessel);
					
                    // use resources
                    foreach (KeyValuePair<string, float> pair in uis.requiredresources)
                    {
                        // If resource is "JetFuel", rename to "LiquidFuel"
                        string res = pair.Key;
                        if (pair.Key == "JetFuel")
                        {
                             res = "LiquidFuel";
                        }

                        // Calculate resource cost based on slider position - note use pair.Key NOT res! we need to use the position of the dedicated LF slider not the LF component of LFO slider
                        double tot = pair.Value * uis.resourcesliders[pair.Key];
                        // Remove the resource from the vessel doing the building
                        this.part.RequestResource(res, tot);

                        // If doing a partial fill, remove unfilled amount from the spawned ship
                        // ToDo: Only subtract LiquidFuel from a part when it does not also have Oxidizer in it - try to leave fuel tanks in a sane state!
                        double ptot = pair.Value - tot;
                        foreach (Part p in nship.parts)
                        {
                            if (ptot > 0)
                            {
                                ptot -= p.RequestResource(res, ptot);
                            }
                            else
                            {
                                break;
                            }
                        }
                        /*
						foreach (Part p in nship.parts) {
							if (tot>pair.Value*uis.resourcesliders[res]) {
								tot -= p.RequestResource(res, tot-pair.Value);
							} else {
								break;
							}
						}
                        */
                    }
					
					//Remove the kerbals who get spawned with the ship
					foreach (Part p in nship.parts)
					{
						if (p.CrewCapacity>0) {
							print ("Part has crew");
							foreach (ProtoCrewMember m in p.protoModuleCrew)
							{	
								print("Removing crewmember:");
								print (m.name);
								p.RemoveCrewmember(m);
								m.rosterStatus = ProtoCrewMember.RosterStatus.AVAILABLE;
							}
						}
					}

                    // Reset the UI
                    uis.craftselected = false;
                    uis.requiredresources = null;
                    uis.resourcesliders = null;

                    // Close the UI
                    HideBuildMenu();
					uis.builduiactive = false;
                }
            }
            else
            {
                GUILayout.Box("You do not have the resources to build this craft", redSty);
            }
        }
        else
        {
            GUILayout.Box("You must select a craft before you can build", redSty);
        }
        GUILayout.EndVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close"))
        {
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
        uis.craftnode = ConfigNode.Load(filename);
        ConfigNode[] nodes = uis.craftnode.GetNodes("PART");

        // Get list of resources required to build vessel
        uis.requiredresources = getBuildCost(nodes);
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

    private void drawGUI()
    {
        GUI.skin = HighLogic.Skin;
        uis.windowpos = GUILayout.Window(1, uis.windowpos, WindowGUI, "Extraplanetary Launchpads", GUILayout.Width(600));
    }

    //public override void OnAwake()
	public override void OnFixedUpdate()
    {
        base.OnAwake();
        // ToDo: why do I need to load the config file here?
        // Doing it in OnLoad instead breaks the "Show on Startup" function
        LoadConfigFile();

		if (((this.vessel.situation==Vessel.Situations.LANDED) || 
		(this.vessel.situation==Vessel.Situations.PRELAUNCH) || 
		(this.vessel.situation==Vessel.Situations.SPLASHED)) && (this.vessel==FlightGlobals.ActiveVessel)){
	        if (uis.showbuilduionload)
	        {
		        ShowBuildMenu();
			} //else {
				//HideBuildMenu();
			//}
		} else {
			HideBuildMenu();
		}
    }

    // Fired each Tick?
    public override void OnUpdate()
    {
        Events["ShowBuildMenu"].active = !uis.builduiactive;
        Events["HideBuildMenu"].active = uis.builduiactive;
    }

    // When is this fired?
    private void OnGUI()
    {
        if (uis.showcraftbrowser)
        {
            uis.craftlist.OnGUI();
        }
    }

    /*
    // ToDo: What Does this Do?
    private void OnLoad()
    {
        bases = FlightGlobals.fetch.vessels;
        foreach (Vessel v in bases)
        {
            print(v.name);
        }
    }
    */

    public override void OnSave(ConfigNode node)
    {
        PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
        config.SetValue("Window Position", uis.windowpos);
        config.SetValue("Show Build Menu on StartUp", uis.showbuilduionload);
        config.save();
    }

    
    public override void OnLoad(ConfigNode node)
    {
        //LoadConfigFile();
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
        RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
        uis.builduiactive = true;
    }

    [KSPEvent(guiActive = true, guiName = "Hide Build Menu", active = false)]
    public void HideBuildMenu()
    {
        RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI
        uis.builduiactive = false;
    }

    [KSPAction("Show Build Menu")]
    public void ShowBuildMenuAction(KSPActionParam param)
    {
        ShowBuildMenu();
    }

    [KSPAction("Hide Build Menu")]
    public void HideBuildMenuAction(KSPActionParam param)
    {
        HideBuildMenu();
    }

    [KSPAction("Toggle Build Menu")]
    public void ToggleBuildMenuAction(KSPActionParam param)
    {
        if (uis.builduiactive)
        {
            HideBuildMenu();
        }
        else
        {
            ShowBuildMenu();
        }
    }

    // =====================================================================================================================================================
    // Build Helper Functions

    // Gets connected resources to a part. Note fuel lines are NOT reversible! Add flow going TO the constructing part!
    private static List<PartResource> GetConnectedResources(Part part, String resourceName)
    {
        var resources = new List<PartResource>();
        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resourceName).id, resources);
        return resources;
    }

    public Dictionary<string, float> getBuildCost(ConfigNode[] nodes)
    {
        Part p;
        float mass = 0;
        Dictionary<string, float> resources = new Dictionary<string, float>();

        foreach (ConfigNode node in nodes) {
			print (node.name);
			p = PartLoader.getPartInfoByName(node.GetValue("part").Remove(node.GetValue("part").LastIndexOf("_"))).partPrefab;
			mass = mass + p.mass;
			foreach (PartResource r in p.Resources) {
                // Ignore intake Air
                if (r.resourceName == "IntakeAir")
                {
                    continue;
                }
				float val;
				if (resources.TryGetValue(r.resourceName, out val))
				{
					resources[r.resourceName] = val+(float)r.amount;
				}
                else
                {
					resources.Add(r.resourceName, (float)r.amount);
				}
			}
		}
        resources.Add("RocketParts", mass);

        // If Solid Fuel is used, convert to RocketParts
        if (resources.ContainsKey("SolidFuel"))
        {
            resources["RocketParts"] += resources["SolidFuel"];
            resources.Remove("SolidFuel");
        }

        // If there is JetFuel (ie LF only tanks as well as LFO tanks - eg a SpacePlane) then split the Surplus LF off as "JetFuel"
        if (resources.ContainsKey("Oxidizer") && resources.ContainsKey("LiquidFuel"))
        {
            float tmp = resources["LiquidFuel"] - ((resources["Oxidizer"] / 55) * 45);
            resources.Add("JetFuel", tmp);
            resources["LiquidFuel"] -= tmp;
        }
        else
        {
            resources.Add("JetFuel", 0f);
        }

        return resources;
    }

    // =====================================================================================================================================================
    // Unused

    /*
     * A simple test to see if other DLLs can call funcs
     * to use - add reference to this dll in other project and then use this code:
     * 
     ExLaunchPad exl = new ExLaunchPad();
            string tmp = exl.evilCTest();
    */
    public string evilCTest()
    {
        return "Hello!";
    }

    private void destroyShip(ShipConstruct nship, float availableRocketParts, float availableLiquidFuel, float availableOxidizer, float availableMonoPropellant)
    {
        this.part.RequestResource("RocketParts", -availableRocketParts);
        this.part.RequestResource("LiquidFuel", -availableLiquidFuel);
        this.part.RequestResource("Oxidizer", -availableOxidizer);
        this.part.RequestResource("MonoPropellant", -availableMonoPropellant);
        nship.parts[0].localRoot.explode();
    }

    /*
	[KSPField]
	public bool debug = false;
    */

}


