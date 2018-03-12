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
	public class ConverterRecipe
	{
		public string name;
		// input and output recipes form curve keys based on efficiency
		// indexed by key (from efficiency)
		public Recipe[] InputRecipes;
		public Recipe[] OutputRecipes;

		// each value is the key efficiency for the range
		public double[] InputEfficiencies;
		public double[] OutputEfficiencies;

		public double[] InputHeats;
		public double[] OutputHeats;

		Recipe[] LoadRecipes (ConfigNode []nodes, string type)
		{
			Recipe[] recipes = new Recipe [nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				recipes[i] = new Recipe (nodes[i]);
				if (!recipes[i].HasIngredient ("efficiency")) {
					Debug.LogWarning (String.Format ("[ConverterRecipe] {0}: {1}:{2} recipe has no efficiency, assuming 1", name, type, i));
					recipes[i].AddIngredient (new Ingredient("efficiency", 1.0));
				}
			}
			return recipes;
		}

		void SortRecipes (Recipe[] recipes)
		{
			for (int i = 0; i < recipes.Length; i++) {
				for (int j = i + 1; j < recipes.Length; j++) {
					if (recipes[i]["efficiency"].ratio > recipes[j]["efficiency"].ratio) {
						Recipe t = recipes[i];
						recipes[i] = recipes[j];
						recipes[j] = t;
					}
				}
			}
		}

		static bool ResourceDefined (string resource)
		{
			var resdef = PartResourceLibrary.Instance.GetDefinition (resource);
			return resdef != null;
		}

		void ExpandResourceRecipes (Recipe recipe)
		{
			for (int i = recipe.ingredients.Count; i-- > 0; ) {
				Ingredient ingredient = recipe.ingredients[i];
				if (!ResourceDefined (ingredient.name)) {
					var resRecipe = ELRecipeDatabase.ResourceRecipe (ingredient.name);
					if (resRecipe != null) {
						resRecipe = resRecipe.Bake (ingredient.ratio);
						recipe.ingredients.RemoveAt (i);
						for (int j = resRecipe.ingredients.Count; j-- > 0; ) {
							recipe.AddIngredient (resRecipe.ingredients[j]);
						}
					}
				}
			}
		}

		void SetupRecipes (Recipe[] recipes, out double[] efficiences,
						   out double[] heats, bool expand)
		{
			var resourceSet = new HashSet<string> ();
			efficiences = new double[recipes.Length];
			heats = new double[recipes.Length];
			for (int i = 0; i < recipes.Length; i++) {
				efficiences[i] = recipes[i]["efficiency"].ratio;
				recipes[i].RemoveIngredient ("efficiency");
				if (recipes[i].HasIngredient ("heat")) {
					heats[i] = recipes[i]["heat"].ratio;
					recipes[i].RemoveIngredient ("heat");
				}
				if (expand) {
					ExpandResourceRecipes (recipes[i]);
				}
				for (int j = recipes[i].ingredients.Count; j-- > 0; ) {
					resourceSet.Add (recipes[i].ingredients[j].name);
				}
			}
			// ensure all recipes have all ingredients
			for (int i = 0; i < recipes.Length; i++) {
				foreach (string res in resourceSet) {
					// new ingredients will have ratios of 0, already existing
					// ingredients will not be affected
					recipes[i].AddIngredient (new Ingredient (res, 0));
				}
			}
		}

		public ConverterRecipe (ConfigNode recipe)
		{
			name = recipe.GetValue ("name");
			ConfigNode []inputs = recipe.GetNodes("Input");
			ConfigNode []outputs = recipe.GetNodes("Output");
			if (inputs.Length < 1) {
				Debug.LogError (String.Format("[ConverterRecipe] {0}: no input recipes", name));
			}
			if (outputs.Length < 1) {
				Debug.LogError (String.Format("[ConverterRecipe] {0}: no output recipes", name));
			}
			if (inputs.Length < 1 || outputs.Length < 1) {
				Debug.LogError (String.Format("[ConverterRecipe] {0}: dropping unusable recipe", name));
				return;
			}
			InputRecipes = LoadRecipes (inputs, "input");
			OutputRecipes = LoadRecipes (outputs, "output");
			SortRecipes (InputRecipes);
			SortRecipes (OutputRecipes);
			SetupRecipes (InputRecipes, out InputEfficiencies,
						  out InputHeats, true);
			SetupRecipes (OutputRecipes, out OutputEfficiencies,
						  out OutputHeats, false);
			for (int i = 0; i < InputRecipes.Length; i++) {
				Debug.LogFormat ("[ConverterRecipe] {0} input {1} {2}",
								 name, InputEfficiencies[i], InputHeats[i]);
				for (int j = 0; j < InputRecipes[i].ingredients.Count; j++) {
					var ing = InputRecipes[i].ingredients[j];
					Debug.LogFormat ("    {0}: {1}", ing.name, ing.ratio);
				}
			}
			for (int i = 0; i < OutputRecipes.Length; i++) {
				Debug.LogFormat ("[ConverterRecipe] {0} output {1} {2}",
								 name, OutputEfficiencies[i], OutputHeats[i]);
				for (int j = 0; j < OutputRecipes[i].ingredients.Count; j++) {
					var ing = OutputRecipes[i].ingredients[j];
					Debug.LogFormat ("    {0}: {1}", ing.name, ing.ratio);
				}
			}
		}
	}
}
