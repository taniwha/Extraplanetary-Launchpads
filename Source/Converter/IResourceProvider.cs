using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class RPLocation {
		public Vector3 location;
		public CelestialBody body;
		public double altitude
		{
			get {
				return body.GetAltitude (location);
			}
		}
		public double latitude
		{
			get {
				return body.GetLatitude(location);
			}
		}
		public double longitude
		{
			get {
				return body.GetLongitude(location);
			}
		}
		public int bodyIndex
		{
			get {
				return body.flightGlobalsIndex;
			}
		}
		public RPLocation (CelestialBody body, Vector3 location)
		{
			this.location = location;
			this.body = body;
		}
	}

	public interface IResourceProvider
	{
		double GetAmount (string ResourceName, RPLocation location, double rate);
		void ExtractResource (string ResourceName, RPLocation location, double amount);
	}
}
