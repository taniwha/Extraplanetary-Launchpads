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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

using ExtraplanetaryLaunchpads;

namespace ExtraplanetaryLaunchpads.KAS {

	public class KASModuleStrut {
		static Type KASModuleAttachCore_class;

		object obj;

		static FieldInfo vesselInfo_field;
		static FieldInfo dockedPartID_field;
		public DockedVesselInfo vesselInfo
		{
			get {
				return (DockedVesselInfo) vesselInfo_field.GetValue (obj);
			}
		}
		public uint dockedPartID
		{
			get {
				var str = (string) dockedPartID_field.GetValue (obj);
				uint id;
				uint.TryParse (str, out id);
				return id;
			}
		}

		public KASModuleStrut (object obj)
		{
			this.obj = obj;
		}
		const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
		internal static void Initialize (Assembly KASasm)
		{
			KASModuleAttachCore_class = KASasm.GetTypes().Where (t => t.Name.Equals ("KASModuleAttachCore")).FirstOrDefault ();
			Debug.Log($"[KASModuleAttachCore] Initialize {KASModuleAttachCore_class}");
			vesselInfo_field = KASModuleAttachCore_class.GetField ("vesselInfo", bindingFlags);
			dockedPartID_field = KASModuleAttachCore_class.GetField ("dockedPartID", bindingFlags);
			Debug.Log($"[KASModuleAttachCore] Initialize {vesselInfo_field} {dockedPartID_field}");
		}
	}

	public class KASWrapper {

		public static bool Initialize ()
		{
			var KASasm = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name.Equals ("KAS", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault ();
			if (KASasm == null) {
				return false;
			}

			KASModuleStrut.Initialize (KASasm.assembly);
			return true;
		}
	}
}
