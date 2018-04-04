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

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class Ingredient
	{
		public string name;
		public double ratio;
		public double heat;
		public bool discardable;

		public Ingredient (Ingredient ingredient)
		{
			name = ingredient.name;
			ratio = ingredient.ratio;
			heat = ingredient.heat;
			discardable = ingredient.discardable;
			isReal = ingredient.isReal;
			Density = ingredient.Density;
			hasMass = ingredient.hasMass;
		}

		public Ingredient (string name, double ratio, double heat = 0, bool discardable = false)
		{
			this.name = name;
			this.ratio = ratio;
			this.heat = heat;
			this.discardable = discardable;
			SetProperties ();
		}

		public Ingredient (ConfigNode.Value ingredient)
		{
			name = ingredient.name;
			discardable = name.EndsWith ("*");
			if (discardable) {
				name = name.Substring (0, name.Length - 1);
			}
			ratio = 0;
			heat = 0;
			string value = ingredient.value;
			var values = ParseExtensions.ParseArray (value, ',', ' ', '\t');
			if (values.Length > 0) {
				double.TryParse (values[0], out ratio);
			}
			if (values.Length > 1) {
				double.TryParse (values[1], out heat);
			}
			SetProperties ();
		}

		void SetProperties ()
		{
			PartResourceDefinition res_def;
			res_def = PartResourceLibrary.Instance.GetDefinition (name);
			if (res_def != null) {
				isReal = true;
				Density = res_def.density;
				hasMass = res_def.density != 0;
			} else {
				isReal = false;
				Density = 1;	// unknown, but need something
				hasMass = true;	// assume undefined resources have mass
			}
		}

		public bool hasMass { get; private set; }
		public bool isReal { get; private set; }
		public double Density { get; private set; }
	}
}
