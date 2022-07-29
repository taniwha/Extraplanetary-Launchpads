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

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	class Points
	{
		public Dictionary<string, Vector3d> points;
		public Dictionary<string, Vector3d> bounds;
		CelestialBody body;
		public Vector3d center;

		public Points (SurveySite site)
		{
			Dictionary<string, int> counts = new Dictionary<string, int> ();
			Dictionary<string, int> bcounts = new Dictionary<string, int> ();
			int count = 0;

			points = new Dictionary<string, Vector3d> ();
			bounds = new Dictionary<string, Vector3d> ();

			body = site.Body;

			center = Vector3d.zero;
			foreach (var stake in site) {
				if (stake.stake == null) {
					continue;
				}
				string key = ELSurveyStake.StakeUses[stake.use];
				var pos = stake.stake.part.transform.position;
				center += pos;
				count++;

				Dictionary<string, Vector3d> pd;
				Dictionary<string, int> cd;
				if (stake.bound && key != "Origin") {
					pd = bounds;
					cd = bcounts;
				} else {
					pd = points;
					cd = counts;
				}

				if (pd.ContainsKey (key)) {
					pd[key] += pos;
					cd[key] += 1;
				} else {
					pd[key] = pos;
					cd[key] = 1;
				}
			}
			center /= (double) count;
			foreach (var key in ELSurveyStake.StakeUses) {
				if (points.ContainsKey (key)) {
					points[key] /= (double) counts[key];
				}
			}
			if (points.ContainsKey ("Origin")) {
				center = points["Origin"];
			}
			foreach (var key in ELSurveyStake.StakeUses) {
				if (bounds.ContainsKey (key)) {
					bounds[key] /= (double) bcounts[key];
					bounds[key] -= center;
				}
			}
		}

		public Vector3d LocalUp ()
		{
			//return body.LocalUp (center);
			double lat = body.GetLatitude (center);
			double lon = body.GetLongitude (center);
			return body.GetSurfaceNVector (lat, lon);
		}

		public Vector3d GetDirection (string dir)
		{
			Vector3d v;
			string p = "+" + dir;
			string m = "-" + dir;

			if (points.ContainsKey (p)) {
				if (points.ContainsKey (m)) {
					v = Vector3d.Normalize (points[p] - points[m]);
				} else {
					v = Vector3d.Normalize (points[p] - center);
				}
			} else if (points.ContainsKey (m)) {
				v = Vector3d.Normalize (center - points[m]);
			} else {
				v = Vector3d.zero;
			}
			return v;
		}

		public Quaternion ChooseRotation (Vector3d r, Vector3d f)
		{
			// find a reference frame that is close to the given possibly
			// non-orthogonal frame, but where up is always up
			Vector3d u = LocalUp ();
			r = Vector3d.Normalize (r - Vector3d.Dot (r, u) * u);
			f = Vector3d.Normalize (f - Vector3d.Dot (f, u) * u);
			f = Vector3d.Normalize (f + Vector3d.Cross (r, u));
			return Quaternion.LookRotation (f, u);
		}

		public Quaternion ChooseRotation (Vector3d r, Vector3d f, Vector3d u)
		{
			// find a reference frame that is close to the given possibly
			// non-orthogonal frame
			u = u + Vector3d.Normalize (Vector3d.Cross (f, r));
			u.Normalize ();
			r = Vector3d.Normalize (r - Vector3d.Dot (r, u) * u);
			f = Vector3d.Normalize (f - Vector3d.Dot (f, u) * u);
			f = Vector3d.Normalize (f + Vector3d.Cross (r, u));
			return Quaternion.LookRotation (f, u);
		}

		public Quaternion GetOrientation ()
		{
			var x = GetDirection ("X");
			var y = GetDirection ("Y");
			var z = GetDirection ("Z");
			Quaternion rot;
			if (y.IsZero ()) {
				if (z.IsZero () && x.IsZero ()) {
					x = Vector3d.Cross (LocalUp (), Vector3d.up);
					x.Normalize ();
					z = Vector3d.Cross (x, LocalUp ());
				} else if (z.IsZero ()) {
					z = Vector3d.Cross (x, LocalUp ());
					z.Normalize ();
				} else if (x.IsZero ()) {
					x = Vector3d.Cross (LocalUp (), z);
					x.Normalize ();
				}
				rot = ChooseRotation (x, z);
			} else if (x.IsZero ()) {
				// y is not zero
				if (z.IsZero ()) {
					// use local up for x
					z = Vector3d.Cross (LocalUp (), y);
					z.Normalize ();
				} else {
					z = Vector3d.Normalize (z - Vector3d.Dot (z, LocalUp ()) * LocalUp ());
				}
				rot = Quaternion.LookRotation (z, y);
			} else if (z.IsZero ()) {
				// neither x nor y are zero
				rot = ChooseRotation (y, x);
			} else {
				// no direction is 0
				rot = ChooseRotation (x, z, y);
			}
			return rot;
		}

		public Vector3 ShiftBounds (Transform frame, Vector3 pos, Box box)
		{
			Vector3 shift = new Vector3 (-pos.x, -box.min.y, -pos.z);
			Vector3 mins = box.min - pos;
			Vector3 maxs = box.max - pos;
			Vector3 mid = (mins + maxs) / 2;

			if (bounds.ContainsKey ("+X")) {
				float max_x = Vector3.Dot (frame.right, bounds["+X"]);
				if (bounds.ContainsKey ("-X")) {
					float min_x = Vector3.Dot (frame.right, bounds["-X"]);
					shift.x += (max_x + min_x) / 2 - mid.x;
				} else {
					shift.x += max_x - maxs.x;
				}
			} else if (bounds.ContainsKey ("-X")) {
				float min_x = Vector3.Dot (frame.right, bounds["-X"]);
				shift.x += min_x - mins.x;
			}
			if (bounds.ContainsKey ("+Y")) {
				shift.y = -pos.y;
				float max_y = Vector3.Dot (frame.up, bounds["+Y"]);
				if (bounds.ContainsKey ("-Y")) {
					float min_y = Vector3.Dot (frame.up, bounds["-Y"]);
					shift.y += (max_y + min_y) / 2 - mid.y;
				} else {
					shift.y += max_y - maxs.y;
				}
			} else if (bounds.ContainsKey ("-Y")) {
				shift.y = -pos.y;
				float min_y = Vector3.Dot (frame.up, bounds["-Y"]);
				shift.y += min_y - mins.y;
			}
			if (bounds.ContainsKey ("+Z")) {
				float max_z = Vector3.Dot (frame.forward, bounds["+Z"]);
				if (bounds.ContainsKey ("-Z")) {
					float min_z = Vector3.Dot (frame.forward, bounds["-Z"]);
					shift.z += (max_z + min_z) / 2 - mid.z;
				} else {
					shift.z += max_z - maxs.z;
				}
			} else if (bounds.ContainsKey ("-Z")) {
				float min_z = Vector3.Dot (frame.forward, bounds["-Z"]);
				shift.z += min_z - mins.z;
			}

			return shift;
		}
	}
}
