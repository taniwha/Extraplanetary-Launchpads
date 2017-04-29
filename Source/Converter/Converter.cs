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
	public class ExConverter: BaseConverter, IModuleInfo
	{
		ConversionRecipe ratio_recipe;

		[KSPField]
		public float EVARange = 1.5f;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart (state);
			EL_Utils.SetupEVAEvent (Events["StartResourceConverter"], EVARange);
			EL_Utils.SetupEVAEvent (Events["StopResourceConverter"], EVARange);
		}

		public ConversionRecipe Recipe
		{
			get {
				if (ratio_recipe == null) {
					ratio_recipe = LoadRecipe ();
				}
				return ratio_recipe;
			}
		}

		public override string GetInfo ()
		{
			StringBuilder sb = StringBuilderCache.Acquire ();
			sb.Append (ConverterName);
			if (Recipe.Inputs.Count > 0) {
				sb.Append ("\n\n<color=#bada55>Inputs:</color>");
				for (int i = 0, c = Recipe.Inputs.Count; i < c; i++) {
					EL_Utils.PrintResource (sb, Recipe.Inputs[i]);
				}
			}
			if (Recipe.Outputs.Count > 0) {
				sb.Append ("\n<color=#bada55>Outputs:</color>");
				for (int i = 0, c = Recipe.Outputs.Count; i < c; i++) {
					EL_Utils.PrintResource (sb, Recipe.Outputs[i]);
				}
			}
			if (Recipe.Requirements.Count > 0) {
				sb.Append ("\n<color=#bada55>Requirements:</color>");
				for (int i = 0, c = Recipe.Requirements.Count; i < c; i++) {
					EL_Utils.PrintResource (sb, Recipe.Requirements[i]);
				}
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

		List<ResourceRatio> ProcessRatio (List<ResourceRatio> mass_ratios, double mass = 1)
		{
			var unit_ratios = new List<ResourceRatio> ();
			for (int i = 0; i < mass_ratios.Count; i++) {
				var ratio = new ResourceRatio ();
				var def = PartResourceLibrary.Instance.GetDefinition (mass_ratios[i].ResourceName);
				if (def == null) {
					Debug.LogError (String.Format ("[EL Converter] unknown resource '{0}'", mass_ratios[i].ResourceName));
					continue;
				}
				ratio.ResourceName = mass_ratios[i].ResourceName;
				ratio.Ratio = mass * mass_ratios[i].Ratio;
				if (def.density > 0) {
					ratio.Ratio /= def.density;
				}
				ratio.DumpExcess = mass_ratios[i].DumpExcess;
				ratio.FlowMode = mass_ratios[i].FlowMode;
				unit_ratios.Add (ratio);
			}
			return unit_ratios;
		}

		double GetResourceMass (List<ResourceRatio> ratios)
		{
			double mass = 0;
			for (int i = 0; i < ratios.Count; i++) {
				var def = PartResourceLibrary.Instance.GetDefinition (ratios[i].ResourceName);
				mass += ratios[i].Ratio * def.density;
			}
			return mass;
		}

		ConversionRecipe LoadRecipe ()
		{
			var recipe = new ConversionRecipe ();
			recipe.Inputs.AddRange (ProcessRatio (inputList));
			var inputMass = GetResourceMass (recipe.Inputs);
			recipe.Outputs.AddRange (ProcessRatio (outputList, inputMass));
			recipe.Requirements.AddRange (ProcessRatio (reqList));
			return recipe;
		}

		protected override ConversionRecipe PrepareRecipe(double deltatime)
		{
			UpdateConverterStatus();
			if (!IsActivated) {
				return null;
			}
			return Recipe;
		}
	}
}
