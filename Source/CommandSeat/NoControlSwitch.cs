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
using System.Reflection;
using UnityEngine;

using KSP.IO;
using Experience;

namespace ExtraplanetaryLaunchpads {

public class ELNoControlSwitch : PartModule
{
	static FieldInfo refXformPartField;
	Part refXformPart;

	IEnumerator WaitAndResetReferenceTransform ()
	{
		yield return null;
		vessel.SetReferenceTransform (refXformPart);
	}

	void onPartCouple (GameEvents.FromToAction<Part, Part> action)
	{
		if (action.to == part && action.from.vessel.isEVA) {
			refXformPart = (Part) refXformPartField.GetValue (vessel);
			StartCoroutine (WaitAndResetReferenceTransform ());
		}
	}

	public override void OnStart (StartState state)
	{
		if (refXformPartField == null) {
			var fields = typeof (Vessel).GetFields (BindingFlags.NonPublic | BindingFlags.Instance);
			int count = 0;
			for (int i = 0, c = fields.Length; i < c; i++) {
				if (fields[i].FieldType == typeof(Part)) {
					count++;
					if (count == 1) {
						refXformPartField = fields[i];
						break;
					}
				}
			}
		}
		GameEvents.onPartCouple.Add (onPartCouple);
	}

	void OnDestroy ()
	{
		GameEvents.onPartCouple.Remove (onPartCouple);
	}
}

}
