using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExLP {
	class ExShipInfoEventCatcher : PartModule
	{
		public ExShipInfo shipinfo;

		[KSPEvent (guiActive=false, active = true)]
		void OnResourcesModified (BaseEventData data)
		{
			Part part = data.Get<Part> ("part");
			Debug.Log (String.Format ("[EL GUI] res modify: {0}", part));
			shipinfo.resources.RemovePart (part);
			shipinfo.resources.AddPart (part);
		}
		[KSPEvent (guiActive=false, active = true)]
		void OnMassModified (BaseEventData data)
		{
			Part part = data.Get<Part> ("part");
			float oldmass = data.Get<float> ("oldmass");
			Debug.Log (String.Format ("[EL GUI] mass modify: {0} {1} {2}", part, oldmass, part.mass));
			shipinfo.mass -= oldmass;
			shipinfo.mass += part.mass;
		}

		public override void OnSave (ConfigNode node)
		{
			node.ClearData ();
			node.name = "IGNORE_THIS_NODE";
		}
	}

	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExShipInfo : MonoBehaviour
	{

		public VesselResources resources;
		public double mass;
		double rpDensity;
		static Rect winpos;

		void addPart (Part part)
		{
			Debug.Log (String.Format ("[EL GUI] attach: {0}", part));
			resources.AddPart (part);
			mass += part.mass;

			ExShipInfoEventCatcher ec = (ExShipInfoEventCatcher)part.AddModule ("ExShipInfoEventCatcher");
			ec.shipinfo = this;
		}

		void removePart (Part part)
		{
			Debug.Log (String.Format ("[EL GUI] remove: {0}", part));
			resources.RemovePart (part);
			mass -= part.mass;

			ExShipInfoEventCatcher ec = part.GetComponent<ExShipInfoEventCatcher> ();
			part.RemoveModule (ec);
		}

		void addRootPart (Part root)
		{
			Debug.Log (String.Format ("[EL GUI] root: {0}", root));
			resources = new VesselResources (root);
			mass = root.mass;

			ExShipInfoEventCatcher ec = (ExShipInfoEventCatcher)root.AddModule ("ExShipInfoEventCatcher");
			ec.shipinfo = this;
		}

		void onRootPart (GameEvents.FromToAction<ControlTypes, ControlTypes>h)
		{
			var ship = EditorLogic.fetch.ship;

			if (ship.parts.Count > 0) {
				if (resources == null) {
					Part root = ship.parts[0];
					addRootPart (root);
					foreach (Part p in root.GetComponentsInChildren<Part>()) {
						if (p != root) {
							addPart (p);
						}
					}
					enabled = true;
				}
			} else {
				Debug.Log (String.Format ("[EL GUI] new"));
				resources = null;
				mass = 0;
				enabled = false;
			}
		}
		void onPartAttach (GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part part = host_target.host;
			Part parent = host_target.target;
			if (resources == null) {
				addRootPart (parent);
				enabled = true;
			}
			foreach (Part p in part.GetComponentsInChildren<Part>()) {
				addPart (p);
			}
		}
		void onPartRemove (GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part part = host_target.target;
			if (resources != null) {
				foreach (Part p in part.GetComponentsInChildren<Part>()) {
					removePart (p);
				}
			}
		}
		void Awake ()
		{
			GameEvents.onInputLocksModified.Add (onRootPart);
			GameEvents.onPartAttach.Add (onPartAttach);
			GameEvents.onPartRemove.Add (onPartRemove);

			PartResourceDefinition rp_def;
			rp_def = PartResourceLibrary.Instance.GetDefinition ("RocketParts");
			rpDensity = rp_def.density;

			enabled = false;
		}
		void OnDestroy ()
		{
			GameEvents.onInputLocksModified.Remove (onRootPart);
			GameEvents.onPartAttach.Remove (onPartAttach);
			GameEvents.onPartRemove.Remove (onPartRemove);
		}
		void OnGUI ()
		{
			if (winpos.x == 0 && winpos.y == 0) {
				winpos.x = Screen.width / 2;
				winpos.y = Screen.height / 2;
				winpos.width = 300;
				winpos.height = 100;
			}
			winpos = GUILayout.Window (1324, winpos, InfoWindow,
									  "Build Resources",
									  GUILayout.MinWidth (200));
		}
		void InfoWindow (int windowID)
		{
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			double dmass = Math.Round (mass, 4);
			double parts = Math.Round (mass / rpDensity, 4);
			GUILayout.Label ("Dry mass:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t ({1}u)", dmass, parts));
			GUILayout.EndHorizontal ();
			var reslist = resources.resources.Keys.ToList ();
			reslist.Sort ();
			double rpmass = 0;
			double resource_mass = 0;
			double total_mass = mass;
			foreach (string res in reslist) {
				double amount = resources.ResourceAmount (res);
				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (res);
				double resmass = amount * res_def.density;

				if (res_def.resourceTransferMode == ResourceTransferMode.NONE
					|| res_def.resourceFlowMode == ResourceFlowMode.NO_FLOW) {
					rpmass += resmass;
				} else {
					resource_mass += resmass;
				}
				total_mass += resmass;

				double damount = Math.Round (amount, 4);
				double dresmass = Math.Round (resmass, 4);
				GUILayout.BeginHorizontal ();
				GUILayout.Label (String.Format ("{0}:", res));
				GUILayout.FlexibleSpace ();
				GUILayout.Label (String.Format ("{0}u ({1}t)", damount, dresmass));
				GUILayout.EndHorizontal ();
			}

			dmass = Math.Round (rpmass, 4);
			parts = Math.Round (rpmass / rpDensity, 4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Extra Hull mass:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t ({1}u)", dmass, parts));
			GUILayout.EndHorizontal ();

			dmass = Math.Round (mass + rpmass, 4);
			parts = Math.Round ((mass + rpmass) / rpDensity, 4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Required RocketParts:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t ({1}u)", dmass, parts));
			GUILayout.EndHorizontal ();

			dmass = Math.Round (resource_mass, 4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resources mass:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t", dmass));
			GUILayout.EndHorizontal ();

			dmass = Math.Round (total_mass, 4);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Total mass:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t", dmass));
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
