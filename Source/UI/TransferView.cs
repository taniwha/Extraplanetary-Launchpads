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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KodeUI;

using KSP.IO;
using CBDLoadType = KSP.UI.Screens.CraftBrowserDialog.LoadType;

namespace ExtraplanetaryLaunchpads {

	public class ELTransferView : Layout
	{
		public class TransferResource : IResourceLine, IResourceLineAdjust
		{
			ELBuildControl control;
			BuildResource optional;
			RMResourceInfo capacity;
			RMResourceInfo available;

			public string ResourceName { get { return optional.name; } }
			public string ResourceInfo
			{
				get {
					return null;
				}
			}
			public double BuildAmount { get { return optional.amount; } }
			public double AvailableAmount { get { return available.amount; } }
			public double ResourceFraction
			{
				get {
					return optional.amount / capacity.maxAmount;
				}
				set {
					optional.amount = value * capacity.maxAmount;
				}
			}

			public TransferResource (BuildResource opt, RMResourceInfo cap, RMResourceInfo pad, ELBuildControl control)
			{
				optional = opt;
				capacity = cap;
				available = pad;
				this.control = control;
			}
		}

		UIButton releaseButton;

		ScrollView craftView;
		Layout selectedCraft;
		ELResourceDisplay resourceList;
		UIText craftName;

		List<IResourceLine> transferResources;

		ELBuildControl control;

		public override void CreateUI()
		{
			if (transferResources == null) {
				transferResources = new List<IResourceLine> ();
			}

			base.CreateUI ();

			var leftMin = new Vector2 (0, 0);
			var leftMax = new Vector2 (0.5f, 1);
			var rightMin = new Vector2 (0.5f, 0);
			var rightMax = new Vector2 (1, 1);

			UIScrollbar scrollbar;
			Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)

				.Add<LayoutPanel>()
					.Background("KodeUI/Default/background")
					.BackgroundColor(UnityEngine.Color.white)
					.Vertical()
					.Padding(8)
					.ControlChildSize(true, true)
					.ChildForceExpand(false, false)
					.Anchor(AnchorPresets.HorStretchTop)
					.FlexibleLayout(true,true)
					.PreferredHeight(300)
					.Add<Layout> (out selectedCraft)
						.Horizontal ()
						.ControlChildSize(true, true)
						.ChildForceExpand(false, false)
						.FlexibleLayout(true,false)
						.Add<UIText> ()
							.Text (ELLocalization.SelectedCraft)
							.Finish ()
						.Add<UIEmpty>()
							.MinSize(15,-1)
							.Finish()
						.Add<UIText> (out craftName)
							.Finish ()
						.Finish ()
					.Add<ScrollView> (out craftView)
						.Horizontal (false)
						.Vertical (true)
						.Horizontal()
						.ControlChildSize (true, true)
						.ChildForceExpand (false, true)
						.Add<UIScrollbar> (out scrollbar, "Scrollbar")
							.Direction(Scrollbar.Direction.BottomToTop)
							.PreferredWidth (15)
							.Finish ()
						.Finish ()
					.Finish ()
				.Add<UIButton> (out releaseButton)
					.Text (ELLocalization.Release)
					.OnClick (ReleaseCraft)
					.FlexibleLayout (true, true)
					.Finish()
				.Finish();

			craftView.VerticalScrollbar = scrollbar;
			craftView.Viewport.FlexibleLayout (true, true);
			craftView.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				.Add<ELResourceDisplay> (out resourceList)
					.Finish ()
				.Finish ();
		}

		void ReleaseCraft ()
		{
			control.ReleaseVessel ();
		}

		public void UpdateControl (ELBuildControl control)
		{
			this.control = control;
			if (control != null
				&& (control.state == ELBuildControl.State.Transfer)) {
				gameObject.SetActive (true);
				craftName.Text (control.craftName);
				StartCoroutine (WaitAndRebuildResources ());
			} else {
				gameObject.SetActive (false);
			}
		}

		static BuildResource FindResource (List<BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void RebuildResources ()
		{
			transferResources.Clear ();
			foreach (var res in control.buildCost.optional) {
				var opt = control.craftResources[res.name];
				var available = control.padResources[res.name];
				var line = new TransferResource (res, opt, available, control);
				transferResources.Add (line);
			}
			resourceList.Resources (transferResources);
		}

		IEnumerator WaitAndRebuildResources ()
		{
			while (control.padResources == null) {
				yield return null;
			}
			RebuildResources ();
		}
	}
}
