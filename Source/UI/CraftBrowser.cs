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
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public enum ELCraftType { VAB, SPH, SubAss, Part };
	public delegate void SelectFileCallback(string fullPath,
											ELCraftType craftType);
	public delegate void CancelledCallback();

	public class ELCraftBrowser : Window
	{
		SelectFileCallback OnFileSelected;
		CancelledCallback OnBrowseCancelled;

		public ELCraftType craftType { get; private set; }

		ELCraftTypeSelector typeSelector;
		ELCraftSelector craftSelector;
		UIButton loadButton;

		public override void CreateUI ()
		{
			base.CreateUI ();

			this.Title (ELLocalization.SelectCraft)
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.PreferredSizeFitter (true, true)
				.Anchor (AnchorPresets.MiddleCenter)
				.Pivot (PivotPresets.TopLeft)
				.Add <ELCraftTypeSelector> (out typeSelector)
					.OnSelectionChanged (CraftTypeSelected)
					.FlexibleLayout (true, true)
					.Finish ()
				.Add <ELCraftSelector> (out craftSelector)
					.OnSelectionChanged (OnSelectionChanged)
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
						.OnClick (LoadSelection)
						.Finish ()
					.Finish ()
				.Add<UIText> ()
					.Text (ELVersionReport.GetVersion ())
					.Alignment (TextAlignmentOptions.Center)
					.Size (12)
					.FlexibleLayout (true, false)
					.Finish ()
				.Finish ();
		}

		void Cancel ()
		{
			SetActive (false);
			OnBrowseCancelled ();
		}

		void LoadSelection ()
		{
			SetActive (false);
			craftSelector.LoadCraft ();
		}

		void OnSelectionChanged (bool canLoad, bool doLoad)
		{
			if (canLoad && doLoad) {
				LoadSelection ();
			} else {
				loadButton.interactable = canLoad;
			}
		}

		void CraftTypeSelected ()
		{
			craftType = typeSelector.craftType;
			var stockCraft = typeSelector.stockCraft;
			craftSelector.SetCraftType (craftType, stockCraft);
		}

		void SetDelegates (SelectFileCallback onFileSelected,
						   CancelledCallback onCancel)
		{
			OnFileSelected = onFileSelected;
			OnBrowseCancelled = onCancel;
			craftSelector.SetDelegates (onFileSelected, onCancel);
		}

		void SetCraftType (ELCraftType craftType, bool stock)
		{
			SetActive (true);
			typeSelector.SetCraftType (craftType, stock);
			craftSelector.SetCraftType (craftType, stock);
		}

		protected override void Awake ()
		{
			GameEvents.onGameSceneSwitchRequested.Add (onGameSceneSwitchRequested);
		}

		protected override void OnDestroy ()
		{
			GameEvents.onGameSceneSwitchRequested.Remove (onGameSceneSwitchRequested);
		}

		void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data)
		{
			// scene change is effectively Cancel :)
			Cancel ();
		}

		static ELCraftBrowser craftBrowser;

		public static void OpenDialog (ELCraftType craftType, string path,
									   SelectFileCallback onFileSelected,
									   CancelledCallback onCancel)
		{
			if (!craftBrowser) {
				craftBrowser = UIKit.CreateUI<ELCraftBrowser> (ELWindowManager.appCanvasRect, "ELCraftBrowser");
			}
			craftBrowser.SetDelegates (onFileSelected, onCancel);
			craftBrowser.SetCraftType (craftType, false);
		}
	}
}
