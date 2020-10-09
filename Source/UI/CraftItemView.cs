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
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using KSP.Localization;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftItemView : LayoutPanel, IPointerEnterHandler, IPointerExitHandler
	{
		ELCraftItem craft;
		new UIText name;
		UIText stats;
		UIText cost;
		UIText message;
		ELCraftThumb thumb;

		public class ELCraftItemViewEvent : UnityEvent<ELCraftItem> { }
		ELCraftItemViewEvent onSelected;

		Toggle toggle;

		public override void CreateUI()
		{
			base.CreateUI ();

			onSelected = new ELCraftItemViewEvent ();

			toggle = gameObject.AddComponent<Toggle> ();
			toggle.targetGraphic = BackGround;
			toggle.onValueChanged.AddListener (onValueChanged);

			var statsMin = new Vector2 (0, 0);
			var statsMax = new Vector2 (124f/234f, 1);
			var costMin = new Vector2 (124f/234f, 0);
			var costMax = new Vector2 (1, 1);

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Padding (3)
				.Add<Layout> ()
					.Vertical ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.Add<UIText> (out name)
						.Alignment (TextAlignmentOptions.Left)
						.Size (18)
						.PreferredSize (234, -1)
						.Finish ()
					.Add<LayoutAnchor> ()
						.DoPreferredHeight (true)
						.FlexibleLayout (true, true)
						.Add<UIText> (out stats)
							.Alignment (TextAlignmentOptions.Left)
							.Size (15)
							.Anchor (statsMin, statsMax)
							.SizeDelta (0, 0)
							.Finish ()
						.Add<UIText> (out cost)
							.Alignment (TextAlignmentOptions.Left)
							.Size (15)
							.Anchor (costMin, costMax)
							.SizeDelta (0, 0)
							.Finish ()
						.Finish ()
					.Add<UIText> (out message)
						.Alignment (TextAlignmentOptions.Left)
						.Size (15)
						.FlexibleLayout (true, false)
						.Finish ()
					.Finish ()
				.Add<UIEmpty> ()
					.PreferredSize (64, 64)
					.Add<ELCraftThumb> (out thumb)
					.AspectRatioSizeFitter (AspectRatioFitter.AspectMode.FitInParent, 1)
					.Finish ()
				;
			//
		}

		void onValueChanged (bool on)
		{
			if (on) {
				onSelected.Invoke (craft);
			}
		}

		public ELCraftItemView Group (ToggleGroup group)
		{
			toggle.group = group;
			return this;
		}

		public ELCraftItemView OnSelected (UnityAction<ELCraftItem> action)
		{
			onSelected.AddListener (action);
			return this;
		}

		public ELCraftItemView Select ()
		{
			toggle.isOn = true;
			return this;
		}

		public ELCraftItemView Craft (ELCraftItem craft)
		{
			this.craft = craft;
			name.Text (craft.name);
			stats.Text (Localizer.Format("#autoLOC_452442", craft.partCount,
										 craft.stageCount));
			cost.Text (Localizer.Format("#autoLOC_6003099", craft.cost));
			message.Text (craft.message);
			thumb.Craft (craft.thumbPath);
			return this;
		}
#region OnPointerEnter/Exit
		public void OnPointerEnter (PointerEventData eventData)
		{
		}

		public void OnPointerExit (PointerEventData eventData)
		{
		}
#endregion
	}
}
