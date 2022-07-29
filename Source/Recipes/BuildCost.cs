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

namespace ExtraplanetaryLaunchpads {
	public class BuildCost
	{
		public RMResourceSet resources;
		public RMResourceSet container;
		public RMResourceSet hullResoures;
		public double mass;

		public BuildCost ()
		{
			resources = new RMResourceSet ();
			container = new RMResourceSet ();
			hullResoures = new RMResourceSet ();
		}

		public void addPart (Part part)
		{
			double resMass = 0;
			// Stock container mass includes resources in contained parts
			resMass = StockResourceContainer.GetResources (part, container.resources);
			if (ELSettings.KIS_Present) {
				// KIS container mass includes resources in contained parts
				resMass += KIS.KISWrapper.GetResources (part, container.resources);
			}
			ELRecipeDatabase.ProcessPart (part, hullResoures.resources, resMass);
			resources.AddPart (part);
			mass += part.mass - resMass;
		}

		public void removePart (Part part)
		{
			container.RemovePart (part);
			hullResoures.RemovePart (part);
			resources.RemovePart (part);
			mass -= part.mass;
		}

		void ProcessResources (RMResourceSet resources, BuildResourceSet report_resources, BuildResourceSet required_resources = null)
		{
			var reslist = resources.resources.Keys.ToList ();
			foreach (string res in reslist) {
				double amount = resources.ResourceAmount (res);
				var recipe = ELRecipeDatabase.ResourceRecipe (res);

				if (recipe != null) {
					double density = ELRecipeDatabase.ResourceDensity (res);
					double mass = amount * density;
					recipe = recipe.Bake (mass);
					foreach (var ingredient in recipe.ingredients) {
						var br = new BuildResource (ingredient);
						var resset = report_resources;
						if (required_resources != null)  {
							resset = required_resources;
						}
						resset.Add (br);
					}
				} else {
					var br = new BuildResource (res, amount);
					report_resources.Add (br);
				}
			}
		}

		public CostReport cost
		{
			get {
				var required = new BuildResourceSet ();
				var optional = new BuildResourceSet ();
				ProcessResources (resources, optional, required);
				ProcessResources (container, required);
				ProcessResources (hullResoures, required);
				var report = new CostReport (required, optional);
				return report;
			}
		}
	}
}
