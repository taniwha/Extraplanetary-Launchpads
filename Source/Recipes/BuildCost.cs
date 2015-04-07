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
		public double mass;

		public BuildCost ()
		{
			resources = new VesselResources ();
		}

		public void addPart (Part part)
		{
			resources.AddPart (part);
			mass += part.mass;
		}

		public void removePart (Part part)
		{
			resources.RemovePart (part);
			mass -= part.mass;
		}

		public CostReport cost
		{
			get {
				var report = new CostReport ();
				double hullMass = mass;
				var reslist = resources.resources.Keys.ToList ();
				foreach (string res in reslist) {
					double amount = resources.ResourceAmount (res);
					var br = new BuildResource (res, amount);

					if (br.hull) {
						//FIXME better hull resources check
						hullMass += br.mass;
					} else {
						report.optional.Add (br);
					}
				}
				var parts = new BuildResource ("RocketParts", (float) hullMass);
				report.required.Add (parts);
				return report;
			}
		}
	}
}
