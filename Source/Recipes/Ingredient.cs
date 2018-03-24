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

		public Ingredient (string name, double ratio, double heat = 0, bool discardable = false)
		{
			this.name = name;
			this.ratio = ratio;
			this.heat = heat;
			this.discardable = discardable;
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
			string []values = ingredient.value.Split (new char[]{' '});
			if (values.Length > 0) {
				double.TryParse (values[0], out ratio);
			}
			if (values.Length > 1) {
				double.TryParse (values[1], out heat);
			}
		}
		public bool isReal
		{
			get {
				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				return res_def != null;
			}
		}
	}
}
