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

	public class ELDisposablePad : PartModule, IModuleInfo, IPartMassModifier, ELBuildControl.IBuilder, ELControlInterface, ELWorkSink
	{
		[KSPField (isPersistant = false)]
		public string SpawnTransform;
		[KSPField (isPersistant = true, guiActive = true, guiName = "Pad name")]
		public string PadName = "";

		[KSPField (isPersistant = true)]
		public bool Operational = true;

		Transform launchTransform;
		double craft_mass;

		public override string GetInfo ()
		{
			return "DisposablePad";
		}

		public string GetPrimaryField ()
		{
			return null;
		}

		public string GetModuleTitle ()
		{
			return "EL Disposable Pad";
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

		Part AttachmentPart ()
		{
			AttachNode node = part.FindAttachNode ("bottom");
			if (node == null || node.attachedPart == null) {
				node = part.srfAttachNode;
			}
			return node.attachedPart;
		}

		void PostCapture ()
		{
			Part attachPart = AttachmentPart ();
			if (vessel.rootPart == part) {
				// the pad is (somehow) the vessel's root part that will
				// cause problems when the pad is destroyed, so set the part
				// to which the spawn will be attached as the root
				attachPart.SetHierarchyRoot (attachPart);
			}
			Part spawnRoot = control.craftRoot;
			part.children.Remove (spawnRoot);

			attachPart.addChild (spawnRoot);
			spawnRoot.parent = attachPart;

			AttachNode spawnNode = FindNode (spawnRoot);
			spawnNode.attachedPart = attachPart;
			AttachNode attachNode = attachPart.FindAttachNodeByPart (part);
			if (attachNode != null) {
				attachNode.attachedPart = spawnRoot;
			}

			spawnRoot.CreateAttachJoint (attachPart.attachMode);

			control.DestroyPad ();
		}

		void SetLaunchTransform ()
		{
			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				//Debug.LogFormat ("[EL] launchTransform:{0}:{1}",
				//				   launchTransform, SpawnTransform);
			}
			if (launchTransform == null) {
				launchTransform = part.FindModelTransform ("EL launch pos");
			}
			if (launchTransform == null) {
				Transform t = part.transform;
				GameObject launchPos = new GameObject ("EL launch pos");
				launchPos.transform.parent = t;
				launchPos.transform.position = t.position;
				launchPos.transform.rotation = t.rotation;
				launchTransform = launchPos.transform;
				//Debug.LogFormat ("[EL] launchPos {0}", launchTransform);
			}
		}

		AttachNode FindNode (Part p)
		{
			AttachNode node;

			if ((node = p.FindAttachNode ("top")) != null) {
				if (node.attachedPart == null) {
					return node;
				}
			}
			if ((node = p.FindAttachNode ("bottom")) != null) {
				if (node.attachedPart == null) {
					return node;
				}
			}
			for (int i = 0; i < p.attachNodes.Count; i++) {
				if (node.attachedPart == null) {
					return node;
				}
			}
			return null;
		}

		public Transform PlaceShip (ShipConstruct ship, ELBuildControl.Box vessel_bounds)
		{
			SetLaunchTransform ();

			float angle;
			Vector3 axis;

			Part rootPart = ship.parts[0].localRoot;
			Transform rootXform = rootPart.transform;
			AttachNode n = FindNode (rootPart);

			Vector3 nodeAxis = rootXform.TransformDirection(n.orientation);
			Quaternion launchRot = launchTransform.rotation;
			launchRot *= Quaternion.FromToRotation (nodeAxis, Vector3.up);
			launchRot.ToAngleAxis (out angle, out axis);
			Vector3 pos = rootXform.TransformPoint (n.position);
			Vector3 shift = -pos;
			//Debug.Log (String.Format ("[EL] pos: {0} shift: {1}", pos, shift));
			shift += launchTransform.position;
			//Debug.Log (String.Format ("[EL] shift: {0}", shift));
			rootXform.Translate (shift, Space.World);
			rootXform.RotateAround (launchTransform.position, axis, angle);
			return launchTransform;
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
			control.PostCapture = PostCapture;
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
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

		public double CalculateWork ()
		{
			return control.CalculateWork();
		}
	}
}
