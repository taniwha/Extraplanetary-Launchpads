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
using Experience;

namespace ExtraplanetaryLaunchpads {

using KerbalStats;

public class ELWorkshop : PartModule, IModuleInfo, ELWorkSource
{
	[KSPField]
	public float ProductivityFactor = 1.0f;

	[KSPField]
	public float UnmannedProductivity = 0;

	[KSPField]
	public bool FullyEquipped = false;

	[KSPField]
	public bool IgnoreCrewCapacity = true;

	[KSPField (guiName = "Productivity", guiActive = true)]
	double _Productivity;
	public double Productivity
	{
		get { return _Productivity; }
		private set { _Productivity = value; }
	}

	[KSPField (guiName = "Vessel Productivity", guiActive = true)]
	public double VesselProductivity;

	public bool SupportInexperienced
	{
		get {
			return FullyEquipped || ProductivityFactor >= 1;
		}
	}
	public bool isActive { get; private set; }
	public ELVesselWorkNet workNet { get; set; }
	public bool ready { get { return true; } }
	private bool enableSkilled;
	private bool enableUnskilled;

	public override string GetInfo ()
	{
		return String.Format ("Workshop: productivity factor {0:G2}", ProductivityFactor);
	}

	public string GetPrimaryField ()
	{
		return String.Format ("Productivity Factor: {0:G2}", ProductivityFactor);
	}

	public string GetModuleTitle ()
	{
		return "EL Workshop";
	}

	public Callback<Rect> GetDrawModulePanelCallback ()
	{
		return null;
	}

	private double Normal (float stupidity, float courage, float experience)
	{
		double s = stupidity;
		double c = courage;
		double e = experience;

		double w = e / 3.6e5 * (1-0.8*s);

		return 1 - s * (1 + c * c) + (0.5+s)*(1-Math.Exp(-w));
	}

	private double Baddass (float stupidity, float courage, float experience)
	{
		double s = stupidity;
		double c = courage;
		double e = experience;

		double a = -2;
		double v = 2 * (1 - s);
		double y = 1 - 2 * s;
		double w = e / 3.6e5 * (1-0.8*s);

		return y + (v + a * c / 2) * c + (1+2*s)*(1-Math.Exp(-w));
	}

	public static double HyperCurve (double x)
	{
		return (Math.Sqrt (x * x + 1) + x) / 2;
	}

	private double KerbalContribution (ProtoCrewMember crew)
	{
		string expstr = KerbalExt.Get (crew, "experience:task=Workshop");
		float experience = 0;
		if (expstr != null) {
			float.TryParse (expstr, out experience);
		}

		double contribution;

		if (crew.isBadass) {
			contribution = Baddass (crew.stupidity, crew.courage, experience);
		} else {
			contribution = Normal (crew.stupidity, crew.courage, experience);
		}
		bool hasConstructionSkill = crew.GetEffect<ELConstructionSkill> () != null;
		if (!hasConstructionSkill) {
			if (!enableUnskilled) {
				// can't work here, but may not know to keep out of the way.
				contribution = Math.Min (contribution, 0);
			}
			if (crew.experienceLevel >= 3) {
				// can resist "ooh, what does this button do?"
				contribution = Math.Max (contribution, 0);
			}
		} else {
			switch (crew.experienceLevel) {
			case 0:
				if (!enableSkilled && !SupportInexperienced) {
					// can't work here, but knows to keep out of the way.
					contribution = 0;
				}
				break;
			case 1:
				break;
			case 2:
				if (SupportInexperienced) {
					// He's learned the ropes.
					contribution = HyperCurve (contribution);
				}
				break;
			default:
				// He's learned the ropes very well.
				contribution = HyperCurve (contribution);
				break;
			}
		}
		/*
		Debug.LogFormat ("[EL Workshop] Kerbal: "
						 + "{0} s:{1} c:{2} b:{3} e:{4}({5}) c:{6} hs:{7} l:{8} es:{9} si:{10}",
						 crew.name, crew.stupidity, crew.courage,
						 crew.isBadass, experience, expstr,
						 contribution, hasConstructionSkill,
						 crew.experienceLevel,
						 enableSkilled, SupportInexperienced);
		*/
		return contribution;
	}

	public void UpdateProductivity ()
	{
		double kh = 0;
		enableSkilled = false;
		enableUnskilled = false;
		var crewList = EL_Utils.GetCrewList (part);
		foreach (var crew in crewList) {
			if (crew.GetEffect<ELConstructionSkill> () != null) {
				if (crew.experienceLevel >= 4) {
					enableSkilled = true;
				}
				if (crew.experienceLevel >= 5) {
					enableUnskilled = true;
				}
			}
		}
		foreach (var crew in crewList) {
			kh += KerbalContribution (crew);
		}
		Productivity = kh * ProductivityFactor + UnmannedProductivity;
		//Debug.LogFormat("[ELWorkshop] UpdateProductivity: {0}", Productivity);
	}

	void onCrewTransferred (GameEvents.HostedFromToAction<ProtoCrewMember,Part> hft)
	{
		if (hft.from != part && hft.to != part)
			return;
		//Debug.LogFormat ("[EL Workshop] transfer: {0} {1} {2}",
		//				  hft.host, hft.from, hft.to);
		if (workNet != null) {
			workNet.ForceProductivityUpdate ();
		}
	}

	private IEnumerator WaitAndUpdateProductivity ()
	{
		for (int i = 0; i < 5; i++) {
			yield return null;
		}
		if (workNet != null) {
			workNet.ForceProductivityUpdate ();
		}
	}

	void onPartCouple (GameEvents.FromToAction<Part,Part> hft)
	{
		//Debug.LogFormat ("[EL Workshop] couple: {0} {1}", hft.from, hft.to);
		if (hft.to != part)
			return;
		StartCoroutine (WaitAndUpdateProductivity ());
	}

	void onPartUndock (Part p)
	{
		//Debug.LogFormat ("[EL Workshop] undock: {0} {1}", p, p.parent);
		if (p.parent != part)
			return;
		StartCoroutine (WaitAndUpdateProductivity ());
	}

	public override void OnLoad (ConfigNode node)
	{
		if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
			//Debug.LogFormat ("[EL Workshop] {0} cap: {1} seats: {2}",
			//		  part, part.CrewCapacity,
			//		  part.FindModulesImplementing<KerbalSeat> ().Count);
			bool functional = false;
			if (IgnoreCrewCapacity || part.CrewCapacity > 0) {
				GameEvents.onCrewTransferred.Add (onCrewTransferred);
				functional = true;
			}
			if (part.FindModulesImplementing<KerbalSeat> ().Count > 0) {
				GameEvents.onPartCouple.Add (onPartCouple);
				GameEvents.onPartUndock.Add (onPartUndock);
				functional = true;
			}
			if (!functional) {
				Fields["Productivity"].guiActive = false;
				Fields["VesselProductivity"].guiActive = false;
			}
			isActive = functional;
		}
	}

	public override void OnSave (ConfigNode node)
	{
	}

	void OnDestroy ()
	{
		GameEvents.onCrewTransferred.Remove (onCrewTransferred);
		GameEvents.onPartCouple.Remove (onPartCouple);
		GameEvents.onPartUndock.Remove (onPartUndock);
	}

	public override void OnStart (PartModule.StartState state)
	{
		if (vessel == null) {
			// in an editor
			return;
		}
		workNet = vessel.FindVesselModuleImplementing<ELVesselWorkNet> ();
		StartCoroutine (WaitAndUpdateProductivity ());
	}

	private void Update ()
	{
		if (workNet != null) {
			VesselProductivity = workNet.Productivity;
		}
	}
}

}
