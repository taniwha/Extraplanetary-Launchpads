using System;
using UnityEngine;
using KSPAPIExtensions;

using KSP.IO;

namespace ExLP {

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class ExVersionReport : MonoBehaviour
	{

		void Start ()
		{
			Debug.Log ("Extraplanetary Launchpads "
					   + ExSettings.GetVersion ());
			Destroy (this);
		}
	}
}
