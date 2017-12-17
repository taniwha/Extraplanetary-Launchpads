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
using System.Reflection;
using System.Linq;
using UnityEngine;
using RUI.Icons.Selectable;

using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {
	[KSPAddon (KSPAddon.Startup.EditorAny, false) ]
	public class ELEditorToolbar : MonoBehaviour
	{
		static Texture texture;
		static Icon icon;
		static string[] elResources = {"MetalOre", "Metal", "RocketParts"};
		static HashSet<string> elItems;

		bool elItemFilter (AvailablePart ap)
		{
			return elItems.Contains (ap.name);
		}

		void onGUIEditorToolbarReady ()
		{
			var cat = PartCategorizer.Instance.filters.Where (c => c.button.categoryName == "Filter by Module").FirstOrDefault ();
			foreach (var subcat in cat.subcategories) {
				if (subcat.button.categoryName.StartsWith ("EL ")) {
					subcat.button.SetIcon (icon);
				}
			}
			cat = PartCategorizer.Instance.filters.Find (c => c.button.categoryName == "Filter by Function");
			PartCategorizer.AddCustomSubcategoryFilter (cat, "EL Items", "EL Items", icon, elItemFilter);
		}

		void Awake ()
		{
			GameEvents.onGUIEditorToolbarReady.Add (onGUIEditorToolbarReady);
			if (texture == null) {
				texture = GameDatabase.Instance.GetTexture ("ExtraplanetaryLaunchpads/Textures/icon_filter", false);
				icon = new Icon ("EL icon", texture, texture, true);
				elItems = new HashSet<string> ();
			}
			elItems.Clear ();
			foreach (AvailablePart ap in PartLoader.LoadedPartsList) {
				if (ap.name.StartsWith ("kerbalEVA") || !ap.partPrefab) {
					continue;
				}
				bool isELItem = false;
				if (ap.partPrefab.Modules != null) {
					foreach (PartModule mod in ap.partPrefab.Modules) {
						if (mod.moduleName != null
							&& mod.moduleName.StartsWith ("EL")) {
							isELItem = true;
							break;
						}
					}
				}
				if (!isELItem && ap.partPrefab.Resources != null) {
					foreach (PartResource res in ap.partPrefab.Resources) {
						if (elResources.Contains (res.resourceName)) {
							isELItem = true;
							break;
						}
					}
				}
				Debug.Log (String.Format ("[EL PF] checking: {0} {1}", ap.name, isELItem));
				if (isELItem) {
					elItems.Add (ap.name);
				}
			}
			elItems.Add ("OMD");
			elItems.Add ("ELMallet");
			elItems.Add ("Magnetometer");
		}

		void OnDestroy ()
		{
			GameEvents.onGUIEditorToolbarReady.Remove (onGUIEditorToolbarReady);
		}
	}
}
