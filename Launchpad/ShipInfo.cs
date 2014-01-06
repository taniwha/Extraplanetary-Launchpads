using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExLP {
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class ExShipInfo : MonoBehaviour
	{
		VesselResources resources;
		double mass;
		double rpDensity;
		static Rect winpos;

		void addPart(Part part)
		{
			resources.AddPart(part);
			mass += part.mass;
		}

		void removePart(Part part)
		{
			resources.RemovePart(part);
			mass -= part.mass;
		}

		void onRootPart(GameEvents.FromToAction<ControlTypes, ControlTypes>h)
		{
			var ship = EditorLogic.fetch.ship;

			if (ship.parts.Count > 0) {
				if (resources == null) {
					Part root = ship.parts[0];
					Debug.Log(String.Format("[EL GUI] root: {0}", root));
					resources = new VesselResources(root);
					mass = root.mass;
					enabled = true;
				}
			} else {
				Debug.Log(String.Format("[EL GUI] new"));
				resources = null;
				mass = 0;
				enabled = false;
			}
		}
		void onPartAttach(GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part part = host_target.host;
			Debug.Log(String.Format("[EL GUI] attach: {0}", part));
			if (resources != null) {
				foreach (Part p in part.GetComponentsInChildren<Part>()) {
					addPart(p);
				}
			}
		}
		void onPartRemove(GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part part = host_target.target;
			Debug.Log(String.Format("[EL GUI] remove: {0}", part));
			if (resources != null) {
				foreach (Part p in part.GetComponentsInChildren<Part>()) {
					removePart(p);
				}
			}
		}
		void Awake()
		{
			GameEvents.onInputLocksModified.Add(onRootPart);
			GameEvents.onPartAttach.Add(onPartAttach);
			GameEvents.onPartRemove.Add(onPartRemove);

			PartResourceDefinition rp_def;
			rp_def = PartResourceLibrary.Instance.GetDefinition("RocketParts");
			rpDensity = rp_def.density;

			enabled = false;
		}
		void OnDestroy()
		{
			GameEvents.onInputLocksModified.Remove(onRootPart);
			GameEvents.onPartAttach.Remove(onPartAttach);
			GameEvents.onPartRemove.Remove(onPartRemove);
		}
		void OnGUI()
		{
			if (winpos.x == 0 && winpos.y == 0) {
				winpos.x = Screen.width / 2;
				winpos.y = Screen.height / 2;
				winpos.width = 260;
				winpos.height = 100;
			}
			winpos = GUILayout.Window(1324, winpos, InfoWindow,
									  "Build Resources",
									  GUILayout.MinWidth(200));
		}
		void InfoWindow(int windowID)
		{
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal();
			double parts = mass / rpDensity;
			GUILayout.Label("Dry mass: " + Math.Round(mass,4) + "t (" + Math.Round(parts,4) + "u)");
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
		}
	}
}
