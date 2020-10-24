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
using System.Collections.Generic;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELMainWindow : Window
	{
		TabController tabController;
		ELBuildManagerView buildManager;
		ELResourceManagerView resourceManager;

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
				.SetSkin ("EL.Default")

				.Add<TabController> (out tabController)
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand(false,false)
					.FlexibleLayout (true, false)
					.Finish ()

				.Add<ELBuildManagerView> (out buildManager)
					.Finish ()
				.Add<ELResourceManagerView> (out resourceManager)
					.Finish ()
				.Finish();

			var tabItems = new List<TabController.ITabItem> () {
				buildManager,
				resourceManager,
			};
			tabController.Items (tabItems);
		}

		public override void Style ()
		{
			base.Style ();
		}

		public void SetVessel (Vessel vessel)
		{
			buildManager.SetVessel (vessel);
			resourceManager.SetVessel (vessel);
			tabController.UpdateTabStates ();
		}

		public void SetControl (ELBuildControl control)
		{
			buildManager.SetControl (control);
			tabController.UpdateTabStates ();
		}

		public void SetVisible (bool visible)
		{
			SetActive (visible);
		}
	}
}
