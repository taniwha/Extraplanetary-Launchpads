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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	public class ELStatusBar : Layout
	{
		Vessel vessel;
		ELVesselWorkNet worknet;

		UIText vesselName;
		UIText situation;
		UIText productivityLabel;
		UIText productivity;

		public override void CreateUI()
		{
			base.CreateUI ();
			Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.FlexibleLayout (true,false)
				.Add<UIText> (out vesselName, "Name")
					.Alignment (TextAlignmentOptions.Left)
					.FlexibleLayout (false, true)
					.Finish ()
				.Add<UIEmpty> ()
					.FlexibleLayout (true, true)
					.Finish ()
				.Add<UIText> (out situation, "Situation")
					.Alignment (TextAlignmentOptions.Left)
					.FlexibleLayout (false, true)
					.Finish ()
				.Add<UIEmpty> ()
					.PreferredWidth (10)
					.Finish()
				.Add<LayoutPanel> ("Productivity")
					.Horizontal()
					.Padding (3, 3, 3, 3)
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.Add<UIText> (out productivityLabel, "Label")
						.Text (ELLocalization.Productivity)
						.Finish()
					.Add<UIEmpty> ()
						.PreferredWidth (3)
						.Finish()
					.Add<UIText> (out productivity, "Value")
						.Text (ELLocalization.Productivity)
						.Alignment(TextAlignmentOptions.Right)
						.MinSize (75, -1)
						.PreferredSize (75, -1)
						.Finish()
					.Finish();
		}

		public override void Style ()
		{
			base.Style ();
		}

		void UpdateVesselName ()
		{
			if (vessel) {
				vesselName.Text (vessel.vesselName);
			} else {
				vesselName.Text ("----");
			}
		}

		void UpdateSituation ()
		{
			if (vessel) {
				situation.Text (vessel.situation.displayDescription ());
			} else {
				situation.Text ("----");
			}
		}

		void UpdateProductivity ()
		{
			string productivityStr = "----";
			Color c = UnityEngine.Color.red;		//FIXME styles

			if (worknet != null) {
				double p = worknet.Productivity;
				c = UnityEngine.Color.green;		//FIXME styles
				if (p <= 0) {
					c = UnityEngine.Color.red;		//FIXME styles
				} else if (p < 1) {
					c = UnityEngine.Color.yellow;	//FIXME styles
				}
				productivityStr = $"{p:G3}";
			}
			productivityLabel.Color (c).Style();
			if (productivity.tmpText.text != productivityStr) {
				productivity.Text(productivityStr).Color (c).Style();
			}
		}

		public void SetVessel (Vessel vessel)
		{
			this.vessel = vessel;
			if (vessel) {
				worknet = vessel.FindVesselModuleImplementing<ELVesselWorkNet> ();
			} else {
				worknet = null;
			}
			UpdateVesselName ();
			UpdateSituation ();
			UpdateProductivity ();
		}

		void onVesselRename (GameEvents.HostedFromToAction<Vessel, string> vs)
		{
			if (vs.host == vessel) {
				UpdateVesselName ();
			}
		}

		void onVesselSituationChange (GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
		{
			if (vs.host == vessel) {
				UpdateSituation ();
			}
		}

		protected override void OnEnable ()
		{
			base.OnEnable ();
			GameEvents.onVesselSituationChange.Add (onVesselSituationChange);
			GameEvents.onVesselRename.Add (onVesselRename);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			GameEvents.onVesselRename.Remove (onVesselRename);
		}

		void Update ()
		{
			UpdateProductivity ();
		}
	}
}
