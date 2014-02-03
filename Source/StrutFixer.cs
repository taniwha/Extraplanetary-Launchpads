using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class StrutFixer
	{
		class Strut {
			GameObject gameObject;
			Vector3 pos;
			Vector3 dir;
			string targetName;
			float maxLength;
			public Part target;

			public Strut (Part part, string[] parms)
			{
				gameObject = part.gameObject;
				if (part is StrutConnector) {
					maxLength = ((StrutConnector)part).maxLength;
				} else if (part is FuelLine) {
					maxLength = ((FuelLine)part).maxLength;
				} else {
					// not expected to happen, but...
					maxLength = 10;
				}
				for (int i = 0; i < parms.Length; i++) {
					string[] keyval = parms[i].Split (':');
					string Key = keyval[0].Trim ();
					string Value = keyval[1].Trim ();
					if (Key == "tgt") {
						targetName = Value.Split ('_')[0];
					} else if (Key == "pos") {
						pos = KSPUtil.ParseVector3 (Value);
					} else if (Key == "dir") {
						dir = KSPUtil.ParseVector3 (Value);
					}
				}
				target = null;
				Transform xform = gameObject.transform;
				RaycastHit hitInfo;
				Vector3 castPos = xform.TransformPoint (pos);
				Vector3 castDir = xform.TransformDirection (dir);
				if (Physics.Raycast (castPos, castDir, out hitInfo,
									 maxLength)) {
					GameObject hit = hitInfo.collider.gameObject;
					target = EditorLogic.GetComponentUpwards<Part>(hit);
				}
				Debug.Log (String.Format ("[EL] {0} {1} {2} {3}", target,
										  targetName, xform.position,
										  xform.rotation));
			}
		}

		private static void HackStrutCData (ShipConstruct ship, Part p,
											int part_base)
		{
			Debug.Log (String.Format ("[EL] before {0}", p.customPartData));
			string[] Params = p.customPartData.Split (';');
			for (int i = 0; i < Params.Length; i++) {
				string[] keyval = Params[i].Split (':');
				string Key = keyval[0].Trim ();
				string Value = keyval[1].Trim ();
				if (Key == "tgt") {
					string[] pnameval = Value.Split ('_');
					string pname = pnameval[0];
					int val = int.Parse (pnameval[1]);
					if (val == -1) {
						Strut strut = new Strut (p, Params);
						if (strut.target != null) {
							val = ship.parts.IndexOf (strut.target);
						}
					}
					if (val != -1) {
						val += part_base;
					}
					Params[i] = "tgt: " + pname + "_" + val.ToString ();
					break;
				}
			}
			p.customPartData = String.Join ("; ", Params);
			Debug.Log (String.Format ("[EL] after {0}", p.customPartData));
		}

		public static void HackStruts (ShipConstruct ship, int part_base)
		{
			var all_struts = ship.parts.OfType<StrutConnector>();
			var struts = all_struts.Where (p => p.customPartData != "");
			foreach (Part part in struts) {
				HackStrutCData (ship, part, part_base);
			}
			var all_fuelLines = ship.parts.OfType<FuelLine>();
			var fuelLines = all_fuelLines.Where (p => p.customPartData != "");
			foreach (Part part in fuelLines) {
				HackStrutCData (ship, part, part_base);
			}
		}
	}
}
