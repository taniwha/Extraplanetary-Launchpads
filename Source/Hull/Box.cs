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
using System.Reflection;
using System.IO;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

	public class Box
	{
		public Vector3 min;
		public Vector3 max;

		public Box (Vector3 min, Vector3 max)
		{
			this.min = min;
			this.max = max;
		}

		public Box (Bounds b)
		{
			min = new Vector3 (b.min.x, b.min.y, b.min.z);
			max = new Vector3 (b.max.x, b.max.y, b.max.z);
		}

		public Box (Box b)
		{
			min = b.min;
			max = b.max;
		}

		public Box (ConfigNode node)
		{
			if (node.HasValue ("min")) {
				min = ConfigNode.ParseVector3 (node.GetValue ("min"));
			}
			if (node.HasValue ("min")) {
				max = ConfigNode.ParseVector3 (node.GetValue ("max"));
			}
		}

		public void Add (Bounds b)
		{
			min.x = Mathf.Min (min.x, b.min.x);
			min.y = Mathf.Min (min.y, b.min.y);
			min.z = Mathf.Min (min.z, b.min.z);
			max.x = Mathf.Max (max.x, b.max.x);
			max.y = Mathf.Max (max.y, b.max.y);
			max.z = Mathf.Max (max.z, b.max.z);
		}

		public void Save (ConfigNode node)
		{
			node.AddValue("min", min);
			node.AddValue("max", max);
		}

		public override string ToString ()
		{
			return "[" + min + "," + max + "]";
		}
	}
}
