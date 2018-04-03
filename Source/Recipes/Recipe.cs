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

		public Recipe (Recipe recipe)
		{
			ingredients = new List<Ingredient> ();
			for (int i = 0; i < recipe.ingredients.Count; i++) {
				ingredients.Add (new Ingredient (recipe.ingredients[i]));
			}
		}

		public Recipe (ConfigNode recipe)
		{
			var resdict = new Dictionary<string,Ingredient>();
			foreach (ConfigNode.Value res in recipe.values) {
				var ingredient = new Ingredient (res);
				if (resdict.ContainsKey (ingredient.name)) {
					var ing = resdict[ingredient.name];
					ing.ratio += ingredient.ratio;
					ing.heat += ingredient.heat;
					ing.discardable |= ingredient.discardable;
				} else {
					resdict[ingredient.name] = ingredient;
				}
			}
			ingredients = new List<Ingredient> (resdict.Values);
		}

		public Recipe (string recipe) : this (ConfigNode.Parse (recipe))
		{
		}

		public Ingredient this[string ingredient]
		{
			get {
				for (int i = 0; i < ingredients.Count; i++) {
					if (ingredients[i].name == ingredient) {
						return ingredients[i];
					}
				}
				return null;
			}
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

		public bool RemoveIngredient (string ingredient)
		{
			for (int i = 0; i < ingredients.Count; i++) {
				if (ingredients[i].name == ingredient) {
					ingredients.RemoveAt (i);
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
					ingredients[i].heat += ingredient.heat;
					ingredients[i].discardable |= ingredient.discardable;
					return;
				}
			}
			ingredients.Add (ingredient);
		}

		public Recipe Bake (double mass)
		{
			double total = 0;
			for (int i = 0; i < ingredients.Count; i++) {
				if (ingredients[i].hasMass) {
					total += ingredients[i].ratio;
				}
			}

			Recipe bake = new Recipe ();
			for (int i = 0; i < ingredients.Count; i++) {
				var name = ingredients[i].name;
				var ratio = mass * ingredients[i].ratio / total;
				var heat = mass * ingredients[i].heat / total;
				var discard = ingredients[i].discardable;
				//Debug.Log(String.Format("Bake: {0} {1} {2} {3} {4} {5}", name, ratio, heat, ingredients[i].ratio, ingredients[i].heat, total));
				var ingredient = new Ingredient (name, ratio, heat, discard);
				bake.ingredients.Add (ingredient);
			}
			return bake;
		}

		public double Mass ()
		{
			double mass = 0;
			for (int i = ingredients.Count; i-- > 0; ) {
				var ingredient = ingredients[i];
				if (ingredient.hasMass) {
					mass += ingredient.ratio;
				}
			}
			return mass;
		}
	}
}
