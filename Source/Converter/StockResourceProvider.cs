using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class StockResourceProvider:IResourceProvider
	{
		public double GetAmount (string ResourceName, RPLocation location, double rate)
		{
			AbundanceRequest request = new AbundanceRequest {
				Altitude = location.altitude,
				BodyId = location.bodyIndex,
				CheckForLock = false,
				Latitude = location.latitude,
				Longitude = location.longitude,
				ResourceType = 0,
				ResourceName = ResourceName
			};
			return ResourceMap.Instance.GetAbundance(request) * rate;
		}

		public void ExtractResource (string ResourceName, RPLocation location, double amount)
		{
//			if (CausesDepletion) {
//				float factor = (float)Math.Min(1, result.TimeFactor / deltaTime);
//				Vector2 depletionNode = ResourceMap.Instance.GetDepletionNode(location.latitude, location.longitude);
//				float depletionNodeValue = ResourceMap.Instance.GetDepletionNodeValue(location.bodyIndex, ResourceName, (int)depletionNode.x, (int)depletionNode.y);
//				float depletionAmount = DepletionRate * factor;
//				float value = depletionNodeValue - depletionNodeValue * depletionAmount;
//				ResourceMap.Instance.SetDepletionNodeValue(location.bodyIndex, ResourceName, (int)depletionNode.x, (int)depletionNode.y, value);
//			}
		}

		public static StockResourceProvider Create ()
		{
			return new StockResourceProvider ();
		}
	}
}
