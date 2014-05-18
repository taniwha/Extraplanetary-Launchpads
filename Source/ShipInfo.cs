using KSPAPIExtensions;
using KSPAPIExtensions.PartMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExLP {
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExShipInfo : MonoBehaviour
	{
		static Rect winpos;
		static bool showGUI = true;

		int parts_count;
		public BuildCost buildCost;
		Vector2 scrollPosR, scrollPosO;

		public static void ToggleGUI ()
		{
			showGUI = !showGUI;
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				winpos.x = pos.x;
				winpos.y = pos.y;
				winpos.width = pos.z;
				winpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out showGUI);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			Quaternion pos;
			pos.x = winpos.x;
			pos.y = winpos.y;
			pos.z = winpos.width;
			pos.w = winpos.height;
			node.AddValue ("rect", KSPUtil.WriteQuaternion (pos));
			node.AddValue ("visible", showGUI);
		}

		void addPart (Part part)
		{
			//Debug.Log (String.Format ("[EL GUI] attach: {0}", part));
			buildCost.addPart (part);
            parts_count++;
		}

        [PartMessageListener(typeof(PartMassChanged), relations: PartRelationship.Unknown, scenes: GameSceneFilter.AnyEditor)]
        private void MassChanged(float mass)
        {
            RebuildList();
        }

        [PartMessageListener(typeof(PartHeirachyChanged), relations: PartRelationship.Unknown, scenes: GameSceneFilter.AnyEditor)]
        [PartMessageListener(typeof(PartResourcesChanged), relations: PartRelationship.Unknown, scenes: GameSceneFilter.AnyEditor)]
        public void RebuildList()
        {
            buildCost = null;
            parts_count = 0;

			var ship = EditorLogic.fetch.ship;

			if (ship.parts.Count > 0) {
				Part root = ship.parts[0];

                buildCost = new BuildCost();
				addPart (root);
				foreach (Part p in root.GetComponentsInChildren<Part>()) {
					if (p != root) {
						addPart (p);
					}
				}
			}
        }

		void Awake ()
		{
            PartMessageService.Register<ExShipInfo>(this);
		}

		void OnGUI ()
		{
            if (!showGUI || buildCost == null)
                return;

			if (winpos.x == 0 && winpos.y == 0) {
				winpos.x = Screen.width / 2;
				winpos.y = Screen.height / 2;
				winpos.width = 300;
				winpos.height = 100;
			}
			string ver = ExSettings.GetVersion ();
			winpos = GUILayout.Window (GetInstanceID (), winpos, InfoWindow,
									  "Build Resources: " + ver,
									  GUILayout.MinWidth (200));
		}

		private void UnitLabel (string title, double amount, string units)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (title + ":");
			GUILayout.FlexibleSpace ();
			GUILayout.Label (String.Format ("{0}{1}", amount, units));
			GUILayout.EndHorizontal ();
		}

		private void MassLabel (string title, double mass)
		{
			mass = Math.Round (mass, 4);
			UnitLabel (title, mass, "t");
		}

		private Vector2 ResourcePanel (string title,
									   List<BuildCost.BuildResource> resources,
									   Vector2 scrollPos)
		{
			GUILayout.Label (title + ":");
			GUILayout.BeginVertical (GUILayout.Height (100));
			scrollPos = GUILayout.BeginScrollView (scrollPos);
			foreach (var res in resources) {
				double damount = Math.Round (res.amount, 4);
				double dresmass = Math.Round (res.mass, 4);
				GUILayout.BeginHorizontal ();
				GUILayout.Label (String.Format ("{0}:", res.name));
				GUILayout.FlexibleSpace ();
				GUILayout.Label (String.Format ("{0}u ({1}t)", damount, dresmass));
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			GUILayout.EndVertical ();
			return scrollPos;
		}

		void InfoWindow (int windowID)
		{
			var cost = buildCost.cost;
			double required_mass = 0;
			double resource_mass = 0;
			double kerbalHours = 0;

			foreach (var res in cost.required) {
				kerbalHours += res.kerbalHours;
				required_mass += res.mass;
			}
			kerbalHours = Math.Round (kerbalHours, 4);

			foreach (var res in cost.optional) {
				resource_mass += res.mass;
			}

			GUILayout.BeginVertical ();

			MassLabel ("Dry mass", buildCost.mass);
			MassLabel ("Resource mass", resource_mass);
			MassLabel ("Total mass", required_mass + resource_mass);
			UnitLabel ("Build Time", kerbalHours, "Kh");

			cost.optional.Sort ();
			GUILayout.Label (" ");
			scrollPosR = ResourcePanel ("Required", cost.required, scrollPosR);
			scrollPosO = ResourcePanel ("Optional", cost.optional, scrollPosO);

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
