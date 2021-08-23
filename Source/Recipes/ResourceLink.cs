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
	public class ResourceLink : ICollection<string>
	{
		HashSet<string> links;

		public int Count { get { return links.Count; } }

		public bool IsSynchronized { get { return false; } }

		public object SyncRoot { get { return this; } }

		public bool IsReadOnly { get { return false; } }

		public bool Contains (string res)
		{
			return links.Contains(res);
		}

		public void CopyTo (string []array, int index)
		{
			foreach (var res in links) {
				array.SetValue (res, index++);
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return links.GetEnumerator ();
		}

		public IEnumerator<string> GetEnumerator ()
		{
			return links.GetEnumerator ();
		}

		public ResourceLink (ConfigNode node)
		{
			links = new HashSet<string> ();
			for (int i = 0; i < node.values.Count; i++) {
				var val = node.values[i];
				if (val.name == "resource") {
					links.Add (val.value);
				}
			}
		}

		public void Add (string res)
		{
			links.Add (res);
		}

		public bool Remove (string res)
		{
			return links.Remove (res);
		}

		public void Clear ()
		{
			links.Clear ();
		}
	}
}
