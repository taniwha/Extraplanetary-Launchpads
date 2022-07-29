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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class MiniToggle : Layout, IPointerClickHandler
	{
		UIToggle toggle;

		public bool isOn
		{
			get { return toggle.isOn; }
			set { toggle.isOn = value; }
		}

		public bool interactable
		{
			get { return toggle.interactable; }
			set { toggle.interactable = value; }
		}

		public override void CreateUI()
		{
			var toggleMin = new Vector2 (0, 0.25f);
			var toggleMax = new Vector2 (1, 0.75f);
			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.ChildAlignment (TextAnchor.UpperCenter)
				.Add<LayoutAnchor> ()
					.DoPreferredWidth (true)
					.FlexibleLayout (false, true)
					.MinSize (15, -1)
					.Add<UIToggle> (out toggle)
						.Anchor(toggleMin, toggleMax)
						.AspectRatioSizeFitter (AspectRatioFitter.AspectMode.WidthControlsHeight, 1)
						.Finish ()
					.Finish ()
				;
		}

		public MiniToggle Group (ToggleGroup group)
		{
			toggle.Group (group);
			return this;
		}

		public MiniToggle OnValueChanged (UnityAction<bool> action)
		{
			toggle.OnValueChanged (action);
			return this;
		}

		public MiniToggle SetIsOnWithoutNotify (bool on)
		{
			toggle.SetIsOnWithoutNotify (on);
			return this;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			toggle.OnPointerClick (eventData);
		}
	}
}
