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
using System.Reflection;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class Recipe
	{
		public List<Ingredient> ingredients;

		public Recipe ()
		{
			ingredients = new List<Ingredient> ();
		}

		public Recipe (ConfigNode recipe)
		{
			var resdict = new Dictionary<string,Ingredient>();
			foreach (ConfigNode.Value res in recipe.values) {
				var ingredient = new Ingredient (res);
				if (ingredient.ratio <= 0) {
					continue;
				}
				if (resdict.ContainsKey (ingredient.name)) {
					resdict[ingredient.name].ratio += ingredient.ratio;
				} else {
					resdict[ingredient.name] = ingredient;
				}
			}
			ingredients = new List<Ingredient> (resdict.Values);
		}

		public Recipe (string recipe) : this (ConfigNode.Parse (recipe))
		{
		}

		public bool HasIngredient (string ingredient)
		{
			for (int i = 0; i < ingredients.Count; i++) {
				if (ingredients[i].name == ingredient) {
					return true;
				}
			}
			return false;
		}

		public void AddIngredient (Ingredient ingredient)
		{
			for (int i = 0; i < ingredients.Count; i++) {
				if (ingredients[i].name == ingredient.name) {
					ingredients[i].ratio += ingredient.ratio;
					return;
				}
			}
			ingredients.Add (ingredient);
		}
	}
}
