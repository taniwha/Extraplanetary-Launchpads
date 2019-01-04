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

public class ELControlReference : PartModule
{
	Material material;
	[KSPField(isPersistant = true)]
	public uint pushedRefId;

	public void SetIndicators (bool bright)
	{
		if (material != null) {
			Color emissionColor = material.GetColor ("_EmissiveColor");
			emissionColor.a = bright ? 1.0f : 0.1f;
			material.SetColor ("_EmissiveColor", emissionColor);
		}
	}

	void onVesselReferenceTransformSwitch (Transform oldXform, Transform newXform)
	{
		if (oldXform == transform) {
			SetIndicators (false);
		}
		if (newXform == transform) {
			SetIndicators (true);
		}
	}

	[KSPEvent(guiName = "Control From Here", guiActive = true)]
	public void MakeReference()
	{
		vessel.SetReferenceTransform (part);
	}

	[KSPAction("Toggle Reference")]
	public void ToggleReference (KSPActionParam param)
	{
		Part refPart = vessel.GetReferenceTransformPart();
		if (refPart != part) {
			pushedRefId = refPart.flightID;
			vessel.SetReferenceTransform (part);
		} else {
			Part rp = vessel[pushedRefId];
			if (rp != null) {
				vessel.SetReferenceTransform (rp);
			}
		}
	}

	public override void OnStart (StartState state)
	{
		if (HighLogic.LoadedSceneIsFlight) {
			material = null;
			Renderer r = part.FindModelComponent<Renderer>();
			if (r != null) {
				material = r.material;
			}
			Part refPart = vessel.GetReferenceTransformPart();
			Debug.LogFormat ("[ELControlReference] {0} {1}", vessel, refPart);
			SetIndicators (refPart == this);
			GameEvents.onVesselReferenceTransformSwitch.Add (onVesselReferenceTransformSwitch);
		}
	}

	void OnDestroy ()
	{
		GameEvents.onVesselReferenceTransformSwitch.Remove (onVesselReferenceTransformSwitch);
	}
}

}
