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
using UnityEngine.UI;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELBuildCraftView : Layout
	{
		class RequiredResource : IResourceLine
		{
			BuildResource required;
			RMResourceInfo available;

			public string ResourceName { get { return required.name; } }
			public string ResourceInfo { get { return null; } }
			public double BuildAmount { get { return required.amount; } }
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
					if (this.available == null || this.required == null) {
						return 0;
					}
					double required = this.required.amount;
					double available = this.available.amount;

					if (available < 0) {
						return 0;
					} else if (available < required) {
						return available / required;
					}
					return 1;
				}
			}

			public RequiredResource (BuildResource build, RMResourceInfo pad)
			{
				this.required = build;
				this.available = pad;
			}
		}

		Sprite flagTexture;

		UIButton selectCraftButton;
		UIButton selectFlagButton;
		UIButton reloadButton;
		UIButton clearButton;
		UIButton buildButton;

		ScrollView craftView;
		Layout selectedCraft;
		ELResourceDisplay resourceList;
		UIText craftName;
		UIText craftBoM;
		ELCraftThumb craftThumb;

		ELBuildControl control;

		FlagBrowser flagBrowser;

		List<IResourceLine> requiredResources;

		static Texture2D genericCraftThumb;

		static FlagBrowser _flagBrowserPrefab;
		static FlagBrowser flagBrowserPrefab
		{
			get {
				if (!_flagBrowserPrefab) {
					var fbObj = AssetBase.GetPrefab ("FlagBrowser");
					_flagBrowserPrefab = fbObj.GetComponent<FlagBrowser> ();
				}
				return _flagBrowserPrefab;
			}
		}

		public override void CreateUI()
		{
			if (!genericCraftThumb) {
				genericCraftThumb = AssetBase.GetTexture("craftThumbGeneric");
			}
			if (requiredResources == null) {
				requiredResources = new List<IResourceLine> ();
			}

			base.CreateUI ();

			UIScrollbar scrollbar;
			UIText overflowText;

			Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)

				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.Anchor(AnchorPresets.HorStretchTop)
					.FlexibleLayout (true,false)
					.Add<UIButton> (out selectCraftButton)
						.Text (ELLocalization.SelectCraft)
						.OnClick (SelectCraft)
						.FlexibleLayout (true, true)
						.Finish ()
					.Add<UIButton> (out selectFlagButton)
						.Image (flagTexture)
						.OnClick (SelectFlag)
						.PreferredSize (48, 30)
						.Finish ()
					.Add<UIButton> (out reloadButton)
						.Text (ELLocalization.Reload)
						.OnClick (Reload)
						.Finish ()
					.Add<UIButton> (out clearButton)
						.Text (ELLocalization.Clear)
						.OnClick (Clear)
						.Finish ()
					.Finish ()
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
				.Add<UIButton> (out buildButton)
					.Text (ELLocalization.Build)
					.OnClick (BuildCraft)
					.FlexibleLayout (true, true)
					.Finish()
				.Finish();

			craftView.VerticalScrollbar = scrollbar;
			craftView.Viewport.FlexibleLayout (true, true);
			craftView.Content
				.VertiLink ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				.Add<ELResourceDisplay> (out resourceList)
					.Finish ()
				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, false)
					.ChildForceExpand (false, false)
					.Anchor (AnchorPresets.HorStretchTop)
					.FlexibleLayout (true, false)
					.Add<UIText> (out craftBoM)
						.Alignment (TextAlignmentOptions.TopLeft)
						.FlexibleLayout (true, false)
						.SizeDelta (0, 256)
						.Finish ()
					.Add<ELCraftThumb> (out craftThumb)
						.Anchor (AnchorPresets.TopLeft)
						.Pivot (PivotPresets.TopLeft)
						.Finish ()
					.Finish ()
				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.FlexibleLayout (true, false)
					.Add<UIText> (out overflowText)
						.Alignment (TextAlignmentOptions.TopLeft)
						.FlexibleLayout (true, false)
						.Finish()
					.Finish ()
				.Finish ();

			craftBoM.tmpText.overflowMode = TextOverflowModes.Linked;
			craftBoM.tmpText.linkedTextComponent = overflowText.tmpText;

			selectFlagButton.ChildImage.AspectRatioSizeFitter (AspectRatioFitter.AspectMode.FitInParent, 1.6f);
		}

		void BuildCraft ()
		{
			if (control != null && control.builder.canBuild) {
				control.BuildCraft ();
			}
		}

		void craftSelectComplete (string filename, ELCraftType craftType)
		{
			control.craftType = craftType;

			control.LoadCraft (filename, control.flagname);

			bool enable = control.craftConfig != null;
			selectCraftButton.interactable = true;
			reloadButton.interactable = enable;
			clearButton.interactable = enable;
			buildButton.interactable = enable && control.builder.canBuild;
			UpdateCraftInfo ();
		}

		void craftSelectCancel ()
		{
			selectCraftButton.interactable = true;
		}

		void SelectCraft ()
		{
			string path = "";

			ELCraftBrowser.OpenDialog (control.craftType, path,
									   craftSelectComplete, craftSelectCancel);
			selectCraftButton.interactable = false;
		}

		void OnFlagCancel ()
		{
			flagBrowser = null;
			selectFlagButton.interactable = true;
		}

		void OnFlagSelected (FlagBrowser.FlagEntry selected)
		{
			Destroy (flagTexture);

			control.flagname = selected.textureInfo.name;
			UpdateFlag ();
			flagBrowser = null;
			selectFlagButton.interactable = true;
		}

		void SelectFlag ()
		{
			flagBrowser = Instantiate<FlagBrowser> (flagBrowserPrefab);
			flagBrowser.OnDismiss = OnFlagCancel;
			flagBrowser.OnFlagSelected = OnFlagSelected;
			selectFlagButton.interactable = false;
		}

		void Reload ()
		{
			control.LoadCraft (control.filename, control.flagname);
		}

		void Clear ()
		{
			control.UnloadCraft ();
			reloadButton.interactable = false;
			clearButton.interactable = false;
			buildButton.interactable = false;
		}

		public override void Style ()
		{
			base.Style ();
		}

		void UpdateFlag ()
		{
			if (control != null) {
				if (String.IsNullOrEmpty (control.flagname)) {
					control.flagname = control.builder.part.flagURL;
				}
				var tex = GameDatabase.Instance.GetTexture (control.flagname, false);
				flagTexture = EL_Utils.MakeSprite (tex);
				selectFlagButton.Image (flagTexture);
			}
		}

		void UpdateCraftInfo ()
		{
			if (control != null) {
				bool enable = control.craftConfig != null;
				selectedCraft.gameObject.SetActive (enable);
				craftView.gameObject.SetActive (enable);

				if (enable) {
					craftName.Text (control.craftName);
					StartCoroutine (WaitAndRebuildResources ());
					if (control.craftBoM == null || control.craftBoMdirty) {
						control.CreateBoM ();
					}
					if (control.craftBoM != null) {
						craftBoM.Text (String.Join ("\n", control.craftBoM));
					} else {
						craftBoM.Text ("");
					}
					craftThumb.Craft (control.craftType, control.filename);
				}
			} else {
				selectedCraft.gameObject.SetActive (false);
				craftView.gameObject.SetActive (false);
			}
		}

		public void UpdateControl (ELBuildControl control)
		{
			this.control = control;
			if (control != null
				&& (control.state == ELBuildControl.State.Idle
					|| control.state == ELBuildControl.State.Planning)) {
				gameObject.SetActive (true);
				UpdateFlag ();
				UpdateCraftInfo ();
				selectCraftButton.interactable = true;
				selectFlagButton.interactable = true;
				bool enable = control.craftConfig != null;
				reloadButton.interactable = enable;
				clearButton.interactable = enable;
				buildButton.interactable = enable && control.builder.canBuild;
			} else {
				gameObject.SetActive (false);
				selectCraftButton.interactable = false;
				selectFlagButton.interactable = false;
				reloadButton.interactable = false;
				clearButton.interactable = false;
				buildButton.interactable = false;
			}
		}

		void RebuildResources ()
		{
			requiredResources.Clear ();
			foreach (var res in control.buildCost.required) {
				var available = control.padResources[res.name];
				requiredResources.Add (new RequiredResource (res, available));
			}
			resourceList.Resources (requiredResources);
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
