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

	public int Count => edgeFaces.Count;

	public void Add (Triangle face)
	{
		for (int i = 0; i < 3; i++) {
			if (edgeFaces.ContainsKey (face.edges[i])) {
				Debug.Log ($"[Connectivity] duplicate edge");
			} else {
				edgeFaces.Add (face.edges[i], face);
			}
		}
	}

	public void Extend (FaceSet faceset)
	{
		var faces = faceset.faces;
		for (int i = 0; i < faces.Count; i++) {
			Add (faces[i]);
		}
	}

	public void Remove (Triangle face)
	{
		for (int i = 0; i < 3; i++) {
			edgeFaces.Remove (face.edges[i]);
		}
	}
}

}
