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
	public class PartRecipe
	{
		public Recipe part_recipe;
		public Recipe structure_recipe;

		public PartRecipe (ConfigNode recipe)
		{
			part_recipe = new Recipe (recipe);
			if (!part_recipe.HasIngredient ("structure")) {
				part_recipe.AddIngredient (new Ingredient ("structure", 5));
			}
			if (recipe.HasNode ("Resources")) {
				structure_recipe = new Recipe (recipe.GetNode ("Resources"));
			} else {
				structure_recipe = ELRecipeDatabase.default_structure_recipe;
			}
		}

		public PartRecipe ()
		{
			part_recipe = new Recipe ();
			part_recipe.AddIngredient (new Ingredient ("structure", 5));
			structure_recipe = ELRecipeDatabase.default_structure_recipe;
		}

		public Recipe Bake (double mass)
		{
			var recipe = new Recipe ();
			var prec = part_recipe.Bake (mass);
			for (int i = 0; i < prec.ingredients.Count; i++) {
				var pi = prec.ingredients[i];
				Recipe rec;
				if (pi.name == "structure") {
					rec = structure_recipe;
				} else {
					rec = ELRecipeDatabase.module_recipes[pi.name];
				}
				var subr = rec.Bake (pi.ratio);
				for (int j = 0; j < subr.ingredients.Count; j++) {
					var si = subr.ingredients[j];
					recipe.AddIngredient (subr.ingredients[j]);
				}
			}
			return recipe;
		}
	}
}
