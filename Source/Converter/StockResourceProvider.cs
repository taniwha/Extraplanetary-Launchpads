using A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class StockResourceProvider:IResourceProvider
	{
		public float GetAbundance (string ResourceName, Vessel vessel, Vector3 location)
		{
			var latitude = vessel.mainBody.GetLatitude(location);;
			var longitude = vessel.mainBody.GetLongitude(location);;
			AbundanceRequest request = new AbundanceRequest {
				Altitude = vessel.altitude,
				BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
				CheckForLock = false,
				Latitude = latitude,
				Longitude = longitude,
				ResourceType = 0,
				ResourceName = ResourceName
			};
			return ResourceMap.Instance.GetAbundance(request);
		}

		public void ExtractResource (string ResourceName, Vessel vessel, Vector3 location, float amount)
		{
		}

		public static StockResourceProvider Create ()
		{
			return new StockResourceProvider ();
		}
	}
}
