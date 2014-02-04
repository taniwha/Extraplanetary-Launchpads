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

		public static bool timed_builds = false;
		public static bool kethane_checked;
		public static bool kethane_present;
		public static bool force_resource_use;
		public static bool use_resources;

		public enum CraftType { SPH, VAB, SUB };

		public CraftType craftType = CraftType.VAB;
		public string flagname = null;
		public ConfigNode craftConfig = null;

		internal VesselResources padResources;
		public BuildCost.CostReport buildCost = null;
		public BuildCost.CostReport builtStuff = null;

		public bool autoRelease;
		public DockedVesselInfo vesselInfo;

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
			return builtStuff != null;
		}

		public void DoWork (double kerbalHours)
		{
			var required = builtStuff.required;

			//padResources.TransferResource (br.name, -br.amount);
			Debug.Log (String.Format ("[EL Launchpad] KerbalHours: {0}",
									  kerbalHours));
			bool did_work;
			do {
				int count = required.Where (r => r.amount > 0).Count ();
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

		internal void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			int numParts = vessel.parts.Count;
			if (craftType != CraftType.SUB)
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

			//XXX UseResources (vsl);

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

		private void Start ()
		{
		}

		public override void OnFixedUpdate ()
		{
			if (vesselInfo != null && !vessel.packed) {
				if (autoRelease) {
					ReleaseVessel ();
				}
			}
		}

		private void OnGUI ()
		{
			BuildWindow.OnGUI (this);
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
			//Debug.Log (String.Format ("[EL] {0}", n + t.name));
			//foreach (Transform c in t)
			//	dumpxform (c, n + t.name + ".");
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
			part.force_activate ();
		}

		void OnDestroy ()
		{
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);

			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			GameEvents.onVesselChange.Remove (onVesselChange);
		}

		[KSPEvent (guiActive = true, guiName = "Show Build Menu", active = true)]
		public void ShowBuildMenu ()
		{
		}

		[KSPEvent (guiActive = true, guiName = "Hide Build Menu", active = false)]
		public void HideBuildMenu ()
		{
		}

		[KSPEvent (guiActive = true, guiName = "Release", active = false)]
		public void ReleaseVessel ()
		{
			vessel[vesselInfo.rootPartUId].Undock (vesselInfo);
			vesselInfo = null;
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
			//if (builduiactive) {
			//	HideBuildMenu ();
			//} else {
			//	ShowBuildMenu ();
			//}
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

		void onVesselChange (Vessel v)
		{
		}

		void onHideUI ()
		{
		}

		void onShowUI ()
		{
		}
	}

}
