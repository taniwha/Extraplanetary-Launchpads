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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELPartPreview : UIObject
	{
		GameObject partIcon;

		public override void CreateUI ()
		{
			this.Pivot (PivotPresets.MiddleCenter);
		}

		public override void Style ()
		{
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();
			Destroy (partIcon);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			Destroy (partIcon);
		}

		public ELPartPreview AvailablePart (AvailablePart availablePart)
		{
			Destroy (partIcon);

			var rect = rectTransform.rect;
			float size = Mathf.Min (rect.width, rect.height) / 2;
			partIcon = GameObject.Instantiate (availablePart.iconPrefab);
			partIcon.transform.SetParent (rectTransform, false);
			partIcon.transform.localPosition = new Vector3 (0, 0, -size);
			partIcon.transform.localScale = Vector3.one * size;
			var rot = Quaternion.Euler (-15, 0, 0);
			rot = rot * Quaternion.Euler (0, -30, 0);
			partIcon.transform.rotation = rot;
			partIcon.SetActive(true);
			int layer = LayerMask.NameToLayer ("UIAdditional");
			EL_Utils.SetLayer (partIcon, layer, true);
			return this;
		}
	}
}
