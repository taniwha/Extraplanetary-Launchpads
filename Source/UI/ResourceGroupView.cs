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

	public class ELResourceGroupView : Layout
	{
		ResourceGroup resource;

		UIToggle openToggle;
		ELResourceLine resourceLine;
		Layout moduleView;
		public Layout ModuleView { get { return moduleView; } }

		public override void CreateUI()
		{
			base.CreateUI ();

			this.Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)
				.Add<Layout> ()
					.Horizontal ()
					.ControlChildSize(true, true)
					.ChildForceExpand(false,false)

					.Add<UIEmpty> ()
						.PreferredSize (32, -1)
						.FlexibleLayout (false, true)
						.SizeDelta (0, 0)
						.Add<UIToggle> (out openToggle)
							.OnValueChanged (SetOpen)
							.AspectRatioSizeFitter (AspectRatioFitter.AspectMode.FitInParent, 1)
							.Finish ()
						.Finish ()
					.Add<ELResourceLine> (out resourceLine)
						.FlexibleLayout (true, false)
						.SizeDelta (0, 0)
						.Finish ()
					.Finish ()
				.Add<Layout> (out moduleView)
					.Vertical ()
					.ControlChildSize(true, true)
					.ChildForceExpand(false,false)
					.Finish ()
				;
			moduleView.SetActive (false);
		}

		void SetOpen (bool open)
		{
			moduleView.SetActive (open);
			resource.isOpen = open;
		}

		public ELResourceGroupView Resource (ResourceGroup resource)
		{
			this.resource = resource;
			resourceLine.Resource (resource);
			resource.modules.Content = moduleView;
			UIKit.UpdateListContent (resource.modules);
			SetOpen (resource.isOpen);
			openToggle.SetIsOnWithoutNotify (resource.isOpen);
			return this;
		}
	}
}
