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
	[KSPAddon (KSPAddon.Startup.Instantly, false)]
	public class ExRecipeDatabase: MonoBehaviour
	{
		public static Dictionary<string, PartRecipe> part_recipes;
		public static Dictionary<string, Recipe> module_recipes;
		public static Dictionary<string, Recipe> resource_recipes;
		public static Dictionary<string, Recipe> recycle_recipes;
		public static Recipe default_structure_recipe;

		public static double ResourceDensity (string name)
		{
			PartResourceDefinition res_def;
			res_def = PartResourceLibrary.Instance.GetDefinition (name);
			return res_def.density;
		}

		public static Recipe ResourceRecipe (string name)
		{
			if (resource_recipes.ContainsKey (name)) {
				return resource_recipes[name];
			}
			return null;
		}

		public static Recipe RecycleRecipe (string name)
		{
			if (recycle_recipes.ContainsKey (name)) {
				return recycle_recipes[name];
			}
			return null;
		}

		public static PartRecipe KerbalRecipe ()
		{
			var name = "kerbalEVA";
			if (part_recipes.ContainsKey (name)) {
				return part_recipes[name];
			}
			return null;
		}

		public static void ProcessPart (Part part, Dictionary<string, ResourceInfo> resources)
		{
			string name = part.name;
			if (!part_recipes.ContainsKey (name)) {
				print ("ExRecipeDatabase.ProcessPart: no part recipe for " + name);
				return;
			}
			var recipe = part_recipes[name].Bake (part.mass);
			for (int i = 0; i < recipe.ingredients.Count; i++) {
				var ingredient = recipe.ingredients[i];
				ingredient.ratio /= ResourceDensity (ingredient.name);

				ResourceInfo resourceInfo;
				if (!resources.ContainsKey (ingredient.name)) {
					resourceInfo = new ResourceInfo ();
					resources[ingredient.name] = resourceInfo;
				}
				resourceInfo = resources[ingredient.name];
				resourceInfo.containers.Add (new RecipeResourceContainer (part, ingredient));
			}
		}

		void Awake ()
		{
			part_recipes = new Dictionary<string, PartRecipe> ();
			module_recipes = new Dictionary<string, Recipe> ();
			resource_recipes = new Dictionary<string, Recipe> ();
			recycle_recipes = new Dictionary<string, Recipe> ();
			default_structure_recipe = new Recipe ("RocketParts = 1");

			List<LoadingSystem> list = LoadingScreen.Instance.loaders;
			if (list != null) {
				for (int i = 0; i < list.Count; i++) {
					if (list[i] is ExRecipeLoader) {
						print("[EL Recipes] found ExRecipeLoader: " + i);
						(list[i] as ExRecipeLoader).done = false;
						break;
					}
					if (list[i] is PartLoader) {
						print("[EL Recipes] found PartLoader: " + i);
						GameObject go = new GameObject();
						ExRecipeLoader scanner = go.AddComponent<ExRecipeLoader>();
						list.Insert (i, scanner);
						break;
					}
				}
			}
		}
	}
}
