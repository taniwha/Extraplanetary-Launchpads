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
	public class BuildResource: IEquatable<BuildResource>, IComparable<BuildResource>, IConfigNode
	{
		public string name;
		public double amount;
		public double deltaAmount = 0;
		public double density;
		public double mass;
		public double kerbalHours;

		public override int GetHashCode ()
		{
			return name.GetHashCode ();
		}

		public bool Equals (BuildResource other)
		{
			return name.Equals (other.name);
		}

		public int CompareTo (BuildResource other)
		{
			return name.CompareTo (other.name);
		}

		private double KerbalHours ()
		{
			if (density > 0) {
				return ELRecipeDatabase.ResourceRate (name) * density;
			} else {
				return ELRecipeDatabase.ResourceRate (name) / 3600;
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
			kerbalHours = KerbalHours ();
		}

		public BuildResource (Ingredient ingredient)
		{
			this.name = ingredient.name;
			this.mass = ingredient.ratio;

			PartResourceDefinition res_def;
			res_def = PartResourceLibrary.Instance.GetDefinition (name);
			density = res_def.density;
			if (density > 0) {
				amount = mass / density;
			} else {
				amount = mass;
				mass = 0;
			}
			//Debug.Log($"BuildResource: {name} {ingredient.ratio} {density} {amount} {mass}");
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
			kerbalHours = KerbalHours ();
		}

		public void Save (ConfigNode node)
		{
			node.AddValue ("name", name);
			// 17 digits is enough to uniquely identify any double
			node.AddValue ("amount", amount.ToString ("G17"));
		}

		public void Merge (BuildResource res)
		{
			amount += res.amount;
			mass += res.mass;
		}
	}
}
