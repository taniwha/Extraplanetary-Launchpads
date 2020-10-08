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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	using OptionData = TMP_Dropdown.OptionData;

	public class ELPadLaunchpadView : LayoutPanel
	{
		ELLaunchpad launchpad;

		UIButton leftButton;
		UIButton rightButton;
		UIText rotationDisplay;

		static string []rotationLabels = {"12:00", "09:00", "06:00", "03:00"};

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.StretchAll)
				.SizeDelta (0, 0)
				.Sprite(SpriteLoader.GetSprite("KodeUI/Default/background"))
				.Add<UIButton> (out leftButton)//, "RotateLeft") FIXME
					.Text ("<-")
					.OnClick (RotateLeft)
					.FlexibleLayout (false, true)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<UIText> (out rotationDisplay, "Rotation")
					.Text ("12:00")
					.Alignment (TextAlignmentOptions.Center)
					.FlexibleLayout (true, true)
					.Finish ()
				.Add<UIButton> (out rightButton)//, "RotateRight") FIXME
					.Text ("->")
					.OnClick (RotateRight)
					.FlexibleLayout (false, true)
					.SizeDelta (0, 0)
					.Finish ();
		}

		void UpdateRotationDisplay ()
		{
			rotationDisplay.Text (rotationLabels[launchpad.rotationIndex]);
		}

		void RotateLeft ()
		{
			launchpad.RotateLeft ();
			UpdateRotationDisplay ();
		}

		void RotateRight ()
		{
			launchpad.RotateRight ();
			UpdateRotationDisplay ();
		}

		public override void Style ()
		{
		}

		public void SetControl (ELBuildControl control)
		{
			launchpad = control?.builder as ELLaunchpad;
			if (launchpad != null) {
				SetActive (true);
				UpdateRotationDisplay ();
			} else {
				SetActive (false);
			}
		}

		protected override void OnEnable ()
		{
			base.OnEnable ();
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
		}
	}
}
