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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class ELRecipeLoader: LoadingSystem
	{
		public bool done;

		ConfigNode LoadRecipeNode (string node_name)
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes (node_name);
			return node_list.LastOrDefault ();
		}

		void LoadDefautStructureRecipe ()
		{
			var node = LoadRecipeNode ("EL_DefaultStructureRecipe");
			if (node != null) {
				var recipe = new Recipe (node);
				if (recipe.ingredients.Count > 0) {
					ELRecipeDatabase.default_structure_recipe = recipe;
				}
			}
		}

		void LoadKerbalRecipe ()
		{
			var node = LoadRecipeNode ("EL_KerbalRecipe");
			if (node != null) {
				var recipe = new PartRecipe (node);
				ELRecipeDatabase.part_recipes["kerbalEVA"] = recipe;
			}
		}

		ConfigNode GetResources (ConfigNode node, string recipe_name)
		{
			var resources = node.GetNode ("Resources");
			if (resources == null) {
				//print ("[EL Recipes] skipping " + recipe_name + " "
				//	   + name + " with no Resources");
			}
			return resources;
		}

		IEnumerator LoadResourceLinks ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_ResourceLink");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_ResourceLink");
					continue;
				}

				var link = new ResourceLink (node);
				//print ("[EL ResourceRecipe] " + name);
				ELRecipeDatabase.resource_links[name] = link;
				yield return null;
			}
		}

		IEnumerator LoadResourceRates ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_ResourceRates");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				for (int j = 0; j < node.values.Count; j++) {
					var val = node.values[j];
					string name = val.name;
					double rate;
					if (name != "default"
						&& PartResourceLibrary.Instance.GetDefinition (name) == null) {
						print ("warning: unknown resource: " + name);
					}
					if (double.TryParse (val.value, out rate)
						&& rate > 0) {
						print("EL_ResourceRates: " + name + " " + rate);
						if (name == "default") {
							ELRecipeDatabase.default_resource_rate = rate;
						} else {
							ELRecipeDatabase.resource_rates[name] = rate;
						}
					}
				}
				yield return null;
			}
		}

		IEnumerator LoadResourceRecipes ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_ResourceRecipe");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_ResourceRecipe");
					continue;
				}

				var recipe_node = GetResources (node, "EL_ResourceRecipe");
				if (recipe_node == null) {
					continue;
				}
				var recipe = new Recipe (recipe_node);
				//print ("[EL ResourceRecipe] " + name);
				ELRecipeDatabase.resource_recipes[name] = recipe;
				yield return null;
			}
		}

		IEnumerator LoadRecycleRecipes ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_RecycleRecipe");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_RecycleRecipe");
					continue;
				}

				var recipe_node = GetResources (node, "EL_RecycleRecipe");
				if (recipe_node == null) {
					continue;
				}
				var recipe = new Recipe (recipe_node);
				//print ("[EL RecycleRecipe] " + name);
				ELRecipeDatabase.recycle_recipes[name] = recipe;
				yield return null;
			}
		}

		IEnumerator LoadTransferRecipes ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_TransferRecipe");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_TransferRecipe");
					continue;
				}

				var recipe_node = GetResources (node, "EL_TransferRecipe");
				if (recipe_node == null) {
					continue;
				}
				var recipe = new Recipe (recipe_node);
				//print ("[EL TransferRecipe] " + name);
				ELRecipeDatabase.transfer_recipes[name] = recipe;
				yield return null;
			}
		}

		IEnumerator LoadModuleRecipes ()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_ModuleRecipe");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_ModuleRecipe");
					continue;
				}

				Type mod;
				mod = AssemblyLoader.GetClassByName(typeof(PartModule), name);
				if (mod == null) {
					print ("[EL ModuleRecipe] no such module: " + name);
					continue;
				}
				var recipe_node = GetResources (node, "EL_ModuleRecipe");
				if (recipe_node == null) {
					continue;
				}
				//print ("[EL ModuleRecipe] " + name);
				var recipe = new Recipe (recipe_node);
				ELRecipeDatabase.module_recipes[name] = recipe;
				yield return null;
			}
		}

		IEnumerator LoadPartRecipes()
		{
			//print ("[EL Recipes] LoadPartRecipes");
			var dbase = GameDatabase.Instance;
			var configurls = dbase.GetConfigs("PART");
			var module_recipes = ELRecipeDatabase.module_recipes;
			foreach (var c in configurls) {
				var node = c.config;
				string name = node.GetValue("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed PART");
					continue;
				}
				name = name.Replace('_', '.');
				//print("[EL Recipes] " + name);
				if (node.HasNode ("EL_Recipe")) {
					var recipe_node = node.GetNode ("EL_Recipe");
					//print($"[EL Recipes] {name} {recipe_node}");
					var recipe = new PartRecipe (recipe_node);
					ELRecipeDatabase.part_recipes[name] = recipe;
				} else {
					var recipe = new PartRecipe ();
					var modules = node.GetNodes ("MODULE");
					for (int i = 0; i < modules.Length; i++) {
						var mod_name = modules[i].GetValue ("name");
						if (String.IsNullOrEmpty (mod_name)) {
							print ("[EL Recipes] skipping unnamed MODULE");
							continue;
						}
						if (module_recipes.ContainsKey (mod_name)) {
							//print ("[EL Recipes] adding module " + mod_name);
							var mod_ingredient = new Ingredient (mod_name, 1);
							recipe.part_recipe.AddIngredient (mod_ingredient);
						}
					}
					ELRecipeDatabase.part_recipes[name] = recipe;
				}
				yield return null;
			}
		}

		IEnumerator LoadConverterRecipes()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("EL_ConverterRecipe");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					print ("[EL Recipes] skipping unnamed EL_ConverterRecipe");
					continue;
				}
				print ("[EL ConverterRecipe] " + name);
				var recipe = new ConverterRecipe (node);
				ELRecipeDatabase.converter_recipes[name] = recipe;
				yield return null;
			}
		}

		IEnumerator LoadRecipes()
		{
			LoadDefautStructureRecipe ();
			LoadKerbalRecipe ();
			yield return StartCoroutine (LoadResourceLinks ());
			yield return StartCoroutine (LoadResourceRates ());
			yield return StartCoroutine (LoadResourceRecipes ());
			yield return StartCoroutine (LoadRecycleRecipes ());
			yield return StartCoroutine (LoadTransferRecipes ());
			yield return StartCoroutine (LoadModuleRecipes ());
			yield return StartCoroutine (LoadPartRecipes ());
			yield return StartCoroutine (LoadConverterRecipes ());
			done = true;
		}

		public override bool IsReady ()
		{
			return done;
		}

		public override float ProgressFraction ()
		{
			return 0;
		}

		public override string ProgressTitle ()
		{
			return  "Extraplanetary Launchpads Recipes";
		}

		public override void StartLoad()
		{
			done = false;
			StartCoroutine (LoadRecipes ());
		}
	}
}
