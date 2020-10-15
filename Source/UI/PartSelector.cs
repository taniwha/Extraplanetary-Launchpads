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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;

using KodeUI;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	using TTC = KSP.UI.Screens.Editor.PartListTooltipController;

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

#region ELPartCategory
		class ELPartCategory
		{
			public delegate void CategorySelectedCallback (PartCategories category);
			public CategorySelectedCallback CategorySelected;
			public Sprite onIcon { get; private set; }
			public Sprite offIcon { get; private set; }
			public PartCategories category { get; private set; }

			static Sprite []elFilterSprites = new Sprite[2];
			static string []elFilterNames = {
				"icon_filter_n",
				"icon_filter_s"
			};

			static Sprite FindSprite (PartCategories category, bool on)
			{
				int ind = on ? 1 : 0;
				if (elFilterSprites[ind] == null) {
					string path = $"GameData/ExtraplanetaryLaunchpads/Textures/{elFilterNames[ind]}.png";
					var tex = new Texture2D (32, 32, TextureFormat.ARGB32, false);
					bool ok = EL_Utils.LoadImage (ref tex, path);
					elFilterSprites[ind] = EL_Utils.MakeSprite (tex);
					Debug.Log ($"[ELPartCategory] FindSprite {category} {path} {ok}");
				}
				return elFilterSprites[ind];
			}

			public ELPartCategory (PartCategories category)
			{
				Debug.Log ($"[ELPartCategory] {category}");
				this.category = category;
				onIcon = FindSprite (category, true);
				offIcon = FindSprite (category, false);
			}

			public void Select ()
			{
				CategorySelected (category);
			}

#region ELPartCategory.List
			public class List : List<ELPartCategory>, UIKit.IListObject
			{
				ToggleGroup group;
				public Layout Content { get; set; }
				public RectTransform RectTransform
				{
					get { return Content.rectTransform; }
				}

				public void Create (int index)
				{
					Content
						.Add<ELPartCategoryView> ()
							.Group (group)
							.Category (this[index])
							.Finish ()
						;
				}

				public void Update (GameObject obj, int index)
				{
					var view = obj.GetComponent<ELPartCategoryView> ();
					view.Category (this[index]);
				}

				public List (ToggleGroup group)
				{
					this.group = group;
				}

				public void Select (int index)
				{
					if (index >= 0 && index < Count) {
						group.SetAllTogglesOff (false);
						var child = Content.rectTransform.GetChild (index);
						var view = child.GetComponent<ELPartCategoryView> ();
						view.Select ();
					}
				}
			}
#endregion
		}
#endregion

#region ELPartCategoryView
		class ELPartCategoryView : UIObject, ILayoutElement
		{
			Toggle toggle;
			Image image;
			UIImage icon;

			ELPartCategory category;

			public override void CreateUI ()
			{
				image = gameObject.AddComponent<Image> ();
				image.type = UnityEngine.UI.Image.Type.Sliced;

				this.PreferredSize (32, 32)
					.Add<UIImage> (out icon)
						.Anchor (AnchorPresets.StretchAll)
						.Pivot (PivotPresets.MiddleCenter)
						.SizeDelta(0, 0)
						.Finish ()
					;

				toggle = gameObject.AddComponent<Toggle> ();
				toggle.onValueChanged.AddListener (onValueChanged);
				toggle.targetGraphic = image;
				toggle.graphic = icon.image;
			}

			public override void Style ()
			{
				image.sprite = style.sprite;
				image.color = style.color ?? UnityEngine.Color.white;

				toggle.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
				toggle.transition = Selectable.Transition.ColorTint;
			}

			void onValueChanged (bool on)
			{
				if (on) {
					Select ();
				}
			}

			public void Select ()
			{
				category.Select ();
			}

			public ELPartCategoryView Group (ToggleGroup group)
			{
				toggle.group = group;
				return this;
			}

			public ELPartCategoryView Category (ELPartCategory category)
			{
				this.category = category;
				image.sprite = category.offIcon;
				style.sprite = image.sprite;
				icon.image.sprite = category.onIcon;
				return this;
			}

#region ILayoutElement
			Vector2 minSize;
			Vector2 preferredSize;

			public void CalculateLayoutInputHorizontal()
			{
				minSize = Vector2.zero;
				float i, s;
				i = image.preferredWidth;
				s = icon.image.preferredWidth;
				preferredSize.x = Mathf.Max (i, s);
			}

			public void CalculateLayoutInputVertical()
			{
				float i, s;
				i = image.preferredHeight;
				s = icon.image.preferredHeight;
				preferredSize.y = Mathf.Max (i, s);
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
#endregion

#region ELPartItemView
		class ELPartItemView : UIObject
		{
			AvailablePart availablePart;
			Image background;
			Toggle toggle;
			GameObject partIcon;

			public Material []materials;

			public override void CreateUI ()
			{
				background = gameObject.AddComponent<Image> ();
				background.type = UnityEngine.UI.Image.Type.Sliced;

				toggle = gameObject.AddComponent<Toggle> ();
				toggle.targetGraphic = background;

				this.Pivot (PivotPresets.MiddleCenter);
			}

			public override void Style ()
			{
				background.sprite = style.sprite;
				background.color = style.color ?? UnityEngine.Color.white;

				toggle.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
				toggle.transition = style.transition ?? Selectable.Transition.ColorTint;
				if (style.stateSprites.HasValue) {
					toggle.spriteState = style.stateSprites.Value;
				}
			}

			public ELPartItemView Group (ToggleGroup group)
			{
				toggle.group = group;
				return this;
			}

			protected override void OnDestroy ()
			{
				Destroy (partIcon);
			}

			public ELPartItemView OnValueCanged (UnityAction<bool> action)
			{
				toggle.onValueChanged.AddListener (action);
				return this;
			}

			static Material []CollectMaterials (GameObject obj)
			{
				var materials = new List<Material> ();
				var renderers = obj.GetComponentsInChildren<Renderer> ();
				for (int i = renderers.Length; i-- > 0; ) {
					var mats = renderers[i].materials;
					for (int j = mats.Length; j-- > 0; ) {
						if (!mats[j].HasProperty (PropertyIDs._MinX)) {
							continue;
						}
						materials.Add (mats[j]);
					}
				}
				return materials.ToArray ();
			}

			public ELPartItemView AvailablePart (AvailablePart availablePart)
			{
				if (partIcon) {
					Destroy (partIcon);
				}
				this.availablePart = availablePart;
				partIcon = GameObject.Instantiate (availablePart.iconPrefab);
				partIcon.transform.SetParent (rectTransform, false);
				partIcon.transform.localPosition = new Vector3 (0, 0, -39);
				partIcon.transform.localScale = new Vector3 (39, 39, 39);
				var rot = Quaternion.Euler (-15, 0, 0);
				rot = rot * Quaternion.Euler (0, -30, 0);
				partIcon.transform.rotation = rot;
				partIcon.SetActive(true);
				int layer = LayerMask.NameToLayer ("UIAdditional");
				EL_Utils.SetLayer (partIcon, layer, true);
				materials = CollectMaterials (partIcon);
				return this;
			}
		}
#endregion

#region ELPartList
		class ELPartList : List<AvailablePart>, UIKit.IListObject
		{
			ToggleGroup group;

			public UnityAction<AvailablePart> onSelected { get; set; }
			public Layout Content { get; set; }
			public RectTransform RectTransform
			{
				get { return Content.rectTransform; }
			}

			public void Create (int index)
			{
				Content
					.Add<ELPartItemView> ()
						.Group (group)
						.OnValueCanged (on => select (index, on))
						.AvailablePart (this[index])
						.Finish ()
					;
			}

			public void Update (GameObject obj, int index)
			{
				var item = obj.GetComponent<ELPartItemView> ();
				item.AvailablePart (this[index]);
			}

			public ELPartList (ToggleGroup group)
			{
				this.group = group;
			}

			void select (int index, bool on)
			{
				if (on) {
					onSelected.Invoke (this[index]);
				}
			}
		}
#endregion

		class PartSelectionEvent : UnityEvent<bool, bool> { }
		PartSelectionEvent onSelectionChanged;

		SelectFileCallback OnFileSelected;
		CancelledCallback OnBrowseCancelled;

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
			onSelectionChanged = new PartSelectionEvent ();

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
			CreateLight (partListView.transform);

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

		public override void Style ()
		{
		}

		void OnSelected (AvailablePart availablePart)
		{
			if (availablePart != null) {
				selectedPart = availablePart;
				partPreview.AvailablePart (availablePart);
				partInfo.Text (availablePart.title);
				onSelectionChanged.Invoke (true, Mouse.Left.GetDoubleClick (true));
			} else {
				partPreview.AvailablePart (null);
				partInfo.Text ("");
				onSelectionChanged.Invoke (false, false);
			}
		}

		public ELPartSelector OnSelectionChanged (UnityAction<bool, bool> action)
		{
			onSelectionChanged.AddListener (action);
			return this;
		}

		void CreateLight (Transform parent)
		{
			var lightObj = new GameObject ("ELPartList light");
			lightObj.transform.SetParent (parent, false);

			var light = lightObj.AddComponent<Light> ();
			light.type = LightType.Directional;
			light.intensity = 0.5f;
			light.colorTemperature = 6570;
			light.cullingMask = 0x2000020;
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

		void BuildCategories ()
		{
			partCategories = new CategoryDict ();
			for (int i = categoryOrder.Length; i-- > 0; ) {
				partCategories[categoryOrder[i]] = new APList ();
			}
			for (int i = PartLoader.LoadedPartsList.Count; i-- > 0; ) {
				var ap = PartLoader.LoadedPartsList[i];
				var cat = ap.category;
				if (cat == PartCategories.none) {
					if (ap.TechHidden || ap.TechRequired == "Unresearcheable") {
						continue;
					}
					if (ap.name.StartsWith ("kerbalEVA") || ap.name == "flag") {
						continue;
					}
				} else if (cat == PartCategories.Propulsion) {
					if (ap.moduleInfos.Exists (p => p.moduleName == "Engine")) {
						cat = PartCategories.Engine;
					} else {
						cat = PartCategories.FuelTank;
					}
				}
				partCategories[cat].Add (ap);
			}
			for (int i = categoryOrder.Length; i-- > 0; ) {
				partCategories[categoryOrder[i]].Sort ((a, b) => a.title.CompareTo(b.title));
			}
		}

		public void SetDelegates (SelectFileCallback onFileSelected,
								  CancelledCallback onCancel)
		{
			OnFileSelected = onFileSelected;
			OnBrowseCancelled = onCancel;
		}

		public void SetCraftType (ELCraftType craftType, bool stock)
		{
			if (craftType == ELCraftType.Part) {
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

		public void LoadPart ()
		{
			ConfigNode node = CreateShip (selectedPart);
			Debug.Log ($"[ELPartSelector] LoadPart {selectedPart.title}\n{node}");
			OnFileSelected (node.ToString (), ELCraftType.Part);
		}
	}
}
