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

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	public class ELMainWindow : Window
	{
		ELStatusBar statusBar;
		ELPadView padView;
		ELCraftView craftView;
		ELBuildView buildView;
		ELTransferView transferView;

		public override void CreateUI()
		{
			base.CreateUI ();
			Title (ELVersionReport.GetVersion ())
				.Vertical()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.PreferredSizeFitter(true, true)
				.Anchor(AnchorPresets.MiddleCenter)
				.Pivot(PivotPresets.TopLeft)
				.PreferredWidth(695)

				.Add<ELStatusBar>(out statusBar, "StatusBar")
					.Finish()
				.Add<ELPadView>(out padView, "PadView")
					.Finish()
				.Add<ELCraftView>(out craftView, "CraftView")
					.Finish()
				.Add<ELBuildView>(out buildView, "BuildView")
					.Finish()
				.Add<ELTransferView>(out transferView, "TransferView")
					.Finish()
				.Finish();

			craftView.gameObject.SetActive (false);
			buildView.gameObject.SetActive (false);
			transferView.gameObject.SetActive (false);
			padView.AddListener (craftView.UpdateControl);
			padView.AddListener (buildView.UpdateControl);
			padView.AddListener (transferView.UpdateControl);
		}

		public override void Style ()
		{
			base.Style ();
		}

		public void SetVessel (Vessel vessel)
		{
			statusBar.SetVessel (vessel);
			padView.SetVessel (vessel);
		}

		public void SetControl (ELBuildControl control)
		{
			statusBar.SetVessel (control.builder.vessel);
			padView.SetControl (control);
		}

		public void SetVisible (bool visible)
		{
			gameObject.SetActive (visible);
		}
	}
}
