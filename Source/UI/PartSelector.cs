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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	using TTC = KSP.UI.Screens.Editor.PartListTooltipController;

	public delegate void SelectPartCallback (AvailablePart availablePart);

	public class ELPartSelector : Layout
	{
		static PartCategories []categoryOrder = {
			PartCategories.Pods,
			PartCategories.FuelTank,
			PartCategories.Engine,
			PartCategories.Control,
			PartCategories.Structural,
			PartCategories.Robotics,
			PartCategories.Coupling,
			PartCategories.Payload,
			PartCategories.Aero,
			PartCategories.Ground,
			PartCategories.Thermal,
			PartCategories.Electrical,
			PartCategories.Communication,
			PartCategories.Science,
			PartCategories.Cargo,
			PartCategories.Utility,
			PartCategories.none,
		};

		SelectPartCallback OnPartSelected;
		CancelledCallback OnPartCancelled;

		ScrollView categoryView;
		ScrollView partListView;
		ELPartPreview partPreview;
		ScrollView partInfoView;
		UIText partInfo;

		ELPartList partList;
		ELPartCategory.List categoryList;

		AvailablePart selectedPart;

		class APList : List<AvailablePart> { }
		class CategoryDict : Dictionary<PartCategories, APList> { }
		CategoryDict partCategories;

		public override void CreateUI ()
		{
			base.CreateUI ();

			UIScrollbar part_scrollbar;
			UIScrollbar info_scrollbar;

			this.Horizontal()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.PreferredSize (-1, 512)
				.Add<ScrollView> (out categoryView)
					.Horizontal (false)
					.Vertical (true)
					.Horizontal()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.Finish ()
				.Add<ScrollView> (out partListView)
					.Horizontal (false)
					.Vertical (true)
					.Horizontal()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.FlexibleLayout (true, true)
					.PreferredSize (215, 512)
					.Add<UIScrollbar> (out part_scrollbar, "Scrollbar")
						.Direction(Scrollbar.Direction.BottomToTop)
						.PreferredWidth (15)
						.Finish ()
					.Finish ()
				.Add<Layout> ()
					.Vertical ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.FlexibleLayout (true, true)
					.Add<ELPartPreview> (out partPreview)
						.PreferredSize (256, 256)
						.Finish ()
					.Add<ScrollView> (out partInfoView)
						.Horizontal (false)
						.Vertical (true)
						.Horizontal()
						.ControlChildSize (true, true)
						.ChildForceExpand (false, true)
						.FlexibleLayout (true, true)
						.Add<UIScrollbar> (out info_scrollbar, "Scrollbar")
							.Direction(Scrollbar.Direction.BottomToTop)
							.PreferredWidth (15)
							.Finish ()
						.Finish ()
					.Add<HorizontalLayout> ()
						.Add<UIButton> ()
							.Text (ELLocalization.Select)
							.OnClick (SelectPart)
							.Finish ()
						.Add<UIButton> ()
							.Text (ELLocalization.Cancel)
							.OnClick (CancelPart)
							.Finish ()
						.Finish ()
					.Finish ()
				;

			ToggleGroup categoryGroup;
			categoryView.Viewport.PreferredSize (32, -1);
			categoryView.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.SizeDelta (0, 0)
				.ToggleGroup (out categoryGroup)
				.Finish ();

			categoryList = new ELPartCategory.List (categoryGroup);
			categoryList.Content = categoryView.Content;

			ToggleGroup partGroup;
			partListView.VerticalScrollbar = part_scrollbar;
			partListView.Viewport.FlexibleLayout (true, true);
			partListView.Content
				.Grid ()
				.Spacing (2)
				.Padding (2)
				.StartCorner (GridLayoutGroup.Corner.UpperLeft)
				.StartAxis (GridLayoutGroup.Axis.Horizontal)
				.CellSize (64, 64)
				.Constraint (GridLayoutGroup.Constraint.FixedColumnCount)
				.ConstraintCount (3)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter (true, false)
				.SizeDelta (0, 0)
				.ToggleGroup (out partGroup)
				.Finish ();
			partListView.ScrollRect.onValueChanged.AddListener (onPartListUpdate);

			partList = new ELPartList (partGroup);
			partList.Content = partListView.Content;
			partList.onSelected = OnSelected;

			partInfoView.VerticalScrollbar = info_scrollbar;
			partInfoView.Viewport.FlexibleLayout (true, true);
			partInfoView.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				.Add<UIText> (out partInfo)
					.Alignment (TextAlignmentOptions.TopLeft)
					.FlexibleLayout (true, true)
					.Finish ()
				.Finish ();
		}

		protected override void OnEnable ()
		{
			ELPartListLight.Enable ();
		}

		protected override void OnDisable ()
		{
			ELPartListLight.Disable ();
		}

		public override void Style ()
		{
		}

		void UpdateSelectedPart (AvailablePart availablePart)
		{
			selectedPart = availablePart;
			partPreview.AvailablePart (availablePart);
			partInfo.Text (availablePart.title);
		}

		void OnSelected (AvailablePart availablePart)
		{
			if (availablePart != null) {
				bool selected = Mouse.Left.GetDoubleClick (true);
				UpdateSelectedPart (availablePart);
				if (selected) {
					OnPartSelected (selectedPart);
				}
			} else {
				partPreview.AvailablePart (null);
				partInfo.Text ("");
			}
		}

		void SelectPart ()
		{
			OnPartSelected (selectedPart);
		}

		void CancelPart ()
		{
			OnPartCancelled ();
		}

		void onPartListUpdate (Vector2 pos)
		{
			TTC.SetupScreenSpaceMask (partListView.Viewport.rectTransform);

			var parts = partListView.Content.rectTransform;
			for (int i = parts.childCount; i-- > 0; ) {
				var item = parts.GetChild (i).GetComponent<ELPartItemView> ();
				TTC.SetScreenSpaceMaskMaterials (item.materials);
			}
		}

		void CategorySelected (PartCategories category)
		{
			var available = partCategories[category];
			partList.Clear ();
			for (int i = 0; i < available.Count; i++) {
				var ap = available[i];
				if (ResearchAndDevelopment.PartTechAvailable (ap)) {
					partList.Add (ap);
				}
			}
			UIKit.UpdateListContent (partList);
		}

		PartCategories GetCategory (AvailablePart ap)
		{
			var cat = ap.category;
			if (cat == PartCategories.Propulsion) {
				if (ap.moduleInfos.Exists (p => p.moduleName == "Engine")) {
					cat = PartCategories.Engine;
				} else {
					cat = PartCategories.FuelTank;
				}
			}
			return cat;
		}

		void BuildCategories ()
		{
			partCategories = new CategoryDict ();
			for (int i = categoryOrder.Length; i-- > 0; ) {
				partCategories[categoryOrder[i]] = new APList ();
			}
			for (int i = PartLoader.LoadedPartsList.Count; i-- > 0; ) {
				var ap = PartLoader.LoadedPartsList[i];
				var cat = GetCategory (ap);
				if (cat == PartCategories.none) {
					if (ap.TechHidden || ap.TechRequired == "Unresearcheable") {
						continue;
					}
					if (ap.name.StartsWith ("kerbalEVA") || ap.name == "flag") {
						continue;
					}
				}
				partCategories[cat].Add (ap);
			}
			for (int i = categoryOrder.Length; i-- > 0; ) {
				partCategories[categoryOrder[i]].Sort ((a, b) => a.title.CompareTo(b.title));
			}
		}

		public ELPartSelector SetDelegates (SelectPartCallback onPartSelected,
											CancelledCallback onPartCancel)
		{
			OnPartSelected = onPartSelected;
			OnPartCancelled = onPartCancel;
			return this;
		}

		public void SetVisible (bool visible)
		{
			if (visible) {
				SetActive (true);
				if (partCategories == null) {
					BuildCategories ();
				}
				categoryList.Clear ();
				for (int i = 0; i < categoryOrder.Length; i++) {
					var category = categoryOrder[i];
					if (partCategories[category].Count > 0) {
						var cat = new ELPartCategory (category);
						cat.CategorySelected = CategorySelected;
						categoryList.Add (cat);
					}
				}
				Debug.Log ($"[ELPartSelector] SetVisible {categoryList.Count}");
				UIKit.UpdateListContent (categoryList);
			} else {
				SetActive (false);
				partList.Clear ();
				UIKit.UpdateListContent (partList);
			}
		}

		public void SetSelectedPart (AvailablePart availablePart)
		{
			var cat = GetCategory (availablePart);
			categoryList.Select (cat);
			UpdateSelectedPart (availablePart);
		}

		static ConfigNode CreateShip(AvailablePart availablePart)
		{
			var part = GameObject.Instantiate (availablePart.partPrefab) as Part;
			ConfigNode node = new ConfigNode();

			node.AddValue("ship", availablePart.title);
			node.AddValue("version", Versioning.version_major + "." + Versioning.version_minor + "." + Versioning.Revision);
			node.AddValue("description", "EL constructed part");
			node.AddValue("type", "VAB");
			node.AddValue("persistentId", 0);
			node.AddValue("rot", Quaternion.identity);
			node.AddValue("vesselType", part.vesselType);

			part.onBackup();
			ConfigNode partNode = node.AddNode("PART");

			partNode.AddValue("part", part.partInfo.name + "_" + part.craftID);
			partNode.AddValue("partName", part.partName);
			partNode.AddValue("persistentId", part.persistentId);
			partNode.AddValue("pos", Vector3.zero);
			partNode.AddValue("attPos", Vector3.zero);
			partNode.AddValue("attPos0", Vector3.zero);
			partNode.AddValue("rot", Quaternion.identity);
			partNode.AddValue("attRot", Quaternion.identity);
			partNode.AddValue("attRot0", Quaternion.identity);

			return node;
		}
	}
}
