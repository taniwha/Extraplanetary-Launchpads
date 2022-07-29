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
using System.IO;
using UnityEngine;

using KSP.Localization;

namespace ExtraplanetaryLaunchpads {

	public class ELBuildControl : ELWorkSink
	{
		public static readonly EventData<ELBuildControl> onBuildStateChanged = new EventData<ELBuildControl> ("onBuildStateChanged");
		public static readonly EventData<ELBuildControl, string, string> onPadRenamed = new EventData<ELBuildControl, string, string> ("onPadRenamed");

		public interface IBuilder
		{
			void Highlight (bool on);
			void UpdateMenus (bool visible);
			void SetCraftMass (double craft_mass);
			void SetShipTransform (Transform shipTransform, Part rootPart);
			Transform PlaceShip (Transform shipTransform, Box vessel_bounds);
			void RepositionShip (Vessel ship);
			void PostBuild (Vessel craftVessel);

			bool canBuild { get; }
			bool capture { get; }
			Vessel vessel { get; }
			Part part { get; }
			ELBuildControl control { get; }
			string Name { get; set; }
			string LandedAt { get; }
			string LaunchedFrom { get; }

			uint ID { get; }
		}

		public IBuilder builder { get; private set; }

		public enum State { Idle, Planning, Building, Canceling, Dewarping, Complete, Transfer };

		public ELCraftType craftType = ELCraftType.VAB;

		public string filename { get; private set; }
		public string flagname { get; set; }
		public string thumbPath { get; private set; }
		public bool lockedParts { get; private set; }
		public ConfigNode craftConfig { get; private set; }
		public CraftHull craftHull { get; private set; }
		public GameObject craftHullObject { get; private set; }
		RMResourceManager padResourceManager;
		public RMResourceSet padResources
		{
			get {
				if (padResourceManager != null) {
					return padResourceManager.masterSet;
				}
				return null;
			}
		}
		RMResourceManager craftResourceManager;
		public RMResourceSet craftResources
		{
			get {
				if (craftResourceManager != null) {
					return craftResourceManager.masterSet;
				}
				return null;
			}
		}
		public List<string> craftBoM { get; private set; }
		public bool craftBoMdirty { get; private set; }
		public CostReport buildCost { get; private set; }
		public CostReport builtStuff { get; private set; }
		State _state;
		public State state {
			get { return _state; }
			private set {
				_state = value;
				onBuildStateChanged.Fire (this);
			}
		}
		public bool paused { get; private set; }
		public string KACalarmID = "";

		public ELVesselWorkNet workNet { get; set; }

		public bool ready { get { return padResources != null; } }

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
		public Part craftRoot { get; private set; }
		ModuleGroundPart groundPartModule;
		public string craftName
		{
			get {
				if (vesselInfo != null) {
					return vesselInfo.name;
				} else if (craftConfig != null) {
					return craftConfig.GetValue ("ship");
				} else {
					return "";
				}
			}
			set {
				if (vesselInfo != null) {
					vesselInfo.name = value;
				} else if (craftConfig != null) {
					craftConfig.SetValue ("ship", value);
				}
			}
		}

		string _savesPath;
		string savesPath
		{
			get {
				if (_savesPath == null) {
					_savesPath = EL_Utils.ApplicationRootPath + "saves/";
					_savesPath += HighLogic.SaveFolder;
				}
				return _savesPath;
			}
		}

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
				if (!paused) {
					paused = true;
					onBuildStateChanged.Fire (this);
				}
			}
		}

		public void ResumeBuild ()
		{
			if (paused) {
				paused = false;
				onBuildStateChanged.Fire (this);
			}
		}

		public bool isActive
		{
			get {
				return ((state == State.Building || state == State.Canceling)
						&& !paused);
			}
		}

		static BuildResource FindResource (List<BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
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
					var cres = FindResource (cost, res.name);
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
					var cres = FindResource (cost, bres.name);
					mass += (cres.amount - bres.amount) * bres.density;
				}
			}
			builder.SetCraftMass (mass);
		}

		private void DoWork_Build (double kerbalHours)
		{
			if (builtStuff == null) {
				Debug.Log ("DoWork_Build: builtStuff null");
			}
			var required = builtStuff.required;
			if (required == null) {
				Debug.Log ("DoWork_Build: required null");
			}

			bool did_work;
			int count;

			for (int i = required.Count; i-- > 0; ) {
				required[i].deltaAmount = 0;
			}

			do {
				//Debug.LogFormat ("[EL Launchpad] KerbalHours: {0}", kerbalHours);
				count = 0;
				for (int i = required.Count; i-- > 0; ) {
					var res = required[i];
					if (res == null) {
						Debug.Log ($"DoWork_Build: res {i} null");
					}
					if (padResources == null) {
						Debug.Log ($"DoWork_Build: padResources null");
					}
					if (res.amount > 0
						&& padResources.ResourceAmount (res.name) > 0) {
						count++;
					}
				}
				if (kerbalHours == 0) {
					break;
				}
				if (count == 0)
					break;
				did_work = false;
				for (int i = required.Count; i-- > 0; ) {
					var res = required[i];
					if (res == null) {
						Debug.Log ($"DoWork_Build: res {i} null");
					}
					if (res.amount == 0) {
						continue;
					}
					double avail = padResources.ResourceAmount (res.name);
					if (avail == 0) {
						continue;
					}
					double work = kerbalHours / count--;
					double amount = work / res.kerbalHours;
					double base_amount = Math.Abs (amount);

					if (amount > res.amount)
						amount = res.amount;
					if (amount > avail)
						amount = avail;
					//Debug.LogFormat ("[EL Launchpad] work:{0}:{1}:{2}:{3}:{4}",
					//				 res.name, res.kerbalHours, res.amount, avail, amount);
					if (amount <= 0)
						continue;
					kerbalHours -= work;
					did_work = true;
					// do only the work required to process the actual amount
					// of consumed resource
					double dkH = work * amount / base_amount;
					if (dkH > work) {
						dkH = work;
					}
					work -= dkH;
					// return any unused kerbal-hours to the pool
					kerbalHours += work;
					res.amount -= amount;
					//Debug.Log($"add delta: {amount}");
					res.deltaAmount += amount;
					padResources.TransferResource (res.name, -amount);
				}
				//Debug.LogFormat ("[EL Launchpad] work:{0}:{1}", did_work, kerbalHours);
			} while (did_work);

			SetPadMass ();

			// recount resources that still need to be processed into the build
			count = required.Where (r => r.amount > 0).Count ();
			if (count == 0) {
				(builder as PartModule).StartCoroutine (DewarpAndBuildCraft ());
				KACalarmID = "";
			}
		}

		int CountResources (List<BuildResource> built, List<BuildResource> cost)
		{
			int count = 0;
			foreach (var bres in built) {
				var cres = FindResource (cost, bres.name);
				if (cres.amount - bres.amount > 0) {
					count++;
				}
			}
			return count;
		}

		private void DoWork_Cancel (double kerbalHours)
		{
			var built = builtStuff.required;
			var cost = buildCost.required;

			for (int i = built.Count; i-- > 0; ) {
				built[i].deltaAmount = 0;
			}

			bool did_work;
			int count;
			do {
				count = CountResources (built, cost);
				if (kerbalHours == 0) {
					break;
				}
				if (count == 0) {
					break;
				}

				did_work = false;
				foreach (var bres in built) {
					var cres = FindResource (cost, bres.name);
					double remaining = cres.amount - bres.amount;
					if (remaining <= 0) {
						continue;
					}
					double work = kerbalHours / count--;
					double amount = work / bres.kerbalHours;
					double base_amount = Math.Abs (amount);

					if (amount > remaining) {
						amount = remaining;
					}
					kerbalHours -= work;
					did_work = true;
					// do only the work required to process the actual amount
					// of returned or disposed resource
					double dkH = work * amount / base_amount;
					if (dkH > work) {
						dkH = work;
					}
					work -= dkH;
					// return any unused kerbal-hours to the pool
					kerbalHours += work;

					bres.amount += amount;
					//Debug.Log("remove delta: "+amount);
					bres.deltaAmount += amount;

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

			// recount resources that still need to be taken from the build
			count = CountResources (built, cost);
			if (count == 0) {
				state = State.Planning;
				KACalarmID = "";
				builtStuff = null;	// ensure pad mass gets reset
			}

			SetPadMass ();
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
			Part rootPart = vsl.parts[0].localRoot;
			craftResourceManager = new RMResourceManager (vsl.parts, rootPart,
														  false);
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

		private void SetCraftOrbit (Vessel craftVessel, OrbitDriver.UpdateMode mode)
		{
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

		private void CoupleWithCraft (Vessel craftVessel)
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

		void HookGroundPartPickup ()
		{
			groundPartModule = craftRoot.FindModuleImplementing<ModuleGroundPart> ();
			if (groundPartModule != null) {
				// craft with ModuleGroundPart should have only the one part as
				// properly configured deployable parts do not allow attachment
				// in the editor and KIS refuses to touch them
				ELGroundPart.HookPickup (groundPartModule, this);
			}
		}

		private IEnumerator CaptureCraft (Vessel craftVessel)
		{
			FlightGlobals.overrideOrbit = true;
			while (true) {
				Debug.Log($"[EL] Waiting to capture {craftVessel.vesselName}");
				bool partsInitialized = true;
				foreach (Part p in craftVessel.parts) {
					if (!p.started) {
						partsInitialized = false;
						break;
					}
				}
				builder.RepositionShip (craftVessel);
				SetCraftOrbit (craftVessel, OrbitDriver.UpdateMode.UPDATE);
				if (partsInitialized) {
					break;
				}
				OrbitPhysicsManager.HoldVesselUnpack (2);
				yield return null;
			}

			FlightGlobals.overrideOrbit = false;
			SetCraftOrbit (craftVessel, OrbitDriver.UpdateMode.UPDATE);
			craftVessel.GoOffRails ();

			builder.SetCraftMass (0);

			CoupleWithCraft (craftVessel);
			HookGroundPartPickup ();
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
			RotateLaunchClamps (ship);

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
			Part root = ship.parts[0].localRoot;
			Debug.Log ($"[EL] GetVesselBox {root.transform.position} {box}");
			Debug.Log ($"[EL]   {root.transform.InverseTransformPoint(box.min)} {root.transform.InverseTransformPoint(box.max)}");
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

		void UpdateAttachNode (Part p, AttachNode vnode)
		{
			var pnode = p.FindAttachNode (vnode.id);
			if (pnode != null) {
				pnode.originalPosition = vnode.originalPosition;
				pnode.position = vnode.position;
				pnode.size = vnode.size;
			}
		}

		void ApplyNodeVariants (ShipConstruct ship)
		{
			for (int i = 0; i < ship.parts.Count; i++) {
				var p = ship.parts[i];
				var pv = p.FindModulesImplementing<ModulePartVariants> ();
				for (int j = 0; j < pv.Count; j++) {
					var variant = pv[j].SelectedVariant;
					for (int k = 0; k < variant.AttachNodes.Count; k++) {
						var vnode = variant.AttachNodes[k];
						UpdateAttachNode (p, vnode);
					}
				}
			}
		}

		void RotateLaunchClamps (ShipConstruct ship)
		{
			for (int i = 0; i < ship.parts.Count; i++) {
				var p = ship.parts[i];
				var elc = p.FindModulesImplementing<ELExtendingLaunchClamp> ();
				for (int j = 0; j < elc.Count; j++) {
					elc[j].RotateTower ();
				}
				if (elc.Count < 1) {
					if (p.Modules.GetModule("ModuleRestockLaunchClamp") != null) {
						p.SendMessage("RotateTower", SendMessageOptions.DontRequireReceiver);
					}
				}
			}
		}

		void EnableExtendingLaunchClamps (ShipConstruct ship)
		{
			for (int i = 0; i < ship.parts.Count; i++) {
				var p = ship.parts[i];
				var elc = p.FindModulesImplementing<ELExtendingLaunchClamp> ();
				for (int j = 0; j < elc.Count; j++) {
					elc[j].EnableExtension ();
				}
				if (p.name == "launchClamp1") {
					// ReStock replaces the stock module with its own, which is
					// derived from the stock module. However,
					// ReplaceLaunchClamps will not do any replacement because
					// the module uses a different name. Thus this will pick up
					// the ReStock module.
					var lc = p.Modules.GetModule("ModuleRestockLaunchClamp");
					if (lc != null) {
						(lc as LaunchClamp).EnableExtension ();
					}
				}
			}
		}

		internal void BuildAndLaunchCraft ()
		{
			DestroyCraftHull ();
			// build craft
			ShipConstruct nship = new ShipConstruct ();
			nship.LoadShip (craftConfig);

			ApplyNodeVariants (nship);

			string landedAt = "";
			string flag = flagname;
			Game game = FlightDriver.FlightStateCache;
			VesselCrewManifest crew = new VesselCrewManifest ();

			Box vessel_bounds = GetVesselBox (nship);
			var rootPart = nship.parts[0].localRoot;
			var ro = rootPart.gameObject;
			var od = ro.GetComponent<OrbitDriver> () ?? ro.AddComponent<OrbitDriver> ();
			od.orbit = new Orbit(builder.vessel.orbit);
			var shipTransform = rootPart.transform;
			builder.SetShipTransform (shipTransform, rootPart);
			builder.PlaceShip (shipTransform, vessel_bounds);

			EnableExtendingLaunchClamps (nship);
			ShipConstruction.AssembleForLaunch (nship, landedAt, landedAt,
												flag, game, crew);
			var craftVessel = nship.parts[0].localRoot.GetComponent<Vessel> ();
			craftVessel.launchedFrom = builder.LaunchedFrom;
			od.vessel = craftVessel;

			FlightGlobals.ForceSetActiveVessel (craftVessel);
			SetCraftOrbit (craftVessel, OrbitDriver.UpdateMode.IDLE);
			builder.PostBuild (craftVessel);
			if (builder.capture) {
				craftVessel.Splashed = craftVessel.Landed = false;
			} else {
				bool loaded = craftVessel.loaded;
				bool packed = craftVessel.packed;
				craftVessel.loaded = true;
				craftVessel.packed = false;
				// The default situation for new vessels is PRELAUNCH, but
				// that is not so good for bases because contracts check for
				// LANDED. XXX should this be selectable?
				craftVessel.situation = Vessel.Situations.LANDED;
				craftVessel.GetHeightFromTerrain ();
				Debug.Log ($"[ELBuildControl] height from terrain {craftVessel.heightFromTerrain}");
				craftVessel.loaded = loaded;
				craftVessel.packed = packed;
			}

			SetupCraftResources (craftVessel);

			KSP.UI.Screens.StageManager.BeginFlight ();

			builtStuff = null;	// ensure pad mass gets reset
			SetPadMass ();
			if (builder.capture) {
				var b = builder as PartModule;
				b.StartCoroutine (CaptureCraft (craftVessel));
			} else {
				CleanupAfterRelease ();
			}
		}

		void ReplaceLaunchClamps (ConfigNode craft)
		{
			foreach (ConfigNode node in craft.nodes) {
				if (node.name == "PART") {
					string name = ShipTemplate.GetPartName (node);
					string id = ShipTemplate.GetPartId (node);
					if (name == "launchClamp1") {
						foreach (ConfigNode subnode in node.nodes) {
							if (subnode.name == "MODULE") {
								string modname = subnode.GetValue ("name");
								if (modname == "LaunchClamp") {
									subnode.SetValue ("name", "ELExtendingLaunchClamp");
									node.SetValue ("part", "ELExtendingLaunchClamp_" + id);
								}
							}
						}
					}
				}
			}
		}

		public void LoadCraft (string filename, string flagname, string thumbPath)
		{
			this.filename = filename;
			this.flagname = flagname;
			this.thumbPath = thumbPath;

			if (!File.Exists (filename)) {
				Debug.LogWarning ($"File '{filename}' does not exist");
				return;
			}

			string craftText = File.ReadAllText(filename);
			ConfigNode craft = ConfigNode.Parse(craftText);
			ReplaceLaunchClamps(craft);

			craftName = Localizer.Format(craft.GetValue("ship"));
			if ((buildCost = getBuildCost(craft, craftText)) != null) {
				craftConfig = craft;
			}
			PlaceCraftHull();
			state = State.Planning;
			craftBoMdirty = true;
		}

		public void UnloadCraft ()
		{
			craftConfig = null;
			craftBoM = null;
			buildCost = null;
			CleanupAfterRelease ();
		}

		public bool CreateBoM ()
		{
			string str;

			craftBoMdirty = false;
			if (craftConfig != null) {
				craftBoM = new List<string> ();
				var partCounts = new Dictionary<string, int> ();

				str = craftConfig.GetValue ("description");
				if (!string.IsNullOrEmpty (str)) {
					str = Localizer.Format(str).Replace ('¨', '\n');
					craftBoM.Add (str);
					craftBoM.Add ("");	// blank line
				}
				foreach (var part in craftConfig.GetNodes ("PART")) {
					string name = part.GetValue ("part").Split ('_')[0];
					if (!partCounts.ContainsKey (name)) {
						partCounts[name] = 0;
					}
					partCounts[name]++;
				}
				foreach (var key in partCounts.Keys) {
					var ap = PartLoader.getPartInfoByName (key);
					craftBoM.Add($"{partCounts[key]}x  {ap.title}");
				}
				return true;
			}
			craftBoM = null;
			return false;
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
			if (thumbPath != null) {
				node.AddValue ("thumbPath", thumbPath);
			}
			if (craftConfig != null) {
				craftConfig.name = "CraftConfig";
				node.AddNode (craftConfig);
			}
			if (craftHull != null) {
				var ch = node.AddNode ("CraftHull");
				craftHull.Save (ch);
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

		internal HashSet<Part> CraftParts ()
		{
			var part_set = new HashSet<Part> ();
			if (craftRoot != null) {
				var root = craftRoot;
				Debug.Log (String.Format ("[EL] CraftParts root {0}", root));
				part_set.Add (root);
				part_set.UnionWith (root.FindChildParts<Part> (true));
			}
			return part_set;
		}

		void FindVesselResources ()
		{
			var craft_parts = CraftParts ();
			var pad_parts = new HashSet<Part> (builder.vessel.parts);
			pad_parts.ExceptWith (craft_parts);
			//Debug.Log ($"[ELBuildControl] FindVesselResources pad {pad_parts.Count} craft {craft_parts.Count}");

			string vesselName = builder.vessel.vesselName;
			Part vesselRoot = builder.part.localRoot;
			//Debug.Log ($"[ELBuildControl] FindVesselResources pad");
			padResourceManager = new RMResourceManager (vesselName,
														pad_parts,
														vesselRoot,
														craft_parts,
														false);

			if (craft_parts.Count > 0) {
				//Debug.Log ($"[ELBuildControl] FindVesselResources craft");
				craftResourceManager = new RMResourceManager (craftName,
															  craft_parts,
															  craftRoot,
															  null,
															  false);
			}
		}

		IEnumerator WaitAndFindVesselResources ()
		{
			yield return null;
			if (builder.vessel != null) {
				FindVesselResources ();
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
			if (node.HasValue ("thumbPath")) {
				thumbPath = node.GetValue ("thumbPath");
			}
			if (node.HasNode ("CraftConfig")) {
				craftConfig = node.GetNode ("CraftConfig");
			}
			if (node.HasNode ("CraftHull")) {
				craftHull = new CraftHull ();
				craftHull.Load (node.GetNode ("CraftHull"));
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
				_state = (State) Enum.Parse (typeof (State), s);
				if (_state == State.Dewarping) {
					// The game got saved while the Dewarping state was still
					// active. Rather than restarting the dewarp coroutine,
					// Just jump straight to the Complete state.
					// DO want to fire the state for switch to Complete
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
			GameEvents.onPartUnpack.Add (onPartUnpack);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onPartDie.Add (onPartDie);
			if (vesselInfo != null) {
				craftRoot = builder.vessel[vesselInfo.rootPartUId];
				if (craftRoot == null) {
					CleanupAfterRelease ();
				} else {
					HookGroundPartPickup ();
				}
			}
			PlaceCraftHull ();
			(builder as PartModule).StartCoroutine (WaitAndFindVesselResources ());
			SetPadMass ();
		}

		public void PlaceCraftHull ()
		{
			if (craftHull != null && ELSettings.ShowCraftHull) {
				if (craftHullObject == null) {
					if (!craftHull.LoadHull (savesPath)) {
						craftHull.MakeBoxHull ();
					}
					craftHullObject = craftHull.CreateHull (craftName+":hull");
				}
				craftHullObject.transform.SetParent (null);
				craftHull.RestoreTransform (craftHullObject.transform);
				craftHull.PlaceHull (builder, craftHullObject);
			}
		}

		void DestroyCraftHull ()
		{
			if (craftHull != null) {
				craftHull.Destroy();
				craftHull = null;
			}
			if (craftHullObject != null) {
				UnityEngine.Object.Destroy (craftHullObject);
				craftHullObject = null;
			}
		}

		internal void OnDestroy ()
		{
			GameEvents.onPartUnpack.Remove (onPartUnpack);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onPartDie.Remove (onPartDie);
			DestroyCraftHull ();
		}

		IEnumerator WaitAndStopCoroutines (MonoBehaviour behavior)
		{
			yield return null;
			behavior.StopAllCoroutines ();
		}

		void onPartUnpack (Part p)
		{
			if (p != builder.part) {
				return;
			}
			if (groundPartModule != null) {
				var b = builder as PartModule;
				b.StartCoroutine (WaitAndStopCoroutines (groundPartModule));
			}
		}

		public void OnRename (string oldName)
		{
			onPadRenamed.Fire (this, oldName, builder.Name);
		}

		void CleanupAfterRelease ()
		{
			craftRoot = null;
			groundPartModule = null;
			vesselInfo = null;
			builtStuff = null;	// ensure pad mass gets reset
			SetPadMass ();
			state = State.Idle;
			DestroyCraftHull ();
		}

		IEnumerator WaitAndSwitchVessel (Vessel vessel)
		{
			for (int count = 15; count-- > 0; ) {
				yield return null;
			}
			FlightGlobals.ForceSetActiveVessel (vessel);
		}

		public void ReleaseVessel (bool switchVessel = true)
		{
			TransferResources ();
			craftRoot.Undock (vesselInfo);
			var vesselCount = FlightGlobals.Vessels.Count;
			Vessel vsl = FlightGlobals.Vessels[vesselCount - 1];
			if (switchVessel) {
				(builder as PartModule).StartCoroutine (WaitAndSwitchVessel (vsl));
			}
			vsl.StartCoroutine (FixAirstreamShielding (vsl));

			CleanupAfterRelease ();
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

		void SanitizePart(Part part) {
			// Remove module that triggers OrbitDriver and cause temporary vessel being despawned due to illegal orbit parameters
			if (part.Modules.Contains("kOSProcessor")) {
				var kos = part.Modules["kOSProcessor"];
				if (kos != null) {
					part.Modules.Remove(kos);
				}
			}
		}

		void RemoveFX(Part part)
		{
			for (int i = part.fxGroups.Count; i-- > 0; ) {
				var group = part.fxGroups[i];
				for (int j = group.fxEmittersNewSystem.Count; j-- > 0; ) {
					var psys = group.fxEmittersNewSystem[j];
					if (psys && psys.gameObject) {
						UnityEngine.Object.Destroy (psys.gameObject);
					}
				}
				group.fxEmittersNewSystem.Clear();
				for (int j = group.lights.Count; j-- > 0; ) {
					var light = group.lights[j];
					if (light && light.gameObject) {
						UnityEngine.Object.Destroy (light.gameObject);
					}
				}
				group.lights.Clear();
				if (group.sfx) {
					UnityEngine.Object.Destroy (group.sfx);
					group.sfx = null;
				}
				if (group.audio) {
					UnityEngine.Object.Destroy (group.audio);
					group.audio = null;
				}
			}
		}

		public CostReport getBuildCost (ConfigNode craft, string craftText = null)
		{
			lockedParts = false;
			ShipConstruct ship = new ShipConstruct ();
			if (!ship.LoadShip (craft)) {
				lockedParts = true;
				return null;
			}
			if (ship.vesselDeltaV != null) {
				// The delta-v module is not needed. It has its own gameObject
				// for ShipConstruct.
				UnityEngine.Object.Destroy (ship.vesselDeltaV.gameObject);
				ship.vesselDeltaV = null;
			}
			if (!ship.shipPartsUnlocked) {
				lockedParts = true;
			}
			GameObject ro = ship.parts[0].localRoot.gameObject;
			var od = ro.GetComponent<OrbitDriver> () ?? ro.AddComponent<OrbitDriver> ();
			od.orbit = new Orbit(builder.vessel.orbit);

			//EL_Utils.dumpxform (ro.transform, true);
			Vessel craftVessel = ro.AddComponent<Vessel>();
			craftVessel.vesselName = "EL craftVessel - " + craft.GetValue ("ship");
			craftVessel.Initialize (true);
			SetCraftOrbit (craftVessel, OrbitDriver.UpdateMode.IDLE);
			foreach (Part part in craftVessel.parts) {
				RemoveFX(part);
				SanitizePart(part);
				part.ModulesOnStart();
			}

			if (ELSettings.B9Wings_Present) {
				if (!InitializeB9Wings (craftVessel)
					&& ELSettings.FAR_Present) {
					InitializeFARSurfaces (craftVessel);
				}
			} else if (ELSettings.FAR_Present) {
				InitializeFARSurfaces (craftVessel);
			}

			BuildCost resources = new BuildCost ();

			foreach (Part p in craftVessel.parts) {
				resources.addPart (p);
			}

			if (craftText != null && ELSettings.ShowCraftHull) {
				Quickhull.dump_points = ELSettings.DebugCraftHull;
				Quickhull.points_path = savesPath;
				DestroyCraftHull ();
				craftHull = new CraftHull (craftText);
				// GetVesselBox will rotate and minimize any launchclamps,
				// which is probably a good thing.
				var rootPart = ship.parts[0].localRoot;
				var rootPos = rootPart.transform.position;
				craftHull.SetBox (GetVesselBox (ship), rootPos);
				builder.SetShipTransform (rootPart.transform, rootPart);
				craftHull.SetTransform (rootPart.transform);
				if (ELSettings.DebugCraftHull
					|| !craftHull.LoadHull (savesPath)) {
					craftHull.BuildConvexHull (craftVessel);
					craftHull.SaveHull (savesPath);
				}
			}

			craftVessel.Die ();

			return resources.cost;
		}

		void onPartDie (Part p)
		{
			if (p == craftRoot) {
				CleanupAfterRelease ();
			}
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == builder.vessel) {
				if (craftRoot != null && craftRoot.vessel != builder.vessel) {
					CleanupAfterRelease ();
				}
				(builder as PartModule).StartCoroutine (WaitAndFindVesselResources ());
			}
		}
	}

}
