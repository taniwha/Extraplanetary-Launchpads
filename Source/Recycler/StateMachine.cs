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
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public class RecyclerFSM
	{
		class RecFSM : KerbalFSM {
			public KFSMState FindState (string name)
			{
				for (int i = 0; i < States.Count; i++) {
					if (States[i].name == name) {
						return States[i];
					}
				}
				return null;
			}
		}
		RecFSM fsm;

		KFSMState start_state;

		KFSMState state_off;
		KFSMState state_idle;
		KFSMState state_captured_idle;
		KFSMState state_processing_part;
		KFSMState state_transferring_resources;

		KFSMEvent event_enabled;
		KFSMEvent event_disabled;
		KFSMEvent event_enter_field;
		KFSMEvent event_part_selected;
		KFSMEvent event_resources_collected;
		KFSMEvent event_resources_transferred;
		KFSMEvent event_parts_exhausted;

		bool recycler_active;
		ELRecycler recycler;
		Collider RecycleField;
		RMResourceSet recycler_resources;
		Part active_part;
		ELVesselWorkNet workNet => recycler.workNet;
		HashSet<uint> recycle_parts;
		List<BuildResource> part_resources;
		int res_index;
		double deltat;

		void onenter_off (KFSMState s)
		{
		}
		bool check_enabled (KFSMState s)
		{
			return recycler_active;
		}
		bool check_disabled (KFSMState s)
		{
			return !recycler_active;
		}

		void onenter_idle (KFSMState s)
		{
			if (RecycleField != null) {
				RecycleField.enabled = true;
			}
		}
		void onleave_idle (KFSMState s)
		{
			if (RecycleField != null) {
				RecycleField.enabled = false;
			}
		}

		void onenter_captured_idle (KFSMState s)
		{
			active_part = SelectPart ();
		}
		void onleave_captured_idle (KFSMState s) { }
		bool check_part_selected (KFSMState s)
		{
			return active_part != null;
		}
		bool check_parts_exhausted (KFSMState s)
		{
			return active_part == null;
		}

		void onenter_processing_part (KFSMState s)
		{
			part_resources = PartResources (active_part);
		}
		void onleave_processing_part (KFSMState s)
		{
			Debug.Log (String.Format ("[EL RSM] onleave_processing_part"));
			recycle_parts.Remove (active_part.flightID);
			active_part.Die ();
			active_part = null;
		}
		bool check_resources_collected (KFSMState s)
		{
			return part_resources != null;
		}

		void onenter_transferring_resources (KFSMState s) { }
		void onleave_transferring_resources (KFSMState s)
		{
			part_resources = null;
		}
		void onupdate_transferring_resources ()
		{
			bool did_something;

			//Debug.Log (String.Format ("[EL RSM] onupdate_transferring_resources: {0}", deltat));
			if (part_resources.Count < 1) {
				return;
			}
			do {
				if (res_index >= part_resources.Count) {
					res_index = 0;
				}
				var br = part_resources[res_index];
				var old_amount = br.amount;
				deltat = TransferResource (br, deltat);
				//Debug.Log (String.Format ("[EL RSM] {0} {1} {2} {3} {4}",
										  //br.name, deltat,
										  //old_amount, br.amount,
										  //old_amount - br.amount));
				did_something = old_amount != br.amount;
				if (br.amount <= 0) {
					part_resources.RemoveAt (res_index);
				} else {
					res_index++;
				}
			} while (deltat > 1e-6 && part_resources.Count > 0 && did_something);
		}
		bool check_resources_transferred (KFSMState s)
		{
			return part_resources.Count < 1;
		}

		void LogEvent (string name)
		{
			Debug.Log (String.Format ("[EL RSM] event: {0}", name));
		}

		public RecyclerFSM (ELRecycler recycler)
		{
			this.recycler = recycler;

			state_off = new KFSMState ("Off");
			state_off.OnEnter = onenter_off;

			state_idle = new KFSMState ("Idle");
			state_idle.OnEnter = onenter_idle;
			state_idle.OnLeave = onleave_idle;

			state_captured_idle = new KFSMState ("Captured Idle");
			state_captured_idle.OnEnter = onenter_captured_idle;
			state_captured_idle.OnLeave = onleave_captured_idle;

			state_processing_part = new KFSMState ("Processing Part");
			state_processing_part.OnEnter = onenter_processing_part;
			state_processing_part.OnLeave = onleave_processing_part;

			state_transferring_resources = new KFSMState ("Transferring Resources");
			state_transferring_resources.OnEnter = onenter_transferring_resources;
			state_transferring_resources.OnLeave = onleave_transferring_resources;
			state_transferring_resources.OnFixedUpdate = onupdate_transferring_resources;

			event_enabled = new KFSMEvent ("Enabled");
			event_enabled.GoToStateOnEvent = state_idle;
			event_enabled.OnCheckCondition = check_enabled;
			event_enabled.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_enabled.OnEvent = delegate { LogEvent ("Enabled"); };

			event_disabled = new KFSMEvent ("Disabled");
			event_disabled.GoToStateOnEvent = state_off;
			event_disabled.OnCheckCondition = check_disabled;
			event_disabled.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_disabled.OnEvent = delegate { LogEvent ("Disabled"); };

			event_enter_field = new KFSMEvent ("Enter Field");
			event_enter_field.GoToStateOnEvent = state_captured_idle;
			event_enter_field.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			event_enter_field.OnEvent = delegate { LogEvent ("Enter Field"); };

			event_part_selected = new KFSMEvent ("Part Selected");
			event_part_selected.GoToStateOnEvent = state_processing_part;
			event_part_selected.OnCheckCondition = check_part_selected;
			event_part_selected.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_part_selected.OnEvent = delegate { LogEvent ("Part Selected"); };

			event_resources_collected = new KFSMEvent ("Resources Collected");
			event_resources_collected.GoToStateOnEvent = state_transferring_resources;
			event_resources_collected.OnCheckCondition = check_resources_collected;
			event_resources_collected.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_resources_collected.OnEvent = delegate { LogEvent ("Resources Collected"); };

			event_resources_transferred = new KFSMEvent ("Resources Transferred");
			event_resources_transferred.GoToStateOnEvent = state_captured_idle;
			event_resources_transferred.OnCheckCondition = check_resources_transferred;
			event_resources_transferred.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_resources_transferred.OnEvent = delegate { LogEvent ("Resources Transferred"); };

			event_parts_exhausted = new KFSMEvent ("Parts Exhausted");
			event_parts_exhausted.GoToStateOnEvent = state_idle;
			event_parts_exhausted.OnCheckCondition = check_parts_exhausted;
			event_parts_exhausted.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			event_parts_exhausted.OnEvent = delegate { LogEvent ("Parts Exhausted"); };

			fsm = new RecFSM ();
			fsm.AddState (state_off);
			fsm.AddState (state_idle);
			fsm.AddState (state_captured_idle);
			fsm.AddState (state_processing_part);
			fsm.AddState (state_transferring_resources);

			fsm.AddEvent (event_enabled, new KFSMState [] {state_off});
			fsm.AddEvent (event_disabled, new KFSMState [] {state_idle});
			fsm.AddEvent (event_enter_field, new KFSMState [] {state_idle});
			fsm.AddEvent (event_part_selected, new KFSMState [] {state_captured_idle});
			fsm.AddEvent (event_resources_collected, new KFSMState [] {state_processing_part});
			fsm.AddEvent (event_resources_transferred, new KFSMState [] {state_transferring_resources});
			fsm.AddEvent (event_parts_exhausted, new KFSMState [] {state_captured_idle});

			start_state = state_off;


			recycle_parts = new HashSet<uint> ();
		}

		public void Start (Collider field)
		{
			RecycleField = field;
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			recycler_resources = new RMResourceSet (recycler.vessel, recycle_parts);
			fsm.StartFSM (start_state);
		}

		public void Save (ConfigNode node)
		{
			if (recycle_parts != null) {
				var rp = recycle_parts.Select(s => s.ToString()).ToArray();
				node.AddValue ("recycle_parts", String.Join (" ", rp));
			}
			if (part_resources != null) {
				foreach (var res in part_resources) {
					var pr = node.AddNode ("part_resource");
					res.Save (pr);
				}
			}
			if (fsm.CurrentState != null) {
				node.AddValue ("state", fsm.CurrentState.name);
			}
		}

		public void Load (ConfigNode node)
		{
			recycle_parts = new HashSet<uint> ();
			if (node.HasValue ("recycle_parts")) {
				var ids = node.GetValue ("recycle_parts").Split (new char[]{' '});
				uint id;
				for (int i = 0; i < ids.Length; i++) {
					if (uint.TryParse (ids[i], out id)) {
						recycle_parts.Add (id);
					}
				}
			}
			if (node.HasNode ("part_resource")) {
				part_resources = new List<BuildResource> ();
				foreach (var pr in node.GetNodes ("part_resource")) {
					var res = new BuildResource ();
					res.Load (pr);
					part_resources.Add (res);
				}
			}
			if (node.HasValue ("state")) {
				var state = fsm.FindState (node.GetValue ("state"));
				if (state != null) {
					start_state = state;
				}
			}
		}

		void OnDestroy ()
		{
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
		}

		public void FixedUpdate ()
		{
			deltat = TimeWarp.fixedDeltaTime;
			fsm.FixedUpdateFSM ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == recycler.vessel) {
				recycler_resources = new RMResourceSet (recycler.vessel, recycle_parts);
			}
		}

		public void Enable ()
		{
			recycler_active = true;
		}

		public void Disable ()
		{
			recycler_active = false;
		}

		public string CurrentState
		{
			get {
				if (fsm.CurrentState != null) {
					return fsm.CurrentState.name;
				}
				return "FSM not started";
			}
		}

		public bool isBusy
		{
			get {
				return (fsm.CurrentState != state_off
						&& fsm.CurrentState != state_idle);
			}
		}

		public double ResourceMass
		{
			get {
				double mass = 0;
				if (part_resources != null) {
					for (int i = 0; i < part_resources.Count; i++) {
						var br = part_resources[i];
						mass += br.amount * br.density;
					}
				}
				return mass;
			}
		}

		IEnumerator WaitAndCouple (Part part)
		{
			yield return null;
			part.Couple (recycler.part);
			fsm.RunEvent (event_enter_field);
		}

		public void CollectParts (Part part)
		{
			if (FlightGlobals.ActiveVessel == part.vessel) {
				FlightGlobals.ForceSetActiveVessel (recycler.vessel);
			}
			recycle_parts.Clear ();
			foreach (var p in part.vessel.parts) {
				recycle_parts.Add (p.flightID);
			}
			recycler.StartCoroutine (WaitAndCouple (part));
		}

		static float random (float min, float max)
		{
			return UnityEngine.Random.Range (min, max);
		}

		static int random (int min, int max)
		{
			return UnityEngine.Random.Range (min, max);
		}

		Part SelectPart ()
		{
			var count = recycle_parts.Count;
			if (count < 1) {
				return null;
			}
			double prod = 0;
			if (workNet != null) {
				prod = workNet.Productivity;
			}
			prod = ELWorkshop.HyperCurve (prod);
			Part p;
			do {
				var ar = recycle_parts.ToArray ();
				var id = ar[random (0, recycle_parts.Count)];
				p = recycler.vessel[id];
				if (p == null) {
					recycle_parts.Remove (id);
				}
			} while (p == null && recycle_parts.Count > 0);
			while (p != null && p.children.Count > 0) {
				if (prod < 1 && random (0, 1f) < 1 - prod) {
					break;
				}
				p = p.children[random (0, p.children.Count)];
			}
			return p;
		}

		void ProcessIngredient (Ingredient ingredient, BuildResourceSet rd, bool xfer)
		{
			var res = ingredient.name;
			Recipe recipe = null;

			// If the resource is being transfered from a tank (rather than
			// coming from the part body itself), then a transfer recipe will
			// override a recycle recipe
			if (xfer) {
				recipe = ELRecipeDatabase.TransferRecipe (res);
			}
			if (recipe == null) {
				recipe = ELRecipeDatabase.RecycleRecipe (res);
			}

			if (recipe != null) {
				recipe = recipe.Bake (ingredient.ratio);
				foreach (var ing in recipe.ingredients) {
					if (ing.isReal) {
						var br = new BuildResource (ing);
						rd.Add (br);
					}
				}
			} else {
				if (ELRecipeDatabase.ResourceRecipe (res) != null) {
				} else {
					if (ingredient.isReal) {
						var br = new BuildResource (ingredient);
						rd.Add (br);
					}
				}
			}
		}

		void ProcessResource (RMResourceSet vr, string res, BuildResourceSet rd, bool xfer)
		{
			var amount = vr.ResourceAmount (res);
			var mass = amount * ELRecipeDatabase.ResourceDensity (res);

			ProcessIngredient (new Ingredient (res, mass), rd, xfer);
		}

		void ProcessKerbal (ProtoCrewMember crew, BuildResourceSet rd)
		{
			string message = crew.name + " was mulched";
			ScreenMessages.PostScreenMessage (message, 30.0f, ScreenMessageStyle.UPPER_CENTER);

			var part_recipe = ELRecipeDatabase.KerbalRecipe ();
			if (part_recipe == null) {
				return;
			}
			var kerbal_recipe = part_recipe.Bake (0.09375);//FIXME
			for (int i = 0; i < kerbal_recipe.ingredients.Count; i++) {
				var ingredient = kerbal_recipe.ingredients[i];
				ProcessIngredient (ingredient, rd, false);
			}
			foreach (var br in rd.Values) {
				Debug.Log (String.Format ("[EL RSM] ProcessKerbal: {0} {1} {2}", crew.name, br.name, br.amount));
			}
		}

		List<BuildResource> PartResources (Part p)
		{
			Debug.Log (String.Format ("[EL RSM] PartResources: {0} {1}", p.name, p.CrewCapacity));
			var bc = new BuildCost ();
			bc.addPart (p);
			var rd = new BuildResourceSet ();
			bool xfer = true;
			RMResourceProcessor process = delegate (RMResourceSet vr, string res) {
				ProcessResource (vr, res, rd, xfer);
			};
			bc.resources.Process (process);
			var reslist = rd.Values;
			rd.Clear ();
			bc.container.Process (process);
			reslist.AddRange (rd.Values);
			rd.Clear ();
			xfer = false;
			bc.hullResoures.Process (process);
			reslist.AddRange (rd.Values);
			if (p.CrewCapacity > 0 && !p.name.Contains ("kerbalEVA")) {
				rd.Clear ();
				for (int i = 0; i < p.protoModuleCrew.Count; i++) {
					var crew = p.protoModuleCrew[i];
					ProcessKerbal (crew, rd);
				}
				reslist.AddRange (rd.Values);
			}
			return reslist;
		}

		double TransferResource (BuildResource br, double deltat)
		{
			if (br.density > 0) {
				var amount = recycler.RecycleRate * deltat / br.density;
				var base_amount = amount;
				if (amount > br.amount) {
					amount = br.amount;
				}
				recycler_resources.TransferResource (br.name, amount);
				br.amount -= amount;	// any untransfered resource is lost
				br.mass = br.amount * br.density;
				deltat = deltat * (base_amount - amount) / base_amount;
			} else {
				// Massless resources are transferred in one tick (for now),
				// but consume the whole tick.
				recycler_resources.TransferResource (br.name, br.amount);
				br.amount = 0;
				deltat = 0;
			}
			return deltat;
		}
	}
}
