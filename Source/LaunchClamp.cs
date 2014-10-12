using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {
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
		}
	}
}
