using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Toolbar;

namespace ExLP {
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExToolbarTrampoline : MonoBehaviour
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
}
