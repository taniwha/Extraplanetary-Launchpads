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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ToggleText : Layout
	{
		MiniToggle toggle;
		UIText label;

		public override void CreateUI()
		{
			var toggleMin = new Vector2 (0, 0.25f);
			var toggleMax = new Vector2 (1, 0.75f);
			var textMargins = new Vector4 (5, 5, 10, 10);
			this.Horizontal ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.SizeDelta(0, 0)
				.Add<MiniToggle> (out toggle)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<UIText> (out label)
					.Alignment (TextAlignmentOptions.Left)
					.Margin (textMargins)
					.SizeDelta(0, 0)
					.Finish ()
				;
		}

		public ToggleText Group (ToggleGroup group)
		{
			toggle.Group (group);
			return this;
		}

		public ToggleText Text (string text)
		{
			label.Text (text);
			return this;
		}

		public ToggleText OnValueChanged (UnityAction<bool> action)
		{
			toggle.OnValueChanged (action);
			return this;
		}
	}
}
