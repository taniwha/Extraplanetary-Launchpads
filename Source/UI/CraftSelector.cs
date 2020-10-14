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

	public class ELCraftSelector : Layout
	{
		public ELCraftType craftType { get; private set; }

		class CraftSelectionEvent : UnityEvent<bool, bool> { }
		CraftSelectionEvent onSelectionChanged;

		SelectFileCallback OnFileSelected;
		CancelledCallback OnBrowseCancelled;

		ScrollView craftList;
		ELCraftThumb craftThumb;
		ScrollView craftInfo;
		UIText craftDescription;
		UIButton generateThumb;

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

		public override void CreateUI ()
		{
			onSelectionChanged = new CraftSelectionEvent ();

			base.CreateUI ();

			UIScrollbar craftList_scrollbar;
			UIScrollbar info_scrollbar;

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)

				.Add <ScrollView> (out craftList)
					.Horizontal (false)
					.Vertical (true)
					.Horizontal()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, true)
					.PreferredSize (321, -1)
					.Add<UIScrollbar> (out craftList_scrollbar, "Scrollbar")
						.Direction(Scrollbar.Direction.BottomToTop)
						.PreferredWidth (15)
						.Finish ()
					.Finish ()
				.Add <Layout> ()
					.Vertical ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false,false)
					.Add<ELCraftThumb> (out craftThumb)
						.Add<UIButton> (out generateThumb)
							.Text ("Generate")
							.OnClick (GenerateThumb)
							.Anchor (AnchorPresets.StretchAll)
							.SizeDelta (0, 0)
							.Color (new UnityEngine.Color (0,0,0,0))
							.Finish ()
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
				;

			craftList.VerticalScrollbar = craftList_scrollbar;
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
			craftThumb.Craft ("");

			craftItems = new ELCraftItem.List (craftGroup);
			craftItems.Content = craftList.Content;
			craftItems.onSelected = OnSelected;
		}

		public ELCraftSelector OnSelectionChanged (UnityAction<bool, bool> action)
		{
			onSelectionChanged.AddListener (action);
			return this;
		}

		void GenerateThumb ()
		{
			ELCraftThumb.Capture (selectedCraft.node, selectedCraft.type,
								  selectedCraft.fullPath);
		}

		string relativePath;

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

		public void LoadCraft ()
		{
			KSPUpgradePipeline.Process (selectedCraft.node, selectedCraft.name,
										SaveUpgradePipeline.LoadContext.Craft,
										node => { pipelineSucceed (node, selectedCraft); },
										(opt, node) => { pipelineFail (opt, node, selectedCraft); });
		}

		void OnSelected (ELCraftItem craft)
		{
			if (selectedCraft == craft && Mouse.Left.GetDoubleClick (true)) {
				onSelectionChanged.Invoke (craft != null, true);
			} else {
				selectedCraft = craft;
				//FIXME unloadable craft (missing parts)
				onSelectionChanged.Invoke (craft != null, false);
			}
		}

		void UpdateCraftInfo ()
		{
			if (selectedCraft != null) {
				craftThumb.Craft (selectedCraft.thumbPath);
				craftDescription.Text (selectedCraft.description);
			} else {
				craftDescription.Text ("");
				craftThumb.Craft ("");
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
			if (craftType != ELCraftType.Part) {
				SetActive (true);
				SetRelativePath (craftType, stock, "");
			} else {
				SetActive (false);
			}
		}

		void SetRelativePath (ELCraftType craftType, bool stock, string path)
		{
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
	}
}
