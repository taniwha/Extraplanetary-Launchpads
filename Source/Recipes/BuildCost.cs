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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class BuildCost
	{
		VesselResources resources;
		VesselResources container;
		public double mass;

		public BuildCost ()
		{
			resources = new VesselResources ();
			container = new VesselResources ();
		}

		public void addPart (Part part)
		{
			if (ExSettings.KIS_Present) {
				KIS.KISWrapper.GetResources (part, container.resources);
			}
			resources.AddPart (part);
			mass += part.mass;
		}

		public void removePart (Part part)
		{
			if (ExSettings.KIS_Present) {
				container.RemovePart (part);
			}
			resources.RemovePart (part);
			mass -= part.mass;
		}

		double ProcessResources (VesselResources resources, List<BuildResource> report_resources)
		{
			double hullMass = 0;
			var reslist = resources.resources.Keys.ToList ();
			foreach (string res in reslist) {
				double amount = resources.ResourceAmount (res);
				var br = new BuildResource (res, amount);

				if (br.hull) {
					//FIXME better hull resources check
					hullMass += br.mass;
				} else {
					report_resources.Add (br);
				}
			}
			return hullMass;
		}

		public CostReport cost
		{
			get {
				var report = new CostReport ();
				double hullMass = mass - container.ResourceMass ();
				hullMass += ProcessResources (resources, report.optional);
				hullMass += ProcessResources (container, report.required);
				var parts = new BuildResource ("RocketParts", (float) hullMass);
				report.required.Add (parts);
				return report;
			}
		}
	}
}
