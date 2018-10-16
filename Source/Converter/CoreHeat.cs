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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class ELCoreHeat: ModuleCoreHeat
	{
		public override void UpdateConverterModuleCache()
		{
			converterCache = new List<BaseConverter> ();
			foreach (var conv in part.FindModulesImplementing<ELConverter> ()) {
				converterCache.Add (conv);
			}
		}
		protected override void ResolveConverterEnergy (double deltaTime)
		{
			double partEnergy = part.thermalMass * part.temperature;
			if (CoreThermalEnergy > partEnergy && energyTemp > 0) {
				energyTemp = 0;
			}
			for (int i = converterCache.Count; i-- > 0; ) {
				var conv = converterCache[i] as ELConverter;

				if (conv == null || !conv.IsActivated || !conv.GeneratesHeat) {
					continue;
				}

				double flux = conv.HeatFlux * deltaTime;
				AddEnergyToCore (flux * conv.lastTimeFactor);
			}
		}
	}
}
