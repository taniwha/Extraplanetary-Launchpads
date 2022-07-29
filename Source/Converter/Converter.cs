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
		double heatFlux;
		double efficiency;
		double prevEfficiency = -1;
		double prevDeltaTime = -1;

		Vector2d[] efficiencyCurve;

		ConversionRecipe ratio_recipe;

		ModuleCoreHeat coreHeat;

		[KSPField]
		public float EVARange = 1.5f;

		[KSPField]
		public string ConverterRecipe = "";

		[KSPField]
		public double Rate;

		[KSPField (guiActive = true, guiName = "Furnace Temp")]
		public string furnaceTemp;
		BaseField furnaceTempField;

		public double HeatFlux { get { return heatFlux; } }

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart (state);
			furnaceTempField = Fields["furnaceTemp"];
			EL_Utils.SetupEVAEvent (Events["StartResourceConverter"], EVARange);
			EL_Utils.SetupEVAEvent (Events["StopResourceConverter"], EVARange);

			coreHeat = part.FindModuleImplementing<ModuleCoreHeat> ();
		}

		void RemoveConflictingNodes (ConfigNode node, string name)
		{
			if (node.HasNode (name)) {
				Debug.LogFormat ("[ELConverter] removing conflicting {0} nodes",
								 name);
				node.RemoveNodes (name);
			}
		}

		Vector2d[] SinglePoint (double y)
		{
			var v = new Vector2d (0, y);
			return new Vector2d[] { v };
		}

		Vector2d[] MulitplePoints (string []values)
		{
			var v = new Vector2d[values.Length];
			for (int i = values.Length; i-- > 0; ) {
				ParseExtensions.TryParseVector2d (values[i], out v[i]);
			}
			return v;
		}

		public override void OnLoad (ConfigNode node)
		{
			var effKeys = node.GetValues("efficiency");
			if (effKeys.Length == 1) {
				double eff;
				double.TryParse (effKeys[0], out eff);
				efficiencyCurve = SinglePoint (eff);
			} else if (effKeys.Length > 1) {
				efficiencyCurve = MulitplePoints (effKeys);
			} else {
				ELConverter prefab = null;
				// loading in-flight?
				if (part.partInfo != null
					&& part.partInfo.partPrefab != null) {
					int index = part.Modules.IndexOf (this);
					PartModule module = part.partInfo.partPrefab.Modules[index];
					prefab = module as ELConverter;
				}
				if (prefab != null) {
					efficiencyCurve = prefab.efficiencyCurve;
				} else {
					efficiencyCurve = SinglePoint (1);
				}
			}
			converter_recipe = ELRecipeDatabase.ConverterRecipe (ConverterRecipe);
			for (int i = 0; i < efficiencyCurve.Length; i++) {
				Debug.LogFormat ("efficiency: {0}", efficiencyCurve[i]);
			}
			if (converter_recipe == null) {
				Debug.LogFormat ("[ELConverter] unknown recipe \"{0}\"",
								 ConverterRecipe);
			} else {
				Debug.LogFormat ("[ELConverter] found recipe \"{0}\"",
								 ConverterRecipe);
				current_recipe = converter_recipe.Bake (0.5, current_recipe);
			}
			// two birds with one stone: make it clear that the config is
			// broken and ensure the stock converter doesn't mess with us
			RemoveConflictingNodes (node, "INPUT_RESOURCE");
			RemoveConflictingNodes (node, "OUTPUT_RESOURCE");
			RemoveConflictingNodes (node, "REQUIRED_RESOURCE");
			base.OnLoad (node);
		}

		void PrintRecipe (StringBuilder sb, Recipe recipe, bool disc = false)
		{
			for (int i = 0, c = recipe.ingredients.Count; i < c; i++) {
				if (EL_Utils.PrintIngredient (sb, recipe.ingredients[i], "kg")
					&& disc && recipe.ingredients[i].discardable) {
					sb.Append("+");
				}
			}
		}

		public override string GetInfo ()
		{
			StringBuilder sb = StringBuilderCache.Acquire ();
			if (current_recipe != null) {
				double mass = current_recipe.Masses[0] * Rate;
				Recipe inputs = current_recipe.InputRecipes[0].Bake (mass);
				Recipe outputs = current_recipe.OutputRecipes[0].Bake (mass);
				double heat = 0;
				for (int i = inputs.ingredients.Count; i-- > 0; ) {
					heat -= inputs.ingredients[i].heat;
				}
				for (int i = outputs.ingredients.Count; i-- > 0; ) {
					heat += outputs.ingredients[i].heat;
				}
				sb.Append (ConverterName);
				if (efficiencyCurve.Length > 1) {
					sb.Append (" at 50% efficiency");
				}

				sb.AppendFormat ("\n\n<color=#bada55>Mass flow: {0:0.00} {1}/{2}</color>", mass, "kg", "s");
				sb.AppendFormat ("\n\n<color=#bada55>Heat flow: {0:0.00} {1}/{2}</color>", heat, "MJ", "s");
				sb.Append ("\n\n<color=#bada55>Inputs:</color>");
				PrintRecipe (sb, inputs);

				sb.Append ("\n<color=#bada55>Outputs:</color>");
				PrintRecipe (sb, outputs, true);
			} else {
				sb.Append ("broken configuration");
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

		public override string GetModuleDisplayName ()
		{
			return GetModuleTitle ();
		}

		public Callback<Rect> GetDrawModulePanelCallback ()
		{
			return null;
		}

		double SetRatios (Recipe recipe, List<ResourceRatio> ratios)
		{
			double heat = 0;
			for (int i = recipe.ingredients.Count, j = ratios.Count; i-- > 0; ) {
				var ingredient = recipe.ingredients[i];
				// non-real ingredients might still have heat associated with them
				heat += ingredient.heat;
				if (!ingredient.isReal) {
					continue;
				}
				j--;
				var r = ratios[j];
				r.ResourceName = ingredient.name;
				if (ingredient.Density > 0) {
					r.Ratio = ingredient.ratio / ingredient.Density;
					r.Ratio /= 1000;	// convert from kg/s to t/s
				} else {
					r.Ratio = ingredient.ratio;
				}
				r.DumpExcess = ingredient.discardable;
				r.FlowMode = ResourceFlowMode.ALL_VESSEL;
				ratios[j] = r;
			}
			return heat;
		}

		int RealIngredients (Recipe recipe)
		{
			int real_ingredients = 0;
			for (int i = recipe.ingredients.Count; i-- > 0; ) {
				if (recipe.ingredients[i].isReal) {
					real_ingredients += 1;
				}
			}
			return real_ingredients;
		}

		protected override void PostProcess (ConverterResults result, double deltaTime)
		{
			if (result.TimeFactor < ResourceUtilities.FLOAT_TOLERANCE) {
				status = result.Status;
			} else {
				if (efficiencyCurve.Length > 1) {
					double eff = efficiency * 100;
					status = eff.ToString("0.00") + "% eff.";
				} else {
					status = "Operating";
				}
			}
			//if (deltaTime < 1000) {
			//	part.thermalInternalFlux += heatFlux * result.TimeFactor;
			//}
		}

		double GetTemperature ()
		{
			if (coreHeat != null) {
				return coreHeat.CoreTemperature;
			}
			return part.temperature;
		}

		double DetermineEfficiency (double temperature)
		{
			//FIXME ensure sorted
			//smooth curves
			if (efficiencyCurve.Length == 0) {
				return 1;
			}
			int max = efficiencyCurve.Length - 1;
			if (max == 0 || temperature <= efficiencyCurve[0].x) {
				return efficiencyCurve[0].y;
			}
			if (temperature >= efficiencyCurve[max].x) {
				return efficiencyCurve[max].y;
			}
			int i = max;
			while (i > 0 && temperature < efficiencyCurve[--i].x) { }
			double x1 = efficiencyCurve[i].x;
			double x2 = efficiencyCurve[i + 1].x;
			double y1 = efficiencyCurve[i].y;
			double y2 = efficiencyCurve[i + 1].y;
			double a = (temperature - x1) / (x2 - x1);
			return y1 * (1 - a) + y2 * a;
		}

		protected override ConversionRecipe PrepareRecipe(double deltatime)
		{
			if (!IsActivated && ratio_recipe != null) {
				return null;
			}
			if (ratio_recipe == null) {
				ratio_recipe = new ConversionRecipe ();
				int real_inputs = RealIngredients (current_recipe.InputRecipes[0]);
				int real_outputs = RealIngredients (current_recipe.OutputRecipes[0]);
				ratio_recipe.Inputs.AddRange (new ResourceRatio[real_inputs]);
				ratio_recipe.Outputs.AddRange (new ResourceRatio[real_outputs]);
			}
			efficiency = DetermineEfficiency (GetTemperature ());
			if (efficiency != prevEfficiency) {
				prevEfficiency = efficiency;
				prevDeltaTime = -1;		//force rebake
				current_recipe = converter_recipe.Bake (efficiency, current_recipe);
			}
			if (deltatime != prevDeltaTime) {
				prevDeltaTime = deltatime;
				double mass = current_recipe.Masses[0] * Rate;
				Recipe inputs = current_recipe.InputRecipes[0].Bake (mass);
				Recipe outputs = current_recipe.OutputRecipes[0].Bake (mass);
				heatFlux = 0;
				// positive input heat consumes heat
				heatFlux -= SetRatios (inputs, ratio_recipe.Inputs);
				// positive output heat generates heat
				heatFlux += SetRatios (outputs, ratio_recipe.Outputs);
				heatFlux *= 1e3;
			}
			return ratio_recipe;
		}

		void Update ()
		{
			if (furnaceTempField != null) {
				furnaceTempField.guiActive = PhysicsGlobals.ThermalDataDisplay;
			}
			furnaceTemp = GetTemperature ().ToString("G4");
		}
	}
}
