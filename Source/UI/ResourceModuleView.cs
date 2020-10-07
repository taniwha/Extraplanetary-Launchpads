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

	public class ELResourceModuleView : Layout
	{
		ResourceModule module;

		ELResourceLine resourceLine;
		ToggleText holdToggle;
		ToggleText inToggle;
		ToggleText outToggle;
		MiniToggle flowToggle;

		public override void CreateUI()
		{
			base.CreateUI ();

			ToggleGroup modeGroup;

			this.Horizontal ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.Padding (0, 8, 0, 0)
				.ToggleGroup (out modeGroup)
				.Add<UIEmpty> ()
					.PreferredSize (32, -1)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ELResourceLine> (out resourceLine)
					.FlexibleLayout (true, false)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<ToggleText> (out holdToggle)
					.Group (modeGroup)
					.Text (ELLocalization.Hold)
					.OnValueChanged ((b) => { SetState (b, XferState.Hold); })
					.Finish ()
				.Add<ToggleText> (out inToggle)
					.Group (modeGroup)
					.Text (ELLocalization.In)
					.OnValueChanged ((b) => { SetState (b, XferState.In); })
					.Finish ()
				.Add<ToggleText> (out outToggle)
					.Group (modeGroup)
					.Text (ELLocalization.Out)
					.OnValueChanged ((b) => { SetState (b, XferState.Out); })
					.Finish ()
				.Add<UIEmpty> ()
					.PreferredSize (8, -1)
					.FlexibleLayout (false, true)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<MiniToggle> (out flowToggle)
					.OnValueChanged (SetFlowState)
					.Finish ()
				;
		}

		void SetState (bool on, XferState state)
		{
			if (on) {
			}
		}

		void SetFlowState (bool on)
		{
			module.flowState = on;
		}

		public ELResourceModuleView Module (ResourceModule module)
		{
			this.module = module;
			resourceLine.Resource (module);
			flowToggle.SetIsOnWithoutNotify (module.flowState);
			return this;
		}
	}
}
