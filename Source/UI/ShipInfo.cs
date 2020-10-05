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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {
	[KSPAddon (KSPAddon.Startup.EditorAny, true)]
	public class ELShipInfo : MonoBehaviour
	{
		public static ELShipInfoWindow shipInfoWindow { get; set; }

		int rebuild_list_wait_frames = 0;

		private IEnumerator WaitAndRebuildList ()
		{
			while (--rebuild_list_wait_frames > 0) {
				yield return null;
			}
			ShipConstruct ship = EditorLogic.fetch.ship;
			//Debug.LogFormat ("ELShipInfo.WaitAndRebuildList: {0}", ship);

			BuildCost buildCost = null;

			if (ship == null || ship.parts == null || ship.parts.Count < 1
				|| ship.parts[0] == null) {
				yield break;
			}

			if (ship.parts.Count > 0) {
				Part root = ship.parts[0].localRoot;

				buildCost = new BuildCost ();
				buildCost.addPart (root);
				foreach (Part p in root.GetComponentsInChildren<Part>()) {
					if (p != root) {
						buildCost.addPart (p);
					}
				}
			}
			if (shipInfoWindow) {
				shipInfoWindow.UpdateInfo (buildCost);
			}
		}

		void RebuildList()
		{
			// some parts/modules fire the event before doing things
			const int wait_frames = 2;
			if (rebuild_list_wait_frames < wait_frames) {
				rebuild_list_wait_frames += wait_frames;
				if (rebuild_list_wait_frames == wait_frames) {
					StartCoroutine (WaitAndRebuildList ());
				}
			}
		}

		void onEditorShipModified (ShipConstruct ship)
		{
			RebuildList ();
		}

		void onEditorRestart ()
		{
		}

		private void onEditorLoad (ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
		{
			Debug.LogFormat ("ELShipInfo.onEditorLoad: {0} {1}", ship, loadType);
			RebuildList ();
		}

		void Awake ()
		{
			GameEvents.onEditorShipModified.Add (onEditorShipModified);
			GameEvents.onEditorRestart.Add (onEditorRestart);
			GameEvents.onEditorLoad.Add (onEditorLoad);
		}

		void OnDestroy ()
		{
			GameEvents.onEditorShipModified.Remove (onEditorShipModified);
			GameEvents.onEditorRestart.Remove (onEditorRestart);
			GameEvents.onEditorLoad.Remove (onEditorLoad);
		}
	}
}
