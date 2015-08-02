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
		ExRecycler recycler;
		Collider RecycleField;
		VesselResources recycler_resources;
		Part active_part;
		ExWorkshop master;
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
			do {
				var br = part_resources[res_index];
				var old_amount = br.amount;
				deltat = ReclaimResource (br, deltat);
				//Debug.Log (String.Format ("[EL RSM] {0} {1} {2} {3} {4}",
										  //br.name, deltat,
										  //old_amount, br.amount,
										  //old_amount - br.amount));
				did_something = old_amount != br.amount;
				if (br.amount < 1e-6) {
					part_resources.RemoveAt (res_index);
				} else {
					res_index++;
				}
				if (res_index >= part_resources.Count) {
					res_index = 0;
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

		public RecyclerFSM (ExRecycler recycler)
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
			fsm.StartFSM (start_state);
		}

		public void Save (ConfigNode node)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
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

		public void SetMaster (ExWorkshop master)
		{
			this.master = master;
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == recycler.vessel) {
				recycler_resources = new VesselResources (recycler.vessel, recycle_parts);
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

		IEnumerator WaitAndCouple (Part part)
		{
			yield return null;
			part.Couple (recycler.part);
			fsm.RunEvent (event_enter_field);
		}

		public void CollectParts (Part part)
		{
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
			float prod = 0;
			if (master != null) {
				prod = ExWorkshop.HyperCurve (master.vessel_productivity);
			}
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

		void ProcessResource (VesselResources vr, string res, BuildResourceSet rd)
		{
			var amount = vr.ResourceAmount (res);
			var recipe = ExRecipeDatabase.RecycleRecipe (res);

			if (recipe != null) {
				var br = new BuildResource (res, amount);
				recipe = recipe.Bake (br.mass);
				foreach (var ingredient in recipe.ingredients) {
					br = new BuildResource (ingredient);
					rd.Add (br);
				}
			} else {
				if (ExRecipeDatabase.ResourceRecipe (res) != null) {
				} else {
					var br = new BuildResource (res, amount);
					rd.Add (br);
				}
			}
		}

		List<BuildResource> PartResources (Part p)
		{
			var bc = new BuildCost ();
			bc.addPart (p);
			var rd = new BuildResourceSet ();
			VesselResources.ResourceProcessor process = delegate (VesselResources vr, string res) {
				ProcessResource (vr, res, rd);
			};
			bc.resources.Process (process);
			var reslist = rd.Values;
			rd.Clear ();
			bc.container.Process (process);
			reslist.AddRange (rd.Values);
			rd.Clear ();
			bc.hullResoures.Process (process);
			reslist.AddRange (rd.Values);
			return reslist;
		}

		double TransferResource (BuildResource br, double amount)
		{
			var capacity = recycler_resources.ResourceCapacity (br.name);
			if (amount > capacity) {
				amount = capacity;
			}
			br.amount -= amount;
			br.mass = br.amount * br.density;
			return recycler_resources.TransferResource (br.name, amount);
		}

		double ReclaimResource (BuildResource br, double deltat)
		{
			if (br.density > 0) {
				var amount = recycler.RecycleRate * deltat / br.density;
				var base_amount = amount;
				if (amount > br.amount) {
					amount = br.amount;
				}
				amount -= TransferResource (br, amount);
				deltat = deltat * amount / base_amount;
			} else {
				TransferResource (br, br.amount);
				br.amount = 0;
				deltat = 0;
			}
			return deltat;
		}
	}
}
