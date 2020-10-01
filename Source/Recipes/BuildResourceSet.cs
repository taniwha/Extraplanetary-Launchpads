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
using System.Linq;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class BuildResourceSet : ICollection<BuildResource>
	{
		Dictionary<string, BuildResource> resources;

		public int Count
		{
			get {
				return resources.Count;
			}
		}

		public bool IsSynchronized
		{
			get {
				return false;
			}
		}

		public object SyncRoot
		{
			get {
				return this;
			}
		}

		public bool IsReadOnly { get { return false; } }

		public bool Contains (BuildResource res)
		{
			return resources.ContainsKey(res.name);
		}

		public void CopyTo (BuildResource []array, int index)
		{
			foreach (var res in resources.Values) {
				array.SetValue (res, index++);
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return resources.Values.GetEnumerator ();
		}

		public IEnumerator<BuildResource> GetEnumerator ()
		{
			return resources.Values.GetEnumerator ();
		}

		public BuildResource this[string res]
		{
			get {
				return resources[res];
			}
		}

		public BuildResourceSet ()
		{
			resources = new Dictionary<string, BuildResource> ();
		}

		public void Add (BuildResource res)
		{
			if (resources.ContainsKey (res.name)) {
				resources[res.name].Merge (res);
			} else {
				resources[res.name] = res;
			}
		}

		public bool Remove (BuildResource res)
		{
			return resources.Remove (res.name);
		}

		public void Clear ()
		{
			resources.Clear ();
		}

		public List<BuildResource> Values
		{
			get {
				return resources.Values.ToList ();
			}
		}
	}
}
