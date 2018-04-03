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

		// input mass ratios relative to smallest input mass
		public double[] Masses;

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

		void SortIngredients (List<Ingredient> ingredients)
		{
			for (int i = 0; i < ingredients.Count; i++) {
				for (int j = i + 1; j < ingredients.Count; j++) {
					if (string.Compare(ingredients[i].name, ingredients[j].name) > 0) {
						Ingredient t = ingredients[i];
						ingredients[i] = ingredients[j];
						ingredients[j] = t;
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
						resRecipe.ingredients[0].heat = ingredient.heat;
						recipe.ingredients.RemoveAt (i);
						for (int j = resRecipe.ingredients.Count; j-- > 0; ) {
							recipe.AddIngredient (resRecipe.ingredients[j]);
						}
					}
				}
			}
		}

		void SetupRecipes (Recipe[] recipes, out double[] efficiences,
						   bool expand)
		{
			var resourceSet = new HashSet<string> ();
			efficiences = new double[recipes.Length];
			for (int i = 0; i < recipes.Length; i++) {
				efficiences[i] = recipes[i]["efficiency"].ratio;
				recipes[i].RemoveIngredient ("efficiency");
				if (recipes[i].HasIngredient ("heat")) {
					recipes[i].RemoveIngredient ("heat");
				}
				if (expand) {
					ExpandResourceRecipes (recipes[i]);
				}
				for (int j = recipes[i].ingredients.Count; j-- > 0; ) {
					resourceSet.Add (recipes[i].ingredients[j].name);
				}
			}
			// ensure all recipes have all ingredients and sort ingredients
			for (int i = 0; i < recipes.Length; i++) {
				foreach (string res in resourceSet) {
					// new ingredients will have ratios of 0, already existing
					// ingredients will not be affected
					recipes[i].AddIngredient (new Ingredient (res, 0));
				}
				SortIngredients (recipes[i].ingredients);
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
			SetupRecipes (InputRecipes, out InputEfficiencies, true);
			SetupRecipes (OutputRecipes, out OutputEfficiencies, false);
			Masses = new double[InputRecipes.Length];
			double smallest = double.PositiveInfinity;
			for (int i = 0; i < InputRecipes.Length; i++) {
				Debug.LogFormat ("[ConverterRecipe] {0} input {1}",
								 name, InputEfficiencies[i]);
				double total = InputRecipes[i].Mass ();
				for (int j = 0; j < InputRecipes[i].ingredients.Count; j++) {
					var ing = InputRecipes[i].ingredients[j];
					Debug.LogFormat ("    {0}: {1} {2}", ing.name, ing.ratio,
									 ing.heat);
				}
				if (total > 0 && total < smallest) {
					smallest = total;
				}
				Masses[i] = total;
				Debug.LogFormat("    total: {0}", total);
			}
			for (int i = 0; i < InputRecipes.Length; i++) {
				Masses[i] /= smallest;
			}
			for (int i = 0; i < OutputRecipes.Length; i++) {
				Debug.LogFormat ("[ConverterRecipe] {0} output {1}",
								 name, OutputEfficiencies[i]);
				double total = OutputRecipes[i].Mass ();
				for (int j = 0; j < OutputRecipes[i].ingredients.Count; j++) {
					var ing = OutputRecipes[i].ingredients[j];
					Debug.LogFormat ("    {0}: {1} {2}", ing.name, ing.ratio,
									 ing.heat);
				}
				Debug.LogFormat("    total: {0}", total);
			}
		}

		ConverterRecipe ()
		{
			InputRecipes = new Recipe[1];
			InputRecipes[0] = new Recipe ();
			OutputRecipes = new Recipe[1];
			OutputRecipes[0] = new Recipe ();
			InputEfficiencies = new double[1];
			OutputEfficiencies = new double[1];
			Masses = new double[1];
		}

		void CopyRecipe (Recipe dst, Recipe src)
		{
			for (int i = src.ingredients.Count; i-- > 0; ) {
				var srcIng = src.ingredients[i];
				var dstIng = dst.ingredients[i];
				dstIng.name = srcIng.name;
				dstIng.ratio = srcIng.ratio;
			}
		}

		double Blend (double blend, double a, double b)
		{
			return blend * b + (1 - blend) * a;
		}

		void BlendRecipes (Recipe recipe, double blend, Recipe r1, Recipe r2)
		{
			for (int i = r1.ingredients.Count; i-- > 0; ) {
				Ingredient outi = recipe.ingredients[i];
				Ingredient in1 = r1.ingredients[i];
				Ingredient in2 = r2.ingredients[i];
				outi.name = in1.name;
				outi.ratio = Blend (blend, in1.ratio, in2.ratio);
				outi.heat = Blend (blend, in1.heat, in2.heat);
				outi.discardable = in1.discardable;
			}
		}

		void ProcessInputs (ConverterRecipe recipe,
							 double blend, int ind1, int ind2)
		{
			BlendRecipes (recipe.InputRecipes[0], blend,
						  InputRecipes[ind1], InputRecipes[ind2]);
			recipe.Masses[0] = Blend (blend, Masses[ind1], Masses[ind2]);
		}

		void ProcessOutputs (ConverterRecipe recipe,
							 double blend, int ind1, int ind2)
		{
			BlendRecipes (recipe.OutputRecipes[0], blend,
						  OutputRecipes[ind1], OutputRecipes[ind2]);
		}

		public ConverterRecipe Bake (double efficiency, ConverterRecipe recipe)
		{
			if (recipe == null) {
				recipe = new ConverterRecipe ();

				recipe.InputRecipes[0] = new Recipe (InputRecipes[0]);
				recipe.OutputRecipes[0] = new Recipe (OutputRecipes[0]);
			}
			int maxI = InputRecipes.Length - 1;
			if (efficiency <= InputEfficiencies[0]) {
				ProcessInputs (recipe, 0, 0, 0);
			} else if (efficiency >= InputEfficiencies[maxI]) {
				ProcessInputs (recipe, 1, maxI, maxI);
			} else {
				int i = maxI;
				while (i > 0 && efficiency < InputEfficiencies[--i]) { }
				double gap = InputEfficiencies[i + 1] - InputEfficiencies[i];
				double blend = (efficiency - InputEfficiencies[i]) / gap;
				ProcessInputs (recipe, blend, i, i + 1);
			}

			int maxO = OutputRecipes.Length - 1;
			if (efficiency <= OutputEfficiencies[0]) {
				ProcessOutputs (recipe, 0, 0, 0);
			} else if (efficiency >= OutputEfficiencies[maxO]) {
				ProcessOutputs (recipe, 1, maxO, maxO);
			} else {
				int i = maxO;
				while (i > 0 && efficiency < OutputEfficiencies[--i]) { }
				double gap = OutputEfficiencies[i + 1] - OutputEfficiencies[i];
				double blend = (efficiency - OutputEfficiencies[i]) / gap;
				ProcessOutputs (recipe, blend, i, i + 1);
			}
			return recipe;
		}
	}
}
