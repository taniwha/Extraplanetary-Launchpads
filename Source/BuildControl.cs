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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public class ExBuildControl : ExWorkSink
	{
		public class Box
		{
			public Vector3 min;
			public Vector3 max;

			public Box (Bounds b)
			{
				min = new Vector3 (b.min.x, b.min.y, b.min.z);
				max = new Vector3 (b.max.x, b.max.y, b.max.z);
			}

			public void Add (Bounds b)
			{
				min.x = Mathf.Min (min.x, b.min.x);
				min.y = Mathf.Min (min.y, b.min.y);
				min.z = Mathf.Min (min.z, b.min.z);
				max.x = Mathf.Max (max.x, b.max.x);
				max.y = Mathf.Max (max.y, b.max.y);
				max.z = Mathf.Max (max.z, b.max.z);
			}
		}
		public interface IBuilder
		{
			void Highlight (bool on);
			void UpdateMenus (bool visible);
			void SetCraftMass (double craft_mass);
			Transform PlaceShip (ShipConstruct ship, Box vessel_bounds);
			void PadSelection_start ();
			void PadSelection ();
			void PadSelection_end ();

			bool canBuild
			{
				get;
			}
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
				set;
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

		public string filename
		{
			get;
			private set;
		}
		public string flagname
		{
			get;
			private set;
		}
		public bool lockedParts
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
		public CostReport buildCost
		{
			get;
			private set;
		}
		public CostReport builtStuff
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
		public string KACalarmID = "";

		DockedVesselInfo vesselInfo;
		Transform launchTransform;
		Part craftRoot;
		Vessel craftVessel;
		Vector3 craftOffset;

		public void CancelBuild ()
		{
			if (state == State.Building || state == State.Complete) {
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

		private IEnumerator DewarpAndBuildCraft ()
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
			builder.SetCraftMass (mass);
		}

		private void DoWork_Build (double kerbalHours)
		{
			var required = builtStuff.required;
			var base_kerbalHours = Math.Abs (kerbalHours);

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
					double amount = work / res.kerbalHours;
					double base_amount = Math.Abs (amount);

					if (amount > res.amount)
						amount = res.amount;
					double avail = padResources.ResourceAmount (res.name);
					if (amount > avail)
						amount = avail;
					//Debug.Log (String.Format ("[EL Launchpad] work:{0}:{1}:{2}:{3}:{4}",
					//						  res.name, res.kerbalHours, res.amount, avail, amount));
					if (amount / base_amount < 1e-10)
						continue;
					did_work = true;
					// do only the work required to process the actual amount
					// of consumed resource
					kerbalHours -= work * amount / base_amount;
					res.amount -= amount;
					//Debug.Log("add delta: "+amount);
					res.deltaAmount = amount;
					padResources.TransferResource (res.name, -amount);
				}
				//Debug.Log (String.Format ("[EL Launchpad] work:{0}:{1}:{2}",
				//						  did_work, kerbalHours, kerbalHours/base_kerbalHours));
			} while (did_work && kerbalHours / base_kerbalHours > 1e-10);

			SetPadMass ();

			if (count == 0) {
				(builder as PartModule).StartCoroutine (DewarpAndBuildCraft ());
				KACalarmID = "";
			}
		}

		private void DoWork_Cancel (double kerbalHours)
		{
			var built = builtStuff.required;
			var cost = buildCost.required;
			var base_kerbalHours = Math.Abs (kerbalHours);

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
					double amount = work / bres.kerbalHours;
					double base_amount = Math.Abs (amount);

					if (amount > remaining) {
						amount = remaining;
					}
					count++;
					did_work = true;
					// do only the work required to process the actual amount
					// of returned or disposed resource
					kerbalHours -= work * amount / base_amount;
					bres.amount += amount;
					//Debug.Log("remove delta: "+amount);
					bres.deltaAmount = amount;

					double capacity = padResources.ResourceCapacity (bres.name)
									- padResources.ResourceAmount (bres.name);
					if (amount > capacity) {
						amount = capacity;
					}
					if (amount / base_amount <= 1e-10)
						continue;
					padResources.TransferResource (bres.name, amount);
				}
			} while (did_work && kerbalHours / base_kerbalHours > 1e-10);

			SetPadMass ();

			if (count == 0) {
				state = State.Planning;
				KACalarmID = "";
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

		private IEnumerator CaptureCraft ()
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

			builder.SetCraftMass (0);

			CoupleWithCraft ();
			state = State.Transfer;
		}

		Collider[] get_colliders (Part p)
		{
			var o = p.transform.FindChild("model");
			return o.GetComponentsInChildren<Collider> ();
		}

		Box GetVesselBox (ShipConstruct ship)
		{
			PartHeightQuery phq = null;
			Box box = null;
			for (int i = 0; i < ship.parts.Count; i++) {
				Part p = ship[i];
				var colliders = get_colliders (p);
				for (int j = 0; j < colliders.Length; j++) {
					var col = colliders[j];
					if (col.gameObject.layer != 21 && col.enabled) {
						if (box == null) {
							box = new Box (col.bounds);
						} else {
							box.Add (col.bounds);
						}

						float min_y = col.bounds.min.y;
						if (phq == null) {
							phq = new PartHeightQuery (min_y);
						}
						phq.lowestPoint = Mathf.Min (phq.lowestPoint, min_y);

						if (!phq.lowestOnParts.ContainsKey (p)) {
							phq.lowestOnParts.Add (p, min_y);
						}
						phq.lowestOnParts[p] = Mathf.Min (phq.lowestOnParts[p], min_y);
					}
				}
			}
			Debug.Log (String.Format ("[EL] GetVesselBox {0} {1}", box.min, box.max));
			for (int i = 0; i < ship.parts.Count; i++) {
				Part p = ship[i];
				p.SendMessage ("OnPutToGround", phq,
							   SendMessageOptions.DontRequireReceiver);
			}
			return box;
		}

		IEnumerator FixAirstreamShielding (Vessel v)
		{
			yield return null;
			int num_parts = v.parts.Count;
			var AS = new AirstreamShield (builder);
			for (int i = 0; i < num_parts; i++) {
				v.parts[i].AddShield (AS);
			}
			yield return null;
			for (int i = 0; i < num_parts; i++) {
				v.parts[i].RemoveShield (AS);
			}
		}

		void EnableExtendingLaunchClamps (ShipConstruct ship)
		{
			for (int i = 0; i < ship.parts.Count; i++) {
				var p = ship.parts[i];
				var elc = p.FindModulesImplementing<ExtendingLaunchClamp> ();
				for (int j = 0; j < elc.Count; j++) {
					(elc[j] as ExtendingLaunchClamp).EnableExtension ();
				}
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

			string landedAt = "External Launchpad";
			string flag = flagname;
			Game game = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			Box vessel_bounds = GetVesselBox (nship);
			launchTransform = builder.PlaceShip (nship, vessel_bounds);

			EnableExtendingLaunchClamps (nship);
			ShipConstruction.AssembleForLaunch (nship, landedAt, flag, game,
												crew);
			var FlightVessels = FlightGlobals.Vessels;
			craftVessel = FlightVessels[FlightVessels.Count - 1];

			FlightGlobals.ForceSetActiveVessel (craftVessel);
			if (builder.capture) {
				craftVessel.Splashed = craftVessel.Landed = false;
			} else {
				bool loaded = craftVessel.loaded;
				bool packed = craftVessel.packed;
				craftVessel.loaded = true;
				craftVessel.packed = false;
				craftVessel.GetHeightFromTerrain ();
				Debug.Log (String.Format ("[EL] hft {0}", craftVessel.heightFromTerrain));
				craftVessel.loaded = loaded;
				craftVessel.packed = packed;
			}

			Vector3 offset = craftVessel.transform.position - launchTransform.position;
			craftOffset = launchTransform.InverseTransformDirection (offset);
			SetupCraftResources (craftVessel);

			Staging.beginFlight ();

			if (builder.capture) {
				FlightGlobals.overrideOrbit = true;
				(builder as PartModule).StartCoroutine (CaptureCraft ());
			} else {
				state = State.Idle;
			}
		}

		public void LoadCraft (string filename, string flagname)
		{
			this.filename = filename;
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
				state = State.Building;
				paused = false;
			}
		}

		public void Save (ConfigNode node)
		{
			node.AddValue ("filename", filename);
			node.AddValue ("flagname", flagname);
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
			node.AddValue ("KACalarmID", KACalarmID);
			if (vesselInfo != null) {
				ConfigNode vi = node.AddNode ("DockedVesselInfo");
				vesselInfo.Save (vi);
			}
		}

		internal static void dumpxform (Transform t, string n = "")
		{
			Debug.Log (String.Format ("[EL] xform: {0}", n + t.name));
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
			filename = node.GetValue ("filename");
			flagname = node.GetValue ("flagname");
			craftConfig = node.GetNode ("CraftConfig");
			if (node.HasNode ("BuildCost")) {
				var bc = node.GetNode ("BuildCost");
				buildCost = new CostReport ();
				buildCost.Load (bc);
			}
			if (node.HasNode ("BuiltStuff")) {
				var bs = node.GetNode ("BuiltStuff");
				builtStuff = new CostReport ();
				builtStuff.Load (bs);
			}
			if (node.HasValue ("state")) {
				var s = node.GetValue ("state");
				state = (State) Enum.Parse (typeof (State), s);
				if (state == State.Dewarping) {
					// The game got saved while the Dewarping state was still
					// active. Rather than restarting the dewarp coroutine,
					// Just jump straight to the Complete state.
					state = State.Complete;
				}
			}
			if (node.HasValue ("paused")) {
				var s = node.GetValue ("paused");
				bool p = false;
				bool.TryParse (s, out p);
				paused = p;
			}
			KACalarmID = node.GetValue ("KACalarmID");
			if (node.HasNode ("DockedVesselInfo")) {
				ConfigNode vi = node.GetNode ("DockedVesselInfo");
				vesselInfo = new DockedVesselInfo ();
				vesselInfo.Load (vi);
			}
		}

		public ExBuildControl (IBuilder builder)
		{
			this.builder = builder;
		}

		internal void OnStart ()
		{
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onPartDie.Add (onPartDie);
			if (vesselInfo != null) {
				craftRoot = builder.vessel[vesselInfo.rootPartUId];
				if (craftRoot == null) {
					CleaupAfterRelease ();
				}
			}
			FindVesselResources ();
			SetPadMass ();
		}

		internal void OnDestroy ()
		{
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onPartDie.Remove (onPartDie);
		}

		void CleaupAfterRelease ()
		{
			craftRoot = null;
			craftConfig = null;
			vesselInfo = null;
			buildCost = null;
			builtStuff = null;
			state = State.Idle;
		}

		public void ReleaseVessel ()
		{
			TransferResources ();
			craftRoot.Undock (vesselInfo);
			var vesselCount = FlightGlobals.Vessels.Count;
			Vessel vsl = FlightGlobals.Vessels[vesselCount - 1];
			FlightGlobals.ForceSetActiveVessel (vsl);
			builder.part.StartCoroutine (FixAirstreamShielding (vsl));

			CleaupAfterRelease ();
		}

		bool InitializeB9Wings (Vessel v)
		{
			bool called = false;
			for (int i = 0; i < v.parts.Count; i++) {
				Part p = v.parts[i];
				PartModule moduleB9W;
				if (p.Modules.Contains ("WingProcedural")) {
					moduleB9W = p.Modules["WingProcedural"];
				} else {
					continue;
				}
				Type typeB9W = moduleB9W.GetType ();
				MethodInfo methodB9W = typeB9W.GetMethod ("CalculateAerodynamicValues");
				methodB9W.Invoke (moduleB9W, null);
				called = true;
			}
			return called;
		}

		bool InitializeFARSurfaces (Vessel v)
		{
			bool called = false;
			for (int i = 0; i < v.parts.Count; i++) {
				Part p = v.parts[i];
				PartModule moduleFAR;
				if (p.Modules.Contains ("FARControllableSurface")) {
					moduleFAR = p.Modules["FARControllableSurface"];
				} else if (p.Modules.Contains ("FARWingAerodynamicModel")) {
					moduleFAR = p.Modules["FARWingAerodynamicModel"];
				} else {
					continue;
				}
				Type typeFAR = moduleFAR.GetType ();
				MethodInfo methodFAR = typeFAR.GetMethod ("StartInitialization");
				methodFAR.Invoke (moduleFAR, null);
				called = true;
			}
			return called;
		}

		public CostReport getBuildCost (ConfigNode craft)
		{
			lockedParts = false;
			ShipConstruct ship = new ShipConstruct ();
			if (!ship.LoadShip (craft)) {
				return null;
			}
			if (!ship.shipPartsUnlocked) {
				lockedParts = true;
			}
			GameObject ro = ship.parts[0].localRoot.gameObject;
			Vessel craftVessel = ro.AddComponent<Vessel>();
			craftVessel.Initialize (true);
			if (ExSettings.B9Wings_Present) {
				if (!InitializeB9Wings (craftVessel)
					&& ExSettings.FAR_Present) {
					InitializeFARSurfaces (craftVessel);
				}
			} else if (ExSettings.FAR_Present) {
				InitializeFARSurfaces (craftVessel);
			}

			craftResources = new VesselResources (craftVessel);

			BuildCost resources = new BuildCost ();

			foreach (Part p in ship.parts) {
				resources.addPart (p);
			}
			craftVessel.Die ();

			return resources.cost;
		}

		void onPartDie (Part p)
		{
			if (p == craftRoot) {
				CleaupAfterRelease ();
			}
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == builder.vessel) {
				padResources = new VesselResources (builder.vessel);
				if (craftRoot != null && craftRoot.vessel != builder.vessel) {
					CleaupAfterRelease ();
				}
			}
		}
	}

}
