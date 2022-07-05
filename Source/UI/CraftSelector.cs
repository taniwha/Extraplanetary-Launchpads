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
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using KodeUI;

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
		public ELCraftItem selectedCraft
		{
			get { return _selectedCraft; }
			private set {
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

				.Add <TreeView> (out dirTreeView)
					.Items (dirTreeItems)
					.OnClick (OnDirTreeClicked)
					.OnStateChanged (OnDirTreeStateChanged)
					.PreferredSize (256, -1)
					.FlexibleLayout (true, true)
					.Finish ()
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
							.Image (SpriteLoader.GetSprite ("EL.Default.leftturn"))
							.OnClick (GenerateThumb)
							.Anchor (AnchorPresets.TopLeft)
							.Pivot (PivotPresets.TopLeft)
							.SizeDelta (24, 24)
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


			craftThumb.Thumb ("");
			generateThumb.SetActive (false);

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

		string rootPath;
		string basePath;

		void pipelineSucceed (ConfigNode node, ELCraftItem craft)
		{
			if (node != craft.node) {
				craft.node.Save (craft.fullPath + ".original");
				node.Save (craft.fullPath);
			}
			OnFileSelected (selectedCraft.fullPath, selectedCraft.thumbPath, selectedCraft.type);
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
				craftThumb.Thumb (selectedCraft.thumbPath);
				generateThumb.SetActive (!selectedCraft.isStock && selectedCraft.canLoad);
				if (selectedCraft.canLoad) {
					craftDescription.Text (selectedCraft.description);
				} else {
					craftDescription.Text (ELLocalization.MissingParts + "\n" + String.Join ("\n", selectedCraft.MissingParts));
				}
			} else {
				craftDescription.Text ("");
				craftThumb.Thumb ("");
				generateThumb.SetActive (false);
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
			SetActive (true);

			string profile = HighLogic.SaveFolder;
			basePath = stock ? "" : $"saves/{profile}";

			string path = null;
			switch (craftType) {
				case ELCraftType.VAB:
				case ELCraftType.SPH:
					path = $"{basePath}/Ships/{craftType.ToString ()}";
					break;
				case ELCraftType.SubAss:
					var subassPath = $"{basePath}/Subassemblies";
					if (!Directory.Exists (subassPath)) {
						Directory.CreateDirectory (subassPath);
					}
					path = $"{subassPath}";
					break;
				case ELCraftType.Part:
					var partsPath = $"{basePath}/Parts";
					if (!Directory.Exists (partsPath)) {
						Directory.CreateDirectory (partsPath);
					}
					path = $"{partsPath}";
					break;
			}
			dirTreeItems.Clear();
			rootPath = $"{EL_Utils.ApplicationRootPath}{path}";
			basePath = path;
			this.craftType = craftType;
			ScanTree (rootPath, 0, stock, craftType);
			dirTreeView.Items(dirTreeItems);
			ScanDirectory (dirTreeItems[0].Object as DirItem);
			dirTreeView.SelectItem (0);
		}

		class DirItem {
			public string name;
			public string path;
			public bool hasSubdirs;
			public bool stock;
			public ELCraftType type;
			public DirItem(string name, string path, bool hasSubdirs, bool stock, ELCraftType type)
			{
				this.name = name;
				this.path = path;
				this.hasSubdirs = false;
				this.stock = stock;
				this.type = type;
			}
		}

		TreeView dirTreeView;
		List<TreeView.TreeItem> dirTreeItems = new List<TreeView.TreeItem> ();

		void OnDirTreeClicked (int index)
		{
			var dir = dirTreeItems[index].Object as DirItem;
			ScanDirectory (dir);
			dirTreeView.SelectItem (index);
		}

		void OnDirTreeStateChanged (int index, bool open)
		{
		}

		void ScanTree (string path, int level, bool stock, ELCraftType type)
		{
			var directory = new DirectoryInfo (path);
			var dirs = directory.GetDirectories ();
			int rootLen = rootPath.Length;
			if (level > 0) {
				rootLen += 1;
			}

			Debug.Log ($"[ELCraftSelector] ScanTree: {path} {rootLen} {directory.FullName.Length}");
			var d = new DirItem(directory.Name, directory.FullName.Substring(rootLen), dirs.Length > 0, stock, type);
			var t = new TreeView.TreeItem (d, d => (d as DirItem).name, d => (d as DirItem).hasSubdirs, level);
			dirTreeItems.Add (t);

			for (int i = 0; i < dirs.Length; i++) {
				var dir = dirs[i];
				ScanTree ($"{path}/{dir.Name}", level + 1, stock, type);
			}
		}

		bool ScanDirectory (DirItem dir)
		{
			craftItems.Clear ();
			selectedCraft = null;

			string dir_path = dir.path;
			if (!String.IsNullOrEmpty(dir_path)) {
				dir_path = $"{dir_path}/";
			}

			Debug.Log ($"[ELCraftSelector] ScanDirectory: {dir_path}");

			var directory = new DirectoryInfo ($"{rootPath}/{dir_path}");
			var files = directory.GetFiles ("*.craft");


			for (int i = 0; i < files.Length; i++) {
				var fi = files[i];
				string fp = $"{basePath}/{dir_path}{fi.Name}";
				string mp = fp.Replace (fi.Extension, ".loadmeta");
				string tp = $"{dir_path}{fi.Name}";
				if (dir.stock) {
					tp = ELCraftThumb.StockPath (dir.type, tp);
				} else {
					tp = ELCraftThumb.UserPath (dir.type, tp);
				}
				craftItems.Add (new ELCraftItem (fp, mp, tp, dir.type, dir.stock));
			}

			UIKit.UpdateListContent (craftItems);
			craftItems.Select (0);
			return true;
		}
	}
}
