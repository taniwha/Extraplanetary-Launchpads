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

	class ELPartCategoryView : UIObject, ILayoutElement
	{
		Toggle toggle;
		Image image;
		UIImage icon;

		public ELPartCategory category { get; private set; }

		public override void CreateUI ()
		{
			image = gameObject.AddComponent<Image> ();
			image.type = UnityEngine.UI.Image.Type.Sliced;

			this.PreferredSize (32, 32)
				.Add<UIImage> (out icon)
					.Anchor (AnchorPresets.StretchAll)
					.Pivot (PivotPresets.MiddleCenter)
					.SizeDelta(0, 0)
					.Finish ()
				;

			toggle = gameObject.AddComponent<Toggle> ();
			toggle.onValueChanged.AddListener (onValueChanged);
			toggle.targetGraphic = image;
			toggle.graphic = icon.image;
		}

		public override void Style ()
		{
			image.sprite = style.sprite;
			image.color = style.color ?? UnityEngine.Color.white;

			toggle.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
			toggle.transition = Selectable.Transition.ColorTint;
		}

		void onValueChanged (bool on)
		{
			if (on) {
				Select ();
			}
		}

		public void Select ()
		{
			category.Select ();
		}

		public void SelectCategory ()
		{
			toggle.isOn = true;
		}

		public ELPartCategoryView Group (ToggleGroup group)
		{
			toggle.group = group;
			return this;
		}

		public ELPartCategoryView Category (ELPartCategory category)
		{
			this.category = category;
			image.sprite = category.offIcon;
			style.sprite = image.sprite;
			icon.image.sprite = category.onIcon;
			return this;
		}

#region ILayoutElement
		Vector2 minSize;
		Vector2 preferredSize;

		public void CalculateLayoutInputHorizontal()
		{
			minSize = Vector2.zero;
			float i, s;
			i = image.preferredWidth;
			s = icon.image.preferredWidth;
			preferredSize.x = Mathf.Max (i, s);
		}

		public void CalculateLayoutInputVertical()
		{
			float i, s;
			i = image.preferredHeight;
			s = icon.image.preferredHeight;
			preferredSize.y = Mathf.Max (i, s);
		}

		public int layoutPriority { get { return 0; } }
		public float minWidth { get { return minSize.x; } }
		public float preferredWidth { get { return preferredSize.x; } }
		public float flexibleWidth  { get { return -1; } }
		public float minHeight { get { return minSize.y; } }
		public float preferredHeight { get { return preferredSize.y; } }
		public float flexibleHeight  { get { return -1; } }
#endregion
	}
}
