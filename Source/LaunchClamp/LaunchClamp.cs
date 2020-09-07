/*
This file is part of Extraplanetary Launchpads.

Extraplanetary Launchpads is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Extraplanetary Launchpads is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Extraplanetary Launchpads.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class ELExtendingLaunchClamp : PartModule
	{
		// for compatibility with LaunchClamp
		[KSPField] public string trf_towerPivot_name;
		[KSPField] public string trf_towerStretch_name;
		[KSPField] public string trf_anchor_name;
		[KSPField] public string trf_animationRoot_name;
		[KSPField] public string anim_decouple_name;
		// support cloned sections instead of (just) mesh stretching
		[KSPField] public string trf_cloneSource_name;
		[KSPField] public float cloneStep = 1;

		[KSPField (isPersistant = true)]
		public float height;

		[KSPField (isPersistant = true)]
		public bool released;

		public float baseHeight = -1;

		Quaternion towerRot;
		Transform towerPivot;
		Transform towerStretch;
		Transform anchor;
		Transform animationRoot;
		Transform cloneSource;
		Animation anim;
		AnimationState anim_decouple;
		Material towerMaterial;

		bool can_stretch;
		bool enableExtension;

		FXGroup release_fx = null;
		ConfigurableJoint clampJoint;

		GameObject []clones;

		public void EnableExtension ()
		{
			enableExtension = true;
		}

		void SetupAnimation ()
		{
			if (anim_decouple) {
				anim_decouple.wrapMode = WrapMode.ClampForever;
				anim_decouple.weight = 1;
				anim_decouple.enabled = true;
				anim_decouple.speed = 0;
			}
		}

		void SetState ()
		{
			if (released) {
				if (anim_decouple) {
					anim_decouple.normalizedTime = 1;
				}
			} else {
				if (anim_decouple) {
					anim_decouple.normalizedTime = 0;
				}
			}
		}

		static Vector3 []CapsuleDirection = {
			new Vector3 (1, 0, 0),
			new Vector3 (0, 1, 0),
			new Vector3 (0, 0, 1),
		};

		float DistanceFromTerrain ()
		{
			var collider = Part.FindModelComponent<Collider> (anchor, "");

			RaycastHit hitInfo = new RaycastHit();
			bool hit = false;

			float maxDist = 100;
			var triggers = QueryTriggerInteraction.Ignore;
			int layer = 1 << 15;

			Vector3 direction = -anchor.up;

			if (collider is SphereCollider) {
				var sphere = collider as SphereCollider;
				Vector3 origin = sphere.transform.TransformPoint (sphere.center);
				hit = Physics.SphereCast (origin, sphere.radius, direction, out hitInfo, maxDist, layer, triggers);
			} else if (collider is CapsuleCollider) {
				var capsule = collider as CapsuleCollider;
				float d = capsule.height - 2 * capsule.radius;
				Vector3 axis = CapsuleDirection[capsule.direction];
				Vector3 p1 = capsule.transform.TransformPoint (capsule.center + d * axis);
				Vector3 p2 = capsule.transform.TransformPoint (capsule.center - d * axis);
				hit = Physics.CapsuleCast (p1, p2, capsule.radius, direction, out hitInfo, maxDist, layer, triggers);
			} else if (collider is BoxCollider) {
				var box = collider as BoxCollider;
				Vector3 center = box.transform.TransformPoint (box.center);
				Vector3 extents = box.size / 2;
				Quaternion orientation = box.transform.rotation;
				hit = Physics.BoxCast (center, extents, direction, out hitInfo, orientation, maxDist, layer, triggers);
			} else if (collider is MeshCollider) {
				var meshC = collider as MeshCollider;
				var mesh = meshC.sharedMesh;
				var verts = mesh.vertices;
				//Part root = part.localRoot;
				RaycastHit bestHit = hitInfo;
				float bestDist = float.PositiveInfinity;
				// Not the most efficient, and it will miss peaks hitting faces, but it will do for most cases
				for (int i = 0; i < verts.Length; i++) {
					Vector3 start = meshC.transform.TransformPoint (verts[i]);
					if (Physics.Raycast (start, direction, out hitInfo, maxDist, layer, triggers)) {
						hit = true;
						if (hitInfo.distance < bestDist) {
							bestHit = hitInfo;
							bestDist = hitInfo.distance;
						}
					}
					//Debug.Log ($"[ELExtendingLaunchClamp] mesh {start} {root.transform.InverseTransformPoint (start)} {hitInfo.distance} {hitInfo.distance + baseHeight}");
				}
				if (hit) {
					hitInfo = bestHit;
				}
			} else {
				Debug.LogWarning ("[ELExtendingLaunchClamp] unsupported collider type");
				return -1;
			}
			if (hit) {
				return hitInfo.distance;
			}
			return -1;
		}

		void UpdateClones ()
		{
			Vector3 dist = towerStretch.position - anchor.position;
			float length = Vector3.Dot(dist, towerStretch.up);
			int count = (int)Mathf.Floor(length / cloneStep);
			if (count < 0) {
				count = 0;
			}
			int start = 0;
			if (clones != null) {
				if (count <= clones.Length) {
					for (int i = 0; i < count; i++) {
						clones[i].SetActive(true);
					}
					for (int i = count; i < clones.Length; i++) {
						clones[i].SetActive(false);
					}
					start = clones.Length;
				} else {
					var nc = new GameObject[count];
					for (int i = 0; i < clones.Length; i++) {
						clones[i].SetActive(true);
						nc[i] = clones[i];
					}
					start = clones.Length;
					clones = nc;
				}
			} else {
				clones = new GameObject[count];
			}
			Vector3 position = cloneSource.localPosition;
			float basey = position.y - cloneStep;
			for (int i = start; i < clones.Length; i++) {
				clones[i] = Instantiate(cloneSource.gameObject);
				clones[i].transform.SetParent(cloneSource.transform.parent, false);
				position.y = basey - i * cloneStep;
				clones[i].transform.localPosition = position;
			}
		}

		public void RotateTower ()
		{
			FindTransforms ();
			baseHeight = Vector3.Distance (anchor.position, towerStretch.position);
			towerPivot.localRotation = towerRot;
			anchor.localRotation = towerRot;
			anchor.position = towerStretch.position - towerStretch.up * baseHeight;
		}

		void SetHeight ()
		{
			if (can_stretch) {
				if (baseHeight == -1) {
					baseHeight = Vector3.Distance (anchor.position, towerStretch.position);
				}
				if (HighLogic.LoadedSceneIsEditor) {
					// the base never rotates in the editor
					towerPivot.rotation = new Quaternion(0, 0, 0, 1);
					towerRot = towerPivot.localRotation;
					anchor.localRotation = towerRot;

					height = towerStretch.position.y;
				} else {
					towerPivot.localRotation = towerRot;
					if (enableExtension) {
						// ensure the anchor is in the right position and
						// orientation before checking for distance to the
						// ground
						anchor.localRotation = towerRot;
						anchor.position = towerStretch.position;
						float dist = DistanceFromTerrain ();
						// set height only if the casts actually found
						// something, otherwise keep the VAB/SPH height
						//Debug.Log ($"[ELExtendingLaunchClamp] dist {dist}");
						if (dist > 0) {
							height = dist;
						}
					}
				}
				float towerScale = height / baseHeight;
				if (towerMaterial) {
					towerMaterial.mainTextureScale = new Vector2 (1, towerScale);
				}
				towerStretch.localScale = new Vector3 (1, towerScale, 1);

				anchor.localRotation = towerRot;
				anchor.position = towerStretch.position - towerStretch.up * height;
				if (cloneSource) {
					UpdateClones ();
				}
			}
		}

		public override void OnAwake ()
		{
			GameEvents.onVesselGoOnRails.Add (ClearClampJoint);
			GameEvents.onVesselGoOffRails.Add (SetClampJoint);
		}

		void OnDestroy ()
		{
			GameEvents.onVesselGoOnRails.Remove (ClearClampJoint);
			GameEvents.onVesselGoOffRails.Remove (SetClampJoint);
		}

		void FindTransforms ()
		{
			towerPivot = part.FindModelTransform (trf_towerPivot_name);
			towerStretch = part.FindModelTransform (trf_towerStretch_name);
			anchor = part.FindModelTransform (trf_anchor_name);
			animationRoot = part.FindModelTransform (trf_animationRoot_name);
			cloneSource = part.FindModelTransform (trf_cloneSource_name);

			can_stretch = towerPivot && towerStretch && anchor;
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (part.stagingIcon == string.Empty
				&& overrideStagingIconIfBlank) {
				part.stagingIcon = "STRUT";
			}
			FindTransforms ();

			if (animationRoot) {
				anim = animationRoot.GetComponent<Animation> ();
				if (anim) {
					anim_decouple = anim[anim_decouple_name];
				}
			}
			if (towerStretch) {
				var renderer = towerStretch.GetComponentInChildren<MeshRenderer>();
				if (renderer) {
					towerMaterial = renderer.material;
				}
			}
			SetHeight ();
			SetState ();
		}

		public override void OnLoad (ConfigNode node)
		{
			if (node.HasValue ("towerRot")) {
				string rot = node.GetValue ("towerRot");
				towerRot = KSPUtil.ParseQuaternion (rot);
			}
			if (node.HasValue ("ForceHeightCheck")) {
				bool fhc;
				if (bool.TryParse (node.GetValue ("ForceHeightCheck"), out fhc)) {
					enableExtension |= fhc;
				}
			}
		}

		public override void OnSave (ConfigNode node)
		{
			var rot = KSPUtil.WriteQuaternion (towerRot);
			node.AddValue ("towerRot", rot);
		}

		public void OnPutToGround (PartHeightQuery qr)
		{
			qr.lowestOnParts[part] = 0;

			if (qr.lowestPoint < qr.lowestOnParts[part]) {
				height += qr.lowestOnParts[part] - qr.lowestPoint;
				baseHeight = -1;
			} else {
				qr.lowestPoint = qr.lowestOnParts[part];
			}
		}

		void Update ()
		{
			if (HighLogic.LoadedSceneIsEditor) {
				SetHeight ();
			} else {
				if (vessel != null) {
					vessel.permanentGroundContact = true;
					vessel.skipGroundPositioning = true;
				}
				part.PermanentGroundContact = true;
			}
		}

		void SetClampJoint (Vessel v)
		{
			if (v == vessel && !clampJoint) {
				//Debug.Log ("[ELExtendingLaunchClamp] SetClampJoint");
				clampJoint = gameObject.AddComponent<ConfigurableJoint> ();
				clampJoint.angularXMotion = ConfigurableJointMotion.Locked;
				clampJoint.angularYMotion = ConfigurableJointMotion.Locked;
				clampJoint.angularZMotion = ConfigurableJointMotion.Locked;
				clampJoint.xMotion = ConfigurableJointMotion.Locked;
				clampJoint.yMotion = ConfigurableJointMotion.Locked;
				clampJoint.zMotion = ConfigurableJointMotion.Locked;
				clampJoint.configuredInWorldSpace = false;
				clampJoint.autoConfigureConnectedAnchor = false;
				clampJoint.connectedAnchor = part.transform.position;
			}
		}

		void ClearClampJoint (Vessel v)
		{
			if (v == vessel && clampJoint) {
				Debug.LogFormat ("[ELExtendingLaunchClamp] ClearClampJoint");
				Destroy (clampJoint);
				clampJoint = null;
			}
		}

		public override void OnActive ()
		{
			if (stagingEnabled) {
				Release ();
			}
		}

		[KSPAction("Release Clamp")]
		public void ReleaseClamp(KSPActionParam param)
		{
			Release ();
		}

		[KSPEvent(guiActive = true, guiName = "Release Clamp")]
		public void Release()
		{
			Debug.LogFormat ("[ELExtendingLaunchClamp] Release");
			if (!released && part.parent != null && HighLogic.LoadedSceneIsFlight) {
				released = true;
				part.decouple ();
				if (release_fx != null) {
					release_fx.Burst ();
				}
				if (anim_decouple) {
					anim_decouple.speed = 1;
					anim.Play (anim_decouple_name);
				}
			}
		}

		public int GetStageIndex (int fallback)
		{
			return part.inverseStage;
		}
	}
}
