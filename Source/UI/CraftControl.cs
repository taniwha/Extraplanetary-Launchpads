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
using TMPro;

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftControl : Layout
	{
		Layout selectedCraft;

		Sprite flagTexture;

		UIButton selectCraftButton;
		UIButton selectFlagButton;
		UIButton reloadButton;
		UIButton clearButton;

		UIText craftName;
		UIText craftBoM;
		UIImage craftThumb;

		ELBuildControl control;

		FlagBrowser flagBrowser;
		ELCraftBrowser craftList;

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
			base.CreateUI ();

			KodeUI.ScrollView scrollview;
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
						.Add<UIText> (out craftName)
							.Finish ()
						.Finish ()
					.Add<KodeUI.ScrollView> (out scrollview)
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
				.Finish ();

			scrollview.VerticalScrollbar = scrollbar;
			scrollview.Viewport.FlexibleLayout (true, true);
			scrollview.Content
				.VertiLink ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				// resources
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
					.Add<UIImage> (out craftThumb)
						.Anchor (AnchorPresets.TopLeft)
						.Pivot (PivotPresets.TopLeft)
						.SizeDelta (256, 256)
						.MinSize (256, 256)
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
		}

		void craftSelectComplete (string filename,
								  CraftBrowserDialog.LoadType lt)
		{
			control.craftType = craftList.craftType;
			craftList = null;

			control.LoadCraft (filename, control.flagname);

			bool enable = control.craftConfig != null;
			selectCraftButton.interactable = true;
			reloadButton.interactable = enable;
			clearButton.interactable = enable;
		}

		void craftSelectCancel ()
		{
			craftList = null;
			selectCraftButton.interactable = true;
		}

		void SelectCraft ()
		{
			string path = HighLogic.SaveFolder;

			craftList = ELCraftBrowser.Spawn (control.craftType,
											  path,
											  craftSelectComplete,
											  craftSelectCancel,
											  false);
			selectCraftButton.interactable = false;
		}

		void OnFlagCancel ()
		{
			flagBrowser = null;
			selectFlagButton.interactable = true;
		}

		static Sprite MakeSprite (Texture2D tex)
		{
			var rect = new Rect (0, 0, tex.width, tex.height);
			var pivot = new Vector2 (0.5f, 0.5f);
			float pixelsPerUnity = 100;
			uint extrude = 0;
			var type = SpriteMeshType.Tight;
			var border = Vector4.zero;

			return UnityEngine.Sprite.Create (tex, rect, pivot, pixelsPerUnity,
											  extrude, type, border);
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
				flagTexture = MakeSprite (tex);
				selectFlagButton.Image (flagTexture);
			}
		}

		void UpdateCraftInfo ()
		{
			if (control != null) {
				bool enable = control.craftConfig != null;
				selectedCraft.gameObject.SetActive (enable);

				craftName.Text (control.craftName);
				if (control.craftBoM != null || control.CreateBoM ()) {
					craftBoM.Text(String.Join ("\n", control.craftBoM));
				}
			} else {
				selectedCraft.gameObject.SetActive (false);
			}
		}

		public void SetControl (ELBuildControl control)
		{
			this.control = control;

			if (control != null) {
				UpdateFlag ();
				UpdateCraftInfo ();
				selectCraftButton.interactable = true;
				selectFlagButton.interactable = true;
				bool enable = control.craftConfig != null;
				reloadButton.interactable = enable;
				clearButton.interactable = enable;
			} else {
				selectCraftButton.interactable = false;
				selectFlagButton.interactable = false;
				reloadButton.interactable = false;
				clearButton.interactable = false;
			}
		}
	}
}
