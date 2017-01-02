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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KSP.IO;
using KSP.UI.Screens;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	public enum ELCraftType { VAB, SPH, SubAss };

	public class ELCraftBrowser : CraftBrowserDialog
	{
		const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
		static GameObject prefab;
		static MethodInfo buildCraftList = typeof(CraftBrowserDialog).GetMethod ("BuildCraftList", bindingFlags);

		[SerializeField]
		private Toggle tabSub;

		static EditorFacility []craftFacility = new EditorFacility[] {
			EditorFacility.VAB,
			EditorFacility.SPH,
			EditorFacility.None,
		};

		static ELCraftType []facilityType = new ELCraftType[] {
			ELCraftType.SubAss,
			ELCraftType.VAB,
			ELCraftType.SPH,
		};

		public ELCraftType craftType
		{
			get {
				return facilityType[(int) facility];
			}
		}

		// CraftBrowserDialog has a mask that hides anything outside the
		// default buttons, both visually and for mouse clicks. Since the
		// button count is going from 2 to 3, set the button widths to 80
		// instead of the default 120, and adjust positions accordingly.
		static void AdjustToggles(Transform parent)
		{
			Vector2 ap;
			Vector2 sd;
			bool first = true;

			ap.x = 0; ap.y = 0;
			sd.x = 0; sd.y = 0;
			foreach (Transform child in parent) {
				var rect = child.GetComponent<RectTransform> ();
				if (first) {
					first = false;
					ap = rect.anchoredPosition;
					sd = rect.sizeDelta;
					sd.x = 80;
				}
				rect.anchoredPosition = ap;
				rect.sizeDelta = sd;

				ap.x += sd.x;
			}
		}

		// Set the label text for both enabled and disabled versions of the
		// subassembly toggle button.
		static void SetSubLabels (Transform toggleSub)
		{
			foreach (Transform xform in toggleSub) {
				if (xform.name == "Label") {
					var subLabel = xform;
					var text = subLabel.GetComponent<TMPro.TextMeshProUGUI> ();
					text.text = "Sub";
				}
			}
		}

		static void CreatePrefab ()
		{
			var cbdType = typeof (CraftBrowserDialog);
			var cbdFields = cbdType.GetFields (bindingFlags);

			var cbdPrefab = AssetBase.GetPrefab ("CraftBrowser");
			prefab = Instantiate (cbdPrefab);
			prefab.transform.name = "ELCraftBrowser";
			prefab.name = "ELCraftBrowser";

			var cbd = prefab.GetComponent<CraftBrowserDialog> ();
			var ELcb = prefab.AddComponent<ELCraftBrowser> ();

			foreach (var field in cbdFields) {
				field.SetValue(ELcb, field.GetValue(cbd));
			}

			var toggleMask = prefab.transform.Find ("ToggleMask");
			var toggleSPH = toggleMask.Find ("ToggleSPH");
			var tabSPH = toggleSPH.GetComponent<Toggle> ();

			var toggleSub = Instantiate (toggleSPH.gameObject);
			toggleSub.transform.name = "ToggleSub";
			toggleSub.name = "ToggleSub";
			toggleSub.transform.SetParent (toggleMask);
			SetSubLabels (toggleSub.transform);
			ELcb.tabSub = toggleSub.GetComponent<Toggle> ();
			ELcb.tabSub.group = tabSPH.group;

			AdjustToggles (toggleMask);

			Destroy (cbd);
		}

		public static ELCraftBrowser Spawn (ELCraftType type, string profile, SelectFileCallback onFileSelected, CancelledCallback onCancel, bool showMergeOption)
		{
			if (prefab == null) {
				CreatePrefab ();
			}

			var cb = Instantiate (prefab).GetComponent<ELCraftBrowser> ();

			cb.transform.SetParent (DialogCanvasUtil.DialogCanvasRect, false);

			cb.facility = craftFacility[(int) type];
			cb.showMergeOption = showMergeOption;
			cb.OnBrowseCancelled = onCancel;
			cb.OnFileSelected = onFileSelected;
			cb.title = "Select a craft to load";
			cb.profile = profile;

			return cb;
		}

		public new void Start ()
		{
			if (String.IsNullOrEmpty (profile)) {
				// the process of creating the prefab causes Start() to be
				// called, which results in a stray Ships directory because
				// profile is not yet valid and there is a bug in KSP's
				// CraftBrowserDialog.
				return;
			}
			tabSub.isOn = facility == EditorFacility.None;
			tabSub.onValueChanged.AddListener (onSubTabToggle);
			base.Start ();
			if (tabSub.isOn) {
				LoadSubassemblies ();
			}
		}

		void LoadSubassemblies ()
		{
			craftSubfolder = "../Subassemblies";
			var diff = HighLogic.CurrentGame.Parameters.Difficulty;

			bool stock = diff.AllowStockVessels;
			diff.AllowStockVessels = false;

			buildCraftList.Invoke (this, null);

			diff.AllowStockVessels = stock;
		}

		void onSubTabToggle (bool st)
		{
			if (st) {
				facility = EditorFacility.None;
				LoadSubassemblies ();
			}
		}
	}
}
