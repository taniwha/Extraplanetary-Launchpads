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

namespace ExtraplanetaryLaunchpads {

	public class ELResourceDisplay : Layout
	{
		List<IResourceLine> resources;

		public override void CreateUI()
		{
			this.Vertical ()
				.ChildForceExpand(false,false)
				.FlexibleLayout (true, false)
				.SizeDelta (0, 0);
		}

		void RebuildContent ()
		{
			var contentRect = rectTransform;
			int childCount = contentRect.childCount;
			int childIndex = 0;
			int itemIndex = 0;
			int itemCount = resources.Count;

			while (childIndex < childCount && itemIndex < itemCount) {
				var child = contentRect.GetChild (childIndex);
				var item = child.GetComponent<ELResourceLine> ();
				item.Resource (resources[itemIndex]);
				++childIndex;
				++itemIndex;
			}
			while (childIndex < childCount) {
				var go = contentRect.GetChild (childIndex++).gameObject;
				Destroy (go);
			}
			while (itemIndex < itemCount) {
				this.Add<ELResourceLine> ()
					.Resource (resources[itemIndex])
					.FlexibleLayout(true, false)
					.SizeDelta (0, 0)
					.Finish();
				++itemIndex;
			}
		}

		public ELResourceDisplay Resources (List<IResourceLine> resources)
		{
			this.resources = resources;
			RebuildContent ();
			return this;
		}
	}
}
