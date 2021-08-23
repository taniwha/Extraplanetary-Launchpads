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

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftTypeSelector : UIObject, ILayoutElement
	{
		ToggleText vabToggle;
		ToggleText sphToggle;
		ToggleText subToggle;
		ToggleText partToggle;
		ToggleText stockToggle;

		public class ELCraftTypeSelectorEvent : UnityEvent { }
		ELCraftTypeSelectorEvent onSelectionChanged;

		public ELCraftType craftType { get; private set; }
		public bool stockCraft { get; private set; }

		protected override string GetStylePath(bool isParent=false)
		{
			if (isParent) {
				return GetParentStylePath ();
			} else {
				return base.GetStylePath(isParent);
			}
		}

		public override void CreateUI ()
		{
			onSelectionChanged = new ELCraftTypeSelectorEvent ();

			var vabMin  = new Vector2 (0.00f, 0.5f);
			var vabMax  = new Vector2 (0.25f, 1.0f);
			var sphMin  = new Vector2 (0.25f, 0.5f);
			var sphMax  = new Vector2 (0.50f, 1.0f);
			var subMin  = new Vector2 (0.50f, 0.5f);
			var subMax  = new Vector2 (0.75f, 1.0f);
			var partMin = new Vector2 (0.75f, 0.5f);
			var partMax = new Vector2 (1.00f, 1.0f);
			var stockMin= new Vector2 (0.00f, 0.0f);
			var stockMax= new Vector2 (1.00f, 0.5f);

			ToggleGroup group;

			this.ToggleGroup (out group)
				.Add<ToggleText> (out vabToggle)
					.Text (ELLocalization.VAB)
					.Group (group)
					.OnValueChanged (on => { if (on) { SetType (ELCraftType.VAB); } })
					.Anchor (vabMin, vabMax)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ToggleText> (out sphToggle)
					.Text (ELLocalization.SPH)
					.Group (group)
					.OnValueChanged (on => { if (on) { SetType (ELCraftType.SPH); } })
					.Anchor (sphMin, sphMax)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ToggleText> (out subToggle)
					.Text (ELLocalization.SubAss)
					.Group (group)
					.OnValueChanged (on => { if (on) { SetType (ELCraftType.SubAss); } })
					.Anchor (subMin, subMax)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ToggleText> (out partToggle)
					.Text (ELLocalization.Part)
					.Group (group)
					.OnValueChanged (on => { if (on) { SetType (ELCraftType.Part); } })
					.Anchor (partMin, partMax)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ToggleText> (out stockToggle)
					.Text (ELLocalization.StockVessels)
					.OnValueChanged (SetStockCraft)
					.Anchor (stockMin, stockMax)
					.SizeDelta (0, 0)
					.Finish ()
				;
		}

		public override void Style ()
		{
		}

		void UpdateControls ()
		{
			if (craftType == ELCraftType.VAB || craftType == ELCraftType.SPH) {
				stockToggle.interactable = true;
				stockToggle.SetIsOnWithoutNotify (stockCraft);
			} else {
				stockToggle.interactable = false;
				stockToggle.SetIsOnWithoutNotify (false);
			}
		}

		void SetType (ELCraftType type)
		{
			craftType = type;
			UpdateControls ();
			onSelectionChanged.Invoke ();
		}

		void SetStockCraft (bool on)
		{
			stockCraft = on;
			onSelectionChanged.Invoke ();
		}

		public ELCraftTypeSelector OnSelectionChanged (UnityAction action)
		{
			onSelectionChanged.AddListener (action);
			return this;
		}

		public void SetCraftType (ELCraftType craftType, bool stock)
		{
			this.craftType = craftType;
			stockCraft = stock;
			UpdateControls ();
		}
#region ILayoutElement
		Vector2 minSize;
		Vector2 preferredSize;

		public void CalculateLayoutInputHorizontal()
		{
			float a, b, c, d, e;

			a = LayoutUtility.GetMinSize (vabToggle.rectTransform, 0);
			b = LayoutUtility.GetMinSize (sphToggle.rectTransform, 0);
			c = LayoutUtility.GetMinSize (subToggle.rectTransform, 0);
			d = LayoutUtility.GetMinSize (partToggle.rectTransform, 0);
			e = LayoutUtility.GetMinSize (stockToggle.rectTransform, 0);
			minSize.x = Mathf.Max (a + b + c + d, e);

			a = LayoutUtility.GetPreferredSize (vabToggle.rectTransform, 0);
			b = LayoutUtility.GetPreferredSize (sphToggle.rectTransform, 0);
			c = LayoutUtility.GetPreferredSize (subToggle.rectTransform, 0);
			d = LayoutUtility.GetPreferredSize (partToggle.rectTransform, 0);
			e = LayoutUtility.GetPreferredSize (stockToggle.rectTransform, 0);
			preferredSize.x = Mathf.Max (a + b + c + d, e);
		}

		public void CalculateLayoutInputVertical()
		{
			float a, b, c, d, e;

			a = LayoutUtility.GetMinSize (vabToggle.rectTransform, 1);
			b = LayoutUtility.GetMinSize (sphToggle.rectTransform, 1);
			c = LayoutUtility.GetMinSize (subToggle.rectTransform, 1);
			d = LayoutUtility.GetMinSize (partToggle.rectTransform, 1);
			e = LayoutUtility.GetMinSize (stockToggle.rectTransform, 1);
			minSize.y = Mathf.Max (a, Mathf.Max (b, Mathf.Max (c, d))) + e;

			a = LayoutUtility.GetPreferredSize (vabToggle.rectTransform, 1);
			b = LayoutUtility.GetPreferredSize (sphToggle.rectTransform, 1);
			c = LayoutUtility.GetPreferredSize (subToggle.rectTransform, 1);
			d = LayoutUtility.GetPreferredSize (partToggle.rectTransform, 1);
			e = LayoutUtility.GetPreferredSize (stockToggle.rectTransform, 1);
			preferredSize.y = Mathf.Max (a, Mathf.Max (b, Mathf.Max (c, d))) + e;
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
