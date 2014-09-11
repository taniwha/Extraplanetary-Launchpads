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
		[KSPField (isPersistant = true)]
		public string PadName = "";

		public float spawnOffset = 0;
		Transform launchTransform;

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

		public Transform GetLaunchTransform ()
		{
			if (launchTransform != null) {
				return launchTransform;
			}
			if (SpawnTransform != "") {
				launchTransform = part.FindModelTransform (SpawnTransform);
				Debug.Log (String.Format ("[EL] launchTransform:{0}:{1}",
										  launchTransform, SpawnTransform));
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
				Debug.Log (String.Format ("[EL] launchPos {0}",
										  launchTransform));
			}
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
			control = new ExBuildControl (this);
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

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}
	}
}
