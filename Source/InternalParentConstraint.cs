using System;
using UnityEngine;

public class ELInternalParentConstraint : InternalModule
{
	[KSPField]
	public string parentTransformName;
	[KSPField]
	public string positionOffset;
	[KSPField]
	public string rotationOffset;

	Transform parentTransform;
	Vector3 position;
	Quaternion rotation;

	public override void OnAwake ()
	{
		if (part == null) {
			return;
		}
		if (!String.IsNullOrEmpty (parentTransformName)) {
			parentTransform = part.FindModelTransform (parentTransformName);
		}
		if (!String.IsNullOrEmpty (positionOffset)) {
			try {
				position = ConfigNode.ParseVector3 (positionOffset);
			} catch (Exception e) {
				Debug.LogError ($"[ELInternalParentConstraint] parsing positionOffset {e}");
				position = new Vector3 (0, 0, 0);
			}
		}
		if (!String.IsNullOrEmpty (rotationOffset)) {
			try {
				rotation = ConfigNode.ParseQuaternion (rotationOffset);
			} catch (Exception e) {
				Debug.LogError ($"[ELInternalParentConstraint] parsing rotationOffset {e}");
				rotation = new Quaternion (0, 0, 0, 1);
			}
		}
		// Internals are always rotated relative to their parent part
		rotation = rotation * Quaternion.Euler(90, 180, 0);
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
		xform.rotation = InternalSpace.WorldToInternal (rot) * rotation;
	}
}
