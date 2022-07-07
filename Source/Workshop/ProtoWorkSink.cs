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
	public ELVesselWorkNet workNet { get; set; }
	public bool ready { get { return true; } }
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

}
