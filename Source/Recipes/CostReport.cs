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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class CostReport : IConfigNode
	{
		public List<BuildResource> required;
		public List<BuildResource> optional;

		public CostReport ()
		{
			required = new List<BuildResource> ();
			optional = new List<BuildResource> ();
		}

		public CostReport (BuildResourceSet req, BuildResourceSet opt)
		{
			required = req.Values;
			optional = opt.Values;
		}

		public void Load (ConfigNode node)
		{
			var req = node.GetNode ("Required");
			foreach (var r in req.GetNodes ("BuildResrouce")) {
				var res = new BuildResource ();
				res.Load (r);
				required.Add (res);
			}
			var opt = node.GetNode ("Optional");
			foreach (var r in opt.GetNodes ("BuildResrouce")) {
				var res = new BuildResource ();
				res.Load (r);
				optional.Add (res);
			}
		}

		public void Save (ConfigNode node)
		{
			var req = node.AddNode ("Required");
			foreach (var res in required) {
				var r = req.AddNode ("BuildResrouce");
				res.Save (r);
			}
			var opt = node.AddNode ("Optional");
			foreach (var res in optional) {
				var r = opt.AddNode ("BuildResrouce");
				res.Save (r);
			}
		}
	}
}
