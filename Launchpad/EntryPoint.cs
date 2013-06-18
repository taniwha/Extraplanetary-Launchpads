using System;
using System.Collections.Generic;
using UnityEngine;

public class RCCPartlessLoader : KSP.Testing.UnitTest {
	public RCCPartlessLoader() : base() {
		//Called at the first loading screen
		//When you start the game.
		RCCLoader.Initialize();
	}
}

public static class RCCLoader {
	private static UnityEngine.GameObject MyMonobehaviourObject;

	public static void Initialize() {
		MyMonobehaviourObject = new UnityEngine.GameObject("ModuleAttacherLoader", new Type[] {typeof(RoverCruiseControlAttacher)});
		UnityEngine.GameObject.DontDestroyOnLoad(MyMonobehaviourObject);
	}
}

[Serializable]
public class BaseList {
	public List<int> bases = new List<int>();
}

public class RoverCruiseControlAttacher : UnityEngine.MonoBehaviour {
	
	//private List<Vessel> bases;
	private int state = 0;
	private BaseList list;
	
	public void Update() {
		/*if (HighLogic.LoadedSceneIsEditor) {
			print ("Fetching vessel list!!!!!");
            bases = FlightGlobals.Vessels;
			print ("Got here");
            foreach (Vessel v in bases) {
                print (v.name);
            }
		}*/
		if (HighLogic.LoadedSceneIsFlight && FlightGlobals.fetch != null)
		{
			if (state != 1) {
				state = 1;
				list = new BaseList();
				list.bases.Add(FlightGlobals.ActiveVessel.GetInstanceID());
				
				print ("Writing");
				byte[] ListToSave = KSP.IO.IOUtils.SerializeToBinary(list);
                int HowManyToSave = ListToSave.Length;
                KSP.IO.BinaryWriter Writer = KSP.IO.BinaryWriter.CreateForType<RoverCruiseControlAttacher>("Bases.dat");
                Writer.Write(HowManyToSave);
                Writer.Write(ListToSave);
                Writer.Close();
				print ("Written!");
			}
		}
		else if (HighLogic.LoadedSceneIsEditor)
		{
			print ("loading");
			/*KSP.IO.BinaryReader Loader = KSP.IO.BinaryReader.CreateForType<RoverCruiseControlAttacher>("Bases.dat");
            int HowManyToLoad = Loader.ReadInt32();
            byte[] ListToLoad = new byte[HowManyToLoad];
            Loader.Read(ListToLoad, 0, HowManyToLoad);
            Loader.Close();
            object ObjectToLoad = KSP.IO.IOUtils.DeserializeFromBinary(ListToLoad);
			list = (BaseList)ObjectToLoad;
			print (list.bases[0].ToString());
			int id = list.bases[0];
			//QuickFlightDriver.StartWithNewLaunch(ShipConstruction.GetShipsSubfolderFor(GameScenes.FLIGHT)+"/DRILL.craft", "Launchpad");
			//FlightDriver.StartAndFocusVessel(ShipConstruction.GetShipsSubfolderFor(GameScenes.FLIGHT)+"/../../persistent.sfs",id);
			HighLogic.LoadScene(GameScenes.FLIGHT);*/
		}
	}

}