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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	using OptionData = TMP_Dropdown.OptionData;

	public class ELPadSurveyView : LayoutPanel
	{
		ELSurveyStation surveyStation;

		Layout siteControl;
		UIDropdown siteSelector;
		UIButton renameSite;
		UIText warningNoSite;

		List<OptionData> siteNames;
		List<SurveySite> siteList;

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.StretchAll)
				.SizeDelta (0, 0)
				.Sprite(SpriteLoader.GetSprite("KodeUI/Default/background"))
				.Add<Layout> (out siteControl)
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.FlexibleLayout (true, true)
					.Add<UIDropdown> (out siteSelector, "SiteSelector")
						.OnValueChanged (SelectSite)
						.FlexibleLayout (true, true)
						.Finish ()
					.Add<UIButton> (out renameSite)
						.Text (ELLocalization.Rename)
						.OnClick (RenameSite)
						.FlexibleLayout (false, true)
						.SizeDelta (0, 0)
						.Finish ()
					.Finish ()
				.Add<UIText> (out warningNoSite, "SiteWarning")
					.Text (ELLocalization.WarningNoSite)
					.Alignment (TextAlignmentOptions.Center)
					.FlexibleLayout (true, true)
					.SizeDelta (0, 0)
					.Finish ()
				.Finish ();
			siteNames = new List<OptionData> ();
			siteList = new List<SurveySite> ();
		}

		void SelectSite (int index)
		{
			var site = siteList[index];
			surveyStation.SetSite (site);
		}

		void RenameSite ()
		{
			if (surveyStation.site != null) {
				var site = surveyStation.site;
				ELRenameDialog.OpenDialog (ELLocalization.RenameSite, site);
			}
		}

		public override void Style ()
		{
		}

		void ShowWanring ()
		{
			Debug.Log($"[ELPadSurveyView] ShowWanring");
			siteControl.SetActive (false);
			warningNoSite.SetActive (true);
			if (surveyStation.control.state == ELBuildControl.State.Complete) {
				warningNoSite.Text (ELLocalization.WarningNoSite2);
			} else {
				warningNoSite.Text (ELLocalization.WarningNoSite);
			}
		}

		void ShowControl ()
		{
			Debug.Log($"[ELPadSurveyView] ShowControl");
			siteControl.SetActive (true);
			warningNoSite.SetActive (false);
		}

		void BuildSiteList ()
		{
			siteNames.Clear ();
			if (surveyStation == null) {
				if (gameObject.activeSelf) {
					SetActive (false);
				}
				return;
			}
			if (!gameObject.activeSelf) {
				SetActive (true);
			}
			siteList = surveyStation.available_sites;

			// FIXME make available_sites never null
			if (siteList == null || siteList.Count < 1) {
				ShowWanring ();
			} else {
				ShowControl ();
				for (int i = 0; i < siteList.Count; i++) {
					siteNames.Add (new OptionData (siteList[i].SiteName));
				}
				siteSelector.Options (siteNames);
			}
		}

		IEnumerator WaitAndBuildSiteList ()
		{
			yield return null;
			BuildSiteList ();
		}

		public void SetVessel (Vessel vessel)
		{
			BuildSiteList ();
		}

		public void SetControl (ELBuildControl control)
		{
			surveyStation = control?.builder as ELSurveyStation;
			BuildSiteList ();
		}

		void onSiteRemoved (SurveySite site)
		{
			StartCoroutine (WaitAndBuildSiteList ());
		}

		void onSiteAdded (SurveySite site)
		{
			StartCoroutine (WaitAndBuildSiteList ());
		}

		protected override void OnEnable ()
		{
			base.OnEnable ();
			ELSurveyTracker.onSiteRemoved.Add (onSiteRemoved);
			ELSurveyTracker.onSiteRemoved.Add (onSiteAdded);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			ELSurveyTracker.onSiteRemoved.Remove (onSiteRemoved);
			ELSurveyTracker.onSiteRemoved.Remove (onSiteAdded);
		}
	}
}
