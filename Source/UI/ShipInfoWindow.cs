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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELShipInfoWindow : Window
	{
		ELTextInfoLine dryMass;
		ELTextInfoLine resourceMass;
		ELTextInfoLine totalMass;
		ELTextInfoLine buildTime;

		ScrollView requiredResources;
		ScrollView optionalResources;

		public override void CreateUI()
		{
			base.CreateUI ();

			UIScrollbar reqScrollbar;
			UIScrollbar optScrollbar;

			this.Title (ELVersionReport.GetVersion ())
				.Vertical()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.PreferredSizeFitter(true, true)
				.Anchor(AnchorPresets.MiddleCenter)
				.Pivot(PivotPresets.TopLeft)
				.PreferredWidth(400)

				.Add<ELTextInfoLine> (out dryMass)
					.Label (ELLocalization.DryMass)
					.Finish ()
				.Add<ELTextInfoLine> (out resourceMass)
					.Label (ELLocalization.ResourceMass)
					.Finish ()
				.Add<ELTextInfoLine> (out totalMass)
					.Label (ELLocalization.TotalMass)
					.Finish ()
				.Add<ELTextInfoLine> (out buildTime)
					.Label (ELLocalization.BuildTime)
					.Finish ()

				.Add<ScrollView> (out requiredResources)
					.Horizontal (false)
					.Vertical (true)
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.FlexibleLayout(true, false)
					.PreferredHeight (120)
					.Add<UIScrollbar> (out reqScrollbar, "Scrollbar")
						.Direction (Scrollbar.Direction.BottomToTop)
						.PreferredWidth (15)
						.Finish ()
					.Finish ()
				.Add<ScrollView> (out optionalResources)
					.Horizontal (false)
					.Vertical (true)
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.FlexibleLayout(true, false)
					.PreferredHeight (120)
					.Add<UIScrollbar> (out optScrollbar, "Scrollbar")
						.Direction (Scrollbar.Direction.BottomToTop)
						.PreferredWidth (15)
						.Finish ()
					.Finish ()
				.Finish ();

			requiredResources.VerticalScrollbar = reqScrollbar;
			requiredResources.Viewport.FlexibleLayout (true, true);
			requiredResources.Content
				.Vertical()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.SizeDelta (0, 0)
				.Finish ();

			optionalResources.VerticalScrollbar = optScrollbar;
			optionalResources.Viewport.FlexibleLayout (true, true);
			optionalResources.Content
				.Vertical()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.SizeDelta (0, 0)
				.Finish ();
		}

		public override void Style ()
		{
			base.Style ();
		}

		public void SetVisible (bool visible)
		{
			gameObject.SetActive (visible);
			ELShipInfo.shipInfoWindow = visible ? this : null;
		}

		void SetMass (ELTextInfoLine info, double mass)
		{
			info.Info (EL_Utils.FormatMass (mass));
		}

		void SetUnit (ELTextInfoLine info, double amount, string units)
		{
			info.Info (amount.ToStringSI (4, unit:units));
		}

		void SetResource (ELTextInfoLine info, BuildResource res)
		{
			Debug.Log ($"[ELShipInfoWindow] SetResource {info} {res}");
			info.Label (res.name);
			string amount = res.amount.ToStringSI (4, unit:"u");
			string mass = EL_Utils.FormatMass (res.mass, 4);
			info.Info ($"{amount} {mass}");
		}

		void ResourceList (Layout content, List<BuildResource> resources)
		{
			var contentRect = content.rectTransform;
			int childCount = contentRect.childCount;
			int childIndex = 0;
			int itemIndex = 0;
			int itemCount = resources.Count;
			ELTextInfoLine item;

			while (childIndex < childCount && itemIndex < itemCount) {
				var child = contentRect.GetChild (childIndex);
				item = child.GetComponent<ELTextInfoLine> ();
				SetResource (item, resources[itemIndex]);
				++childIndex;
				++itemIndex;
			}
			while (childIndex < childCount) {
				var go = contentRect.GetChild (childIndex++).gameObject;
				Destroy (go);
			}
			while (itemIndex < itemCount) {
				content.Add<ELTextInfoLine> (out item)
					.FlexibleLayout(true, false)
					.SizeDelta (0, 0)
					.Finish();
				SetResource (item, resources[itemIndex]);
				++itemIndex;
			}
		}

		public void UpdateInfo (BuildCost buildCost)
		{
			var cost = buildCost.cost;
			double required_mass = 0;
			double resource_mass = 0;
			double kerbalHours = 0;

			foreach (var res in cost.required) {
				kerbalHours += res.kerbalHours * res.amount;
				required_mass += res.mass;
			}
			kerbalHours = Math.Round (kerbalHours, 4);

			foreach (var res in cost.optional) {
				resource_mass += res.mass;
			}

			SetMass (dryMass, buildCost.mass);
			SetMass (resourceMass, resource_mass);
			SetMass (totalMass, required_mass + resource_mass);
			SetUnit (buildTime, kerbalHours, ELLocalization.KerbalHours);

			ResourceList (requiredResources.Content, cost.required);
			ResourceList (optionalResources.Content, cost.optional);
		}
	}
}
