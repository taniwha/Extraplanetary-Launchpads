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

	class ELPartItemView : UIObject
	{
		AvailablePart availablePart;
		Image background;
		Toggle toggle;
		GameObject partIcon;

		public Material []materials;

		public override void CreateUI ()
		{
			background = gameObject.AddComponent<Image> ();
			background.type = UnityEngine.UI.Image.Type.Sliced;

			toggle = gameObject.AddComponent<Toggle> ();
			toggle.targetGraphic = background;

			this.Pivot (PivotPresets.MiddleCenter);
		}

		public override void Style ()
		{
			background.sprite = style.sprite;
			background.color = style.color ?? UnityEngine.Color.white;

			toggle.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
			toggle.transition = style.transition ?? Selectable.Transition.ColorTint;
			if (style.stateSprites.HasValue) {
				toggle.spriteState = style.stateSprites.Value;
			}
		}

		public ELPartItemView Group (ToggleGroup group)
		{
			toggle.group = group;
			return this;
		}

		protected override void OnDestroy ()
		{
			Destroy (partIcon);
		}

		public ELPartItemView OnValueCanged (UnityAction<bool> action)
		{
			toggle.onValueChanged.AddListener (action);
			return this;
		}

		public ELPartItemView AvailablePart (AvailablePart availablePart)
		{
			if (partIcon) {
				Destroy (partIcon);
			}
			this.availablePart = availablePart;
			partIcon = GameObject.Instantiate (availablePart.iconPrefab);
			partIcon.transform.SetParent (rectTransform, false);
			partIcon.transform.localPosition = new Vector3 (0, 0, -39);
			partIcon.transform.localScale = new Vector3 (39, 39, 39);
			var rot = Quaternion.Euler (-15, 0, 0);
			rot = rot * Quaternion.Euler (0, -30, 0);
			partIcon.transform.rotation = rot;
			partIcon.SetActive(true);
			int layer = LayerMask.NameToLayer ("UIAdditional");
			EL_Utils.SetLayer (partIcon, layer, true);

			materials = EL_Utils.CollectMaterials (partIcon);

			if (availablePart.Variants != null
				&& availablePart.Variants.Count > 0) {
				var variant = availablePart.partPrefab.baseVariant;
				ModulePartVariants.ApplyVariant (null, partIcon.transform,
												 variant, materials, true);
			}
			return this;
		}
	}
}
