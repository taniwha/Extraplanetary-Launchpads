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
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using KSP.UI.Screens;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftItem
	{
		CraftProfileInfo info;
		public List<string> MissingParts { get; private set; }
		public string fullPath { get; private set; }
		public string name { get { return info.shipName; } }
		public string description { get { return info.description; } }
		public float cost { get { return info.totalCost; } }
		public int partCount { get { return info.partCount; } }
		public int stageCount { get { return info.stageCount; } }
		public string message { get; private set; }
		public string thumbPath { get; private set; }
		public ELCraftType type { get; private set; }
		public bool isStock { get; private set; }
		public bool canLoad
		{
			get {
				if (info.compatibility != VersionCompareResult.COMPATIBLE) {
					return false;
				}
				// EL might one day allow building from "stolen plans", so
				// only actually missing parts blocks loading (tech and
				// missions are irrelevant).
				return MissingParts.Count == 0;
			}
		}
		ConfigNode _node;
		public ConfigNode node
		{
			get {
				if (_node == null) {
					_node = ConfigNode.Load (fullPath);
				}
				return _node;
			}
			set { _node = value; }
		}

		public ELCraftItem (string craftPath, string metaPath, string thumbPath, ELCraftType craftType, bool isStock)
		{
			fullPath = craftPath;
			info = new CraftProfileInfo ();
			if (File.Exists (metaPath)) {
				var node = ConfigNode.Load (metaPath);
				info.Load (node);
			} else {
				var node = ConfigNode.Load (craftPath);
				info.LoadDetailsFromCraftFile (node, craftPath);
				this.node = node;
			}
			// Find the parts that are actually missing and not just blocked
			// by tech or missions.
			MissingParts = new List<string> ();
			for (int i = 0; i < info.UnavailableShipParts.Count; i++) {
				string partName = info.UnavailableShipParts[i];
				if (PartLoader.getPartInfoByName (partName) == null) {
					// UnavailableShipParts is already uniqued
					MissingParts.Add (partName);
				}
			}
			info.description = info.description.Replace ('¨', '\n');
			this.thumbPath = thumbPath;
			type = craftType;
			this.isStock = isStock;
		}

		public class Dict : Dictionary<string, ELCraftItem> { }
		public class List : List<ELCraftItem>, UIKit.IListObject
		{
			ToggleGroup group;
			public UnityAction<ELCraftItem> onSelected { get; set; }
			public Layout Content { get; set; }
			public RectTransform RectTransform
			{
				get { return Content.rectTransform; }
			}

			public void Create (int index)
			{
				Content
					.Add<ELCraftItemView> ()
						.Group (group)
						.OnSelected (onSelected)
						.Craft (this[index])
						.Finish ()
					;
			}

			public void Update (GameObject obj, int index)
			{
				var view = obj.GetComponent<ELCraftItemView> ();
				view.Craft (this[index]);
			}

			public List (ToggleGroup group)
			{
				this.group = group;
			}

			public void Select (int index)
			{
				if (index >= 0 && index < Count) {
					group.SetAllTogglesOff (false);
					var child = Content.rectTransform.GetChild (index);
					var view = child.GetComponent<ELCraftItemView> ();
					view.Select ();
				}
			}
		}
	}
}
