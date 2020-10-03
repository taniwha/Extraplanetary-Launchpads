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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELResourceLine : LayoutAnchor
	{
		UIText resourceName;
		UISlider fraction;
		UIText info;
		UIText amount;
		UIText available;

		IResourceLine resource;

		static string displayAmount (double amount) {
			if (amount > 1e5) {
				return Math.Round((amount / 1e6), 2).ToString() + " M";
			} else {
				return Math.Round(amount, 2).ToString();
			}
		}

		public void UpdateDisplay()
		{
			if (resource.ResourceInfo != null) {
				info.Text(resource.ResourceInfo);
			}
			double frac = resource.ResourceFraction;
			fraction.slider.SetValueWithoutNotify ((float) frac);

			double amount = resource.BuildAmount;
			double available = resource.AvailableAmount;

			if (available >= 0) {
				this.available.Text (displayAmount (available));
			}
			Color color = UnityEngine.Color.green;
			if (available >= 0 && available < amount) {
				color = UnityEngine.Color.yellow;
			} else if (available < 0) {
				color = UnityEngine.Color.white;
			}
			this.amount.Color(color).Style();
			this.amount.Text (displayAmount (amount));
		}

		public override void CreateUI()
		{
			base.CreateUI ();

			Vector2 nameMin = new Vector2 (0, 0);
			Vector2 nameMax = new Vector2 (0.175f, 1);
			Vector2 fractionMin = new Vector2 (0.20f, 0);
			Vector2 fractionMax = new Vector2 (0.65f, 1);
			Vector2 amountsMin = new Vector2 (0.675f, 0);
			Vector2 amountsMax = new Vector2 (1, 1);
			var requiredMin = new Vector2 (0, 0);
			var requiredMax = new Vector2 (0.475f, 1);
			var availableMin = new Vector2 (0.525f, 0);
			var availableMax = new Vector2 (1, 1);
			var textMargins = new Vector4 (5, 5, 10, 10);

			this
				.DoPreferredHeight (true)
				.DoMinHeight (true)
				.FlexibleLayout (true, false)
				.Anchor (AnchorPresets.StretchAll)
				.SizeDelta (0, 0)
				.Add<LayoutAnchor> ()
					.DoPreferredWidth (true)
					.DoPreferredHeight (true)
					.Anchor (nameMin, nameMax)
					.SizeDelta (0, 0)
					.Add<UIImage> ("AmountsPanel")
						.Type (Image.Type.Sliced)
						.Anchor (AnchorPresets.StretchAll)
						.SizeDelta (0, 0)
						.Finish()
					.Add<UIText> (out resourceName)
						.Text("resource")
						.Margin (textMargins)
						.Alignment (TextAlignmentOptions.Left)
						.Anchor (AnchorPresets.StretchAll)
						.SizeDelta (0, 0)
						.Finish ()
					.Finish ()
				.Add<UIEmpty> ()
					.Anchor (fractionMin, fractionMax)
					.SizeDelta (0, 0)
					.Add<UISlider> (out fraction, "ResourceFraction")
						.Direction (Slider.Direction.LeftToRight)
						.ShowHandle (false)
						.OnValueChanged (OnFractionChanged)
						.Anchor (AnchorPresets.StretchAll)
						.SizeDelta (0, 0)
						.Finish ()
					.Add<UIText> (out info, "ResourceInfo")
						.Margin (textMargins)
						.Alignment (TextAlignmentOptions.Center)
						.Anchor (AnchorPresets.StretchAll)
						.SizeDelta (0, 0)
						.Finish ()
					.Finish ()
				.Add<UIEmpty> ()
					.Anchor (amountsMin, amountsMax)
					.SizeDelta (0, 0)
					.Add<LayoutAnchor> ()
						.DoPreferredWidth (true)
						.DoPreferredHeight (true)
						.Anchor (requiredMin, requiredMax)
						.SizeDelta (0, 0)
						.Add<UIImage> ("AmountsPanel")
							.Type (Image.Type.Sliced)
							.Anchor (AnchorPresets.StretchAll)
							.SizeDelta (0, 0)
							.Finish()
						.Add<UIText> (out amount)
							.Text ("500")
							.Size (12)
							.Margin (textMargins)
							.Alignment (TextAlignmentOptions.Right)
							.Anchor (AnchorPresets.StretchAll)
							.SizeDelta (0, 0)
							.Finish ()
						.Finish ()
					.Add<LayoutAnchor> ()
						.DoPreferredWidth (true)
						.DoPreferredHeight (true)
						.Anchor (availableMin, availableMax)
						.SizeDelta (0, 0)
						.Add<UIImage> ("AmountsPanel")
							.Type (Image.Type.Sliced)
							.Anchor (AnchorPresets.StretchAll)
							.SizeDelta (0, 0)
							.Finish ()
						.Add<UIText> (out available)
							.Text ("1000")
							.Size (12)
							.Margin (textMargins)
							.Alignment (TextAlignmentOptions.Right)
							.Anchor (AnchorPresets.StretchAll)
							.SizeDelta (0, 0)
							.Finish ()
						.Finish ()
					.Finish ()
				.Finish ();
			fraction.interactable = false;
			fraction.slider.SetValueWithoutNotify(0.5f);
			info.tmpText.raycastTarget = false;
		}

		public override void Style ()
		{
			base.Style ();
		}

		void OnFractionChanged (float frac)
		{
			(resource as IResourceLineAdjust).ResourceFraction = frac;
		}

		public ELResourceLine Resource (IResourceLine resource)
		{
			this.resource = resource;
			resourceName.Text (resource.ResourceName);
			amount.Text (displayAmount (resource.BuildAmount));
			if (resource.AvailableAmount >= 0) {
				available.Text (displayAmount (resource.AvailableAmount));
			} else {
				available.Text (ELLocalization.NotAvailable);
			}
			bool adjust = resource is IResourceLineAdjust;
			fraction.ShowHandle (adjust);
			fraction.interactable = adjust;
			return this;
		}

		void Update ()
		{
			UpdateDisplay ();
		}
	}
}
