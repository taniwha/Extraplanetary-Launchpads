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
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

	public class ELPartListLight : MonoBehaviour
	{
		static ELPartListLight instance;

		void CreateLight ()
		{
			var light = gameObject.AddComponent<Light> ();
			light.type = LightType.Directional;
			light.intensity = 0.5f;
			light.colorTemperature = 6570;
			light.cullingMask = 0x2000020;
		}

		void Awake ()
		{
			DontDestroyOnLoad (this);
			CreateLight ();
			users = 0;
		}

		int users;

		void AddUser ()
		{
			if (++users > 0) {
				gameObject.SetActive (true);
			}
		}

		void RemoveUser ()
		{
			if (--users < 1) {
				gameObject.SetActive (false);
			}
		}

		public static void Enable ()
		{
			if (!instance) {
				var go = new GameObject ("ELPartList Light",
										 typeof (ELPartListLight));
				instance = go.GetComponent<ELPartListLight> ();
			}
			instance.AddUser ();
		}

		public static void Disable ()
		{
			if (instance) {
				instance.RemoveUser ();
			}
		}
	}
}
