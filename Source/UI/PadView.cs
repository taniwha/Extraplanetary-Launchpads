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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	using OptionData = TMP_Dropdown.OptionData;

	public class ELPadView : LayoutAnchor
	{
		public class PadEvent : UnityEvent<ELBuildControl> {}
		PadEvent padEvent = new PadEvent ();

		public void AddListener (UnityAction<ELBuildControl> action)
		{
			padEvent.AddListener (action);
			action.Invoke (control);
		}


		Vessel vessel;
		ELVesselWorkNet worknet;
		ELBuildControl _control;

		public ELBuildControl control
		{
			get { return _control; }
			private set {
				_control = value;
				padEvent.Invoke (_control);
				launchpadView.SetControl (control);
				surveyView.SetControl (control);
			}
		}

		List<OptionData> padNames;
		List<ELBuildControl> padControls;

		UIDropdown padSelector;
		MiniToggle highlightPad;

		ELPadLaunchpadView launchpadView;
		ELPadSurveyView surveyView;

		protected override void Awake ()
		{
			ELBuildControl.onBuildStateChanged.Add (onBuildStateChanged);
			ELBuildControl.onPadRenamed.Add (onPadRenamed);
		}

		protected override void OnDestroy ()
		{
			ELBuildControl.onBuildStateChanged.Remove (onBuildStateChanged);
			ELBuildControl.onPadRenamed.Remove (onPadRenamed);
		}

		void onBuildStateChanged (ELBuildControl control)
		{
			if (control == this.control) {
				padEvent.Invoke (control);
			}
		}

		void onPadRenamed (ELBuildControl control, string oldName, string newName)
		{
			bool changed = false;
			for (int i = padControls.Count; i-- > 0; ) {
				if (padControls[i] == control) {
					padNames[i].text = PadName (padControls[i].builder, i);
					changed = true;
				}
			}

			if (changed) {
				padSelector.Options (padNames);
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
					.Add<UIDropdown> (out padSelector, "PadSelector")
						.OnValueChanged (SelectPad)
						.FlexibleLayout (true, true)
						.Finish ()
					.Add<MiniToggle> (out highlightPad, "HighlightPad")
						.OnValueChanged (HighlightPad)
						.FlexibleLayout (false, true)
						.PreferredSize (25, 25)
						.Finish ()
					.Finish ()
				.Add<LayoutAnchor> ()
					.DoPreferredHeight (true)
					.DoMinHeight (true)
					.Anchor (rightMin, rightMax)
					.SizeDelta (0, 0)
					.Add<ELPadLaunchpadView> (out launchpadView)
						.Finish ()
					.Add<ELPadSurveyView> (out surveyView)
						.Finish ()
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
			HighlightPad (false);
			control = padControls[index];
			worknet.selectedPad = control.builder.ID;
			HighlightPad (highlightPad.isOn);
			UpdateMenus (gameObject.activeInHierarchy);
		}

		void HighlightPad (bool on)
		{
			if (control != null) {
				control.builder.Highlight (on);
			}
		}

		void Update ()
		{
			// KSP's part highlighter forces highlighting off when the mouse
			// is no longer hovering over the part, so need to turn it on each
			// frame
			// FIXME there might be a better way
			if (control != null && highlightPad.isOn) {
				control.builder.Highlight (true);
			}
		}

		void UpdateMenus (bool visible)
		{
			if (padControls == null) {
				return;
			}
			for (int i = padControls.Count; i-- > 0; ) {
				bool selected = padControls[i] == control;
				padControls[i].builder.UpdateMenus (visible && selected);
			}
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

		string PadName (ELBuildControl.IBuilder pad, int index)
		{
			if (String.IsNullOrEmpty (pad.Name)) {
				return $"{ELLocalization.Pad}-{index}";
			} else {
				return pad.Name;
			}
		}

		void BuildPadList ()
		{
			padNames.Clear ();
			padControls.Clear ();
			worknet = null;
			if (!vessel) {
				return;
			}

			var pads = vessel.FindPartModulesImplementing<ELBuildControl.IBuilder> ();
			int control_index = -1;

			if (pads.Count < 1) {
				padSelector.interactable = false;
			} else {
				worknet = FindWorkNet (vessel);
				if (worknet != null && control == null) {
					control = FindControl (vessel, worknet.selectedPad);
				}

				for (int i = 0; i < pads.Count; i++) {
					padNames.Add (new OptionData (PadName (pads[i], i)));
					padControls.Add (pads[i].control);
					if (control == pads[i].control) {
						control_index = i;
					}
				}
			}
			if (control == null) {
				control_index = 0;
			}
			if (control_index < padControls.Count) {
				control = padControls[control_index];
			}
			padSelector.Options (padNames);
			padSelector.SetValueWithoutNotify (control_index);

			UpdateMenus (gameObject.activeInHierarchy);
		}

		public void SetVessel (Vessel vessel)
		{
			this.vessel = vessel;
			control = null;
			BuildPadList ();
		}

		public void SetControl (ELBuildControl control)
		{
			vessel = control.builder.vessel;
			this.control = control;
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
			UpdateMenus (gameObject.activeInHierarchy);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			HighlightPad (false);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			UpdateMenus (false);
		}
	}
}
