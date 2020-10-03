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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KodeUI;

using KSP.IO;
using CBDLoadType = KSP.UI.Screens.CraftBrowserDialog.LoadType;

namespace ExtraplanetaryLaunchpads {

	public class ELBuildView : Layout
	{
		public class ProgressResource : ELResourceDisplay.BaseResourceLine
		{
			ELBuildControl control;
			BuildResource requiredResource;

			public override string ResourceInfo
			{
				get {
					if (control.paused) {
						return ELLocalization.Paused;
					}
					double fraction = ResourceFraction;
					string percent = (fraction * 100).ToString ("G4") + "%";
					double eta = CalculateETA ();
					if (eta > 0) {
						percent += " " + EL_Utils.TimeSpanString (eta);
					}
					return percent;
				}
			}
			public override double ResourceFraction
			{
				get {
					double required = requiredResource.amount;
					double built = RequiredAmount;

					if (required < 0) {
						return 0;
					}
					if (built < 0) {
						return 1;
					} else if (built <= required) {
						return (required - built) / required;
					} else {
						return 0;
					}
				}
			}

			public ProgressResource (BuildResource built, BuildResource required, RMResourceInfo pad, ELBuildControl control)
				: base (built, pad)
			{
				this.control = control;
				requiredResource = required;
			}

			public double CalculateETA ()
			{
				double frames;
				if (buildResource.deltaAmount <= 0) {
					return 0;
				}
				if (control.state == ELBuildControl.State.Building) {
					frames = buildResource.amount / buildResource.deltaAmount;
				} else {
					double remaining = buildResource.amount;
					remaining -= requiredResource.amount;
					frames = remaining / buildResource.deltaAmount;
				}
				return frames * TimeWarp.fixedDeltaTime;
			}
		}

		UIButton pauseButton;
		UIButton finalizeButton;
		UIButton cancelButton;

		ScrollView craftView;
		Layout selectedCraft;
		ELResourceDisplay resourceList;
		UIText craftName;

		List<IResourceLine> progressResources;

		ELBuildControl control;

		public override void CreateUI()
		{
			if (progressResources == null) {
				progressResources = new List<IResourceLine> ();
			}

			base.CreateUI ();

			UIScrollbar scrollbar;
			Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)

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
						.Add<UIEmpty>()
							.MinSize(15,-1)
							.Finish()
						.Add<UIText> (out craftName)
							.Finish ()
						.Finish ()
					.Add<ScrollView> (out craftView)
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
					.Finish ()
				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.FlexibleLayout (true, false)
					.SizeDelta (0, 0)
					.Add<UIButton> (out pauseButton)
						.Text (PauseResumeText ())
						.OnClick (PauseResume)
						.FlexibleLayout (true, true)
						.Finish()
					.Add<UIButton> (out finalizeButton)
						.Text (ELLocalization.FinalizeBuild)
						.OnClick (FinalizeBuild)
						.FlexibleLayout (true, true)
						.Finish()
					.Add<UIButton> (out cancelButton)
						.Text (CancelRestartText ())
						.OnClick (CancelRestart)
						.FlexibleLayout (true, true)
						.Finish()
					.Finish()
				.Finish();

			craftView.VerticalScrollbar = scrollbar;
			craftView.Viewport.FlexibleLayout (true, true);
			craftView.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				.Add<ELResourceDisplay> (out resourceList)
					.Finish ()
				.Finish ();
		}

		string PauseResumeText ()
		{
			if (control == null) {
				return ELLocalization.PauseBuild;
			}
			if (control.state == ELBuildControl.State.Building) {
				if (control.paused) {
					return ELLocalization.ResumeBuild;
				} else {
					return ELLocalization.PauseBuild;
				}
			} else {
				if (control.paused) {
					return ELLocalization.ResumeTeardown;
				} else {
					return ELLocalization.PauseTeardown;
				}
			}
		}

		void FinalizeBuild ()
		{
			control.BuildAndLaunchCraft ();
		}

		void PauseResume ()
		{
			if (control.paused) {
				control.ResumeBuild ();
			} else {
				control.PauseBuild ();
			}
		}

		void CancelRestart ()
		{
			if (control.paused) {
				control.CancelBuild ();
			} else {
				control.UnCancelBuild ();
			}
		}

		string CancelRestartText ()
		{
			if (control == null) {
				return ELLocalization.CancelBuild;
			}
			if (control.state == ELBuildControl.State.Building) {
				return ELLocalization.CancelBuild;
			} else {
				return ELLocalization.RestartBuild;
			}
		}

		public void UpdateControl (ELBuildControl control)
		{
			this.control = control;
			if (control != null
				&& (control.state == ELBuildControl.State.Building
					|| control.state == ELBuildControl.State.Canceling)) {
				gameObject.SetActive (true);
				craftName.Text (control.craftName);
				StartCoroutine (WaitAndRebuildResources ());
			} else {
				gameObject.SetActive (false);
			}
		}

		static BuildResource FindResource (List<BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void RebuildResources ()
		{
			progressResources.Clear ();
			foreach (var res in control.builtStuff.required) {
				var req = FindResource (control.buildCost.required, res.name);
				var available = control.padResources[res.name];
				var line = new ProgressResource (res, req, available, control);
				progressResources.Add (line);
			}
			resourceList.Resources (progressResources);
		}

		IEnumerator WaitAndRebuildResources ()
		{
			while (control.padResources == null) {
				yield return null;
			}
			RebuildResources ();
		}
	}
}
