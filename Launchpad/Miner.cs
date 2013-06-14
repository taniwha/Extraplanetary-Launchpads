using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
 
using UnityEngine;

namespace Kethane
{
	public class Miner : PartModule
	{
		bool CanDrill = true;
		
		[KSPField(isPersistant = false)]
		public float DrillHeight = 4;
		
		[KSPField(isPersistant = false)]
        public float ExtractionRate = 0.1f;

        [KSPField(isPersistant = false)]
        public float PowerConsumption = 8;
		
		[KSPField(isPersistant = false)]
		public string SpinAnimation;
		
		private AnimationState[] AnimationStates;
		
		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }
			this.part.force_activate();
			AnimationStates = SetUpAnimation(SpinAnimation);
		}
		
		private AnimationState[] SetUpAnimation(string animationName)
        {
            var states = new List<AnimationState>();
            foreach (var animation in this.part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }
		
		private float DrillDepth() {
			float surfh = -((float)vessel.mainBody.pqsController.GetAltitude(part.transform.position) -
				(float)vessel.terrainAltitude)+6;
			print ("DrillDepth: "+surfh.ToString());
			return surfh;
		}
		
		public override void OnUpdate() {
			foreach (var state in AnimationStates)
            {
                state.normalizedTime = Mathf.Clamp01(state.normalizedTime);
				print ("Atime: "+state.normalizedTime.ToString());
            }
		}
		
		public override void OnFixedUpdate()
	    {
	        var DepositUnder = KethaneController.GetInstance(this.vessel).GetDepositUnder();
	
			//DrillDepth();
			
	        if (this.vessel != null && DepositUnder != null && (DepositUnder is OreDeposit))
	        {
				print ("Over deposit");
	            if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex > 0)
	            {
	                CanDrill &= vessel.Landed;
	            }
	            else
	            {
	                float drillDepth = this.DrillDepth();
					print ("Deposit depth: "+DepositUnder.Depth.ToString());
	                CanDrill = (drillDepth >= DepositUnder.Depth) && (drillDepth > 0);
	            }
	
	            if (CanDrill)
	            {
	                var energyRequest = this.PowerConsumption * TimeWarp.fixedDeltaTime;
	                var energy = this.part.RequestResource("ElectricCharge", energyRequest);
	
					float InstantaneousExtractionRate = ExtractionRate;
					foreach (Blast b in DepositUnder.Blasts)
					{
						print ("Blast: dLat: "+Math.Abs(b.lat-part.vessel.latitude).ToString()+", dLon: "+Math.Abs(b.lon-part.vessel.longitude).ToString());
						print ("Siz: "+(1000/part.vessel.mainBody.pqsController.radius).ToString());
						print ("IsInBlastAreaLat: "+(Math.Abs(b.lat-part.vessel.latitude)<(1000/part.vessel.mainBody.pqsController.radius)).ToString());
						print ("IsInBlastAreaLon: "+(Math.Abs(b.lon-part.vessel.longitude)<(1000/part.vessel.mainBody.pqsController.radius)).ToString());
						if (Math.Abs(b.lat-part.vessel.latitude)<(1000/part.vessel.mainBody.pqsController.radius) &&
							Math.Abs(b.lon-part.vessel.longitude)<(1000/part.vessel.mainBody.pqsController.radius) &&
							b.amount > 0) {
							InstantaneousExtractionRate += ExtractionRate*10;
							b.amount -= TimeWarp.fixedDeltaTime * ExtractionRate*10 * (energy / energyRequest);
							print ("Blast!");
						}
					}
					
	                float Amount = TimeWarp.fixedDeltaTime * InstantaneousExtractionRate * (energy / energyRequest);
	                Amount = Math.Min(Amount, DepositUnder.Quantity);
					print ("Deposit has "+DepositUnder.Quantity.ToString()+" - Drillin' "+Amount.ToString());
	                DepositUnder.Quantity += this.part.RequestResource("Ore", -Amount);
	            }
	        }
	    }
	}
}