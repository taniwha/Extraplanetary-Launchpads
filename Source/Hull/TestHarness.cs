using System;
using System.IO;
using System.IO.Compression;
using ExtraplanetaryLaunchpads;

using UnityEngine;
public class ConfigNode
{
	public void AddValue (string name, string val) {}
	public void AddValue (string name, Vector3 val) {}
	public string GetValue (string name) { return null; }
	public bool HasValue (string name) { return false; }
	public static Vector3 ParseVector3 (string val) { return Vector3.zero; }
}

namespace UnityEngine {

	public static class Debug
	{
		public static void Log (string msg)
		{
			Console.WriteLine (msg);
		}
	}

	public static class Mathf
	{
		public static float Min (float a, float b)
		{
			return a < b ? a : b;
		}

		public static float Max (float a, float b)
		{
			return a > b ? a : b;
		}
	}

	public struct Bounds
	{
		public Vector3 min;
		public Vector3 max;
	}

	public struct Vector3
	{
		public float x;
		public float y;
		public float z;

		public Vector3 (float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static Vector3 zero { get { return new Vector3(0, 0, 0); } }

		public float sqrMagnitude
		{
			get {
				return x * x + y * y + z * z;
			}
		}

		public float magnitude
		{
			get {
				return (float) Math.Sqrt (x * x + y * y + z * z);
			}
		}

		public Vector3 normalized
		{
			get {
				float mag = (float) Math.Sqrt (x * x + y * y + z * z);
				return new Vector3 (x / mag, y / mag, z / mag);
			}
		}

		public static float Dot (Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.y * b.y + a.z * b.z;
		}

		public static Vector3 Cross (Vector3 a, Vector3 b)
		{
			return new Vector3 (a.y * b.z - a.z * b.y,
								a.z * b.x - a.x * b.z,
								a.x * b.y - a.y * b.x);
		}

		public static Vector3 operator - (Vector3 a, Vector3 b)
		{
			return new Vector3 (a.x - b.x, a.y - b.y, a.z - b.z);
		}

		public override string ToString ()
		{
			return $"({x:F3}, {y:F3}, {z:F3})";
		}
	}

	public struct Matrix4x4
	{
		public Vector3 MultiplyPoint3x4 (Vector3 v)
		{
			return v;
		}
	}

	public class Mesh
	{
		public Vector3 []vertices;
		public int []triangles;
		public Mesh () {}
		public void RecalculateBounds () {}
		public void RecalculateNormals () {}
		public void RecalculateTangents () {}
	}
}

public class TestHarness
{
	static int Main (string []args)
	{
		bool error = false;
		foreach (var s in args) {
			switch (s) {
				case "--dump":
					Quickhull.dump_faces = true;
					break;
				default:
					BinaryReader bw;
					if (s.Substring(s.Length - 3) == ".gz") {
						bw = new BinaryReader (new GZipStream (File.Open (s, FileMode.Open, FileAccess.Read), CompressionMode.Decompress));
					} else {
						bw = new BinaryReader (File.Open (s, FileMode.Open, FileAccess.Read));
					}
					var mesh = new RawMesh (0);
					mesh.Read (bw);
					Debug.Log ($"{s} - {mesh.verts.Length} points");
					bw.Close ();
					var qh = new Quickhull (mesh);
					var timer = System.Diagnostics.Stopwatch.StartNew ();
					var hull = qh.GetHull ();
					error |= qh.error;
					timer.Stop();
					Debug.Log ($"    - {hull.Count} faces {timer.ElapsedMilliseconds}ms");
					break;
			}
		}
		return error ? 1 : 0;
	}
}
