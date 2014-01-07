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
			shipinfo.buildCost.removePartMassless (part);
			shipinfo.buildCost.addPartMassless (part);
		}
		[KSPEvent (guiActive=false, active = true)]
		void OnMassModified (BaseEventData data)
		{
			Part part = data.Get<Part> ("part");
			float oldmass = data.Get<float> ("oldmass");
			Debug.Log (String.Format ("[EL GUI] mass modify: {0} {1} {2}",
									  part, oldmass, part.mass));
			shipinfo.buildCost.mass -= oldmass;
			shipinfo.buildCost.mass += part.mass;
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

		public BuildCost buildCost;
		double rpDensity;
		static Rect winpos;

		void addPart (Part part)
		{
			Debug.Log (String.Format ("[EL GUI] attach: {0}", part));
			buildCost.addPart (part);

			ExShipInfoEventCatcher ec = (ExShipInfoEventCatcher)part.AddModule ("ExShipInfoEventCatcher");
			ec.shipinfo = this;
		}

		void removePart (Part part)
		{
			Debug.Log (String.Format ("[EL GUI] remove: {0}", part));
			buildCost.removePart (part);

			ExShipInfoEventCatcher ec = part.GetComponent<ExShipInfoEventCatcher> ();
			part.RemoveModule (ec);
		}

		void addRootPart (Part root)
		{
			Debug.Log (String.Format ("[EL GUI] root: {0}", root));
			buildCost = new BuildCost ();
			buildCost.addPart (root);

			ExShipInfoEventCatcher ec = (ExShipInfoEventCatcher)root.AddModule ("ExShipInfoEventCatcher");
			ec.shipinfo = this;
		}

		void onRootPart (GameEvents.FromToAction<ControlTypes, ControlTypes>h)
		{
			var ship = EditorLogic.fetch.ship;

			if (ship.parts.Count > 0) {
				if (buildCost == null) {
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
				buildCost = null;
				enabled = false;
			}
		}
		void onPartAttach (GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part part = host_target.host;
			Part parent = host_target.target;
			if (buildCost == null) {
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
			if (buildCost != null) {
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
			double dmass = Math.Round (buildCost.mass, 4);
			double parts = Math.Round (buildCost.mass / rpDensity, 4);
			GUILayout.Label ("Dry mass:");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}t ({1}u)", dmass, parts));
			GUILayout.EndHorizontal ();
			var cost = buildCost.cost;
			cost.optional.Sort();
			double resource_mass = 0;
			foreach (var res in cost.optional) {
				double damount = Math.Round (res.amount, 4);
				double dresmass = Math.Round (res.mass, 4);
				resource_mass += res.mass;
				GUILayout.BeginHorizontal ();
				GUILayout.Label (String.Format ("{0}:", res.name));
				GUILayout.FlexibleSpace ();
				GUILayout.Label (String.Format ("{0}u ({1}t)", damount, dresmass));
				GUILayout.EndHorizontal ();
			}

			//FIXME this assumes only RocketParts
			var req = cost.required[0];
			dmass = Math.Round (req.mass, 4);
			parts = Math.Round (req.amount, 4);
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

			dmass = Math.Round (req.mass + resource_mass, 4);
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
