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
	[KSPField (isPersistant = true)]
	public double lastUpdate;
	public double Productivity { get; private set; }

	int updateIndex;
	double updateTimer;

	void BuildNetwork ()
	{
		sources = vessel.FindPartModulesImplementing<ELWorkSource> ();
		sinks = vessel.FindPartModulesImplementing<ELWorkSink> ();

		for (int i = 0; i < sources.Count; i++) {
			var source = sources[i];
			source.UpdateProductivity ();
		}
	}

	void onVesselWasModified (Vessel v)
	{
		if (v == vessel) {
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

	protected override void OnStart ()
	{
		BuildNetwork ();
	}

	public override void OnLoadVessel ()
	{
		updateIndex = 0;
		updateTimer = 0;
	}

	double GetDeltaTime ()
	{
		double delta = -1;
		if (Time.timeSinceLevelLoad >= 1
			&& FlightGlobals.ready
			&& ResourceScenario.Instance != null) {
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

	void FixedUpdate ()
	{
		if (sources.Count < 1) {
			return;
		}

		double timeDelta = GetDeltaTime ();
		if (timeDelta < 1e-9) {
			return;
		}

		bool doUpdate = false;
		if (updateTimer > 0) {
			updateTimer -= Time.deltaTime;
		} else {
			doUpdate = true;
			updateTimer += 10;
		}

		double hours = 0;
		Productivity = 0;
		for (int i = 0; i < sources.Count; i++) {
			var source = sources[i];
			if (doUpdate && i == updateIndex) {
				source.UpdateProductivity ();
			}
			if (source.isActive) {
				double prod = source.Productivity;
				hours += prod * timeDelta / 3600.0;
				Productivity += prod;
			}
		}
		if (doUpdate) {
			if (++updateIndex >= sources.Count) {
				updateIndex = 0;
			}
		}
		//Debug.LogFormat ("[EL Workshop] KerbalHours: {0}", hours);
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
		}
	}
}

}
