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

namespace ExLP {
	public class ExRecipe
	{
		public class Ingredient
		{
			public string resource;
			public double ratio;

			public Ingredient (string res, double rat)
			{
				resource = res;
				ratio = rat;
			}
		}

		public List<Ingredient> ingredients;

		public ExRecipe (ConfigNode node)
		{
			var resdict = new Dictionary<string,Ingredient>();
			if (node.HasNode ("ExRecipe")) {
				var reslib = PartResourceLibrary.Instance;
				var recipe = node.GetNode ("ExRecipe");
				double rp_ratio = 0;
				foreach (ConfigNode.Value res in recipe.values) {
					string resname = res.name;
					double ratio;
					if (!double.TryParse (res.value, out ratio)) {
						continue;
					}
					var resdef = reslib.GetDefinition(resname);
					if (resdef == null) {
						rp_ratio += ratio;
						continue;
					}
					if (resdict.ContainsKey (resname)) {
						resdict[resname].ratio += ratio;
					} else {
						resdict[resname] = new Ingredient (resname, ratio);
					}
				}
				if (rp_ratio != 0) {
					if (resdict.ContainsKey ("RocketParts")) {
						resdict["RocketParts"].ratio += rp_ratio;
					} else {
						resdict["RocketParts"] = new Ingredient("RocketParts", rp_ratio);
					}
				}
			}
			if (resdict.Count == 0) {
				resdict["RocketParts"] = new Ingredient("RocketParts", 1);
			}
			ingredients = new List<Ingredient> (resdict.Values);
		}
	}
}
