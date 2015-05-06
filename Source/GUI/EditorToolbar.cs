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

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	[KSPAddon (KSPAddon.Startup.EditorAny, false) ]
	public class ExEditorToolbar : MonoBehaviour
	{
		static Texture texture;
		static Icon icon;

		void onGUIEditorToolbarReady ()
		{
			var cat = PartCategorizer.Instance.filters.Where (c => c.button.categoryName == "Filter by Module").FirstOrDefault ();
			foreach (var subcat in cat.subcategories) {
				if (subcat.button.categoryName.StartsWith ("EL ")) {
					subcat.button.SetIcon (icon);
				}
			}
		}

		void Awake ()
		{
			GameEvents.onGUIEditorToolbarReady.Add (onGUIEditorToolbarReady);
			if (texture == null) {
				texture = GameDatabase.Instance.GetTexture ("ExtraplanetaryLaunchpads/Textures/icon_button", false);
				icon = new Icon ("EL icon", texture, texture);
			}
		}

		void OnDestroy ()
		{
			GameEvents.onGUIEditorToolbarReady.Remove (onGUIEditorToolbarReady);
		}
	}
}
