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

public interface ExWorkSink
{
	void DoWork (double kerbalHours);
	bool isActive ();
}

public class ExWorkshop : PartModule, IModuleInfo
{
	[KSPField]
	public float ProductivityFactor = 1.0f;

	[KSPField]
	public bool FullyEquipped = false;

	[KSPField]
	public bool IgnoreCrewCapacity = true;

	[KSPField (guiName = "Productivity", guiActive = true)]
	public float Productivity;

	[KSPField (guiName = "Vessel Productivity", guiActive = true)]
	public float VesselProductivity;

	public double lastUpdate = 0.0;

	public bool SupportInexperienced
	{
		get {
			return FullyEquipped || ProductivityFactor >= 1;
		}
	}
	private bool workshop_started;
	private ExWorkshop master;
	private List<ExWorkshop> sources;
	private List<ExWorkSink> sinks;
	private bool functional;
	public float vessel_productivity
	{
		get;
		private set;
	}
	private bool enableSkilled;
	private bool enableUnskilled;
	private bool useSkill;

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

	private static ExWorkshop findFirstWorkshop (Part part)
	{
		var shop = part.Modules.OfType<ExWorkshop> ().FirstOrDefault ();
		if (shop != null && shop.functional) {
			return shop;
		}
		foreach (Part p in part.children) {
			shop = findFirstWorkshop (p);
			if (shop != null) {
				return shop;
			}
		}
		return null;
	}

	private void DiscoverWorkshops ()
	{
		ExWorkshop shop = findFirstWorkshop (vessel.rootPart);
		if (shop == this) {
			//Debug.Log (String.Format ("[EL Workshop] master"));
			var data = new BaseEventData (BaseEventData.Sender.USER);
			data.Set<ExWorkshop> ("master", this);
			sources = new List<ExWorkshop> ();
			sinks = new List<ExWorkSink> ();
			data.Set<List<ExWorkshop>> ("sources", sources);
			data.Set<List<ExWorkSink>> ("sinks", sinks);
			vessel.rootPart.SendEvent ("ExDiscoverWorkshops", data);
		} else {
			sources = null;
			sinks = null;
		}
	}

	private IEnumerator UpdateNetwork ()
	{
		yield return null;
		DiscoverWorkshops ();
	}

	private void onVesselWasModified (Vessel v)
	{
		if (v == vessel) {
			StartCoroutine (UpdateNetwork ());
		}
	}

	private double GetProductivity (double timeDelta)
	{
		double prod = Productivity * timeDelta / 3600.0;
		//Debug.log ("GetProductivity: lastupdate = " + lastUpdate.ToString ("F3") + ", currentTime = " + currentTime.ToString ("F3") + ", --> " + prod.ToString ());
		return prod;
	}

	[KSPEvent (guiActive=false, active = true)]
	void ExDiscoverWorkshops (BaseEventData data)
	{
		if (!functional) {
			return;
		}
		// Even the master workshop is its own slave.
		//Debug.Log (String.Format ("[EL Workshop] slave"));
		master = data.Get<ExWorkshop> ("master");
		data.Get<List<ExWorkshop>> ("sources").Add (this);
	}

	private float Normal (float stupidity, float courage, float experience)
	{
		float s = stupidity;
		float c = courage;
		float e = experience;

		float w = e / 3.6e5f * (1-0.8f*s);

		return 1 - s * (1 + c * c) + (0.5f+s)*(1-Mathf.Exp(-w));
	}

	private float Baddass (float stupidity, float courage, float experience)
	{
		float s = stupidity;
		float c = courage;
		float e = experience;

		float a = -2;
		float v = 2 * (1 - s);
		float y = 1 - 2 * s;
		float w = e / 3.6e5f * (1-0.8f*s);

		return y + (v + a * c / 2) * c + (1+2*s)*(1-Mathf.Exp(-w));
	}

	public static float HyperCurve (float x)
	{
		return (Mathf.Sqrt (x * x + 1) + x) / 2;
	}

	private float KerbalContribution (ProtoCrewMember crew, float stupidity,
									  float courage, bool isBadass)
	{
		string expstr = KerbalExt.Get (crew, "experience:task=Workshop");
		float experience = 0;
		if (expstr != null) {
			float.TryParse (expstr, out experience);
		}

		float contribution;

		if (isBadass) {
			contribution = Baddass (stupidity, courage, experience);
		} else {
			contribution = Normal (stupidity, courage, experience);
		}
		if (useSkill) {
			if (!EL_Utils.HasSkill<ExConstructionSkill> (crew)) {
				if (!enableUnskilled) {
					// can't work here, but may not know to keep out of the way.
					contribution = Mathf.Min (contribution, 0);
				}
				if (crew.experienceLevel >= 3) {
					// can resist "ooh, what does this button do?"
					contribution = Mathf.Max (contribution, 0);
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
		}
		Debug.Log (String.Format ("[EL Workshop] Kerbal: "
								  + "{0} {1} {2} {3} {4}({5}) {6} {7} {8} {9} {10}",
								  crew.name, stupidity, courage, isBadass,
								  experience, expstr, contribution,
								  EL_Utils.HasSkill<ExConstructionSkill> (crew),
								  crew.experienceLevel,
								  enableSkilled, SupportInexperienced));
		return contribution;
	}

	private void DetermineProductivity ()
	{
		float kh = 0;
		enableSkilled = false;
		enableUnskilled = false;
		var crewList = EL_Utils.GetCrewList (part);
		if (useSkill) {
			foreach (var crew in crewList) {
				if (EL_Utils.HasSkill<ExConstructionSkill> (crew)) {
					if (crew.experienceLevel >= 4) {
						enableSkilled = true;
					}
					if (crew.experienceLevel >= 5) {
						enableUnskilled = true;
					}
				}
			}
		}
		foreach (var crew in crewList) {
			kh += KerbalContribution (crew, crew.stupidity, crew.courage,
									  crew.isBadass);
		}
		Productivity = kh * ProductivityFactor;
	}

	void onCrewTransferred (GameEvents.HostedFromToAction<ProtoCrewMember,Part> hft)
	{
		if (hft.from != part && hft.to != part)
			return;
		Debug.Log (String.Format ("[EL Workshop] transfer: {0} {1} {2}",
								  hft.host, hft.from, hft.to));
		DetermineProductivity ();
	}

	private IEnumerator WaitAndDetermineProductivity ()
	{
		yield return null;
		DetermineProductivity ();
	}

	void onPartCouple (GameEvents.FromToAction<Part,Part> hft)
	{
		Debug.Log (String.Format ("[EL Workshop] couple: {0} {1}",
								  hft.from, hft.to));
		if (hft.to != part)
			return;
		StartCoroutine (WaitAndDetermineProductivity ());
	}

	void onPartUndock (Part p)
	{
		Debug.Log (String.Format ("[EL Workshop] undock: {0} {1}",
								  p, p.parent));
		if (p.parent != part)
			return;
		StartCoroutine (WaitAndDetermineProductivity ());
	}

	public override void OnLoad (ConfigNode node)
	{
		if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
			Debug.Log (String.Format ("[EL Workshop] {0} cap: {1} seats: {2}",
					  part, part.CrewCapacity,
					  part.FindModulesImplementing<KerbalSeat> ().Count));
			if (IgnoreCrewCapacity || part.CrewCapacity > 0) {
				GameEvents.onCrewTransferred.Add (onCrewTransferred);
				GameEvents.onVesselWasModified.Add (onVesselWasModified);
				functional = true;
			} else if (part.FindModulesImplementing<KerbalSeat> ().Count > 0) {
				GameEvents.onPartCouple.Add (onPartCouple);
				GameEvents.onPartUndock.Add (onPartUndock);
				GameEvents.onVesselWasModified.Add (onVesselWasModified);
				functional = true;
			} else {
				functional = false;
				Fields["Productivity"].guiActive = false;
				Fields["VesselProductivity"].guiActive = false;
			}
		}
		if (node.HasValue ("lastUpdateString")) {
			double.TryParse (node.GetValue ("lastUpdateString"), out lastUpdate);
		}
	}

	public override void OnSave (ConfigNode node)
	{
		node.AddValue ("lastUpdateString", lastUpdate.ToString ("G17"));
	}

	void OnDestroy ()
	{
		GameEvents.onCrewTransferred.Remove (onCrewTransferred);
		GameEvents.onPartCouple.Remove (onPartCouple);
		GameEvents.onPartUndock.Remove (onPartUndock);
		GameEvents.onVesselWasModified.Remove (onVesselWasModified);
	}

	private IEnumerator WaitAndStartWorkshop ()
	{
		yield return null;
		yield return null;
		workshop_started = true;
	}

	public override void OnStart (PartModule.StartState state)
	{
		workshop_started = false;
		if (!functional) {
			enabled = false;
			return;
		}
		if (state == PartModule.StartState.None
			|| state == PartModule.StartState.Editor)
			return;
		DiscoverWorkshops ();
		useSkill = true;
		StartCoroutine (WaitAndDetermineProductivity ());
		StartCoroutine (WaitAndStartWorkshop ());
	}

	private void Update ()
	{
		if (master != null) {
			VesselProductivity = master.vessel_productivity;
		}
	}

	double GetDeltaTime ()
	{
		double delta = -1;
		if (Time.timeSinceLevelLoad >= 1 && FlightGlobals.ready) {
			if (lastUpdate < 1e-9) {
				lastUpdate = Planetarium.GetUniversalTime ();
			} else {
				var currentTime = Planetarium.GetUniversalTime ();
				delta = currentTime - lastUpdate;
				delta = Math.Min (delta, ResourceUtilities.GetMaxDeltaTime ());
				lastUpdate += delta;
			}
		}
		return delta;
	}

	public void FixedUpdate ()
	{
		if (!workshop_started) {
			return;
		}
		double timeDelta = GetDeltaTime ();
		if (timeDelta < 1e-9) {
			return;
		}
		if (this == master) {
			double hours = 0;
			vessel_productivity = 0;
			for (int i = 0; i < sources.Count; i++) {
				var source = sources[i];
				hours += source.GetProductivity (timeDelta);
				vessel_productivity += source.Productivity;
			}
			//Debug.Log (String.Format ("[EL Workshop] KerbalHours: {0}",
			//						  hours));
			int num_sinks = 0;
			for (int i = 0; i < sinks.Count; i++) {
				var sink = sinks[i];
				if (sink.isActive ()) {
					num_sinks++;
				}
			}
			if (num_sinks > 0) {
				double work = hours / num_sinks;
				for (int i = 0; i < sinks.Count; i++) {
					var sink = sinks[i];
					if (sink.isActive ()) {
						sink.DoWork (work);
					}
				}
			}
		}
	}
}

}
