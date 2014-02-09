using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

public interface ExWorkSink
{
	void DoWork (double kerbalHourse);
	bool isActive ();
}

public class ExWorkshop : PartModule
{

	[KSPField]
	public float ProductivityFactor = 1.0f;

	[KSPField (guiName = "Productivity", guiActive = true)]
	public float Productivity;

	private ExWorkshop master;
	private List<ExWorkshop> sources;
	private List<ExWorkSink> sinks;

	public override string GetInfo ()
	{
		return "Workshop";
	}

	private static ExWorkshop findFirstWorkshop (Part part)
	{
		var shop = part.Modules.OfType<ExWorkshop> ().FirstOrDefault ();
		if (shop != null) {
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

	private double GetProductivity ()
	{
		return Productivity * TimeWarp.fixedDeltaTime / 3600;
	}

	[KSPEvent (guiActive=false, active = true)]
	void ExDiscoverWorkshops (BaseEventData data)
	{
		// Even the master workshop is its own slave.
		//Debug.Log (String.Format ("[EL Workshop] slave"));
		master = data.Get<ExWorkshop> ("master");
		data.Get<List<ExWorkshop>> ("sources").Add (this);
	}

	private float KerbalContribution (Kerbal kerbal)
	{
		float s = kerbal.stupidity;
		float c = kerbal.courage;
		float contribution;
		
		if (kerbal.isBadass) {
			float a = -2;
			float v = 2 * (1 - s);
			float y = 1 - 2 * s;
			contribution = y + (v + a * c / 2) * c;
		} else {
			contribution = 1 - s * (1 + c * c);
		}
		return contribution;
	}

	private void DetermineProductivity ()
	{
		float kh = 0;
		foreach (var crew in part.protoModuleCrew) {
			kh += KerbalContribution (crew.KerbalRef);
		}
		Productivity = kh * ProductivityFactor;
	}

	void onCrewBoard (GameEvents.FromToAction<Part,Part> ft)
	{
		Part p = ft.to;

		if (p != part)
			return;
		DetermineProductivity ();
	}

	void onCrewEVA (GameEvents.FromToAction<Part,Part> ft)
	{
		Part p = ft.from;

		if (p != part)
			return;
		DetermineProductivity ();
	}

	public override void OnLoad (ConfigNode node)
	{
		if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
			GameEvents.onCrewBoardVessel.Add (onCrewBoard);
			GameEvents.onCrewOnEva.Add (onCrewEVA);
		}
	}

	void OnDestroy ()
	{
		GameEvents.onCrewBoardVessel.Remove (onCrewBoard);
		GameEvents.onCrewOnEva.Remove (onCrewEVA);
	}

	public override void OnStart (PartModule.StartState state)
	{
		if (state == PartModule.StartState.None
			|| state == PartModule.StartState.Editor)
			return;
		DiscoverWorkshops ();
		DetermineProductivity ();
		part.force_activate ();
	}

	public override void OnFixedUpdate ()
	{
		if (this == master) {
			double hours = 0;
			foreach (var source in sources) {
				hours += source.GetProductivity ();
			}
			//Debug.Log (String.Format ("[EL Workshop] KerbalHours: {0}",
			//						  hours));
			int num_sinks = 0;
			foreach (var sink in sinks) {
				if (sink.isActive ()) {
					num_sinks++;
				}
			}
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
