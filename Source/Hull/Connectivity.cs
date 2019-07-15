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
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

public class Connectivity
{
	public class FaceDict : Dictionary<Edge,Triangle> { }

	public FaceDict edgeFaces = new FaceDict ();

	public Connectivity (params Triangle []faces)
	{
		for (int i = 0; i < faces.Length; i++) {
			Add (faces[i]);
		}
	}

	public Connectivity (FaceSet faceset)
	{
		Add (faceset);
	}

	public int Count => edgeFaces.Count;

	public Triangle this[Edge e]
	{
		get {
			Triangle t;
			if (edgeFaces.TryGetValue (e, out t)) {
				return t;
			}
			return null;
		}
	}

	public bool error = false;

	public void Add (Triangle face)
	{
		for (int i = 0; i < 3; i++) {
			if (edgeFaces.ContainsKey (face.edges[i])) {
				Debug.Log ($"[Connectivity] duplicate edge");
				error = true;
			} else {
				edgeFaces.Add (face.edges[i], face);
			}
		}
	}

	public void Add (FaceSet faceset)
	{
		for (var face = faceset.First; face != null; face = face.Next) {
			Add (face);
		}
	}

	public void Remove (Triangle face)
	{
		for (int i = 0; i < 3; i++) {
			edgeFaces.Remove (face.edges[i]);
		}
	}

	public void Remove (FaceSet faceset)
	{
		for (var face = faceset.First; face != null; face = face.Next) {
			Remove (face);
		}
	}

	int light_run;

	void LightFaces (Triangle face, int point, FaceSet lit_faces)
	{
		if (face.light_run == light_run) {
			return;
		}
		face.light_run = light_run;
		if (face.CanSee (point)) {
			lit_faces.Add (face);
			for (int i = 0; i < 3; i++) {
				var conface = this[face.redges[i]];
				if (conface == null) {
					Debug.Log ($"[Connectivity] incompletely connected face");
					continue;
				}
				LightFaces (conface, point, lit_faces);
			}
		}
	}

	public FaceSet LightFaces (Triangle first_face, int point)
	{
		var lit_faces = new FaceSet (first_face.mesh, first_face);

		light_run++;
		LightFaces (first_face, point, lit_faces);
		return lit_faces;
	}
}

}
