using System;
using UnityEngine;

public class ELInternalParentConstraint : InternalModule
{
	[KSPField]
	public string parentTransformName;

	Transform parentTransform;

	public override void OnAwake ()
	{
		if (part == null) {
			return;
		}
		if (!String.IsNullOrEmpty (parentTransformName)) {
			parentTransform = part.FindModelTransform (parentTransformName);
		}
		if (parentTransform == null) {
			Debug.Log ($"[ELInternalParentConstraint] could not find {parentTransformName}");
		}
	}

	void Update ()
	{
		if (parentTransform == null) {
			return;
		}
		Vector3 pos = parentTransform.position;
		Quaternion rot = parentTransform.rotation;

		var xform = internalModel.transform;
		xform.position = InternalSpace.WorldToInternal (pos);
		xform.rotation = InternalSpace.WorldToInternal (rot) * Quaternion.Euler(90, 180, 0);
	}
}
