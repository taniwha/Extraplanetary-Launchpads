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

public class ExRecycler : PartModule, IModuleInfo
{
	bool recyclerActive;
	[KSPField] public float RecycleRate = 1.0f;
	[KSPField] public string RecycleField_name = "RecycleField";
	[KSPField (guiName = "State", guiActive = true)] public string status;

	Collider RecycleField;

	VesselResources recycler;
	HashSet<uint> recycle_parts;
	ExWorkshop master;
	List<BuildResource> part_resources;

	public override string GetInfo ()
	{
		return "Recycler:\n" + String.Format ("rate: {0:G2}t/s", RecycleRate);
	}

	public string GetPrimaryField ()
	{
		return String.Format ("Recycling Rate: {0:G2}t/s", RecycleRate);
	}

	public string GetModuleTitle ()
	{
		return "EL Recycler";
	}

	public Callback<Rect> GetDrawModulePanelCallback ()
	{
		return null;
	}

	public bool CanRecycle (Vessel vsl)
	{
		if (vsl == null || vsl == vessel) {
			// avoid oroboro
			return false;
		}
		foreach (Part p in vsl.parts) {
			// Don't try to recycle an asteroid or any vessel attached to
			// an asteroid.
			if (p.Modules.Contains ("ModuleAsteroid")) {
				return false;
			}
		}
		return true;
	}

	void CollectParts (Vessel v)
	{
		recycle_parts.Clear ();
		foreach (var p in v.parts) {
			recycle_parts.Add (p.flightID);
		}
	}

	public void OnTriggerStay (Collider col)
	{
		if (!col.CompareTag ("Untagged")
			|| col.gameObject.name == "MapOverlay collider")	// kethane
			return;
		Part p = col.attachedRigidbody.GetComponent<Part>();
		//Debug.Log (String.Format ("[EL] OnTriggerStay: {0}", p));
		if (p != null && CanRecycle (p.vessel)) {
			CollectParts (p.vessel);
			p.Couple (part);
		}
	}

	[KSPEvent (guiActive = true, guiName = "Activate Recycler", active = true)]
	public void Activate ()
	{
		recyclerActive = true;
		Events["Activate"].active = false;
		Events["Deactivate"].active = true;
		status = "Active";
	}

	[KSPEvent (guiActive = true, guiName = "Deactivate Recycler",
	 active = false)]
	public void Deactivate ()
	{
		recyclerActive = false;
		Events["Activate"].active = true;
		Events["Deactivate"].active = false;
		status = "Inactive";
	}

	bool RecyclerActive ()
	{
		return recyclerActive && recycle_parts.Count == 0;
	}

	public bool isActive ()
	{
		return recyclerActive && recycle_parts.Count != 0;
	}

	double RecycleResources (List<BuildResource> resources, double deltat)
	{
		recycler.TransferResource ("", 0);
		return deltat;
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
		var id = recycle_parts.ToArray ()[random (0, recycle_parts.Count)];
		Part p = vessel[id];
		while (p.children.Count > 0) {
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
		var capacity = recycler.ResourceCapacity (br.name);
		if (amount > capacity) {
			amount = capacity;
		}
		br.amount -= amount;
		br.mass = br.amount * br.density;
		return recycler.TransferResource (br.name, amount);
	}

	public IEnumerator RecycleVessel (double deltat)
	{
		while (true) {
			if (part_resources == null) {
				var p = SelectPart ();
				if (p == null) {
					yield break;
				}
				part_resources = PartResources (p);
				recycle_parts.Remove (p.flightID);
				p.Die ();
			}
			int res_index = 0;
			while (part_resources.Count > 0) {
				var br = part_resources[res_index];
				while (br.amount > 0) {
					if (br.density > 0) {
						var amount = deltat / br.density;
						if (amount > br.amount) {
							amount = br.amount;
						}
						if (TransferResource (br, amount) < amount) {
							break;
						}
					} else {
						TransferResource (br, br.amount);
						br.amount = 0;
					}
					yield return null;
				}
				if (br.amount < 1e-6) {
					part_resources.RemoveAt (res_index);
				}
				res_index++;
				if (res_index >= part_resources.Count) {
					res_index = 0;
				}
			}
			yield return null;
		}
	}

	[KSPEvent (guiActive=false, active = true)]
	void ExDiscoverWorkshops (BaseEventData data)
	{
		// Recyclers are not actual work-sinks, but the master is needed
		// to check the vessel producitivity
		master = data.Get<ExWorkshop> ("master");
	}

	public override void OnStart (StartState state)
	{
		if (CompatibilityChecker.IsWin64 ()) {
			return;
		}
		RecycleField = part.FindModelComponent<Collider> (RecycleField_name);
		Debug.Log (String.Format ("[EL Recycler] OnStart: {0}", RecycleField));
		if (RecycleField != null) {
			RecycleField.enabled = RecyclerActive ();
		}
		if (state == PartModule.StartState.None
			|| state == PartModule.StartState.Editor) {
			return;
		}

		GameEvents.onVesselWasModified.Add (onVesselWasModified);

		recycler = new VesselResources (vessel, recycle_parts);

		if (recycle_parts.Count > 0) {
			StartCoroutine (RecycleVessel (0));
		}
	}

	void OnDestroy ()
	{
		GameEvents.onVesselWasModified.Remove (onVesselWasModified);
	}

	void onVesselWasModified (Vessel v)
	{
		if (v == vessel) {
			recycler = new VesselResources (vessel, recycle_parts);
		}
	}

	public override void OnSave (ConfigNode node)
	{
		if (CompatibilityChecker.IsWin64 ()) {
			return;
		}
		var rp = recycle_parts.Select(s => s.ToString()).ToArray();
		node.AddValue ("recycle_parts", String.Join (" ", rp));
		if (part_resources != null) {
			foreach (var res in part_resources) {
				var pr = node.AddNode ("part_resource");
				res.Save (pr);
			}
		}
	}

	public override void OnLoad (ConfigNode node)
	{
		if (CompatibilityChecker.IsWin64 ()) {
			Events["Activate"].active = false;
			Events["Deactivate"].active = false;
			recyclerActive = false;
			return;
		}
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
		Deactivate ();
	}

	public void Update ()
	{
		if (RecycleField != null) {
			RecycleField.enabled = RecyclerActive ();
		}
	}
}

}
