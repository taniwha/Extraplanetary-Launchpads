using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

public class Recycler : PartModule
{
	double busyTime;
	bool recyclerActive;
	[KSPField] public float RecycleRate = 1.0f;
	[KSPField (guiName = "State", guiActive = true)] public string status;

	public void OnTriggerStay(Collider col)
	{
		if (!recyclerActive
			|| Planetarium.GetUniversalTime() <= busyTime
			|| !col.CompareTag("Untagged")
			|| col.gameObject.name == "MapOverlay collider")	// kethane
			return;
		Part p = col.attachedRigidbody.GetComponent<Part>();
		Debug.Log(String.Format("[EL] {0}", p));
		if (p != null && p.vessel != null && p.vessel != vessel) {
			float mass;
			if (p.vessel.isEVA) {
				mass = RecycleKerbal(p.vessel);
			} else {
				mass = RecycleVessel(p.vessel);
			}
			busyTime = Planetarium.GetUniversalTime() + mass / RecycleRate;
		}
	}

	private float ReclaimResource(string resource, double amount,
								  string vessel_name, string name=null)
	{
		PartResourceDefinition res_def;
		res_def = PartResourceLibrary.Instance.GetDefinition(resource);
		VesselResources recycler = new VesselResources(vessel);

		if (res_def == null) {
			return 0;
		}

		if (name == null) {
			name = resource;
		}
		double remain = amount;
		// any resources that can't be pumped or don't flow just "evaporate"
		// FIXME: should this be a little smarter and convert certain such
		// resources into rocket parts?
		if (res_def.resourceTransferMode != ResourceTransferMode.NONE
			&& res_def.resourceFlowMode != ResourceFlowMode.NO_FLOW) {
			remain = recycler.TransferResource(resource, amount);
		}
		Debug.Log(String.Format("[EL] {0}-{1}: {2} taken {3} reclaimed, {4} lost", vessel_name, name, amount, amount - remain, remain));
		return (float) (amount * res_def.density);
	}

	public float RecycleKerbal(Vessel v)
	{
		if (!v.isEVA)
			return 0;

		// idea and numbers taken from Kethane
		if (v.GetVesselCrew()[0].isBadass) {
			v.rootPart.explosionPotential = 10000;
		}
		FlightGlobals.ForceSetActiveVessel(this.vessel);
		v.rootPart.explode();

		float mass = 0;
		mass += ReclaimResource("Kethane", 150, v.name);
		mass += ReclaimResource("Metal", 1, v.name);
		return mass;
	}

	public float RecycleVessel(Vessel v)
	{
		float ConversionEfficiency = 0.8f;
		double amount;
		VesselResources scrap = new VesselResources(v);

		PartResourceDefinition rp_def;
		rp_def = PartResourceLibrary.Instance.GetDefinition("RocketParts");

		float mass = 0;
		foreach (string resource in scrap.resources.Keys) {
			amount = scrap.ResourceAmount (resource);
			mass += ReclaimResource(resource, amount, v.name);
		}
		float hull_mass = v.GetTotalMass();
		amount = hull_mass * ConversionEfficiency / rp_def.density;
		mass += ReclaimResource("RocketParts", amount, v.name, "hull");
		v.Die();
		return mass;
	}

	[KSPEvent(guiActive = true, guiName = "Activate Recycler", active = true)]
	public void Activate()
	{
		recyclerActive = true;
		Events["Activate"].active = false;
		Events["Deactivate"].active = true;
	}

	[KSPEvent(guiActive = true, guiName = "Deactivate Recycler",
	 active = false)]
	public void Deactivate()
	{
		recyclerActive = false;
		Events["Activate"].active = true;
		Events["Deactivate"].active = false;
	}

	public override void OnLoad(ConfigNode node)
	{
		Deactivate();
	}

	public override void OnUpdate()
	{
		if (Planetarium.GetUniversalTime() <= busyTime) {
			status = "Busy";
		} else if (recyclerActive) {
			status = "Active";
		} else {
			status = "Inactive";
		}
	}
}

}
