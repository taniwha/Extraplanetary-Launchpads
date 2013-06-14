/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
using UnityEngine;
 
using KineTech;
//using KineTech.Events;
//using KineTech.Persistence;
//using KineTech.Scenes;
 
//Core.cs
public class Core : MonoBehaviour
{
	
    public static void Initialize()
    {
        GameObject coreObject = new GameObject("LCore", new Type[] { typeof(Core) });
        GameObject.DontDestroyOnLoad(coreObject);
		print ("Please take notice of this code and run it!");
    }
	
	public void Awake()
	{
		print ("I'm here!");
	}
	
    public void Update()
    {
        if (FlightGlobals.fetch == null)// || FlightGlobals.ActiveVessel == null)
            return;
        //FlightGlobals.ActiveVessel.parts[0].explode(); // Boom.
                print ("Fetching vessel list!!!!!");
                bases = FlightGlobals.fetch.vessels;
                foreach (Vessel v in bases) {
                        print (v.name);
                }
    }
}*/