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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class ELConverter: BaseConverter, IModuleInfo
	{
		ConverterRecipe converter_recipe;
		ConverterRecipe current_recipe;

		ConversionRecipe ratio_recipe;

		[KSPField]
		public float EVARange = 1.5f;

		[KSPField]
		public string ConverterRecipe = "";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart (state);
			EL_Utils.SetupEVAEvent (Events["StartResourceConverter"], EVARange);
			EL_Utils.SetupEVAEvent (Events["StopResourceConverter"], EVARange);
		}

		void RemoveConflictingNodes (ConfigNode node, string name)
		{
			if (node.HasNode (name)) {
				Debug.LogFormat ("[ELConverter] removing conflicting {0} nodes",
								 name);
				node.RemoveNodes (name);
			}
		}

		public override void OnLoad (ConfigNode node)
		{
			converter_recipe = ELRecipeDatabase.ConverterRecipe (ConverterRecipe);
			if (converter_recipe == null) {
				Debug.LogFormat ("[ELConverter] unknown recipe \"{0}\"",
								 ConverterRecipe);
			} else {
				Debug.LogFormat ("[ELConverter] found recipe \"{0}\"",
								 ConverterRecipe);
				current_recipe = converter_recipe.Bake (0.5, current_recipe);
			}
			PrepareRecipe (0);
			// two birds with one stone: make it clear that the config is
			// broken and ensure the stock converter doesn't mess with us
			RemoveConflictingNodes (node, "INPUT_RESOURCE");
			RemoveConflictingNodes (node, "OUTPUT_RESOURCE");
			RemoveConflictingNodes (node, "REQUIRED_RESOURCE");
			base.OnLoad (node);
		}

		void PrintRecipe (StringBuilder sb, Recipe recipe, bool []disc = null)
		{
			for (int i = 0, c = recipe.ingredients.Count; i < c; i++) {
				if (EL_Utils.PrintIngredient (sb, recipe.ingredients[i])
					&& disc != null && disc[i]) {
					sb.Append("+");
				}
			}
		}

		public override string GetInfo ()
		{
			StringBuilder sb = StringBuilderCache.Acquire ();
			sb.Append (ConverterName);
			sb.Append (" at 50% efficiency");

			if (current_recipe != null) {
				sb.AppendFormat ("\n\n<color=#bada55>Mass flow: {0:0.00} {1}/{2}</color>", current_recipe.Masses[0], "t", "s");
				sb.AppendFormat ("\n\n<color=#bada55>Heat flow: {0:0.00} {1}/{2}</color>", current_recipe.OutputHeats[0] - current_recipe.InputHeats[0], "MJ", "s");
				sb.Append ("\n\n<color=#bada55>Inputs:</color>");
				PrintRecipe (sb, current_recipe.InputRecipes[0]);

				sb.Append ("\n<color=#bada55>Outputs:</color>");
				PrintRecipe (sb, current_recipe.OutputRecipes[0],
							 current_recipe.Discardable);
			}
			return sb.ToStringAndRelease ();
		}

		public string GetPrimaryField ()
		{
			return null;
		}

		public string GetModuleTitle ()
		{
			return "EL Converter";
		}

		public Callback<Rect> GetDrawModulePanelCallback ()
		{
			return null;
		}

		void SetRatios (Recipe recipe, List<ResourceRatio> ratios)
		{
			if (ratios.Count < 1) {
				ratios.AddRange (new ResourceRatio[recipe.ingredients.Count]);
			}
			for (int i = recipe.ingredients.Count; i-- > 0; ) {
				var ingredient = recipe.ingredients[i];
				var r = ratios[i];
				r.ResourceName = ingredient.name;
				r.Ratio = ingredient.ratio;
				r.DumpExcess = false; //FIXME
			}
			for (int i = recipe.ingredients.Count; i-- > 0; ) {
				Debug.LogFormat("[ELConverter] {0}", ratios[i].ResourceName);
			}
		}

		protected override ConversionRecipe PrepareRecipe(double deltatime)
		{
			if (!IsActivated) {
				return null;
			}
			if (ratio_recipe == null) {
				ratio_recipe = new ConversionRecipe ();
			}
			SetRatios (current_recipe.InputRecipes[0], ratio_recipe.Inputs);
			SetRatios (current_recipe.OutputRecipes[0], ratio_recipe.Outputs);
			return ratio_recipe;
		}
	}
}
