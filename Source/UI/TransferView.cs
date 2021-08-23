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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELTransferView : Layout
	{
		class TransferResource : IResourceLine, IResourceLineAdjust
		{
			ELBuildControl control;
			BuildResource optional;
			RMResourceInfo capacity;
			RMResourceInfo available;
			public LinkedResources link;

			public string ResourceName { get { return optional.name; } }
			public string ResourceInfo
			{
				get {
					return null;
				}
			}
			public double BuildAmount { get { return optional.amount; } }
			public double AvailableAmount
			{
				get {
					if (available == null) {
						return 0;
					}
					return available.amount;
				}
			}
			public double ResourceFraction
			{
				get {
					return optional.amount / capacity.maxAmount;
				}
				set {
					optional.amount = value * capacity.maxAmount;
					if (link != null) {
						link.SetFraction (value);
					}
				}
			}

			public void SetFraction (double fraction)
			{
				optional.amount = fraction * capacity.maxAmount;
			}

			public TransferResource (BuildResource opt, RMResourceInfo cap, RMResourceInfo pad, ELBuildControl control)
			{
				optional = opt;
				capacity = cap;
				available = pad;
				this.control = control;
			}
		}

		class LinkedResources : LayoutAnchor
		{
			List<IResourceLine> resources;
			bool linked;

			ELResourceDisplay display;
			UIEmpty linkView;
			UIToggle linkToggle;

			public void SetFraction (double value)
			{
				if (linked) {
					for (int i = resources.Count; i-- > 0; ) {
						(resources[i] as TransferResource).SetFraction (value);
					}
				}
			}

			public override void CreateUI ()
			{
				base.CreateUI ();
				this.DoPreferredHeight (true)
					.FlexibleLayout (true, false)
					.Add<ELResourceDisplay> (out display)
						.Anchor (AnchorPresets.StretchAll)
						.SizeDelta (-20, 0)
						.Pivot (PivotPresets.TopLeft)
						.Finish ()
					.Add<UIEmpty> (out linkView)
						.Anchor (AnchorPresets.VertStretchRight)
						.Pivot (PivotPresets.MiddleRight)
						.SizeDelta (20, 0)
						.Add<UIImage> ("LinkTop")
							.Type (Image.Type.Sliced)
							.FlexibleLayout (true, true)
							.SizeDelta (0, 0)
							.Finish ()
						.Add<UIToggle> (out linkToggle, "LinkToggle")
							.OnValueChanged (UpdateLinked)
							.SetIsOnWithoutNotify (false)
							.Anchor (AnchorPresets.MiddleRight)
							.Pivot (PivotPresets.MiddleRight)
							.SizeDelta (20, 20)
							.Finish ()
						.Add<UIImage> ("LinkBottom")
							.Type (Image.Type.Sliced)
							.FlexibleLayout (true, true)
							.SizeDelta (0, 0)
							.Finish ()
						.Finish ();
				linkView.gameObject.SetActive (false);
			}

			void UpdateLinked (bool value)
			{
				linked = value;
			}

			public LinkedResources UnLinked ()
			{
				this.linked = false;
				linkView.gameObject.SetActive (false);
				return this;
			}

			public LinkedResources Linked (bool linked)
			{
				this.linked = linked;
				linkView.gameObject.SetActive (true);
				linkToggle.SetIsOnWithoutNotify (linked);
				return this;
			}

			public LinkedResources Resources (List<IResourceLine> resources)
			{
				this.resources = resources;
				for (int i = resources.Count; i-- > 0; ) {
					(resources[i] as TransferResource).link = this;
				}
				display.Resources (resources);
				return this;
			}
		}

		public class Link
		{
			public List<IResourceLine> link { get; }
			public int Count { get { return link.Count; } }
			public void Clear () { link.Clear (); }
			public Link ()
			{
				link = new List<IResourceLine> ();
			}
		}

		UIButton releaseButton;

		ScrollView craftView;
		Layout selectedCraft;
		UIText craftName;

		List<IResourceLine> transferResources;

		ELBuildControl control;

		Dictionary<string, Link> linkMap;
		List<Link> linkList;

		public override void CreateUI()
		{
			if (transferResources == null) {
				transferResources = new List<IResourceLine> ();
			}

			if (linkMap == null) {
				linkMap = new Dictionary<string, Link> ();
				linkList = new List<Link> ();
				foreach (var resLink in ELRecipeDatabase.resource_links.Values) {
					var link = new Link ();
					linkList.Add (link);
					foreach (var res in resLink) {
						linkMap[res] = link;
					}
				}
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
			for (int i = linkList.Count; i-- > 0; ) {
				linkList[i].Clear();
			}
			transferResources.Clear ();
			foreach (var res in control.buildCost.optional) {
				var opt = control.craftResources[res.name];
				var avail = control.padResources[res.name];
				var link = transferResources;
				TransferResource tres;
				if (linkMap.ContainsKey (res.name)) {
					link = linkMap[res.name].link;
					tres = new TransferResource (res, opt, avail, control);
				} else {
					tres = new TransferResource (res, opt, avail, control);
				}
				Debug.Log ($"[ELTransferView] RebuildResources {res.name} {opt} {avail} {link}");
				link.Add (tres);
			}

			int itemCount = 0;
			for (int i = linkList.Count; i-- > 0; ) {
				if(linkList[i].Count > 0) {
					++itemCount;
				}
			}

			var contentRect = craftView.Content.rectTransform;
			int childCount = contentRect.childCount;
			int childIndex = 0;
			int itemIndex = 0;
			if (transferResources.Count > 0) {
				if (childIndex < childCount) {
					var child = contentRect.GetChild (childIndex);
					var item = child.GetComponent<LinkedResources> ();
					item.Resources (transferResources)
						.UnLinked ();
					++childIndex;
				} else {
					craftView.Content
						.Add<LinkedResources> ()
							.Resources (transferResources)
							.UnLinked ()
							.FlexibleLayout(true, false)
							.SizeDelta (0, 0)
							.Finish ();
				}
			}
			while (childIndex < childCount && itemIndex < itemCount) {
				while (linkList[itemIndex].Count < 1) {
					++itemIndex;
					++itemCount;
				}
				var child = contentRect.GetChild (childIndex);
				var item = child.GetComponent<LinkedResources> ();
				item.Resources (linkList[itemIndex].link)
					.Linked (true);	// FIXME save linked state
				++childIndex;
				++itemIndex;
			}
			while (childIndex < childCount) {
				var go = contentRect.GetChild (childIndex++).gameObject;
				Destroy (go);
			}
			while (itemIndex < itemCount) {
				while (linkList[itemIndex].Count < 1) {
					++itemIndex;
					++itemCount;
				}
				craftView.Content
					.Add<LinkedResources> ()
					.Resources (linkList[itemIndex].link)
					.Linked (true)	// FIXME save linked state
					.FlexibleLayout(true, false)
					.SizeDelta (0, 0)
					.Finish ();
				++itemIndex;
			}
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
