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
				if (name == "RocketParts") {
					return 5 * density;
				} else {
					return 0.5 * density;
				}
			} else {
				//FIXME per resource
				// this is probably ElectricCharge
				return 1.0 / 3600;	// 1Ks/u
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
}
