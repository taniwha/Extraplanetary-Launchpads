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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	class ELPartList : List<AvailablePart>, UIKit.IListObject
	{
		ToggleGroup group;

		public UnityAction<AvailablePart> onSelected { get; set; }
		public Layout Content { get; set; }
		public RectTransform RectTransform
		{
			get { return Content.rectTransform; }
		}

		public void Create (int index)
		{
			Content
				.Add<ELPartItemView> ()
					.Group (group)
					.OnValueCanged (on => select (index, on))
					.AvailablePart (this[index])
					.Finish ()
				;
		}

		public void Update (GameObject obj, int index)
		{
			var item = obj.GetComponent<ELPartItemView> ();
			item.AvailablePart (this[index]);
		}

		public ELPartList (ToggleGroup group)
		{
			this.group = group;
		}

		void select (int index, bool on)
		{
			if (on) {
				onSelected.Invoke (this[index]);
			}
		}
	}
}
