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
using KSPAPIExtensions;
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

		private IEnumerator<YieldInstruction> WaitAndRebuildList (ShipConstruct ship)
		{
			yield return null;

            buildCost = null;
            parts_count = 0;

			if (ship.parts.Count > 0) {
				Part root = ship.parts[0];

                buildCost = new BuildCost ();
				addPart (root);
				foreach (Part p in root.GetComponentsInChildren<Part>()) {
					if (p != root) {
						addPart (p);
					}
				}
			}
		}

        public void RebuildList(ShipConstruct ship)
        {
			StartCoroutine (WaitAndRebuildList (ship));
        }

		void Awake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				enabled = false;
				return;
			}
			GameEvents.onEditorShipModified.Add (RebuildList);
		}

		void OnDestroy ()
		{
			GameEvents.onEditorShipModified.Remove (RebuildList);
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
			GUILayout.Label (MathUtils.ToStringSI(amount, 4, unit:units));
			GUILayout.EndHorizontal ();
		}

		private void MassLabel (string title, double mass)
		{
            GUILayout.BeginHorizontal();
            GUILayout.Label(title + ":");
            GUILayout.FlexibleSpace();
            GUILayout.Label(MathUtils.FormatMass(mass));
            GUILayout.EndHorizontal();
        }

		private Vector2 ResourcePanel (string title,
									   List<BuildCost.BuildResource> resources,
									   Vector2 scrollPos)
		{
			GUILayout.Label (title + ":");
			GUILayout.BeginVertical (GUILayout.Height (100));
			scrollPos = GUILayout.BeginScrollView (scrollPos);
			foreach (var res in resources) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label (String.Format ("{0}:", res.name));
				GUILayout.FlexibleSpace ();
				GUILayout.Label (String.Format ("{0} ({1})", res.amount.ToStringSI(4, unit:"u"), MathUtils.FormatMass(res.mass, 4)));
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
