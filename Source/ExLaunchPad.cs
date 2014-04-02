using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ExLaunchPad : PartModule, ExWorkSink
	{
		[KSPField]
		public bool DebugPad = false;
		[KSPField (isPersistant = false)]
		public float SpawnHeightOffset = 0.0f;
		[KSPField (isPersistant = false)]
		public string SpawnTransform;
		[KSPField (isPersistant = true)]
		public string PadName = "";

		public static bool timed_builds = true;
		public static bool kethane_checked;
		public static bool kethane_present;
		public static bool force_resource_use;
		public static bool use_resources;

		public enum CraftType { VAB, SPH, SubAss };
		public enum State { Idle, Planning, Building, Dewarping, Complete, Transfer };

		public CraftType craftType = CraftType.VAB;

		public string flagname
		{
			get;
			private set;
		}
		public ConfigNode craftConfig
		{
			get;
			private set;
		}
		public VesselResources padResources
		{
			get;
			private set;
		}
		public VesselResources craftResources
		{
			get;
			private set;
		}
		public BuildCost.CostReport buildCost
		{
			get;
			private set;
		}
		public BuildCost.CostReport builtStuff
		{
			get;
			private set;
		}
		public State state
		{
			get;
			private set;
		}
		public bool paused
		{
			get;
			private set;
		}

		DockedVesselInfo vesselInfo;
		Transform launchTransform;
		Part craftRoot;
		Vessel craftVessel;
		Vector3 craftOffset;
		float base_mass;

		private static bool CheckForKethane ()
		{
			if (AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name == "MMI_Kethane" || a.assembly.GetName ().Name == "Kethane")) {
				//Debug.Log ("[EL] Kethane found");
				return true;
			}
			//Debug.Log ("[EL] Kethane not found");
			return false;
		}

		public void PauseBuild ()
		{
			if (state == State.Building) {
				paused = true;
			}
		}

		public void ResumeBuild ()
		{
			paused = false;
		}

		public bool isActive ()
		{
			return (state == State.Building) && !paused;
		}

		private IEnumerator<YieldInstruction> DewarpAndBuildCraft ()
		{
			state = State.Dewarping;
			Debug.Log (String.Format ("[EL Launchpad] dewarp"));
			TimeWarp.SetRate (0, false);
			while (vessel.packed) {
				yield return null;
			}
			if (!vessel.LandedOrSplashed) {
				while (vessel.geeForce > 0.1) {
					yield return null;
				}
			}
			state = State.Complete;
			if (!timed_builds) {
				BuildAndLaunchCraft ();
			}
		}

		void SetPadMass ()
		{
			double mass = 0;
			if (builtStuff != null && buildCost!= null) {
				var built = builtStuff.required;
				var cost = buildCost.required;

				foreach (var bres in built) {
					var cres = ExBuildWindow.FindResource (cost, bres.name);
					mass += (cres.amount - bres.amount) * bres.density;
				}
			}
			part.mass = base_mass + (float) mass;
		}

		public void DoWork (double kerbalHours)
		{
			var required = builtStuff.required;

			//Debug.Log (String.Format ("[EL Launchpad] KerbalHours: {0}",
			//						  kerbalHours));
			bool did_work;
			int count;
			do {
				count = required.Where (r => r.amount > 0).Count ();
				if (count == 0)
					break;
				double work = kerbalHours / count;
				did_work = false;
				foreach (var res in required.Where (r => r.amount > 0)) {
					double mass = work / 5;	//FIXME not hard-coded (5Kh/t)
					double amount = mass / res.density;
					double base_amount = amount;

					if (amount > res.amount)
						amount = res.amount;
					double avail = padResources.ResourceAmount (res.name);
					if (amount > avail)
						amount = avail;
					//Debug.Log (String.Format ("[EL Launchpad] work:{0}:{1}:{2}",
					//						  res.amount, avail, amount));
					if (amount <= 0)
						break;
					did_work = true;
					// do only the work required to process the actual amount
					// of consumed resource
					kerbalHours -= work * amount / base_amount;
					res.amount -= amount;
					padResources.TransferResource (res.name, -amount);
				}
			} while (did_work && kerbalHours > 0);

			SetPadMass ();

			if (count == 0) {
				StartCoroutine (DewarpAndBuildCraft ());
			}
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (this);
		}

		private Transform GetLanchTransform ()
		{
			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				//Debug.Log (String.Format ("[EL] launchTransform:{0}:{1}",
				//						  launchTransform, SpawnTransform));
			} else {
				Vector3 offset = Vector3.up * SpawnHeightOffset;
				Transform t = this.part.transform;
				GameObject launchPos = new GameObject ();
				launchPos.transform.position = t.position;
				launchPos.transform.position += t.TransformDirection (offset);
				launchPos.transform.rotation = t.rotation;
				launchTransform = launchPos.transform;
				//Debug.Log (String.Format ("[EL] launchPos {0}",
				//						  launchTransform));
			}
			return launchTransform;
		}

		internal void SetupCraftResources (Vessel vsl)
		{
			craftResources = new VesselResources (vsl);
			foreach (var br in buildCost.optional) {
				var amount = craftResources.ResourceAmount (br.name);
				craftResources.TransferResource (br.name, -amount);
			}
		}

		internal void TransferResources ()
		{
			foreach (var br in buildCost.optional) {
				var a = padResources.TransferResource (br.name, -br.amount);
				a += br.amount;
				var b = craftResources.TransferResource (br.name, a);
				padResources.TransferResource (br.name, b);
			}
		}

		static ConfigNode orbit (Orbit orb)
		{
			var snap = new OrbitSnapshot (orb);
			var node = new ConfigNode ();
			snap.Save (node);
			return node;
		}

		public Vector3 GetVesselWorldCoM (Vessel v)
		{
			var com = v.findLocalCenterOfMass ();
			return v.rootPart.partTransform.TransformPoint (com);
		}

		private void SetCraftOrbit ()
		{
			var mode = OrbitDriver.UpdateMode.UPDATE;
			craftVessel.orbitDriver.SetOrbitMode (mode);

			var craftCoM = GetVesselWorldCoM (craftVessel);
			var vesselCoM = GetVesselWorldCoM (vessel);
			var offset = (Vector3d.zero + craftCoM - vesselCoM).xzy;

			var corb = craftVessel.orbit;
			var orb = vessel.orbit;
			var UT = Planetarium.GetUniversalTime ();
			var body = orb.referenceBody;
			corb.UpdateFromStateVectors (orb.pos + offset, orb.vel, body, UT);

			Debug.Log (String.Format ("[EL] {0} {1}", orbit(orb), orb.pos));
			Debug.Log (String.Format ("[EL] {0} {1}", orbit(corb), corb.pos));
		}

		private void CoupleWithCraft ()
		{
			craftRoot = craftVessel.rootPart;
			vesselInfo = new DockedVesselInfo ();
			vesselInfo.name = craftVessel.vesselName;
			vesselInfo.vesselType = craftVessel.vesselType;
			vesselInfo.rootPartUId = craftRoot.flightID;
			craftRoot.Couple (part);

			if (vessel != FlightGlobals.ActiveVessel) {
				FlightGlobals.ForceSetActiveVessel (vessel);
			}
		}

		private IEnumerator<YieldInstruction> CaptureCraft ()
		{
			Vector3 pos;
			while (true) {
				bool partsInitialized = true;
				foreach (Part p in craftVessel.parts) {
					if (!p.started) {
						partsInitialized = false;
						break;
					}
				}
				pos = launchTransform.TransformPoint (craftOffset);
				craftVessel.SetPosition (pos, true);
				if (partsInitialized) {
					break;
				}
				OrbitPhysicsManager.HoldVesselUnpack (2);
				yield return null;
			}

			FlightGlobals.overrideOrbit = false;
			SetCraftOrbit ();
			craftVessel.GoOffRails ();

			part.mass = base_mass;

			CoupleWithCraft ();
			state = State.Transfer;
		}

		internal void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			int numParts = vessel.parts.Count;
			if (craftType != CraftType.SubAss)
				numParts = 0;

			ShipConstruction.CreateBackup (nship);

			StrutFixer.HackStruts (nship, numParts);

			Vector3 offset = nship.Parts[0].transform.localPosition;
			nship.Parts[0].transform.Translate (-offset);
			string landedAt = "External Launchpad";
			string flag = flagname;
			Game state = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			GetLanchTransform ();
			ShipConstruction.PutShipToGround (nship, launchTransform);
			ShipConstruction.AssembleForLaunch (nship, landedAt, flag, state,
												crew);
			var FlightVessels = FlightGlobals.Vessels;
			craftVessel = FlightVessels[FlightVessels.Count - 1];
			offset = craftVessel.transform.position - launchTransform.position;
			craftOffset = launchTransform.InverseTransformDirection (offset);
			craftVessel.Splashed = craftVessel.Landed = false;
			SetupCraftResources (craftVessel);

			Staging.beginFlight ();

			FlightGlobals.ForceSetActiveVessel (vessel);

			FlightGlobals.overrideOrbit = true;
			StartCoroutine (CaptureCraft ());
		}

		public void LoadCraft (string filename, string flagname)
		{
			this.flagname = flagname;
			ConfigNode craft = ConfigNode.Load (filename);
			if ((buildCost = getBuildCost (craft)) != null) {
				craftConfig = craft;
				state = State.Planning;
			}
		}

		public void UnloadCraft ()
		{
			craftConfig = null;
			state = State.Idle;
		}

		public void BuildCraft ()
		{
			if (craftConfig != null) {
				builtStuff = getBuildCost (craftConfig);
				if (timed_builds) {
					state = State.Building;
					paused = false;
				} else {
					foreach (var res in builtStuff.required) {
						padResources.TransferResource (res.name, -res.amount);
					}
					StartCoroutine (DewarpAndBuildCraft ());
				}
			}
		}

		public override void OnSave (ConfigNode node)
		{
			node.AddValue ("flagname", flagname);
			node.AddValue ("baseMass", base_mass);
			if (craftConfig != null) {
				craftConfig.name = "CraftConfig";
				node.AddNode (craftConfig);
			}
			if (buildCost != null) {
				var bc = node.AddNode ("BuildCost");
				buildCost.Save (bc);
			}
			if (builtStuff != null) {
				var bs = node.AddNode ("BuiltStuff");
				builtStuff.Save (bs);
			}
			node.AddValue ("state", state);
			node.AddValue ("paused", paused);
			if (vesselInfo != null) {
				ConfigNode vi = node.AddNode ("DockedVesselInfo");
				vesselInfo.Save (vi);
			}
		}

		internal static void dumpxform (Transform t, string n = "")
		{
			Debug.Log (String.Format ("[EL] {0}", n + t.name));
			foreach (Transform c in t)
				dumpxform (c, n + t.name + ".");
		}

		internal List<Part> CraftParts ()
		{
			var part_list = new List<Part> ();
			if (craftRoot != null) {
				var root = craftRoot;
				Debug.Log (String.Format ("[EL] CraftParts root {0}", root));
				part_list.Add (root);
				part_list.AddRange (root.FindChildParts<Part> (true));
			}
			return part_list;
		}

		internal void FindVesselResources ()
		{
			padResources = new VesselResources (vessel);
			var craft_parts = CraftParts ();
			if (craft_parts.Count > 0) {
				craftResources = new VesselResources ();
			}
			foreach (var part in craft_parts) {
				padResources.RemovePart (part);
				craftResources.AddPart (part);
			}
			if (craftResources == null && craftConfig != null) {
				getBuildCost (craftConfig);
			}
		}

		public override void OnLoad (ConfigNode node)
		{
			flagname = node.GetValue ("flagname");
			if (node.HasValue ("baseMass")) {
				float.TryParse (node.GetValue ("baseMass"), out base_mass);
			} else {
				base_mass = part.mass;
			}
			craftConfig = node.GetNode ("CraftConfig");
			if (node.HasNode ("BuildCost")) {
				var bc = node.GetNode ("BuildCost");
				buildCost = new BuildCost.CostReport ();
				buildCost.Load (bc);
			}
			if (node.HasNode ("BuiltStuff")) {
				var bs = node.GetNode ("BuiltStuff");
				builtStuff = new BuildCost.CostReport ();
				builtStuff.Load (bs);
			}
			if (node.HasValue ("state")) {
				var s = node.GetValue ("state");
				state = (State) Enum.Parse (typeof (State), s);
			}
			if (node.HasValue ("paused")) {
				var s = node.GetValue ("paused");
				bool p = false;
				bool.TryParse (s, out p);
				paused = p;
			}
			if (node.HasNode ("DockedVesselInfo")) {
				ConfigNode vi = node.GetNode ("DockedVesselInfo");
				vesselInfo = new DockedVesselInfo ();
				vesselInfo.Load (vi);
			}
		}

		public override void OnAwake ()
		{
			if (!kethane_checked) {
				kethane_present = CheckForKethane ();
				kethane_checked = true;
			}
			GameEvents.onVesselSituationChange.Add (onVesselSituationChange);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
			}
			if (force_resource_use || (kethane_present && !DebugPad)) {
				use_resources = true;
			}
			part.force_activate ();
			if (vesselInfo != null) {
				craftRoot = vessel[vesselInfo.rootPartUId];
			}
			FindVesselResources ();
			SetPadMass ();
		}

		void OnDestroy ()
		{
			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
		}

		[KSPEvent (guiActive = true, guiName = "Hide UI", active = false)]
		public void HideUI ()
		{
			ExBuildWindow.HideGUI ();
		}

		[KSPEvent (guiActive = true, guiName = "Show UI", active = false)]
		public void ShowUI ()
		{
			ExBuildWindow.ShowGUI ();
			ExBuildWindow.SelectPad (this);
		}

		[KSPEvent (guiActive = true, guiName = "Release", active = false)]
		public void ReleaseVessel ()
		{
			if (craftRoot != null) {
				craftRoot.Undock (vesselInfo);
				var vesselCount = FlightGlobals.Vessels.Count;
				Vessel vsl = FlightGlobals.Vessels[vesselCount - 1];
				FlightGlobals.ForceSetActiveVessel (vsl);
			}
			craftConfig = null;
			vesselInfo = null;
			buildCost = null;
			builtStuff = null;
			state = State.Idle;
		}

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}

		public BuildCost.CostReport getBuildCost (ConfigNode craft)
		{
			ShipConstruct ship = new ShipConstruct ();
			ship.LoadShip (craft);
			GameObject ro = ship.parts[0].localRoot.gameObject;
			Vessel dummy = ro.AddComponent<Vessel>();
			dummy.Initialize (true);

			craftResources = new VesselResources (dummy);

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
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == vessel) {
				padResources = new VesselResources (vessel);
				if (craftRoot != null && craftRoot.vessel != vessel) {
					craftRoot = null;
					ReleaseVessel ();
				}
			}
		}
	}

}
