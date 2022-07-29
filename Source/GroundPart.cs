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
using System.Reflection;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

	public class ELGroundPart
	{
		const BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Instance;
		static PropertyInfo onEvent = typeof (BaseEvent).GetProperty ("onEvent", bindFlags);
		static MethodInfo retrievePart = typeof (ELGroundPart).GetMethod ("RetrievePart", bindFlags);

		ModuleGroundPart groundPart;
		ELBuildControl control;

		ELGroundPart (ModuleGroundPart groundPart, ELBuildControl control)
		{
			this.groundPart = groundPart;
			this.control = control;
		}

		void RetrievePart ()
		{
			Debug.Log ($"[ELGroundPart] RetrievePart {groundPart}");
			if (groundPart.vessel == control.builder.vessel) {
				control.ReleaseVessel (false);
			}
			groundPart.RetrievePart ();
		}

		public static void HookPickup (ModuleGroundPart groundPart, ELBuildControl control)
		{
			BaseEvent evt = groundPart.Events["RetrievePart"];
			var d = onEvent.GetValue (evt) as Delegate;
			if (d.Target as ModuleGroundPart != groundPart) {
				Debug.Log ($"[ELGroundPart] HookPickup {groundPart} already hooked");
				return;
			}
			var proxy = new ELGroundPart (groundPart, control);
			d = Delegate.CreateDelegate (typeof(BaseEventDelegate), proxy, retrievePart);
			onEvent.SetValue (evt, d);
		}
	}

}
