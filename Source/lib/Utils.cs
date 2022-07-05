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
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Experience;

namespace ExtraplanetaryLaunchpads {

	public static class EL_Utils {
		static string applicationRootPath;

		public static string ApplicationRootPath
		{
			get {
				if (applicationRootPath == null) {
					applicationRootPath = Path.GetFullPath(KSPUtil.ApplicationRootPath).Replace("\\", "/");
					int len = applicationRootPath.Length;
					if (applicationRootPath[len - 1] != '/') {
						applicationRootPath = applicationRootPath + "/";
					}
				}
				return applicationRootPath;
			}
		}

		public static T FindVesselModuleImplementing<T> (this Vessel vessel) where T : class
		{
			for (int i = vessel.vesselModules.Count; i-- > 0; ) {
				VesselModule vm = vessel.vesselModules[i];
				if (vm is T) {
					return vm as T;
				}
			}
			return null;
		}

		public static Vector3d LocalUp (this CelestialBody body, Vector3d pos)
		{
			return (pos - body.position).normalized;
		}

		public static string ToStringSI(this double value, int sigFigs = 3, string unit = null)
		{
			if (unit == null) {
				unit = "";
			}
			return KSPUtil.PrintSI (value, unit, sigFigs, false);
		}

		public static string ToStringSI(this float value, int sigFigs = 3, string unit = null)
		{
			if (unit == null) {
				unit = "";
			}
			return KSPUtil.PrintSI (value, unit, sigFigs, false);
		}

		public static string FormatMass (double mass, int sigFigs = 4)
		{
			if (mass < 1.0) {
				mass *= 1e6;
				return mass.ToStringSI(sigFigs, "g");
			} else {
				return mass.ToStringSI(sigFigs, "t");
			}
		}

		public static List<ProtoCrewMember> GetCrewList (Part part)
		{
			var crew = new List<ProtoCrewMember> ();
			var seats = part.FindModulesImplementing<KerbalSeat> ();
			foreach (var s in seats) {
				if (s.Occupant != null) {
					crew.Add (s.Occupant.protoModuleCrew[0]);
				}
			}
			crew.AddRange (part.protoModuleCrew);
			return crew;
		}

		public static double[] TimeSpan (double seconds)
		{
			var dtFmt = KSPUtil.dateTimeFormatter;
			var years = Math.Floor (seconds / dtFmt.Year);
			seconds -= years * dtFmt.Year;
			var days = Math.Floor (seconds / dtFmt.Day);
			seconds -= days * dtFmt.Day;
			var hours = Math.Floor (seconds / dtFmt.Hour);
			seconds -= hours * dtFmt.Hour;
			var minutes = Math.Floor (seconds / dtFmt.Minute);
			seconds -= minutes * dtFmt.Minute;
			return new double[] {years, days, hours, minutes, seconds};
		}

		static string[] time_formats = {
			"{0:F0}y{1:000}d{2:00}h{3:00}m{4:00}s",
			"{1:F0}d{2:00}h{3:00}m{4:00}s",
			"{2:F0}h{3:00}m{4:00}s",
			"{3:F0}m{4:00}s",
			"{4:F0}s",
		};
		public static string TimeSpanString (double seconds)
		{
			var span = TimeSpan (seconds);
			int i = 0;
			while (i < span.Length - 1 && span[i] == 0) {
				i++;
			}
			return String.Format (time_formats[i], span.Cast<object>().ToArray());
		}

		public static void SetupEVAEvent(BaseEvent evt, float EVARange)
		{
			evt.externalToEVAOnly = true;
			evt.guiActiveUnfocused = true;
			evt.unfocusedRange = EVARange;
		}

		public static void dumpxform (Transform t, bool comps, string n = "")
		{
			Debug.LogFormat ("[EL] xform: {0}", n + t.name);
			if (comps) {
				foreach (var c in t.GetComponents<MonoBehaviour>()) {
					Debug.LogFormat("  {0}", c);
				}
			}
			foreach (Transform c in t)
				dumpxform (c, comps, n + t.name + "/");
		}

		public static void PrintResource (StringBuilder sb, ResourceRatio ratio, string unit)
		{
			var def = PartResourceLibrary.Instance.GetDefinition (ratio.ResourceName);
			sb.Append ("\n - ");
			sb.Append (ratio.ResourceName);
			string period;
			double rate;
			if (def.density > 0) {
				rate = ratio.Ratio * def.density;
			} else {
				rate = ratio.Ratio;
				unit = "u";
			}
			if (rate < 0.1 / KSPUtil.dateTimeFormatter.Hour) {
				rate *= KSPUtil.dateTimeFormatter.Day;
				period = "day";
			} else if (rate < 0.1 / KSPUtil.dateTimeFormatter.Minute) {
				rate *= KSPUtil.dateTimeFormatter.Hour;
				period = "hr";
			} else if (rate < 0.1) {
				rate *= KSPUtil.dateTimeFormatter.Minute;
				period = "m";
			} else {
				period = "s";
			}
			sb.AppendFormat (" {0:0.00} {1}/{2}", rate, unit, period);
		}

		public static bool PrintIngredient (StringBuilder sb, Ingredient ingredient, string unit)
		{
			string name = ingredient.name;
			double ratio = ingredient.ratio;
			ResourceRatio Ratio = new ResourceRatio (name, ratio, false);
			var def = PartResourceLibrary.Instance.GetDefinition (name);
			if (def != null) {
				if (def.density > 0) {
					Ratio.Ratio /= def.density;
				}
				PrintResource (sb, Ratio, unit);
				return true;
			}
			return false;
		}

		public static bool KSPFileExists (string filename)
		{
			return File.Exists (ApplicationRootPath + filename);
		}

		public static DateTime KSPFileTimestamp (string filename)
		{
			return File.GetLastWriteTime (ApplicationRootPath + filename);
		}

		public static bool LoadImage (ref Texture2D tex, string filename)
		{
			bool ret = false;
			string path = $"{ApplicationRootPath}{filename}";
			if (File.Exists (path)) {
				ImageConversion.LoadImage (tex, File.ReadAllBytes (path));
				ret = true;
			}
			return ret;
		}

		public static Sprite MakeSprite (Texture2D tex)
		{
			var rect = new Rect (0, 0, tex.width, tex.height);
			var pivot = new Vector2 (0.5f, 0.5f);
			float pixelsPerUnity = 100;
			uint extrude = 0;
			var type = SpriteMeshType.Tight;
			var border = Vector4.zero;

			return Sprite.Create (tex, rect, pivot, pixelsPerUnity, extrude,
								  type, border);
		}

		public static void DisableColliders (GameObject obj)
		{
			var colliders = obj.GetComponentsInChildren<Collider> ();
			for (int i = colliders.Length; i-- > 0; ) {
				colliders[i].enabled = false;
			}
		}

		public static void RemoveColliders (GameObject obj)
		{
			var colliders = obj.GetComponentsInChildren<Collider> ();
			for (int i = colliders.Length; i-- > 0; ) {
				UnityEngine.Object.Destroy (colliders[i]);
			}
		}

		public static void DisableModules (GameObject obj)
		{
			var modules = obj.GetComponentsInChildren<PartModule> ();
			for (int i = modules.Length; i-- > 0; ) {
				modules[i].enabled = false;
			}
		}

		public static void SetLightMasks (GameObject obj, int mask)
		{
			var lights = obj.GetComponentsInChildren<Light> ();
			for (int i = lights.Length; i-- > 0; ) {
				lights[i].cullingMask = mask;
			}
		}

		public static void SetLayer (GameObject obj, int layer, bool recursive)
		{
			obj.layer = layer;
			if (recursive) {
				for (int i = obj.transform.childCount; i-- > 0; ) {
					var child = obj.transform.GetChild (i).gameObject;
					SetLayer (child, layer, true);
				}
			}
		}

		public static Material []CollectMaterials (GameObject obj)
		{
			var materials = new List<Material> ();
			var renderers = obj.GetComponentsInChildren<Renderer> ();
			for (int i = renderers.Length; i-- > 0; ) {
				var mats = renderers[i].materials;
				for (int j = mats.Length; j-- > 0; ) {
					if (!mats[j].HasProperty (PropertyIDs._MinX)) {
						continue;
					}
					materials.Add (mats[j]);
				}
			}
			return materials.ToArray ();
		}
	}
}
