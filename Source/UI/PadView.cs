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
using TMPro;

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	using OptionData = TMP_Dropdown.OptionData;

	public class ELPadView : LayoutAnchor
	{
		Vessel vessel;
		ELVesselWorkNet worknet;
		ELBuildControl _control;
		public ELBuildControl control
		{
			get { return _control; }
			private set {
				_control = value;
				if (_craftView != null) {
					_craftView.SetControl (_control);
				}
			}
		}

		List<OptionData> padNames;
		List<ELBuildControl> padControls;

		UIDropdown padSelector;
		UIToggle highlightPad;

		ELCraftView _craftView;
		public ELCraftView craftView
		{
			get { return _craftView; }
			set {
				_craftView = value;
				if (_craftView != null) {
					_craftView.SetControl (control);
				}
			}
		}

		public override void CreateUI()
		{
			base.CreateUI ();

			Vector2 leftMin = new Vector2 (0, 0);
			Vector2 leftMax = new Vector2 (0.5f, 1);
			Vector2 rightMin = new Vector2 (0.5f, 0);
			Vector2 rightMax = new Vector2 (1, 1);

			this
				.DoPreferredHeight (true)
				.DoMinHeight (true)
				.FlexibleLayout (true, false)
				.Anchor (AnchorPresets.StretchAll)
				.SizeDelta (0, 0)
				.Add<LayoutPanel> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.Anchor (leftMin, leftMax)
					.SizeDelta (0, 0)
					.Sprite(SpriteLoader.GetSprite("KodeUI/Default/background"))
					.Color(UnityEngine.Color.blue)
					.Add<UIDropdown> (out padSelector, "PadSelector")
						.OnValueChanged (SelectPad)
						.FlexibleLayout (true, true)
						.Finish ()
					.Add<UIToggle> (out highlightPad, "HighlightPad")
						.FlexibleLayout (false, true)
						.PreferredSize (25, 25)
						.Finish ()
					.Finish ()
				.Add<LayoutPanel> ()
					.Horizontal ()
					.ControlChildSize (true, true)
					.ChildForceExpand (false, false)
					.Anchor (rightMin, rightMax)
					.SizeDelta (0, 0)
					.Sprite(SpriteLoader.GetSprite("KodeUI/Default/background"))
					.Color(UnityEngine.Color.red)
					// XXX pad / survey controls
					.Finish ()
				.Finish ();

			padNames = new List<OptionData> ();
			padControls = new List<ELBuildControl> ();
		}

		public override void Style ()
		{
		}

		void SelectPad (int index)
		{
			control = padControls[index];
		}

		static ELVesselWorkNet FindWorkNet (Vessel v)
		{
			for (int i = 0; i < v.vesselModules.Count; i++) {
				var worknet = v.vesselModules[i] as ELVesselWorkNet;
				if (worknet != null) {
					return worknet;
				}
			}
			return null;
		}

		static ELBuildControl FindControl (Vessel v, uint id)
		{
			Part part = v[id];
			if (part != null) {
				var pad = part.FindModuleImplementing<ELBuildControl.IBuilder> ();
				if (pad != null) {
					return pad.control;
				}
			}
			return null;
		}

		void BuildPadList ()
		{
			padNames.Clear ();
			padControls.Clear ();
			worknet = null;
			control = null;
			if (!vessel) {
				return;
			}

			var pads = vessel.FindPartModulesImplementing<ELBuildControl.IBuilder> ();
			int control_index = -1;

			if (pads.Count < 1) {
				padSelector.interactable = false;
			} else {
				worknet = FindWorkNet (vessel);
				if (worknet != null) {
					control = FindControl (vessel, worknet.selectedPad);
				}

				for (int i = 0; i < pads.Count; i++) {
					if (String.IsNullOrEmpty (pads[i].Name)) {
						padNames.Add (new OptionData ($"{ELLocalization.Pad}-{i}"));
					} else {
						padNames.Add (new OptionData (pads[i].Name));
					}
					padControls.Add (pads[i].control);
					if (control == pads[i].control) {
						control_index = i;
					}
				}
			}
			padSelector.Options (padNames);
			padSelector.SetValueWithoutNotify (control_index);
		}

		public void SetVessel (Vessel vessel)
		{
			this.vessel = vessel;
			BuildPadList ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (v == vessel) {
			}
		}

		protected override void OnEnable ()
		{
			base.OnEnable ();
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
		}
	}
}
