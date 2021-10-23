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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using ExtraplanetaryLaunchpads_KACWrapper;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	using OptionData = TMP_Dropdown.OptionData;

	public class ELSettingsWindow : Window
	{
		ToggleText preferBlizzy;
		ToggleText createKACAlarms;
		ToggleText showCraftHull;
		ToggleText debugCraftHull;
		UIDropdown kacAction;

		void SelectKACAction (int index)
		{
			switch (index) {
				case 0:
					ELSettings.KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
					break;
				case 1:
					ELSettings.KACAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
					break;
				case 2:
					ELSettings.KACAction = KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
					break;
				case 3:
					ELSettings.KACAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
					break;
			}
		}

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Title (ELVersionReport.GetVersion ())
				.Vertical()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.PreferredSizeFitter(true, true)
				.Anchor(AnchorPresets.MiddleCenter)
				.Pivot(PivotPresets.TopLeft)
				.PreferredWidth(450)
				.SetSkin ("EL.Default")

				.Add<ToggleText> (out preferBlizzy)
					.Text (ELLocalization.PreferBlizzy)
					.OnValueChanged ((b) => { ELSettings.PreferBlizzy = b; })
					.Finish ()
				.Add<ToggleText> (out createKACAlarms)
					.Text (ELLocalization.CreateKACAlarms)
					.OnValueChanged ((b) => { ELSettings.use_KAC = b; })
					.Finish ()
				.Add<ToggleText> (out showCraftHull)
					.Text (ELLocalization.ShowCraftHull)
					.OnValueChanged ((b) => { ELSettings.ShowCraftHull = b; })
					.Finish ()
				.Add<ToggleText> (out debugCraftHull)
					.Text (ELLocalization.DebugCraftHull)
					.OnValueChanged ((b) => { ELSettings.DebugCraftHull = b; })
					.Finish ()
				.Add<UIDropdown> (out kacAction, "KACAction")
					.OnValueChanged (SelectKACAction)
					.FlexibleLayout (true, true)
					.Finish ()

				.Finish ();

			titlebar
				.Add<UIButton> ()
					.OnClick (CloseWindow)
					.Anchor (AnchorPresets.TopRight)
					.Pivot (new Vector2 (1.25f, 1.25f))
					.SizeDelta (16, 16)
					.Finish();
				;
		}

		void CloseWindow ()
		{
			ELWindowManager.HideSettingsWindow ();
		}

		public override void Style ()
		{
			base.Style ();
		}

		List<OptionData> kacActionNames;

		public void SetVisible (bool visible)
		{
			if (!visible) {
				ELSettings.Save ();
			} else {
				if (kacActionNames == null) {
					kacActionNames = new List<OptionData> ();
					kacActionNames.Add (new OptionData (ELLocalization.KillWarpMessage));
					kacActionNames.Add (new OptionData (ELLocalization.KillWarpOnly));
					kacActionNames.Add (new OptionData (ELLocalization.MessageOnly));
					kacActionNames.Add (new OptionData (ELLocalization.PauseGame));

					kacAction.Options (kacActionNames);
				}
				int actionIndex = 0;
				switch (ELSettings.KACAction) {
					case KACWrapper.KACAPI.AlarmActionEnum.KillWarp:
						actionIndex = 0;
						break;
					case KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly:
						actionIndex = 1;
						break;
					case KACWrapper.KACAPI.AlarmActionEnum.MessageOnly:
						actionIndex = 2;
						break;
					case KACWrapper.KACAPI.AlarmActionEnum.PauseGame:
						actionIndex = 3;
						break;
				}
				kacAction.SetValueWithoutNotify (actionIndex);
			}
			gameObject.SetActive (visible);
		}
	}
}
