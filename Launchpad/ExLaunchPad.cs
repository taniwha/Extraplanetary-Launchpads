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

    private class UIStatus
    {
        static public Rect windowpos;
        static public bool init = true;
        static public bool showvab = true;
        static public bool showsph = false;
        static public bool canbuildcraft = false;
        static public crafttype ct = crafttype.VAB;
        static public string craftfile = null;
        static public CraftBrowser craftlist = null;
        static public bool showcraftbrowser = false;
        static public ConfigNode craftnode = null;
        static public bool craftselected = false;
        static public Vector2 resscroll;
        static public Dictionary<string, float> requiredresources = null;
        static public Dictionary<string, float> resourcesliders = new Dictionary<string, float>();
    }

	[KSPField]
	public bool aero = false;
	[KSPField]
	public bool rocket = true;
	[KSPField]
	public bool debug = false;

	//private CraftBrowser craftBrowser;
	//private bool showCraftBrowser = false;
	
	private List<Vessel> bases;
	
	private void destroyShip(ShipConstruct nship, float availableRocketParts, float availableLiquidFuel, float availableOxidizer, float availableMonoPropellant)
	{
		this.part.RequestResource("RocketParts", -availableRocketParts);
		this.part.RequestResource("LiquidFuel", -availableLiquidFuel);
		this.part.RequestResource("Oxidizer", -availableOxidizer);
		this.part.RequestResource("MonoPropellant", -availableMonoPropellant);
		nship.parts[0].localRoot.explode();
	}

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
        if (UIStatus.init)
        {
            UIStatus.init = false;
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

        if (GUILayout.Toggle(UIStatus.showvab, "VAB", GUILayout.MinWidth(80)))
        {
            UIStatus.showvab = true;
            UIStatus.showsph = false;
            UIStatus.ct = crafttype.VAB;
        }
        if (GUILayout.Toggle(UIStatus.showsph, "SPH"))
        {
            UIStatus.showvab = false;
            UIStatus.showsph = true;
            UIStatus.ct = crafttype.SPH;
        }

        GUILayout.EndHorizontal();

        

        string strpath = HighLogic.CurrentGame.Title.Split(new string[] { " (Sandbox)" }, StringSplitOptions.None).First();

        if (GUILayout.Button("Select Craft", mySty, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
        {
            UIStatus.craftlist = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), UIStatus.ct.ToString(), strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
            UIStatus.showcraftbrowser = true;
        }

        if (UIStatus.craftselected)
        {
            GUILayout.Box("Selected Craft:  " + UIStatus.craftnode.GetValue("ship"), whiSty);

            // Resource requirements
            GUILayout.Label("Resources required to build:", labSty, GUILayout.Width(500));

            UIStatus.resscroll = GUILayout.BeginScrollView(UIStatus.resscroll, GUILayout.Width(500), GUILayout.Height(200));

            GUILayout.BeginHorizontal();
			
            GUILayout.Label("Resource", labSty, GUILayout.Width(200));
            GUILayout.Label("Fill Percentage", labSty, GUILayout.Width(100));
            GUILayout.Label("Required", labSty, GUILayout.Width(80));
            GUILayout.Label("Available", labSty, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            UIStatus.canbuildcraft = true;      // default to can build - if something is stopping us from building, we will set to false later

            // Cycle through required resources
            foreach (KeyValuePair<string, float> pair in UIStatus.requiredresources)
            {
                List<PartResource> res = GetConnectedResources(this.part, pair.Key);
                double tot = 0;
                foreach (PartResource pr in res)
                {
                    tot += pr.amount;
                }

                GUILayout.BeginHorizontal();

                GUILayout.Box(pair.Key, whiSty, GUILayout.Width(200));
                
				if (!UIStatus.resourcesliders.ContainsKey(pair.Key)) {
                   UIStatus.resourcesliders.Add(pair.Key, 1);
                }
                UIStatus.resourcesliders[pair.Key] = GUILayout.HorizontalSlider(UIStatus.resourcesliders[pair.Key], 0, 1, GUILayout.Width(100));
                
				GUIStyle avail = new GUIStyle();
                //if (tot < pair.Value)
                if (tot < pair.Value*UIStatus.resourcesliders[pair.Key])
                {
                    avail = redSty;
                    UIStatus.canbuildcraft = false; // prevent building
                }
                else
                {
                    avail = grnSty;
                }
                GUILayout.Box((pair.Value*UIStatus.resourcesliders[pair.Key]).ToString(), avail, GUILayout.Width(80));
                GUILayout.Box(tot.ToString(), whiSty, GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // Build button
            if (UIStatus.canbuildcraft)
            {
                if(GUILayout.Button("Build", mySty, GUILayout.ExpandWidth(true)))
                {

                    // build craft
                    FlightState state = new FlightState();
                    ShipConstruct nship = ShipConstruction.LoadShip(UIStatus.craftfile);
                    ShipConstruction.PutShipToGround(nship, this.part.transform);
                    ShipConstruction.AssembleForLaunch(nship, "External Launchpad", HighLogic.CurrentGame.flagURL, state);
                    Staging.beginFlight();
                    nship.parts[0].vessel.ResumeStaging();
                    Staging.GenerateStagingSequence(nship.parts[0].localRoot);
                    Staging.RecalculateVesselStaging(nship.parts[0].vessel);
					
                    // use resources
                    foreach (KeyValuePair<string, float> pair in UIStatus.requiredresources)
                    {
                        this.part.RequestResource(pair.Key, pair.Value);
						double tot = pair.Value;
						foreach (Part p in nship.parts) {
							if (tot>pair.Value*UIStatus.resourcesliders[pair.Key]) {
								tot -= p.RequestResource(pair.Key, tot-pair.Value);
							} else {
								break;
							}
						}
                    }
					
                    RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI
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

        //DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is 
        //clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
        //dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
        //it may "cover up" your controls and make them stop responding to the mouse.
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

    }

    // called when the user selects a craft the craft browser
    private void craftSelectComplete(string filename, string flagname)
    {
        UIStatus.showcraftbrowser = false;
        UIStatus.craftfile = filename;
        UIStatus.craftnode = ConfigNode.Load(filename);
        ConfigNode[] nodes = UIStatus.craftnode.GetNodes("PART");

        // Get list of resources required to build vessel
        UIStatus.requiredresources = getBuildCost(nodes);
        UIStatus.craftselected = true;
    }

    // called when the user clicks cancel in the craft browser
    private void craftSelectCancel()
    {
        UIStatus.showcraftbrowser = false;

        UIStatus.requiredresources = null;
        UIStatus.craftselected = false;
    }


    private void drawGUI()
    {
        GUI.skin = HighLogic.Skin;
        UIStatus.windowpos = GUILayout.Window(1, UIStatus.windowpos, WindowGUI, "Extraplanetary Launchpads", GUILayout.MinWidth(400));
    }

    public override void OnAwake()
    {
        base.OnAwake();
        RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
    }

    // Gets connected resources to a part. Note fuel lines are NOT reversible! Add flow going TO the constructing part!
    private static List<PartResource> GetConnectedResources(Part part, String resourceName)
    {
        var resources = new List<PartResource>();
        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resourceName).id, resources);
        return resources;
    }

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
        return resources;
    }

    /*
	// TODO: what is this second string?
    // ANSWER: b is the filename of the image of the selected flag (As in a flag on a pole, not a programming flag)
	private void getAndLoadShip(string filename, string b) 
	{
		ConfigNode cf = ConfigNode.Load(filename);
		ConfigNode[] nodes = cf.GetNodes("PART");
		
		print ("106");
		// Get list of resources required to build vessel
        Dictionary<string, float> resources = getBuildCost(nodes);

		print ("126");
        // Check if resources available by removing and readding them
		bool success = true;
		List<string> keys = new List<string> (resources.Keys);
		foreach (string k in keys) {
			print ("129");
			print (k);
			float avail = this.part.RequestResource(k, resources[k]);
			if (avail!=resources[k]) {
				success = false;
				print ("Not enough "+k);
			}
			resources[k] = avail;
		}
		print ("138");
		if (debug) success = true;
		if (success==false) {
			print ("no success");
			foreach (KeyValuePair<string,float> k in resources) {
				this.part.RequestResource(k.Key, -k.Value);
			}
			return;
		}
		print ("146");
		FlightState state = new FlightState();
		ShipConstruct nship = ShipConstruction.LoadShip(filename);
		print ("149");
		ShipConstruction.PutShipToGround(nship, this.part.transform);
		ShipConstruction.AssembleForLaunch(nship, "External Launchpad", HighLogic.CurrentGame.flagURL, state);
		//ShipConstruction.AssembleForLaunch(nship, "External Launchpad", state);
		Staging.beginFlight();
		//StageManager.beginFlight();
		nship.parts[0].vessel.ResumeStaging();
		Staging.GenerateStagingSequence(nship.parts[0].localRoot);
		Staging.RecalculateVesselStaging(nship.parts[0].vessel);
		//Staging.AddStageAt (0);
		//Staging.AddStageAt(Staging.GetStageCount(nship.Parts));
		print ("Successfully loaded "+filename);
		showCraftBrowser = false;
	}

	private void closed()
    {
        showCraftBrowser = false;
    }

	public void getShip()
	{
		print("Initializing craft browser...");
        //string[] path = Regex.Split(HighLogic.CurrentGame.Title, " (Sandbox)");
        //string strpath = path[0];
        string[] path = HighLogic.CurrentGame.Title.Split(' ');
        Array.Resize<string>(ref path, path.Length - 1);
		string strpath = string.Join(" ", path);
		//if (aero) {
		//	print (ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene)+"/../SPH");
		//	craftBrowser = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene)+"/../SPH",
    	//	//"testing", "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage);
		//	strpath, "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage);
		//} else {
			//craftBrowser = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene),
    		//strpath, "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage);
			craftBrowser = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene),strpath, "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage, true);
		//}
		showCraftBrowser = true;
		//craftBrowser.OnGUI();
	}

	public void getPlane()
	{
		print("Initializing craft browser...");
		string[] path = HighLogic.CurrentGame.Title.Split(' ');
		Array.Resize<string>(ref path, path.Length-1);
		string strpath = string.Join(" ", path);
		craftBrowser = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene)+"/../SPH",
    	//"testing", "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage);
		strpath, "Select a ship to load", getAndLoadShip, closed, HighLogic.Skin, EditorLogic.ShipFileImage, true);
		
		showCraftBrowser = true;
	}
    */

	private void OnGUI()
	{
        if (UIStatus.showcraftbrowser)
		{
            UIStatus.craftlist.OnGUI();
		}
	}

    private void OnLoad()
	{
		bases = FlightGlobals.fetch.vessels;
		foreach (Vessel v in bases) {
			print (v.name);
		}
	}

    public override void OnSave(ConfigNode node)
    {
        PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
        config.SetValue("Window Position", UIStatus.windowpos);
        config.save();
    }

    public override void OnLoad(ConfigNode node)
    {
        PluginConfiguration config = PluginConfiguration.CreateForType<ExLaunchPad>();
        config.load();
        UIStatus.windowpos = config.GetValue<Rect>("Window Position");
    }

    [KSPEvent(active = true, guiActive = true, guiName = "Load Ship")]
	public void toggleLandingSystem()
	{
			//print("Loading ship...");
		    //getShip();
    }
}

public class RocketBuilder: PartModule
{
	private float consumedMetal = 0;
	private bool manufacturing = false;
	[KSPEvent(active = true, guiActive = true, guiName = "Start Manufacturing")]
	public void startManufacturing()
	{
		if (manufacturing) {
			Events["startManufacturing"].guiName = "Stop Manufacturing";
			manufacturing = false;
			print ("Shutting down work.");
		} else {
			print ("Going to work...");
			this.part.force_activate();
			//float gotmetal = this.part.RequestResource("Metal", 1);
			//this.part.RequestResource("RocketParts", -gotmetal);
			manufacturing = true;
		}
	}
	public override void OnFixedUpdate ()
	{
		if (manufacturing)
		{
			consumedMetal = consumedMetal + this.part.RequestResource("Metal",(float)0.2*TimeWarp.fixedDeltaTime);
			if (consumedMetal>1)
			{
				consumedMetal = 0;
				this.part.RequestResource("RocketParts", -1);
			}
		}
	}
}

public class Smelter: PartModule
{
	private bool isSmelting = false;
	private float consumedOre = 0;
	
	[KSPEvent(active = true, guiActive = true, guiName = "Smelt Ore")]
	public void startSmelting()
	{
		this.part.force_activate();
		print ("Smelting..");
		isSmelting = true;
	}
	public override void OnFixedUpdate ()
	{
		print ("Temperature: "+this.part.temperature.ToString());
		if (isSmelting)
		{
			consumedOre = this.part.RequestResource("Ore",(float)0.2*TimeWarp.fixedDeltaTime);
			this.part.RequestResource("Metal", -consumedOre);
			//this.part.temperature = this.part.temperature + 100*TimeWarp.fixedDeltaTime;
			this.part.temperature = 4900;
		}
	}
}

/*public class Dynomite: PartModule
{
	[KSPField]
	public float impact = 100;
	
	public void Explode () {
		var DepositUnder = Kethane.KethaneController.GetInstance(this.vessel).GetDepositUnder();
		print ("KABOOM!!!");
		if (DepositUnder != null && (DepositUnder is Kethane.OreDeposit)) {
			bool found = false;
			foreach (Kethane.Blast b in DepositUnder.Blasts) {
				if (Math.Abs(b.lat-part.vessel.latitude)<1/part.vessel.mainBody.pqsController.radius && Math.Abs(b.lon-part.vessel.longitude)<1/part.vessel.mainBody.pqsController.radius)
				{
					b.amount += impact;
					found = true;
				}
			}
			if (!found) {
				DepositUnder.Blasts.Add(new Kethane.Blast((float)part.vessel.latitude, (float)part.vessel.longitude));
				print ("Adding blast!!!! Boom.");
			}
			DepositUnder.Quantity += 100;
		}
	}
	
	public override void OnLoad(ConfigNode node)
	{
		part.OnJustAboutToBeDestroyed += Explode;
	}
}*/