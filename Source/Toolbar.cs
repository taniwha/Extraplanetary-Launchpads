using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExLP {
	using Toolbar;

	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExToolbar_ShipInfo : MonoBehaviour
	{
		private IButton ExEditorButton;

		public void Awake ()
		{
			ExEditorButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExEditorButton");
			ExEditorButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExEditorButton.ToolTip = "EL Build Resources Display";
			ExEditorButton.OnClick += (e) => ExShipInfo.ToggleGUI ();
		}

		void OnDestroy()
		{
			ExEditorButton.Destroy ();
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExToolbar_BuildWindow : MonoBehaviour
	{
		private IButton ExEditorButton;

		public void Awake ()
		{
			ExEditorButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExEditorButton");
			ExEditorButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExEditorButton.ToolTip = "EL Build Window";
			ExEditorButton.OnClick += (e) => ExBuildWindow.ToggleGUI ();
		}

		void OnDestroy()
		{
			ExEditorButton.Destroy ();
		}
	}
}
