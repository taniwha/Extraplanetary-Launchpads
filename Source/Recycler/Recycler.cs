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

public class ELRecycler : PartModule, IModuleInfo, IPartMassModifier, ELControlInterface, ELWorkNode
{
	[KSPField] public float RecycleRate = 1.0f;
	[KSPField] public string RecycleField_name = "RecycleField";
	[KSPField (guiName = "State", guiActive = true)] public string status;
	[KSPField] public float EVARange = 1.5f;

	[KSPField (isPersistant = true)]
	public bool Operational = true;

	Collider RecycleField;
	RecyclerFSM sm;

	public bool isBusy
	{
		get {return sm != null && sm.isBusy; }
	}

	public bool canOperate
	{
		get { return Operational; }
		set {
			Operational = value;
			Events["Activate"].active = value;
			Events["Deactivate"].active = false;
			if (sm != null) {
				if (value) {
					sm.Enable ();
				} else {
					sm.Disable ();
				}
			}
		}
	}

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

	public void OnTriggerStay (Collider col)
	{
		if (col.attachedRigidbody == null
			|| !col.CompareTag ("Untagged")
			|| col.gameObject.name == "MapOverlay collider")	// kethane
			return;
		if (!RecycleField.enabled) {
			return;
		}
		Part p = col.attachedRigidbody.GetComponent<Part>();
		//Debug.Log (String.Format ("[EL] OnTriggerStay: {0}", p));
		if (p != null && CanRecycle (p.vessel)) {
			RecycleField.enabled = false;
			sm.CollectParts (p);
		}
	}

	[KSPEvent (guiActive = true, guiName = "Activate Recycler", active = true)]
	public void Activate ()
	{
		Events["Activate"].active = false;
		Events["Deactivate"].active = true;
		status = "Active";
		if (sm != null) {
			sm.Enable ();
		}
	}

	[KSPEvent (guiActive = true, guiName = "Deactivate Recycler",
	 active = false)]
	public void Deactivate ()
	{
		Events["Activate"].active = true;
		Events["Deactivate"].active = false;
		status = "Inactive";
		if (sm != null) {
			sm.Disable ();
		}
	}

	public override void OnStart (StartState state)
	{
		RecycleField = part.FindModelComponent<Collider> (RecycleField_name);
		Debug.Log (String.Format ("[EL Recycler] OnStart: {0}", RecycleField));
		if (EVARange > 0) {
			EL_Utils.SetupEVAEvent (Events["Activate"], EVARange);
			EL_Utils.SetupEVAEvent (Events["Deactivate"], EVARange);
		}
		if (RecycleField != null) {
			RecycleField.enabled = false;
			RecycleField.isTrigger = true;	//FIXME workaround for KSP 1.1 bug
		}
		if (state == PartModule.StartState.None
			|| state == PartModule.StartState.Editor) {
			return;
		}
		sm.Start (RecycleField);
	}

	public override void OnSave (ConfigNode node)
	{
		if (sm != null) {
			sm.Save (node);
		}
	}

	public override void OnLoad (ConfigNode node)
	{
		if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
			sm = new RecyclerFSM (this);
			sm.Load (node);
		}
		Deactivate ();
	}

	public void FixedUpdate ()
	{
		if (sm != null) {
			sm.FixedUpdate ();
			status = sm.CurrentState;
		}
	}

	public float GetModuleMass (float defaultMass, ModifierStagingSituation sit)
	{
		if (sm != null) {
			return (float) sm.ResourceMass;
		}
		return 0;
	}

	public ModifierChangeWhen GetModuleMassChangeWhen ()
	{
		return ModifierChangeWhen.CONSTANTLY;
	}

	public ELVesselWorkNet workNet { get; set; }

	public bool ready { get { return true; } }
}

}
