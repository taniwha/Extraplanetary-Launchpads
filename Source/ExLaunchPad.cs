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

		public static bool timed_builds = false;
		public static bool kethane_checked;
		public static bool kethane_present;
		public static bool force_resource_use;
		public static bool use_resources;

		public enum CraftType { VAB, SPH, SubAss };
		public enum State { Idle, Planning, Building, Complete };

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

		DockedVesselInfo vesselInfo;

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
			return state == State.Building;
		}

		public void DoWork (double kerbalHours)
		{
			var required = builtStuff.required;

			Debug.Log (String.Format ("[EL Launchpad] KerbalHours: {0}",
									  kerbalHours));
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
					Debug.Log (String.Format ("[EL Launchpad] work:{0}:{1}:{2}", res.amount, avail, amount));
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

			if (count == 0) {
				BuildAndLaunchCraft ();
				state = State.Complete;
			}
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (this);
		}

		private Transform GetLanchTransform ()
		{

			Transform launchTransform;

			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				Debug.Log (String.Format ("[EL] launchTransform:{0}:{1}",
										  launchTransform, SpawnTransform));
			} else {
				Vector3 offset = Vector3.up * SpawnHeightOffset;
				Transform t = this.part.transform;
				GameObject launchPos = new GameObject ();
				launchPos.transform.position = t.position;
				launchPos.transform.position += t.TransformDirection (offset);
				launchPos.transform.rotation = t.rotation;
				launchTransform = launchPos.transform;
				Destroy (launchPos);
				Debug.Log (String.Format ("[EL] launchPos {0}",
										  launchTransform));
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

		internal void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			int numParts = vessel.parts.Count;
			if (craftType != CraftType.SubAss)
				numParts = 0;

			StrutFixer.HackStruts (nship, numParts);

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

			SetupCraftResources (vsl);

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

			FlightGlobals.ForceSetActiveVessel (vessel);

			Staging.beginFlight ();
		}

		public void LoadCraft (string filename, string flagname)
		{
			this.flagname = flagname;
			ConfigNode craft = ConfigNode.Load (filename);
			if ((buildCost = getBuildCost (craft)) != null) {
				craftConfig = craft;
			}
			state = State.Planning;
		}

		public void BuildCraft ()
		{
			if (craftConfig != null) {
				builtStuff = getBuildCost (craftConfig);
				state = State.Building;
			}
		}

		public override void OnSave (ConfigNode node)
		{
			if (vesselInfo != null) {
				ConfigNode vi = node.AddNode ("DockedVesselInfo");
				vesselInfo.Save (vi);
			}
		}

		private void dumpxform (Transform t, string n = "")
		{
			//Debug.Log (String.Format ("[EL] {0}", n + t.name));
			//foreach (Transform c in t)
			//	dumpxform (c, n + t.name + ".");
		}

		public override void OnLoad (ConfigNode node)
		{
			dumpxform (part.transform);

			enabled = false;

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
			padResources = new VesselResources (vessel);
		}

		void OnDestroy ()
		{
			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
		}

		[KSPEvent (guiActive = true, guiName = "Release", active = false)]
		public void ReleaseVessel ()
		{
			vessel[vesselInfo.rootPartUId].Undock (vesselInfo);
			Vessel vsl = FlightGlobals.Vessels[FlightGlobals.Vessels.Count - 1];
			FlightGlobals.ForceSetActiveVessel (vsl);
			craftConfig = null;
			vesselInfo = null;
			buildCost = null;
			builtStuff = null;
			state = State.Idle;
		}

		[KSPAction ("Toggle Build Menu")]
		public void ToggleBuildMenuAction (KSPActionParam param)
		{
			ExBuildWindow.ToggleGUI ();
		}

		[KSPAction ("Release Vessel")]
		public void ReleaseVesselAction (KSPActionParam param)
		{
			if (vesselInfo != null) {
				ReleaseVessel ();
			}
		}

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
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == vessel) {
				padResources = new VesselResources (vessel);
			}
		}
	}

}
