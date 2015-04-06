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
		public class BuildResource: IComparable<BuildResource>, IConfigNode
		{
			public string name;
			public double amount;
			public double deltaAmount = 0;
			public double density;
			public double mass;
			public bool hull;
			public double kerbalHours;

			public int CompareTo (BuildResource other)
			{
				return name.CompareTo (other.name);
			}

			private static bool isHullResource (PartResourceDefinition res)
			{
				// FIXME need smarter resource "type" handling
				if (res.resourceTransferMode == ResourceTransferMode.NONE
					|| res.resourceFlowMode == ResourceFlowMode.NO_FLOW) {
					return true;
				}
				return false;
			}

			private double KerbalHours ()
			{
				if (density > 0) {
					// 5 Kerbal-hours/ton
					//FIXME per resource
					return 5 * density;
				} else {
					//FIXME per resource
					return 0.125;
				}
			}

			public BuildResource ()
			{
			}

			public BuildResource (string name, float mass)
			{
				this.name = name;
				this.mass = mass;

				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				density = res_def.density;
				if (density > 0) {
					amount = mass / density;
				} else {
					amount = mass;
					mass = 0;
				}
				hull = isHullResource (res_def);
				kerbalHours = KerbalHours ();
			}

			public BuildResource (string name, double amount)
			{
				this.name = name;
				this.amount = amount;
				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				density = res_def.density;
				mass = amount * density;
				hull = isHullResource (res_def);
				kerbalHours = KerbalHours ();
			}

			public void Load (ConfigNode node)
			{
				if (!node.HasValue ("name")) {
					// die?!?
				}
				name = node.GetValue ("name");
				if (node.HasValue ("amount")) {
					double.TryParse (node.GetValue ("amount"), out amount);
				}
				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				density = res_def.density;
				mass = amount * density;
				hull = isHullResource (res_def);
				kerbalHours = KerbalHours ();
			}

			public void Save (ConfigNode node)
			{
				node.AddValue ("name", name);
				// 17 digits is enough to uniquely identify any double
				node.AddValue ("amount", amount.ToString ("G17"));
			}
		}

		public class CostReport : IConfigNode
		{
			public List<BuildResource> required;
			public List<BuildResource> optional;

			public CostReport ()
			{
				required = new List<BuildResource> ();
				optional = new List<BuildResource> ();
			}

			public void Load (ConfigNode node)
			{
				var req = node.GetNode ("Required");
				foreach (var r in req.GetNodes ("BuildResrouce")) {
					var res = new BuildResource ();
					res.Load (r);
					required.Add (res);
				}
				var opt = node.GetNode ("Optional");
				foreach (var r in opt.GetNodes ("BuildResrouce")) {
					var res = new BuildResource ();
					res.Load (r);
					optional.Add (res);
				}
			}

			public void Save (ConfigNode node)
			{
				var req = node.AddNode ("Required");
				foreach (var res in required) {
					var r = req.AddNode ("BuildResrouce");
					res.Save (r);
				}
				var opt = node.AddNode ("Optional");
				foreach (var res in optional) {
					var r = opt.AddNode ("BuildResrouce");
					res.Save (r);
				}
			}
		}

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
