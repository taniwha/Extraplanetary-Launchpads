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

	public class ELResourceManagerView : Layout
	{
		ResourceGroup.List resourceGroups;
		ResourceGroup.Dict resourceGroupDict;
		ScrollView resourceView;
		UIButton transferButton;

		public override void CreateUI()
		{
			if (resourceGroups == null) {
				resourceGroups = new ResourceGroup.List ();
				resourceGroupDict = new ResourceGroup.Dict ();
			}

			base.CreateUI ();

			var leftMin = new Vector2 (0, 0);
			var leftMax = new Vector2 (0.5f, 1);
			var rightMin = new Vector2 (0.5f, 0);
			var rightMax = new Vector2 (1, 1);

			UIScrollbar scrollbar;
			this.Vertical ()
				.ControlChildSize(true, true)
				.ChildForceExpand(false,false)

				.Add<LayoutPanel>()
					.Background("KodeUI/Default/background")
					.BackgroundColor(UnityEngine.Color.white)
					.Vertical()
					.Padding(8)
					.ControlChildSize(true, true)
					.ChildForceExpand(false, false)
					.Anchor(AnchorPresets.HorStretchTop)
					.FlexibleLayout(true,true)
					.PreferredHeight(300)
					.Add<ScrollView> (out resourceView)
						.Horizontal (false)
						.Vertical (true)
						.Horizontal()
						.ControlChildSize (true, true)
						.ChildForceExpand (false, true)
						.Add<UIScrollbar> (out scrollbar, "Scrollbar")
							.Direction(Scrollbar.Direction.BottomToTop)
							.PreferredWidth (15)
							.Finish ()
						.Finish ()
					.Finish ()
				.Add<UIButton> (out transferButton)
					.Text (ELLocalization.StartTransfer)
					.OnClick (ToggleTransfer)
					.FlexibleLayout (true, true)
					.Finish()
				.Finish();

			resourceView.VerticalScrollbar = scrollbar;
			resourceView.Viewport.FlexibleLayout (true, true);
			resourceView.Content
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.HorStretchTop)
				.PreferredSizeFitter(true, false)
				.WidthDelta(0)
				.Finish ();

			resourceGroups.Content = resourceView.Content;
		}

		void onTransferableChanged ()
		{
			transferButton.interactable = false;
		}

		void ToggleTransfer ()
		{
		}

		void RebuildResources (RMResourceManager resourceManager)
		{
			var set = new HashSet<string> ();
			foreach (var s in resourceManager.resourceSets) {
				foreach (string r in s.resources.Keys) {
					set.Add (r);
				}
			}

			foreach (var res in set) {
				if (resourceGroupDict.ContainsKey (res)) {
					Debug.Log ($"[ELResourceManagerView] RebuildResources updating {res}");
					resourceGroupDict[res].BuildModules (resourceManager);
				} else {
					Debug.Log ($"[ELResourceManagerView] RebuildResources adding {res}");
					var group = new ResourceGroup (res, resourceManager);
					resourceGroupDict[res] = group;
					resourceGroups.Add (group);
				}
			}

			for (int i = resourceGroups.Count; i-- > 0; ) {
				var group = resourceGroups[i];
				if (!set.Contains (group.resourceName)) {
					Debug.Log ($"[ELResourceManagerView] RebuildResources removing {group.resourceName}");
					resourceGroupDict.Remove (group.resourceName);
					resourceGroups.RemoveAt (i);
				}
			}

			UIKit.UpdateListContent (resourceGroups);
		}

		IEnumerator WaitAndRebuildResources ()
		{
			int count = 5;
			while (count-- > 0) {
				yield return null;
			}
			var vessel = FlightGlobals.ActiveVessel;
			if (!vessel) {
				yield break;
			}
			SetVessel (vessel);
		}

		public void SetVessel (Vessel vessel)
		{
			var parts = vessel.parts;
			Part rootPart = parts[0].localRoot;
			var manager = new RMResourceManager (parts, rootPart);
			manager.CreateXferControl ();
			manager.xferControl.onTransferableChanged += onTransferableChanged;
			onTransferableChanged ();
			RebuildResources (manager);
		}
	}
}
