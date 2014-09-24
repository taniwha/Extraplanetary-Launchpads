using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

public interface ExWorkSink
{
	void DoWork (double kerbalHours);
	bool isActive ();
}

public class ExWorkshop : PartModule
{
	[KSPField]
	public float ProductivityFactor = 1.0f;

	[KSPField]
	public bool IgnoreCrewCapacity = true;

	[KSPField (guiName = "Productivity", guiActive = true)]
	public float Productivity;

	[KSPField (guiName = "Vessel Productivity", guiActive = true)]
	public float VesselProductivity;

	private ExWorkshop master;
	private List<ExWorkshop> sources;
	private List<ExWorkSink> sinks;
	private bool functional;
	private float vessel_productivity;

	public override string GetInfo ()
	{
		return "Workshop";
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

	private IEnumerator<YieldInstruction> UpdateNetwork ()
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

	private double GetProductivity ()
	{
		return Productivity * TimeWarp.fixedDeltaTime / 3600;
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

	private float KerbalContribution (string name, float stupidity,
									  float courage, bool isBadass)
	{
		float s = stupidity;
		float c = courage;
		float contribution;
		
		if (isBadass) {
			float a = -2;
			float v = 2 * (1 - s);
			float y = 1 - 2 * s;
			contribution = y + (v + a * c / 2) * c;
		} else {
			contribution = 1 - s * (1 + c * c);
		}
		Debug.Log (String.Format ("[EL Workshop] Kerbal: {0} {1} {2} {3} {4}",
								  name, s, c, isBadass, contribution));
		return contribution;
	}

	private void DetermineProductivity ()
	{
		float kh = 0;
		foreach (var crew in part.protoModuleCrew) {
			kh += KerbalContribution (crew.name, crew.stupidity, crew.courage,
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

	public override void OnLoad (ConfigNode node)
	{
		if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
			if (IgnoreCrewCapacity || part.CrewCapacity > 0) {
				GameEvents.onCrewTransferred.Add (onCrewTransferred);
				GameEvents.onVesselWasModified.Add (onVesselWasModified);
				functional = true;
			} else {
				functional = false;
				Fields["Productivity"].guiActive = false;
				Fields["VesselProductivity"].guiActive = false;
			}
		}
	}

	void OnDestroy ()
	{
		GameEvents.onCrewTransferred.Remove (onCrewTransferred);
		GameEvents.onVesselWasModified.Remove (onVesselWasModified);
	}

	public override void OnStart (PartModule.StartState state)
	{
		if (!functional) {
			enabled = false;
			return;
		}
		if (state == PartModule.StartState.None
			|| state == PartModule.StartState.Editor)
			return;
		DiscoverWorkshops ();
		DetermineProductivity ();
	}

	private void Update ()
	{
		VesselProductivity = master.vessel_productivity;
	}

	public void FixedUpdate ()
	{
		if (this == master) {
			double hours = 0;
			vessel_productivity = 0;
			foreach (var source in sources) {
				hours += source.GetProductivity ();
				vessel_productivity += source.Productivity;
			}
			//Debug.Log (String.Format ("[EL Workshop] KerbalHours: {0}",
			//						  hours));
			int num_sinks = 0;
			foreach (var sink in sinks) {
				if (sink.isActive ()) {
					num_sinks++;
				}
			}
			if (num_sinks > 0) {
				double work = hours / num_sinks;
				foreach (var sink in sinks) {
					if (sink.isActive ()) {
						sink.DoWork (work);
					}
				}
			}
		}
	}
}

}
