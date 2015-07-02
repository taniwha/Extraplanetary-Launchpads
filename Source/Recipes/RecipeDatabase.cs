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

namespace ExLP {
	[KSPAddon (KSPAddon.Startup.Instantly, false)]
	public class ExRecipeDatabase: MonoBehaviour
	{
		public static Dictionary<string, ExRecipe> part_recipes;
		void Awake ()
		{
			part_recipes = new Dictionary<string, ExRecipe> ();
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
