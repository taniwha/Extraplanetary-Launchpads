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

	public class ResourceModule : IResourceLine
	{
		string resourceName;
#region IResourceLine
		public string ResourceName { get { return set.name; } }
		public string ResourceInfo { get { return null; } }
		public double ResourceFraction
		{
			get {
				double maxAmount = set.ResourceCapacity (resourceName);
				if (maxAmount <= 0) {
					return 0;
				}
				double amount = set.ResourceAmount (resourceName);
				return amount / maxAmount;
			}
		}
		public double BuildAmount
		{
			get { return set.ResourceAmount (resourceName); }
		}
		public double AvailableAmount
		{
			get {
				return set.ResourceCapacity (resourceName);
			}
		}
#endregion
		public string name { get { return set.name; } }
		public RMResourceSet set { get; set; }
		public XferState xferState { get; private set; }
		public bool flowState {
			get { return set.GetFlowState (resourceName); }
			set { set.SetFlowState (resourceName, value); }
		}

		public ResourceModule (RMResourceSet set, string resourceName)
		{
			this.set = set;
			this.resourceName = resourceName;
			xferState = XferState.Hold;
		}

		public class Dict : Dictionary<string, ResourceModule> { }
		public class List : List<ResourceModule>, UIKit.IListObject
		{
			public Layout Content { get; set; }
			public RectTransform RectTransform
			{
				get { return Content.rectTransform; }
			}

			public void Create (int index)
			{
				Content
					.Add<ELResourceModuleView> ()
						.Module (this[index])
						.Finish ()
					;
			}

			public void Update (GameObject obj, int index)
			{
				var view = obj.GetComponent<ELResourceModuleView> ();
				view.Module (this[index]);
			}
		}
	}
}