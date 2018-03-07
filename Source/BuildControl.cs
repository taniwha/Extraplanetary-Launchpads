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

	public class ELBuildControl : ELWorkSink
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

			public override string ToString ()
			{
				return "[" + min + "," + max + "]";
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
			ELBuildControl control
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

		public enum State { Idle, Planning, Building, Canceling, Dewarping, Complete, Transfer };

		public ELCraftType craftType = ELCraftType.VAB;

		public string filename
		{
			get;
			private set;
		}
		public string flagname
		{
			get;
			set;
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
		public RMResourceSet padResources
		{
			get;
			private set;
		}
		public RMResourceSet craftResources
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

		public ELVesselWorkNet workNet
		{
			get;
			private set;
		}

		public double productivity
		{
			get {
				if (workNet != null) {
					return workNet.Productivity;
				}
				return 0;
			}
		}

		public DockedVesselInfo vesselInfo { get; private set; }
		Transform launchTransform;
		public Part craftRoot { get; private set; }
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

		public bool isActive
		{
			get {
				return ((state == State.Building || state == State.Canceling)
						&& !paused);
			}
		}

		public double CalculateWork ()
		{
			if (paused) {
				return 0;
			}
			double hours = 0;
			var built = builtStuff.required;
			var cost = buildCost.required;
			if (state == State.Building) {
				for (int i = built.Count; i-- > 0; ) {
					var res = built[i];
					hours += res.kerbalHours * res.amount;
				}
			} else if (state == State.Canceling) {
				for (int i = built.Count; i-- > 0; ) {
					var res = built[i];
					var cres = ELBuildWindow.FindResource (cost, res.name);
					hours += res.kerbalHours * (cres.amount - res.amount);
				}
			}
			return hours;
		}

		public void DestroyPad ()
		{
			state = State.Idle;
			builder.part.explosionPotential = 0.1f;
			builder.part.explode ();
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
			if (builtStuff != null && buildCost != null) {
				var built = builtStuff.required;
				var cost = buildCost.required;

				foreach (var bres in built) {
					var cres = ELBuildWindow.FindResource (cost, bres.name);
					mass += (cres.amount - bres.amount) * bres.density;
				}
			}
			builder.SetCraftMass (mass);
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
					double amount = work / res.kerbalHours;
					double base_amount = Math.Abs (amount);

					if (amount > res.amount)
						amount = res.amount;
					double avail = padResources.ResourceAmount (res.name);
					if (amount > avail)
						amount = avail;
					//Debug.Log (String.Format ("[EL Launchpad] work:{0}:{1}:{2}:{3}:{4}",
					//						  res.name, res.kerbalHours, res.amount, avail, amount));
					if (amount <= 0)
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
			} while (did_work);

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

			bool did_work;
			int count;
			do {
				count = 0;
				foreach (var bres in built) {
					var cres = ELBuildWindow.FindResource (cost, bres.name);
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
					var cres = ELBuildWindow.FindResource (cost, bres.name);
					double remaining = cres.amount - bres.amount;
					if (remaining <= 0) {
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
					if (amount <= 0)
						continue;
					padResources.TransferResource (bres.name, amount);
				}
			} while (did_work);

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

		internal void SetupCraftResources (Vessel vsl)
		{
			craftResources = new RMResourceSet (vsl);
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
			var com = v.localCoM;
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

		public delegate void PostCaptureDelegate ();
		public PostCaptureDelegate PostCapture = () => { };

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
			PostCapture ();
		}

		Collider[] get_colliders (Part p)
		{
			var o = p.transform.Find("model");
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
			Debug.Log (String.Format ("[EL] GetVesselBox {0} {1}", ship.parts[0].localRoot.transform.position, box));
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
				var elc = p.FindModulesImplementing<LaunchClamp> ();
				for (int j = 0; j < elc.Count; j++) {
					(elc[j] as LaunchClamp).EnableExtension ();
				}
			}
		}

		internal void BuildAndLaunchCraft ()
		{
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			string landedAt = "External Launchpad";
			string flag = flagname;
			Game game = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			Box vessel_bounds = GetVesselBox (nship);
			launchTransform = builder.PlaceShip (nship, vessel_bounds);

			EnableExtendingLaunchClamps (nship);
			ShipConstruction.AssembleForLaunch (nship, landedAt, landedAt,
												flag, game, crew);
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
				Debug.LogFormat ("[EL] hft {0}", craftVessel.heightFromTerrain);
				craftVessel.loaded = loaded;
				craftVessel.packed = packed;
			}

			Vector3 offset = craftVessel.transform.position - launchTransform.position;
			craftOffset = launchTransform.InverseTransformDirection (offset);
			SetupCraftResources (craftVessel);

			KSP.UI.Screens.StageManager.BeginFlight ();

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
			if (filename != null) {
				node.AddValue ("filename", filename);
			}
			if (flagname != null) {
				node.AddValue ("flagname", flagname);
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
			node.AddValue ("KACalarmID", KACalarmID);
			if (vesselInfo != null) {
				ConfigNode vi = node.AddNode ("DockedVesselInfo");
				vesselInfo.Save (vi);
			}
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
			padResources = new RMResourceSet (builder.vessel);
			var craft_parts = CraftParts ();
			if (craft_parts.Count > 0) {
				craftResources = new RMResourceSet ();
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
			if (node.HasValue ("filename")) {
				filename = node.GetValue ("filename");
			}
			if (node.HasValue ("flagname")) {
				flagname = node.GetValue ("flagname");
			}
			if (node.HasNode ("CraftConfig")) {
				craftConfig = node.GetNode ("CraftConfig");
			}
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
			if (node.HasValue ("KACalarmID")) {
				KACalarmID = node.GetValue ("KACalarmID");
			}
			if (node.HasNode ("DockedVesselInfo")) {
				ConfigNode vi = node.GetNode ("DockedVesselInfo");
				vesselInfo = new DockedVesselInfo ();
				vesselInfo.Load (vi);
			}
		}

		public ELBuildControl (IBuilder builder)
		{
			this.builder = builder;
		}

		internal void OnStart ()
		{
			workNet = builder.vessel.FindVesselModuleImplementing<ELVesselWorkNet> ();
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
			vesselInfo = null;
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
			craftVessel.vesselName = "EL craftVessel - " + craft.GetValue ("ship");
			craftVessel.Initialize (true);
			foreach (Part part in craftVessel.parts) {
				part.ModulesOnStart ();
			}
			if (ELSettings.B9Wings_Present) {
				if (!InitializeB9Wings (craftVessel)
					&& ELSettings.FAR_Present) {
					InitializeFARSurfaces (craftVessel);
				}
			} else if (ELSettings.FAR_Present) {
				InitializeFARSurfaces (craftVessel);
			}

			// needed for displaying optional resources during the planning
			// stage.
			craftResources = new RMResourceSet (craftVessel);

			BuildCost resources = new BuildCost ();

			foreach (Part p in craftVessel.parts) {
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
				padResources = new RMResourceSet (builder.vessel);
				if (craftRoot != null && craftRoot.vessel != builder.vessel) {
					CleaupAfterRelease ();
				}
			}
		}
	}

}
