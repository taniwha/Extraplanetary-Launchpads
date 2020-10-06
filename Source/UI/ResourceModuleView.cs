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
		ELResourceLine resourceLine;
		UIToggle holdToggle;
		UIToggle inToggle;
		UIToggle outToggle;
		UIToggle flowToggle;

		public override void CreateUI()
		{
			base.CreateUI ();

			ToggleGroup modeGroup;

			this.Horizontal ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.ToggleGroup (out modeGroup)

				.Add<UIEmpty> ()
					.SizeDelta (0, 0)
					.PreferredSize (32, -1)
					.Finish ()
				.Add<ELResourceLine> (out resourceLine)
					.FlexibleLayout (true, false)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<UIToggle> (out holdToggle)
					.Group (modeGroup)
					.OnValueChanged ((b) => { SetState (b, XferState.Hold); })
					.Finish ()
				.Add<UIToggle> (out inToggle)
					.Group (modeGroup)
					.OnValueChanged ((b) => { SetState (b, XferState.In); })
					.Finish ()
				.Add<UIToggle> (out outToggle)
					.Group (modeGroup)
					.OnValueChanged ((b) => { SetState (b, XferState.Out); })
					.Finish ()
				.Add<UIToggle> (out flowToggle)
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
		}

		public ELResourceModuleView Module (ResourceModule module)
		{
			resourceLine.Resource (module);
			return this;
		}
	}
}
