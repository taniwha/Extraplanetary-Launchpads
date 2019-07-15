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
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public class ELLaunchpad : PartModule, IModuleInfo, IPartMassModifier, ELBuildControl.IBuilder, ELControlInterface, ELWorkSink, ELRenameWindow.IRenamable
	{
		[KSPField (isPersistant = false)]
		public float SpawnHeightOffset = 0.0f;
		[KSPField (isPersistant = false)]
		public string SpawnTransform;
		[KSPField (isPersistant = true, guiActive = true, guiName = "Pad name")]
		public string PadName = "";

		[KSPField] public float EVARange = 0;

		[KSPField (isPersistant = true)]
		public bool Operational = true;

		public float spawnOffset = 0;
		Transform launchTransform;
		double craft_mass;

		public override string GetInfo ()
		{
			return "Launchpad";
		}

		public string GetPrimaryField ()
		{
			return null;
		}

		public string GetModuleTitle ()
		{
			return "EL Launchpad";
		}

		public Callback<Rect> GetDrawModulePanelCallback ()
		{
			return null;
		}

		public bool canBuild
		{
			get {
				return canOperate;
			}
		}

		public bool capture
		{
			get {
				return true;
			}
		}

		public ELBuildControl control
		{
			get;
			private set;
		}

		public new Vessel vessel
		{
			get {
				return base.vessel;
			}
		}

		public new Part part
		{
			get {
				return base.part;
			}
		}

		public string Name
		{
			get {
				return PadName;
			}
			set {
				PadName = value;
			}
		}

		public string LandedAt { get { return ""; } }
		public string LaunchedFrom { get { return ""; } }

		public void PadSelection_start ()
		{
		}

		public void PadSelection ()
		{
		}

		public void PadSelection_end ()
		{
		}

		public void Highlight (bool on)
		{
			if (on) {
				part.SetHighlightColor (XKCDColors.LightSeaGreen);
				part.SetHighlight (true, false);
			} else {
				part.SetHighlightDefault ();
			}
		}

		public bool isBusy
		{
			get {
				return control.state > ELBuildControl.State.Planning;
			}
		}

		public bool canOperate
		{
			get { return Operational; }
			set { Operational = value; }
		}

		public void SetCraftMass (double mass)
		{
			craft_mass = mass;
		}

		public float GetModuleMass (float defaultMass, ModifierStagingSituation sit)
		{
			return (float) craft_mass;
		}

		public ModifierChangeWhen GetModuleMassChangeWhen ()
		{
			return ModifierChangeWhen.CONSTANTLY;
		}

		public void SetShipTransform (Transform shipTransform, Part rootPart)
		{
		}

		public Transform PlaceShip (Transform shipTransform, Box vessel_bounds)
		{
			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				//Debug.Log (String.Format ("[EL] launchTransform:{0}:{1}",
				//						  launchTransform, SpawnTransform));
			}
			if (launchTransform == null) {
				launchTransform = part.FindModelTransform ("EL launch pos");
			}
			if (launchTransform == null) {
				Vector3 offset = Vector3.up * (SpawnHeightOffset + spawnOffset);
				Transform t = part.partTransform.Find("model");
				GameObject launchPos = new GameObject ("EL launch pos");
				launchPos.transform.SetParent (t, false);;
				launchPos.transform.localPosition = offset;
				launchTransform = launchPos.transform;
				//Debug.Log (String.Format ("[EL] launchPos {0}",
				//						  launchTransform));
			}

			float height = shipTransform.position.y - vessel_bounds.min.y;
			Vector3 pos = new Vector3 (0, height, 0);
			Quaternion rot = shipTransform.rotation;
			shipTransform.rotation = launchTransform.rotation * rot;
			shipTransform.position = launchTransform.TransformPoint (pos);
			return launchTransform;
		}

		public void PostBuild (Vessel craftVessel)
		{
		}

		public override void OnSave (ConfigNode node)
		{
			control.Save (node);
		}

		public override void OnLoad (ConfigNode node)
		{
			control.Load (node);
		}

		public override void OnAwake ()
		{
			control = new ELBuildControl (this);
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
			}
			if (EVARange > 0) {
				EL_Utils.SetupEVAEvent (Events["ShowRenameUI"], EVARange);
			}
			control.OnStart ();
		}

		void OnDestroy ()
		{
			if (control != null) {
				control.OnDestroy ();
			}
		}

		[KSPEvent (guiActive = true, guiName = "Hide UI", active = false)]
		public void HideUI ()
		{
			ELBuildWindow.HideGUI ();
		}

		[KSPEvent (guiActive = true, guiName = "Show UI", active = false)]
		public void ShowUI ()
		{
			ELBuildWindow.ShowGUI ();
			ELBuildWindow.SelectPad (control);
		}

		[KSPEvent (guiActive = true, guiActiveEditor = true,
				   guiName = "Rename", active = true)]
		public void ShowRenameUI ()
		{
			ELRenameWindow.ShowGUI (this);
		}

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}

		public void DoWork (double kerbalHours)
		{
			control.DoWork (kerbalHours);
		}

		public bool isActive
		{
			get {
				return control.isActive;
			}
		}

		public ELVesselWorkNet workNet
		{
			get {
				return control.workNet;
			}
			set {
				control.workNet = value;
			}
		}

		public double CalculateWork ()
		{
			return control.CalculateWork();
		}

		public void OnRename ()
		{
			ELBuildWindow.updateCurrentPads ();
		}
	}
}
