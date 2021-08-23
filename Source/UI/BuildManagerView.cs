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
using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELBuildManagerView : Layout, TabController.ITabItem
	{
		ELStatusBar statusBar;
		ELPadView padView;
		ELBuildCraftView craftView;
		ELBuildView buildView;
		ELTransferView transferView;

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)

				.Add<ELStatusBar>(out statusBar, "StatusBar")
					.Finish()
				.Add<ELPadView>(out padView, "PadView")
					.Finish()
				.Add<ELBuildCraftView>(out craftView, "BuildCraftView")
					.Finish()
				.Add<ELBuildView>(out buildView, "BuildView")
					.Finish()
				.Add<ELTransferView>(out transferView, "TransferView")
					.Finish()
				;

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

		protected override void OnEnable ()
		{
			GameEvents.onVesselChange.Add (SetVessel);
		}

		protected override void OnDisable ()
		{
			GameEvents.onVesselChange.Remove (SetVessel);
		}
#region TabController.ITabItem
		public string TabName { get { return ELLocalization.BuildManager; } }
		public bool TabEnabled { get { return padView.control != null; } }
		public void SetTabVisible (bool visible)
		{
			SetActive (visible);
		}
#endregion
	}
}
