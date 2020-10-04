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
	public class ELRecipeDatabase: MonoBehaviour
	{
		public static Dictionary<string, ResourceLink> resource_links;
		public static Dictionary<string, double> resource_rates;
		public static double default_resource_rate;
		public static Dictionary<string, ConverterRecipe> converter_recipes;
		public static Dictionary<string, PartRecipe> part_recipes;
		public static Dictionary<string, Recipe> module_recipes;
		public static Dictionary<string, Recipe> resource_recipes;
		public static Dictionary<string, Recipe> recycle_recipes;
		public static Dictionary<string, Recipe> transfer_recipes;
		public static Recipe default_structure_recipe;

		public static double ResourceDensity (string name)
		{
			PartResourceDefinition res_def;
			res_def = PartResourceLibrary.Instance.GetDefinition (name);
			return res_def.density;
		}

		public static double ResourceRate (string name)
		{
			double rate;
			if (!resource_rates.TryGetValue (name, out rate)) {
				rate = default_resource_rate;
			}
			return rate;
		}

		public static ConverterRecipe ConverterRecipe (string name)
		{
			if (converter_recipes.ContainsKey (name)) {
				return converter_recipes[name];
			}
			return null;
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

		public static Recipe TransferRecipe (string name)
		{
			if (transfer_recipes.ContainsKey (name)) {
				return transfer_recipes[name];
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

		static string GetPartName (Part part)
		{
			// Extract the actual part name from the part. Root nodes include
			// the vessel name :P
			string pname = part.name;
			if (pname.Contains (" (")) {
				pname = pname.Substring (0, pname.IndexOf (" ("));
			}
			return pname;
		}

		public static void ProcessPart (Part part, Dictionary<string, RMResourceInfo> resources, double massOffset)
		{
			string name = GetPartName (part);
			if (name.Contains ("kerbalEVA")) {
				// kerbalEVA parts have the name of the kerbal appended to the
				// part name.
				name = "kerbalEVA";
			}
			if (!part_recipes.ContainsKey (name)) {
				print ("ELRecipeDatabase.ProcessPart: no part recipe for " + name);
				return;
			}
			var recipe = part_recipes[name].Bake (part.mass - massOffset);
			for (int i = 0; i < recipe.ingredients.Count; i++) {
				var ingredient = recipe.ingredients[i];
				if (!ingredient.isReal) {
					//print ("fake ingredient: " + ingredient.name);
					continue;
				}
				double density = ResourceDensity (ingredient.name);
				//double ratio = ingredient.ratio;
				if (density > 0) {
					ingredient.ratio /= density;
				}
				//Debug.Log($"ProcessPart: {ingredient.name} {ratio} {density} {ingredient.ratio}");

				RMResourceInfo resourceInfo;
				if (!resources.ContainsKey (ingredient.name)) {
					resourceInfo = new RMResourceInfo ();
					resources[ingredient.name] = resourceInfo;
				}
				resourceInfo = resources[ingredient.name];
				resourceInfo.containers.Add (new RecipeResourceContainer (part, ingredient));
			}
		}

		void Awake ()
		{
			resource_links = new Dictionary<string, ResourceLink> ();
			resource_rates = new Dictionary<string, double> ();
			// default to 5 kH/t (or per unit if 0 density)
			default_resource_rate = 5;
			converter_recipes = new Dictionary<string, ConverterRecipe> ();
			part_recipes = new Dictionary<string, PartRecipe> ();
			module_recipes = new Dictionary<string, Recipe> ();
			resource_recipes = new Dictionary<string, Recipe> ();
			recycle_recipes = new Dictionary<string, Recipe> ();
			transfer_recipes = new Dictionary<string, Recipe> ();
			default_structure_recipe = new Recipe ("RocketParts = 1");

			List<LoadingSystem> list = LoadingScreen.Instance.loaders;
			if (list != null) {
				for (int i = 0; i < list.Count; i++) {
					if (list[i] is ELRecipeLoader) {
						//print("[EL Recipes] found ELRecipeLoader: " + i);
						(list[i] as ELRecipeLoader).done = false;
						break;
					}
					if (list[i] is PartLoader) {
						//print("[EL Recipes] found PartLoader: " + i);
						GameObject go = new GameObject();
						ELRecipeLoader scanner = go.AddComponent<ELRecipeLoader>();
						list.Insert (i, scanner);
						break;
					}
				}
			}
		}
	}
}
