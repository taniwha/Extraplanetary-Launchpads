using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ExBuildControl : ExWorkSink
	{
		public interface IBuilder
		{
			void UpdateMenus (bool visible);
			Transform GetLaunchTransform ();
			bool capture
			{
				get;
			}
			Vessel vessel
			{
				get;
			}
			Part part
			{
				get;
			}
			ExBuildControl control
			{
				get;
			}
			string Name
			{
				get;
			}
		}

		public IBuilder builder
		{
			get;
			private set;
		}

		public enum CraftType { VAB, SPH, SubAss };
		public enum State { Idle, Planning, Building, Canceling, Dewarping, Complete, Transfer };

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

		public static bool useResources
		{
			get {
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
					return true;
				}
				return (ExSettings.kethane_present
						|| ExSettings.force_resource_use);
			}
		}

		DockedVesselInfo vesselInfo;
		Transform launchTransform;
		Part craftRoot;
		Vessel craftVessel;
		Vector3 craftOffset;
		float base_mass;

		private static bool CheckForKethane ()
		{
			if (AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name == "Kethane")) {
				Debug.Log ("[EL] Kethane found");
				return true;
			}
			Debug.Log ("[EL] Kethane not found");
			return false;
		}

		public void CancelBuild ()
		{
			if (state == State.Building) {
				state = State.Canceling;
			}
		}

		public void UnCancelBuild ()
		{
			if (state == State.Canceling) {
				state = State.Building;
			}
		}

		public void PauseBuild ()
		{
			if (state == State.Building || state == State.Canceling) {
				paused = true;
			}
		}

		public void ResumeBuild ()
		{
			paused = false;
		}

		public bool isActive ()
		{
			return ((state == State.Building || state == State.Canceling)
					&& !paused);
		}

		private IEnumerator<YieldInstruction> DewarpAndBuildCraft ()
		{
			state = State.Dewarping;
			Debug.Log (String.Format ("[EL Launchpad] dewarp"));
			TimeWarp.SetRate (0, false);
			while (builder.vessel.packed) {
				yield return null;
			}
			if (!builder.vessel.LandedOrSplashed) {
				while (builder.vessel.geeForce > 0.1) {
					yield return null;
				}
			}
			state = State.Complete;
			if (!ExSettings.timed_builds) {
				BuildAndLaunchCraft ();
				TransferResources ();
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
			builder.part.mass = base_mass + (float) mass;
		}

		private void DoWork_Build (double kerbalHours)
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
				(builder as PartModule).StartCoroutine (DewarpAndBuildCraft ());
			}
		}

		private void DoWork_Cancel (double kerbalHours)
		{
			var built = builtStuff.required;
			var cost = buildCost.required;

			bool did_work;
			int count;
			do {
				count = 0;
				foreach (var bres in built) {
					var cres = ExBuildWindow.FindResource (cost, bres.name);
					if (cres.amount - bres.amount > 0) {
						count++;
					}
				}
				if (count == 0) {
					break;
				}

				double work = kerbalHours / count;
				did_work = false;
				count = 0;
				foreach (var bres in built) {
					var cres = ExBuildWindow.FindResource (cost, bres.name);
					double remaining = cres.amount - bres.amount;
					if (remaining < 0) {
						continue;
					}
					double mass = work / 5;	//FIXME not hard-coded (5Kh/t)
					double amount = mass / bres.density;
					double base_amount = amount;

					if (amount > remaining) {
						amount = remaining;
					}
					double capacity = padResources.ResourceCapacity (bres.name)
									- padResources.ResourceAmount (bres.name);
					if (amount > capacity) {
						amount = capacity;
					}
					if (amount <= 0)
						break;
					count++;
					did_work = true;
					// do only the work required to process the actual amount
					// of returned resource
					kerbalHours -= work * amount / base_amount;
					bres.amount += amount;
					padResources.TransferResource (bres.name, amount);
				}
			} while (did_work && kerbalHours > 0);

			SetPadMass ();

			if (count == 0) {
				state = State.Planning;
			}
		}

		public void DoWork (double kerbalHours)
		{
			if (state == State.Building) {
				DoWork_Build (kerbalHours);
			} else if (state == State.Canceling) {
				DoWork_Cancel (kerbalHours);
			}
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (this);
		}

		internal void SetupCraftResources (Vessel vsl)
		{
			craftResources = new VesselResources (vsl);
			foreach (var br in buildCost.optional) {
				var amount = craftResources.ResourceAmount (br.name);
				craftResources.TransferResource (br.name, -amount);
			}
		}

		private void TransferResources ()
		{
			foreach (var br in buildCost.optional) {
				if (useResources) {
					var a = padResources.TransferResource (br.name,
														   -br.amount);
					a += br.amount;
					var b = craftResources.TransferResource (br.name, a);
					padResources.TransferResource (br.name, b);
				} else {
					craftResources.TransferResource (br.name, br.amount);
				}
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
			var vesselCoM = GetVesselWorldCoM (builder.vessel);
			var offset = (Vector3d.zero + craftCoM - vesselCoM).xzy;

			var corb = craftVessel.orbit;
			var orb = builder.vessel.orbit;
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
			craftRoot.Couple (builder.part);

			if (builder.vessel != FlightGlobals.ActiveVessel) {
				FlightGlobals.ForceSetActiveVessel (builder.vessel);
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

			builder.part.mass = base_mass;

			CoupleWithCraft ();
			if (ExSettings.timed_builds) {
				state = State.Transfer;
			}
		}

		internal void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			int numParts = builder.vessel.parts.Count;
			if (craftType != CraftType.SubAss)
				numParts = 0;

			StrutFixer.HackStruts (nship, numParts);

			Vector3 offset = nship.Parts[0].transform.localPosition;
			nship.Parts[0].transform.Translate (-offset);
			string landedAt = "External Launchpad";
			string flag = flagname;
			Game game = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			launchTransform = builder.GetLaunchTransform ();
			ShipConstruction.PutShipToGround (nship, launchTransform);
			ShipConstruction.AssembleForLaunch (nship, landedAt, flag, game,
												crew);
			var FlightVessels = FlightGlobals.Vessels;
			craftVessel = FlightVessels[FlightVessels.Count - 1];
			offset = craftVessel.transform.position - launchTransform.position;
			craftOffset = launchTransform.InverseTransformDirection (offset);
			if (builder.capture) {
				craftVessel.Splashed = craftVessel.Landed = false;
			}
			SetupCraftResources (craftVessel);

			FlightGlobals.ForceSetActiveVessel (craftVessel);

			Staging.beginFlight ();

			if (builder.capture) {
				FlightGlobals.overrideOrbit = true;
				(builder as PartModule).StartCoroutine (CaptureCraft ());
			} else {
				if (ExSettings.timed_builds) {
					state = State.Idle;
				}
			}
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
				if (ExSettings.timed_builds) {
					state = State.Building;
					paused = false;
				} else {
					if (useResources) {
						foreach (var res in builtStuff.required) {
							padResources.TransferResource (res.name,
														   -res.amount);
						}
					}
					(builder as PartModule).StartCoroutine (DewarpAndBuildCraft ());
				}
			}
		}

		public void Save (ConfigNode node)
		{
			node.AddValue ("flagname", flagname);
			if (base_mass != 0) {
				node.AddValue ("baseMass", base_mass);
			}
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
			padResources = new VesselResources (builder.vessel);
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

		public void Load (ConfigNode node)
		{
			flagname = node.GetValue ("flagname");
			if (node.HasValue ("baseMass")) {
				float.TryParse (node.GetValue ("baseMass"), out base_mass);
			} else {
				base_mass = builder.part.mass;
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

		public ExBuildControl (IBuilder builder)
		{
			this.builder = builder;
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
		}

		internal void OnStart ()
		{
			if (vesselInfo != null) {
				craftRoot = builder.vessel[vesselInfo.rootPartUId];
			}
			FindVesselResources ();
			SetPadMass ();
		}

		internal void OnDestroy ()
		{
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
		}

		public void ReleaseVessel ()
		{
			if (ExSettings.timed_builds) {
				TransferResources ();
			}
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

		void onVesselWasModified (Vessel v)
		{
			if (v == builder.vessel) {
				padResources = new VesselResources (builder.vessel);
				if (craftRoot != null && craftRoot.vessel != builder.vessel) {
					craftRoot = null;
					ReleaseVessel ();
				}
			}
		}
	}

}
