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
using UnityEngine.UI;
using TMPro;

using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;

using KodeUI;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	public enum ELCraftType { VAB, SPH, SubAss, Part };

	public class ELCraftBrowser : Window
	{
		public delegate void SelectFileCallback(string fullPath,
												ELCraftType craftType);
		public SelectFileCallback OnFileSelected;

		public delegate void CancelledCallback();
		public CancelledCallback OnBrowseCancelled;

		static EditorFacility []craftFacility = new EditorFacility[] {
			EditorFacility.VAB,
			EditorFacility.SPH,
			EditorFacility.None,
		};

		static ELCraftType []facilityType = new ELCraftType[] {
			ELCraftType.SubAss,
			ELCraftType.VAB,
			ELCraftType.SPH,
		};

		public ELCraftType craftType { get; private set; }

		ELCraftTypeSelector typeSelector;
		ScrollView craftList;
		ELCraftThumb craftThumb;
		ScrollView craftInfo;
		UIText craftDescription;
		UIButton loadButton;

		ELCraftItem.List craftItems;
		ToggleGroup craftGroup;
		ELCraftItem _selectedCraft;
		ELCraftItem selectedCraft
		{
			get { return _selectedCraft; }
			set {
				_selectedCraft = value;
				UpdateCraftInfo ();
			}
		}
/*
		public ELCraftType craftType
		{
			get {
				return facilityType[(int) facility];
			}
		}*/

		public override void CreateUI ()
		{
			base.CreateUI ();

			UIScrollbar list_scrollbar;
			UIScrollbar info_scrollbar;

			this.Title (ELLocalization.SelectCraft)
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.PreferredSizeFitter (true, true)
				.Anchor (AnchorPresets.MiddleCenter)
				.Pivot (PivotPresets.MiddleCenter)
				.Add <ELCraftTypeSelector> (out typeSelector)
					.OnSelectionChanged (CraftTypeSelected)
					.FlexibleLayout (true, true)
					.Finish ()
				.Add <Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false,false)

					.Add <ScrollView> (out craftList)
						.Horizontal (false)
						.Vertical (true)
						.Horizontal()
						.ControlChildSize (true, true)
						.ChildForceExpand (false, true)
						.PreferredSize (321, -1)
						.Add<UIScrollbar> (out list_scrollbar, "Scrollbar")
							.Direction(Scrollbar.Direction.BottomToTop)
							.PreferredWidth (15)
							.Finish ()
						.Finish ()
					.Add <Layout> ()
						.Vertical ()
						.ControlChildSize (true, true)
						.ChildForceExpand (false,false)
						.Add<ELCraftThumb> (out craftThumb)
							.Finish ()
						.Add<ScrollView> (out craftInfo)
							.Horizontal (false)
							.Vertical (true)
							.Horizontal()
							.ControlChildSize (true, true)
							.ChildForceExpand (false, true)
							.FlexibleLayout (true, false)
							.PreferredSize (-1, 256)
							.Add<UIScrollbar> (out info_scrollbar, "Scrollbar")
								.Direction(Scrollbar.Direction.BottomToTop)
								.PreferredWidth (15)
								.Finish ()
							.Finish ()
						.Finish ()
					.Finish ()
				.Add <Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false,false)
					.Add <UIButton> ()
						.Text (ELLocalization.Cancel)
						.OnClick (Cancel)
						.Finish ()
					.Add <UIButton> (out loadButton)
						.Text (ELLocalization.Load)
						.OnClick (LoadCraft)
						.Finish ()
					.Finish ()
				.Add<UIText> ()
					.Text (ELVersionReport.GetVersion ())
					.Alignment (TextAlignmentOptions.Center)
					.Size (12)
					.FlexibleLayout (true, false)
					.Finish ()
				.Finish ();

			craftList.VerticalScrollbar = list_scrollbar;
			craftList.Viewport.FlexibleLayout (true, true);
			craftList.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.SizeDelta (0, 0)
				.ToggleGroup (out craftGroup)
				.Finish ();

			craftInfo.VerticalScrollbar = info_scrollbar;
			craftInfo.Viewport.FlexibleLayout (true, true);
			craftInfo.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.SizeDelta (0, 0)
				.Add<UIText> (out craftDescription)
					.Alignment (TextAlignmentOptions.TopLeft)
					.FlexibleLayout (true, false)
					.SizeDelta (0, 0)
					.Finish ()
				.Finish ();


			relativePath = "";
			craftThumb.Craft (ELCraftType.SubAss, "");

			craftItems = new ELCraftItem.List (craftGroup);
			craftItems.Content = craftList.Content;
			craftItems.onSelected = OnSelected;
		}

		void Cancel ()
		{
			OnBrowseCancelled ();
			SetActive (false);
		}

		void pipelineSucceed (ConfigNode node, ELCraftItem craft)
		{
			if (node != craft.node) {
				craft.node.Save (craft.fullPath + ".original");
				node.Save (craft.fullPath);
			}
			OnFileSelected (selectedCraft.fullPath, selectedCraft.type);
		}

		void pipelineFail (KSPUpgradePipeline.UpgradeFailOption opt, ConfigNode node, ELCraftItem craft)
		{
			if (opt == KSPUpgradePipeline.UpgradeFailOption.Cancel) {
				OnBrowseCancelled ();
			} else if (opt == KSPUpgradePipeline.UpgradeFailOption.LoadAnyway) {
				pipelineSucceed (node, craft);
			}
		}

		void LoadCraft ()
		{
			SetActive (false);
			KSPUpgradePipeline.Process (selectedCraft.node, selectedCraft.name,
										SaveUpgradePipeline.LoadContext.Craft,
										node => { pipelineSucceed (node, selectedCraft); },
										(opt, node) => { pipelineFail (opt, node, selectedCraft); });
		}

		string relativePath;

		void OnSelected (ELCraftItem craft)
		{
			if (selectedCraft == craft && Mouse.Left.GetDoubleClick (true)) {
				Debug.Log ($"    double click!!");
				LoadCraft ();
			} else {
				selectedCraft = craft;
			}
		}

		void UpdateCraftInfo ()
		{
			if (selectedCraft != null) {
				craftThumb.Craft (selectedCraft.thumbPath);
				craftDescription.Text (selectedCraft.description);
				loadButton.interactable = true;
			} else {
				craftDescription.Text ("");
				craftThumb.Craft ("");
				loadButton.interactable = false;
			}
		}

		void CraftTypeSelected ()
		{
			SetRelativePath (typeSelector.craftType, typeSelector.stockCraft, "");
		}

		void SetRelativePath (ELCraftType craftType, bool stock, string path)
		{
			SetActive (true);
			this.craftType = craftType;
			relativePath = path;
			ScanDirectory (craftType, stock, path);
		}

		bool ScanDirectory (ELCraftType type, bool stock, string path)
		{
			string profile = HighLogic.SaveFolder;
			string basePath = KSPUtil.ApplicationRootPath;

			craftItems.Clear ();
			selectedCraft = null;

			if (!stock) {
				basePath = basePath + $"saves/{profile}/";
			}

			switch (type) {
				case ELCraftType.VAB:
				case ELCraftType.SPH:
					path = $"{basePath}Ships/{type.ToString ()}/{path}";
					break;
				case ELCraftType.SubAss:
					var subassPath = $"{basePath}Subassemblies/";
					if (!Directory.Exists (subassPath)) {
						Directory.CreateDirectory (subassPath);
					}
					path = $"{subassPath}{path}";
					break;
			}

			var directory = new DirectoryInfo (path);
			var files = directory.GetFiles ("*.craft");

			for (int i = 0; i < files.Length; i++) {
				var fi = files[i];
				string fp = fi.FullName;
				string mp = fi.FullName.Replace (fi.Extension, ".loadmeta");
				string tp;
				if (stock) {
					tp = ELCraftThumb.StockPath (type, fp);
				} else {
					tp = ELCraftThumb.UserPath (type, fp);
				}
				craftItems.Add (new ELCraftItem (fp, mp, tp, type));
			}

			UIKit.UpdateListContent (craftItems);
			craftItems.Select (0);
			return true;
		}

		static ELCraftBrowser craftBrowser;

		public static void OpenDialog (ELCraftType craftType, string path,
									   SelectFileCallback onFileSelected,
									   CancelledCallback onCancel)
		{
			if (!craftBrowser) {
				craftBrowser = UIKit.CreateUI<ELCraftBrowser> (ELWindowManager.appCanvasRect, "ELCraftBrowser");
			}
			craftBrowser.OnFileSelected = onFileSelected;
			craftBrowser.OnBrowseCancelled = onCancel;
			craftBrowser.SetRelativePath (craftType, false, path);
		}
	}
}
