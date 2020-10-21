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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;

using KodeUI;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	class ELPartCategory
	{
		public delegate void CategorySelectedCallback (PartCategories category);
		public CategorySelectedCallback CategorySelected;
		public Sprite onIcon { get; private set; }
		public Sprite offIcon { get; private set; }
		public PartCategories category { get; private set; }

		static Sprite []elFilterSprites = new Sprite[2];
		static string []elFilterNames = {
			"icon_filter_n",
			"icon_filter_s"
		};

		static Sprite FindSprite (PartCategories category, bool on)
		{
			int ind = on ? 1 : 0;
			if (elFilterSprites[ind] == null) {
				string path = $"GameData/ExtraplanetaryLaunchpads/Textures/{elFilterNames[ind]}.png";
				var tex = new Texture2D (32, 32, TextureFormat.ARGB32, false);
				bool ok = EL_Utils.LoadImage (ref tex, path);
				elFilterSprites[ind] = EL_Utils.MakeSprite (tex);
				Debug.Log ($"[ELPartCategory] FindSprite {category} {path} {ok}");
			}
			return elFilterSprites[ind];
		}

		public ELPartCategory (PartCategories category)
		{
			Debug.Log ($"[ELPartCategory] {category}");
			this.category = category;
			onIcon = FindSprite (category, true);
			offIcon = FindSprite (category, false);
		}

		public void Select ()
		{
			CategorySelected (category);
		}

#region ELPartCategory.List
		public class List : List<ELPartCategory>, UIKit.IListObject
		{
			ToggleGroup group;
			public Layout Content { get; set; }
			public RectTransform RectTransform
			{
				get { return Content.rectTransform; }
			}

			public void Create (int index)
			{
				Content
					.Add<ELPartCategoryView> ()
						.Group (group)
						.Category (this[index])
						.Finish ()
					;
			}

			public void Update (GameObject obj, int index)
			{
				var view = obj.GetComponent<ELPartCategoryView> ();
				view.Category (this[index]);
			}

			public List (ToggleGroup group)
			{
				this.group = group;
			}

			public void Select (PartCategories category)
			{
				group.SetAllTogglesOff (false);
				for (int i = Content.rectTransform.childCount; i-- > 0; ) {
					var child = Content.rectTransform.GetChild (i);
					var view = child.GetComponent<ELPartCategoryView> ();
					if (view.category.category == category) {
						view.SelectCategory ();
						break;
					}
				}
			}
		}
#endregion
	}
}
