using A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class ExHavester: BaseDrill, IResourceConsumer//, IModuleInfo
	{
		[KSPField(guiName = "", guiActive = true, guiActiveEditor = false)]
		public string ResourceStatus = "n/a";

		double heat;

		[KSPField]
		public string ResourceName = "";

		[KSPField(isPersistant = false)]
		public string HeadTransform;

		[KSPField(isPersistant = false)]
		public string TailTransform;

		Transform headTransform;
		Transform tailTransform;

		static List<IResourceProvider> resource_providers;

		public List<PartResourceDefinition> GetConsumedResources()
		{
			var consumed = new List<PartResourceDefinition> ();
			for (int i = 0; i < inputList.Count; i++) {
				var res = inputList[i].ResourceName;
				var def = PartResourceLibrary.Instance.GetDefinition (res);
				consumed.Add (def);
			}
			return consumed;
		}

		protected override float GetHeatMultiplier(ConverterResults result, double deltaTime)
		{
			return 1 / (float)this.heat * this.HeatThrottle;
		}

		public override string GetInfo()
		{
			return "";
		}

		ConversionRecipe LoadRecipe(double rate)
		{
			ConversionRecipe recipe = new ConversionRecipe();
			recipe.Inputs.AddRange(this.inputList);
			bool dumpExcess = false;
			recipe.Outputs.Add(new ResourceRatio {
				ResourceName = this.ResourceName,
				Ratio = rate,
				DumpExcess = dumpExcess
			});
			return recipe;
		}

		void FindTransforms ()
		{
			headTransform = part.FindModelTransform(HeadTransform);
			tailTransform = part.FindModelTransform(TailTransform);
		}

		private bool raycastGround(out RaycastHit hit)
		{
			var mask = 1 << 15;
			var start = tailTransform.position;
			var end = headTransform.position;
			var d = end - start;
			var len = d.magnitude;
			d = d.normalized;

			return Physics.Raycast(start, d, out hit, len, mask);
		}

		public override void OnStart(PartModule.StartState state)
		{
			if (!HighLogic.LoadedSceneIsFlight) {
				return;
			}
			if (resource_providers != null) {
				resource_providers = new List<IResourceProvider> ();
				resource_providers.Add (StockResourceProvider.Create ());
				var kethane = KethaneResourceProvider.Create ();
				if (kethane != null) {
					resource_providers.Add (kethane);
				}
			}
			FindTransforms ();
			base.Fields["ResourceStatus"].guiName = ResourceName + " rate";
			base.OnStart(state);
		}

		protected override void PostProcess(ConverterResults result, double deltaTime)
		{
		}

		protected override void PostUpdateCleanup()
		{
			if (this.IsActivated) {
				this.ResourceStatus = string.Format("{0:0.000000}/sec", heat);
			}
			else {
				this.ResourceStatus = "n/a";
			}
		}

		protected override ConversionRecipe PrepareRecipe(double deltaTime)
		{
			RaycastHit hit;

			if (!raycastGround (out hit)) {
				status = "no ground contact";
				return null;
			}
			float abundance = 0;
			for (int i = 0; i < resource_providers.Count; i++) {
				abundance += resource_providers[i].GetAbundance (ResourceName, vessel, hit.point);
			}
			if (abundance < 1e-6f) {
				status = "insufficient abundance";
				IsActivated = false;
				return null;
			}
			if (!IsActivated) {
				status = "Inactive";
				return null;
			}
			float rate = abundance * this.Efficiency * this.HeatThrottle;
			heat = (double) rate;
			return LoadRecipe((double) rate);
		}
	}
}
