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
	public class ExtendingLaunchClamp : LaunchClamp
	{
		Transform anchor;
		Vector3 [] points = null;

		Collider FindCollider (Transform xform)
		{
			var col = xform.GetComponent<Collider>();
			if (!col) {
				foreach (Transform x in xform) {
					col = FindCollider (x);
					if (col) {
						return col;
					}
				}
			}
			return col;
		}
		Vector3 MakePoint (Transform xform, Vector3 c, float x, float z)
		{
			var p = new Vector3 (c.x + x, c.y, c.z + z);
			return p - xform.position;
		}
		public new void OnPutToGround (PartHeightQuery qr)
		{
			base.OnPutToGround (qr);
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			Debug.Log (String.Format ("[EL ELC] OnPutToGround qr: {0}", qr.lowestPoint));
			foreach (var p in qr.lowestOnParts) {
				Debug.Log (String.Format ("[EL ELC] OnPutToGround qr.lop: {0}", p));
			}
			anchor = part.FindModelTransform (trf_anchor_name);
			var col = FindCollider (anchor);
			if (col) {
				var min = col.bounds.min;
				var max = col.bounds.max;
				var size = max - min;
				Debug.Log (String.Format ("[EL ELC] OnPutToGround: {0} {1} {2}", min, max, size));
				points = new Vector3[] {
					MakePoint (anchor, min, 0,      0),
					MakePoint (anchor, min, size.x, 0),
					MakePoint (anchor, min, 0,      size.z),
					MakePoint (anchor, min, size.x, size.z),
				};
				Debug.Log (String.Format ("[EL ELC] OnPutToGround: {0}", points));
			}
		}
		float DistanceToGround (Vector3 pos)
		{
			var dir = Vector3.down;
			dir = anchor.TransformDirection (dir);
			pos = vessel.vesselTransform.TransformPoint (pos);
			RaycastHit hit;
			if (Physics.Raycast (pos, dir, out hit, 100, 32769)) {
				return hit.distance;
			}
			return -1;
		}
		void ExtendTower ()
		{
			if (points == null) {
				return;
			}
			float dist = -1;
			for (int i = 0; i < 4; i++) {
				RaycastHit hit;
				var start = anchor.TransformPoint (points[i]);
				if (Physics.Raycast (start, -anchor.up, out hit, 100, 32768)) {
					if (dist < 0 || i == 0) {
						dist = hit.distance;
					} else {
						dist = Math.Min (dist, hit.distance);
					}
				}
				Debug.Log (String.Format ("[EL ELC] {0} {1} {2}", points[i], hit.distance, dist));
			}
			if (dist < 0) {
				// didn't hit the ground. too far away.
				return;
			}
			var tower = part.FindModelTransform (trf_towerStretch_name);
			var baseHeight = Vector3.Distance (anchor.position, tower.position);
			height = baseHeight + dist;
			Debug.Log (String.Format ("[EL ELC] {0} {1}", baseHeight, height));
		}

		void ShutdownGenerator ()
		{
			foreach (var mod in part.Modules.OfType<ModuleGenerator> ()) {
				ModuleGenerator gen = mod as ModuleGenerator;
				gen.isAlwaysActive = true;
				gen.generatorIsActive = false;
				gen.Events["Shutdown"].active = false;
				gen.Events["Activate"].active = false;
				gen.Fields["efficiency"].guiActive = false;
				gen.inputList.Clear ();
				gen.outputList.Clear ();
			}
		}

		internal IEnumerator<YieldInstruction> WaitAndShutdownGenerator ()
		{
			yield return null;
			if (part == null || part.Modules == null) {
				// the part may have been destroyed (build cost calculation)
				yield break;
			}
			ShutdownGenerator ();
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				base.OnStart (state);
				return;
			}
			Debug.Log (String.Format ("[EL ELC] OnStart: {0} {1}", HighLogic.LoadedSceneIsFlight, points));
			if (HighLogic.LoadedSceneIsFlight) {
				ExtendTower ();
			}
			base.OnStart (state);
			if (HighLogic.LoadedSceneIsFlight) {
				if ((state & StartState.Landed) == StartState.None
					  || (!vessel.landedAt.Equals ("LaunchPad")
						  && !vessel.landedAt.Equals ("Runway"))) {
					ShutdownGenerator ();
					// do it again next frame just in case
					StartCoroutine (WaitAndShutdownGenerator ());
				}
			}
		}
	}
}
