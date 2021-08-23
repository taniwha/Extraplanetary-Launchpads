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

	public class ELRenameDialog : Window
	{
		public interface IRenamable {
			string Name { get; set; }
			void OnRename ();
		}

		UIInputField nameField;
		UIButton okButton;
		UIButton cancelButton;
		UIText version;

		IRenamable target;

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Title (ELLocalization.Rename)
				.Vertical()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.PreferredSizeFitter (true, true)
				.Anchor (AnchorPresets.MiddleCenter)
				.Pivot (PivotPresets.TopLeft)
				.PreferredWidth (500)
				.SetSkin ("EL.Default")

				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false,true)
					.FlexibleLayout (true, false)
					.Add<UIText> ()
						.Text (ELLocalization.Name)
						.Alignment (TextAlignmentOptions.Left)
						.FlexibleLayout (false, false)
						.Finish ()
					.Add<UIEmpty> ()
						.PreferredSize (5, -1)
						.Finish ()
					.Add<UIInputField> (out nameField)
						.OnSubmit (onSubmit)
						.OnFocusGained (SetControlLock)
						.OnFocusLost (ClearControlLock)
						.FlexibleLayout (true, false)
						.SizeDelta (0,0)
						.Finish ()
					.Finish ()
				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false,false)
					.FlexibleLayout (true, false)
					.Add<UIEmpty> ()
						.FlexibleLayout (1, -1)
						.Finish ()
					.Add<UIButton> (out okButton)
						.Text (ELLocalization.OK)
						.OnClick (onOK)
						.FlexibleLayout (1, -1)
						.Finish ()
					.Add<UIEmpty> ()
						.FlexibleLayout (2, -1)
						.Finish ()
					.Add<UIButton> (out cancelButton)
						.Text (ELLocalization.Cancel)
						.OnClick (onCancel)
						.FlexibleLayout (1, -1)
						.Finish ()
					.Add<UIEmpty> ()
						.FlexibleLayout (1, -1)
						.Finish ()
					.Finish ()
				.Add<UIText> (out version)
					.Text (ELVersionReport.GetVersion ())
					.Alignment (TextAlignmentOptions.Center)
					.Size (12)
					.FlexibleLayout (true, false)
					.Finish ()
				.Finish ();
		}

		void onSubmit (string str)
		{
			target.Name = str;
			target.OnRename ();
			SetActive (false);
			ClearControlLock ();
		}

		void onOK ()
		{
			target.Name = nameField.text;
			target.OnRename ();
			ClearControlLock ();
			SetActive (false);
		}

		void onCancel ()
		{
			ClearControlLock ();
			SetActive (false);
		}

		public override void Style ()
		{
			base.Style ();
		}

		public void SetVisible (bool visible)
		{
			gameObject.SetActive (visible);
		}

		ELRenameDialog SetTarget (IRenamable target)
		{
			this.target = target;
			nameField.text = target.Name;
			SetActive (true);
			return this;
		}

		static void SetControlLock (string str = null)
		{
			InputLockManager.SetControlLock ("ELRenameDialog_lock");
		}

		static void ClearControlLock (string str = null)
		{
			InputLockManager.RemoveControlLock ("ELRenameDialog_lock");
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
			SetActive (false);
		}

		static ELRenameDialog renameDialog;

		public static void OpenDialog (string title, IRenamable target)
		{
			if (!renameDialog) {
				renameDialog = UIKit.CreateUI<ELRenameDialog> (ELWindowManager.appCanvasRect, "ELRenameDialog");
			}
			renameDialog.SetTarget (target).Title (title);
		}
	}
}
