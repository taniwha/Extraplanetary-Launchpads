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
using UnityEngine;

using KSP.IO;
using Experience;

namespace ExtraplanetaryLaunchpads {

public class ELProtoWorkSink : ELWorkSink
{
	double workedHours;
	double maxHours;

	public void DoWork (double kerbalHours)
	{
		if (kerbalHours > 0) {
			if (kerbalHours > maxHours - workedHours) {
				kerbalHours = maxHours - workedHours;
				isActive = false;
			}
		} else {
			if (kerbalHours < -maxHours - workedHours) {
				kerbalHours = -maxHours - workedHours;
				isActive = false;
			}
		}
		//Debug.LogFormat ("[ELProtoWorkSink] DoWork: {0} {1} {2}", kerbalHours, maxHours, workedHours);
		workedHours += kerbalHours;
	}

	public void OnLoadVessel ()
	{
		if (workedHours != 0) {
			isActive = true;
		}
	}

	public bool isActive { get; private set; }
	public double CalculateWork ()
	{
		return maxHours;
	}

	public ELProtoWorkSink (ConfigNode node)
	{
		workedHours = 0;
		maxHours = 0;
		isActive = false;

		string s;
		if (node.HasValue ("workedHours")) {
			s = node.GetValue ("workedHours");
			double.TryParse (s, out workedHours);
		}
		if (node.HasValue ("maxHours")) {
			s = node.GetValue ("maxHours");
			double.TryParse (s, out maxHours);
		}
		if (node.HasValue ("isActive")) {
			s = node.GetValue ("isActive");
			bool active;
			bool.TryParse (s, out active);
			isActive = active;
		}
	}

	public void Save (ConfigNode node)
	{
		ConfigNode n = node.AddNode ("WorkSink");
		n.AddValue ("workedHours", workedHours);
		n.AddValue ("maxHours", maxHours);
		n.AddValue ("isActive", isActive);
	}

	public ELProtoWorkSink (ELWorkSink sink)
	{
		workedHours = 0;
		maxHours = 0;
		if (isActive = sink.isActive) {
			maxHours = sink.CalculateWork ();
		}
	}

	public void CatchUpBacklog (ELWorkSink sink, double hours)
	{
		if (hours > 0) {
			if (hours > workedHours) {
				hours = workedHours;
				isActive = false;
			}
		} else {
			// vessel productivity is negative
			if (hours < workedHours) {
				hours = workedHours;
				isActive = false;
			}
		}
		workedHours -= hours;
		sink.DoWork (hours);
	}
}

public class ELVesselWorkNet : VesselModule
{
	List<ELWorkSource> sources;
	List<ELWorkSink> sinks;
	List<ELProtoWorkSink> protoSinks;

	[KSPField (isPersistant = true)]
	public double lastUpdate;
	public double Productivity { get; private set; }

	int updateIndex;
	double updateTimer;
	bool haveWork;
	bool started;
	bool partModulesStarted;

	void BuildNetwork ()
	{
		Productivity = 0;
		if (vessel.loaded) {
			sources = vessel.FindPartModulesImplementing<ELWorkSource> ();
			sinks = vessel.FindPartModulesImplementing<ELWorkSink> ();

			UpdateProductivity (true);
		}
		//Debug.LogFormat ("[ELVesselWorkNet] productivity {0}:{1}", vessel.vesselName, Productivity);
	}

	public override bool ShouldBeActive ()
	{
		bool active = base.ShouldBeActive ();
		if (!vessel.loaded) {
			active &= (!started || (haveWork && Productivity != 0));
		}
		return active;
	}

	protected override void OnLoad (ConfigNode node)
	{
		if (node.HasValue ("Productivity")) {
			string s = node.GetValue ("Productivity");
			double p;
			double.TryParse (s, out p);
			Productivity = p;
		}
		sinks = new List<ELWorkSink> ();
		protoSinks = new List<ELProtoWorkSink> ();
		for (int i = 0; i < node.nodes.Count; i++) {
			ConfigNode n = node.nodes[i];
			if (n.name == "WorkSink") {
				var sink = new ELProtoWorkSink (n);
				protoSinks.Add (sink);
				sinks.Add (sink);
			}
		}
		// ensure the worknet runs at least once when the vessel is not loaded
		haveWork = true;
	}

	protected override void OnSave (ConfigNode node)
	{
		//Debug.LogFormat ("[ELVesselWorkNet] OnSave {0}", vessel.vesselName);
		node.AddValue ("Productivity", Productivity);
		if (vessel.loaded) {
			for (int i = 0; i < sinks.Count; i++) {
				var ps = new ELProtoWorkSink (sinks[i]);
				ps.Save (node);
			}
		} else {
			if (protoSinks != null) {
				for (int i = 0; i < protoSinks.Count; i++) {
					protoSinks[i].Save (node);
				}
			}
		}
	}

	void onVesselWasModified (Vessel v)
	{
		if (v == vessel) {
			//Debug.LogFormat ("[ELVesselWorkNet] onVesselWasModified {0}", vessel.vesselName);
			BuildNetwork ();
		}
	}

	protected override void OnAwake ()
	{
		GameEvents.onVesselWasModified.Add (onVesselWasModified);
	}

	void OnDestroy ()
	{
		GameEvents.onVesselWasModified.Remove (onVesselWasModified);
	}

	bool ValidVesselType (VesselType type)
	{
		if (type > VesselType.Base) {
			// EVA and Flag
			return false;
		}
		if (type == VesselType.SpaceObject
			|| type == VesselType.Unknown) {
			// asteroids
			return false;
		}
		// Debris, Probe, Relay, Rover, Lander, Ship, Plane, Station, Base
		return true;
	}

	protected override void OnStart ()
	{
		started = true;
		//Debug.LogFormat ("[ELVesselWorkNet] OnStart {0}", vessel.vesselName);
		if (!ValidVesselType (vessel.vesselType)) {
			//Debug.LogFormat ("[ELVesselWorkNet] OnStart removing: {0}", vessel.vesselType);
			vessel.vesselModules.Remove (this);
			Destroy (this);
			return;
		}
	}

	public override void OnLoadVessel ()
	{
		updateIndex = 0;
		updateTimer = 0;
		//Debug.LogFormat ("[ELVesselWorkNet] OnLoadVessel {0}", vessel.vesselName);
		BuildNetwork ();
		// part modules start after vessel modules, so make FixedUpdate wait
		// a frame to give the works sinks a chance to initialize
		partModulesStarted = false;

		if (protoSinks != null) {
			for (int i = 0; i < protoSinks.Count; i++) {
				protoSinks[i].OnLoadVessel ();
			}
		}
	}

	public override void OnUnloadVessel ()
	{
		//Debug.LogFormat ("[ELVesselWorkNet] OnUnloadVessel {0}", vessel.vesselName);
		protoSinks = new List<ELProtoWorkSink> ();
		for (int i = 0; i < sinks.Count; i++) {
			var ps = new ELProtoWorkSink (sinks[i]);
			protoSinks.Add (ps);
			sinks[i] = ps;
		}
	}

	void UpdateProductivity (bool forceUpdate)
	{
		Productivity = 0;
		for (int i = 0; i < sources.Count; i++) {
			var source = sources[i];
			if (forceUpdate || i == updateIndex) {
				source.UpdateProductivity ();
			}
			if (source.isActive) {
				double prod = source.Productivity;
				Productivity += prod;
			}
		}
		if (!forceUpdate) {
			if (++updateIndex >= sources.Count) {
				updateIndex = 0;
			}
		}
	}

	bool CatchUpBacklog ()
	{
		//if (Time.timeSinceLevelLoad < 1
		//	|| ResourceScenario.Instance == null) {
		if (ResourceScenario.Instance == null) {
			return true;
		}

		double currentTime = Planetarium.GetUniversalTime ();
		double delta = currentTime - lastUpdate;
		delta = Math.Min (delta, ResourceUtilities.GetMaxDeltaTime ());
		lastUpdate += delta;

		int num_sinks = 0;
		for (int i = 0; i < protoSinks.Count; i++) {
			if (protoSinks[i].isActive) {
				num_sinks++;
			}
		}
		double hours = Productivity * delta / 3600.0;
		if (num_sinks > 0) {
			double work = hours / num_sinks;
			for (int i = 0; i < protoSinks.Count; i++) {
				var ps = protoSinks[i];
				if (ps.isActive) {
					ps.CatchUpBacklog (sinks[i], work);
				}
			}
			return true;
		} else {
			protoSinks = null;
			return false;
		}
	}

	void FixedUpdate ()
	{
		if (sinks == null) {
			// worknet hasn't been created yet
			return;
		}

		double timeDelta = TimeWarp.fixedDeltaTime;

		if (vessel.loaded) {
			if (!partModulesStarted) {
				partModulesStarted = true;
				return;
			}
			if (updateTimer > 0) {
				updateTimer -= timeDelta;
			} else {
				updateTimer = 10;
				UpdateProductivity (false);
			}
			if (protoSinks != null) {
				if (CatchUpBacklog ()) {
					return;
				}
			}
			lastUpdate = Planetarium.GetUniversalTime ();
		}

		double hours = Productivity * timeDelta / 3600.0;
		//Debug.LogFormat ("[ELVesselWorkNet] KerbalHours: {0} {1}", vessel.vesselName, hours);
		int num_sinks = 0;
		for (int i = 0; i < sinks.Count; i++) {
			var sink = sinks[i];
			if (sink.isActive) {
				num_sinks++;
			}
		}
		//Debug.LogFormat ("[EL Workshop] num_sinks: {0}", num_sinks);
		if (num_sinks > 0) {
			double work = hours / num_sinks;
			for (int i = 0; i < sinks.Count; i++) {
				var sink = sinks[i];
				if (sink.isActive) {
					sink.DoWork (work);
				}
			}
		} else {
			//Debug.LogFormat ("[EL Workshop] loaded: {0}", vessel.loaded);
			if (!vessel.loaded) {
				// run out of work to do, so shut down until the vessel is next
				// loaded
				haveWork = false;
			}
		}
	}
}

}
