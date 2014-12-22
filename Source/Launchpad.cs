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

namespace ExLP {

	public class ExLaunchPad : PartModule, ExBuildControl.IBuilder
	{
		[KSPField (isPersistant = false)]
		public float SpawnHeightOffset = 0.0f;
		[KSPField (isPersistant = false)]
		public string SpawnTransform;
		[KSPField (isPersistant = true, guiActive = true, guiName = "Pad name")]
		public string PadName = "";

		public float spawnOffset = 0;
		Transform launchTransform;
		float base_mass;

		public bool capture
		{
			get {
				return true;
			}
		}

		public ExBuildControl control
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
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (control);
		}

		public void SetCraftMass (double mass)
		{
			part.mass = base_mass + (float) mass;
		}

		public Transform PlaceShip (ShipConstruct ship, ExBuildControl.Box vessel_bounds)
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
				Transform t = part.transform;
				GameObject launchPos = new GameObject ("EL launch pos");
				launchPos.transform.parent = t;
				launchPos.transform.position = t.position;
				launchPos.transform.rotation = t.rotation;
				launchPos.transform.position += t.TransformDirection (offset);
				launchTransform = launchPos.transform;
				//Debug.Log (String.Format ("[EL] launchPos {0}",
				//						  launchTransform));
			}

			float angle;
			Vector3 axis;
			launchTransform.rotation.ToAngleAxis (out angle, out axis);

			Vector3 pos = ship.parts[0].transform.position;
			Vector3 shift = new Vector3 (-pos.x, -vessel_bounds.min.y, -pos.z);
			//Debug.Log (String.Format ("[EL] pos: {0} shift: {1}", pos, shift));
			shift += launchTransform.position;
			//Debug.Log (String.Format ("[EL] shift: {0}", shift));
			ship.parts[0].transform.Translate (shift, Space.World);
			ship.parts[0].transform.RotateAround (launchTransform.position,
												  axis, angle);
			return launchTransform;
		}

		public override void OnSave (ConfigNode node)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			control.Save (node);
			if (base_mass != 0) {
				node.AddValue ("baseMass", base_mass);
			}
		}

		public override void OnLoad (ConfigNode node)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			control.Load (node);
			if (node.HasValue ("baseMass")) {
				float.TryParse (node.GetValue ("baseMass"), out base_mass);
			} else {
				base_mass = part.mass;
			}
		}

		public override void OnAwake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				Events["HideUI"].active = false;
				Events["ShowUI"].active = false;
				return;
			}
			control = new ExBuildControl (this);
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
			}
			control.OnStart ();
		}

		void OnDestroy ()
		{
			control.OnDestroy ();
		}

		[KSPEvent (guiActive = true, guiName = "Hide UI", active = false)]
		public void HideUI ()
		{
			ExBuildWindow.HideGUI ();
		}

		[KSPEvent (guiActive = true, guiName = "Show UI", active = false)]
		public void ShowUI ()
		{
			ExBuildWindow.ShowGUI ();
			ExBuildWindow.SelectPad (control);
		}

		[KSPEvent (guiActive = true, guiName = "Rename", active = true)]
		public void ShowRenameUI ()
		{
			ExRenameWindow.ShowGUI (this);
		}

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExRenameWindow: MonoBehaviour
	{
		private static ExLaunchPad padInstance = null;
		private static ExRenameWindow windowInstance = null;
		private static bool gui_enabled = false;
		private static Rect windowpos = new Rect(Screen.width * 0.35f,Screen.height * 0.1f,1,1);
		private static string newPadName;

		void Awake ()
		{
			windowInstance = this;
			enabled = true;
			gui_enabled = false;
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
		}

		public static void ShowGUI (ExLaunchPad pad)
		{
			padInstance = pad;
			newPadName = pad.PadName;
			gui_enabled = true;
			if (windowInstance != null) {
				windowInstance.enabled = true;
			}
		}

		void WindowGUI (int windowID)
		{
			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Rename launchpad: ");
			newPadName = GUILayout.TextField (newPadName, 20);
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("OK")) {
				padInstance.PadName = newPadName;
				gui_enabled = false;
				ExBuildWindow.updateCurrentPads ();
			}
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Cancel")) {
				gui_enabled = false;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		void OnGUI ()
		{
			if (gui_enabled) {
				//enabled = true;
				GUI.skin = HighLogic.Skin;
				windowpos = GUILayout.Window (GetInstanceID (),
					windowpos, WindowGUI,
					"Rename Launchpad",
					GUILayout.Width(500));
			}
			if (enabled && windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
				InputLockManager.SetControlLock ("EL_Rename_window_lock");
			} else {
				InputLockManager.RemoveControlLock ("EL_Rename_window_lock");
			}
		}
	}
}
