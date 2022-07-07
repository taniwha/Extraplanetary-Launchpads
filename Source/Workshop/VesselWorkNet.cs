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

public class ELVesselWorkNet : VesselModule
{
	List<ELWorkSource> sources;
	List<ELWorkSink> sinks;
	List<ELProtoWorkSink> protoSinks;

	[KSPField (isPersistant = true)]
	public double lastUpdate;
	public double Productivity { get; private set; }

	[KSPField (isPersistant = true)]
	public uint selectedPad;

	public void ForceProductivityUpdate()
	{
		forceProductivityUpdate = true;
	}

	int updateIndex;
	double updateTimer;
	bool haveWork;
	bool started;
	bool forceProductivityUpdate;
	int waitForPartModules;

	void BuildNetwork ()
	{
		Productivity = 0;
		if (vessel.loaded) {
			var nodes = vessel.FindPartModulesImplementing<ELWorkNode> ();
			sources.Clear();
			sinks.Clear();

			for (int i = nodes.Count; i-- > 0; ) {
				var node = nodes[i];
				node.workNet = this;
				if (node is ELWorkSink sink) {
					sinks.Add (sink);
				}
				if (node is ELWorkSource source) {
					sources.Add (source);
				}
			}

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
		sinks.Clear ();
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
		sources = new List<ELWorkSource> ();
		sinks = new List<ELWorkSink> ();
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
		// a few frames to give the work sinks a chance to initialize
		waitForPartModules = 3;

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
		if (ResourceScenario.Instance == null) {
			return true;
		}
		for (int i = 0; i < protoSinks.Count; i++) {
			var ps = protoSinks[i];
			if (ps.isActive) {
				if (!sinks[i].ready) {
					return true;
				}
			}
		}

		double currentTime = Planetarium.GetUniversalTime ();
		double delta = currentTime - lastUpdate;
		//Debug.Log ($"[ELVesselWorkNet] CatchUpBacklog: {delta}");
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
			if (waitForPartModules > 0) {
				--waitForPartModules;
				return;
			}
			if (!forceProductivityUpdate && updateTimer > 0) {
				updateTimer -= timeDelta;
			} else {
				updateTimer = 10;
				UpdateProductivity (forceProductivityUpdate);
				forceProductivityUpdate = false;
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
		//Debug.LogFormat ("[ELVesselWorkNet] num_sinks: {0}", num_sinks);
		if (num_sinks > 0) {
			double work = hours / num_sinks;
			for (int i = 0; i < sinks.Count; i++) {
				var sink = sinks[i];
				if (sink.isActive) {
					sink.DoWork (work);
				}
			}
		} else {
			//Debug.LogFormat ("[ELVesselWorkNet] loaded: {0}", vessel.loaded);
			if (!vessel.loaded) {
				// run out of work to do, so shut down until the vessel is next
				// loaded
				haveWork = false;
			}
		}
	}
}

}
