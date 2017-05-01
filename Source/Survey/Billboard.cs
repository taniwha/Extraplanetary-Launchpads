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

	public class EL_Billboard : MonoBehaviour
	{
		public delegate Vector3 LocalUpDelegate ();
		public LocalUpDelegate LocalUp = () => { return Vector3.up; };

		void Update()
		{
			if (Camera.main != null) {
				Vector3 forward = Camera.main.transform.forward;
				Vector3 up = LocalUp ();
				transform.rotation = Quaternion.LookRotation(forward, up);
			}
		}
	}
}
