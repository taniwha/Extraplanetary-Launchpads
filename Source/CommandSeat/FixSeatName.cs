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

public class ELFixSeatName : PartModule
{
	public string configString;

	Dictionary<string, string> seatNames;

	IEnumerator WaitAndFixSeatNames ()
	{
		yield return null;
		var seats = part.FindModulesImplementing<KerbalSeat> ();
		foreach (var seat in seats) {
			if (seatNames.ContainsKey(seat.seatPivotName)) {
				string name = seatNames[seat.seatPivotName];
				string newName = seat.Events["BoardSeat"].guiName;
				int end = newName.Length - part.partInfo.title.Length;
				if (end >= 0) {
					newName = newName.Substring(0, end);
					seat.Events["BoardSeat"].guiName = newName + name;
					// just in case
					seat.seatName = seatNames[seat.seatPivotName];
				}
			}
		}
	}

	public override void OnStart (StartState state)
	{
		if (HighLogic.LoadedSceneIsEditor) {
			OnLoad(null);
		}
		StartCoroutine (WaitAndFixSeatNames ());
	}

	void OnDestroy ()
	{
	}

	public override void OnLoad (ConfigNode node)
	{
		if (configString == null) {
			configString = node.ToString ();
		}
		node = ConfigNode.Parse(configString).GetNode("MODULE");
		print("ELFixSeatName");
		print(node);

		seatNames = new Dictionary<string, string> ();
		for (int i = 0; i < node.values.Count; i++) {
			var val = node.values[i];
			seatNames[val.name] = val.value;
			Debug.LogFormat("[ELFixSeatName]OnLoad {0} = {1}", val.name, val.value);
		}
	}
}

}
