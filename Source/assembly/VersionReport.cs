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
using System.Reflection;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class ELVersionReport : MonoBehaviour
	{
		static string version = null;

        public static string GetAssemblyVersionString (Assembly assembly)
        {
            string version = assembly.GetName().Version.ToString ();

            var cattrs = assembly.GetCustomAttributes(true);
            foreach (var attr in cattrs) {
                if (attr is AssemblyInformationalVersionAttribute) {
                    var ver = attr as AssemblyInformationalVersionAttribute;
                    version = ver.InformationalVersion;
                    break;
                }
            }

            return version;
        }

        public static string GetAssemblyTitle (Assembly assembly)
        {
            string title = assembly.GetName().Name;

            var cattrs = assembly.GetCustomAttributes(true);
            foreach (var attr in cattrs) {
                if (attr is AssemblyTitleAttribute) {
                    var ver = attr as AssemblyTitleAttribute;
                    title = ver.Title;
                    break;
                }
            }

            return title;
        }

		public static string GetVersion ()
		{
			if (version != null) {
				return version;
			}
			var asm = Assembly.GetCallingAssembly ();
			var title = GetAssemblyTitle (asm);
			version = title + " " + GetAssemblyVersionString (asm);
			return version;
		}

		void Start ()
		{
			Debug.Log (GetVersion ());
			Destroy (this);
		}
	}
}
