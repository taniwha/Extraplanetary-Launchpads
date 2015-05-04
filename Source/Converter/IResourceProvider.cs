using A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public interface IResourceProvider
	{
		float GetAbundance (string ResourceName, Vessel vessel, Vector3 location);
		void ExtractResource (string ResourceName, Vessel vessel, Vector3 location, float amount);
	}
}
