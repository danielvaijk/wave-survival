using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/* Process Example

1. Check if there are any valid GameObjects within the bounds before doing anything (saves unnecessary computations)
2. Get the Z-order of the valid GameObjects inside the projection chamber and save them to an ordered buffer
3. Iterate from first to last GameObject cutting the clipping face (on X and Y axises, for now) based on their shapes */

public class ProjectionChamber : MonoBehaviour
{
	private GameObject clippingFace = null;

	private Bounds bounds = new Bounds();

	Transform testObject;

	private void OnDrawGizmos ()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
	}

	private void Start ()
	{
		// Get the X and Y scales of the projection chamber.
		float scaleX = transform.lossyScale.x;
		float scaleY = transform.lossyScale.y;

		// Get the bounds for this projection chamber.
		bounds = GenerateBounds(transform);

		GameObject[] affectedObjects = GetAffectedObjects(bounds);

		if (affectedObjects.Length == 0)
		{
			Debug.LogWarning("No intersecting bounds, returning...");
			return;
		}

		// Get the starting point for the clipping face within the projection chamber.
		clippingFace = new GameObject("Clipping Face");

		//clippingFace.transform.parent = transform;
		clippingFace.transform.localScale = Vector3.one;
		clippingFace.transform.localRotation = Quaternion.identity;//Quaternion.LookRotation(Vector3.forward, Vector3.up);
		//clippingFace.transform.position = bounds.center + new Vector3(0f, 0f, bounds.extents.z);

		MeshFilter meshFilter = clippingFace.AddComponent<MeshFilter>();
		clippingFace.AddComponent<MeshRenderer>();

		Mesh mesh = new Mesh();

		meshFilter.mesh = mesh;

		// Generate the clipping face mesh.
		Vector3[] vertices = new Vector3[]
		{
			new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), // bottom left
			new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), // bottom right
			new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), // upper right
			new Vector3(bounds.min.x, bounds.max.y, bounds.max.z) // upper left
		};

		/*foreach (Vector3 vertex in vertices)
		{
			GameObject v = new GameObject("V");
			v.transform.position = vertex;
		}*/

		mesh.vertices = vertices;

		int[] triangles = new int[]
		{
			0, 1, 2, // lower right triangle
			2, 3, 0 // upper left triangle
		};

		mesh.triangles = triangles;

		Vector3[] normals = new Vector3[]
		{
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward
		};

		mesh.normals = normals;

		Vector2[] uv = new Vector2[]
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(0, 1),
			new Vector2(1, 1)
		};

		mesh.uv = uv;

		// Sort the order of affected objects by distance to the decal object.
		affectedObjects = affectedObjects.OrderBy(x => 
			Vector3.Distance(clippingFace.transform.position, x.transform.position)).ToArray();

		// Clip the clipping face.
		testObject = affectedObjects[0].transform;
		Bounds testBounds = GenerateBounds(testObject);
		Vector2[] testFace = GetFace(testBounds); // 2D face

		// CHECK WHEN MIN OR MAX FOR BOUNDS IS FACING AWAY OR NEAR SO WE DON'T GET THE FACE FROM
		// THE OTHER SIDE OF THE BOUNDS

		ClipFace(testFace);
	}

	private void Update ()
	{
		// Move the clipping face on the Z-axis based on the projection speed.
		//clippingFace.transform.position += transform.forward * projectionSpeed * Time.deltaTime;
	}

	private Bounds GenerateBounds (Transform trans)
	{
		Vector3 size = trans.lossyScale;
		Vector3 min = -size / 2f;
		Vector3 max =  size / 2f;

		// Set all 8 points of the bound box.
		Vector3[] boundPoints = new Vector3[]
		{
			new Vector3 (min.x, min.y, min.z),
			new Vector3 (max.x, min.y, min.z),
			new Vector3 (min.x, max.y, min.z),
			new Vector3 (max.x, max.y, min.z),

			new Vector3 (min.x, min.y, max.z),
			new Vector3 (max.x, min.y, max.z),
			new Vector3 (min.x, max.y, max.z),
			new Vector3 (max.x, max.y, max.z),
		};

		// Transform all the bound points from local to global space.
		for (int i = 0; i < 8; i++) 
		{
			boundPoints[i] = trans.TransformDirection(boundPoints[i]);
		}

		// Set the starting value for min and max.
		min = max = boundPoints [0];

		// Set min to the smallest point and max to the biggest point.
		foreach (Vector3 point in boundPoints)
		{
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}

		return new Bounds(trans.position, max - min);
	}

	private GameObject[] GetAffectedObjects (Bounds bounds)
	{
		// Get all the GameObjects in the scene with a MeshRenderer component.
		MeshRenderer[] renderers = (MeshRenderer[])FindObjectsOfType<MeshRenderer>();
		List<GameObject> objects = new List<GameObject>();

		foreach (Renderer renderer in renderers)
		{
			// Ignore non-rendered GameObjects.
			if (!renderer.enabled)
				continue;

			// Ignore Decal GameObjects.
			if (renderer.GetComponent<Decal>() != null)
				continue;

			// Add GameObjects whose renderer bounds intersect with our bounds.
			if (bounds.Intersects(renderer.bounds))
			{
				objects.Add(renderer.gameObject);
			}
		}

		return objects.ToArray();
	}

	// Currently 2D clipping only.
	private void ClipFace (Vector2[] objectFace)
	{
		Vector2[] clipFace = GetFace(bounds);
		List<Vector2> points = new List<Vector2>();

		for (int i = 0; i < 4; i++)
		{
			int next = (i + 1) % 4;

			Vector2 thisPoint = objectFace[i];
			Vector2 nextPoint = objectFace[next];

			bool thisInside = ContainsPoint(clipFace, thisPoint);
			bool nextInside = ContainsPoint(clipFace, nextPoint);

			// If the current point is inside and the next one is outside, clip it to the boundary.
			if (thisInside && !nextInside)
			{
				Ray ray = new Ray(thisPoint, nextPoint);
				float distance;

				bounds.IntersectRay(ray, out distance);

				ray.GetPoint(distance);
			}

			if (ContainsPoint(clipFace, point))
			{
				// If this point is contained within the clipping face, then add it to the list.
				GameObject p = new GameObject("CONTAINED POINT");
				p.transform.position = point;
			}
		}
	}

	// <face> = {[min, min], [max, max]}
	private bool ContainsPoint (Vector2[] face, Vector2 point)
	{
		/// FACE MUST HAVE 4 ITEMS

		if (point.x < face[0].x || point.y < face[0].y)
			return false;

		if (point.x > face[1].x || point.y < face[1].y)
			return false;

		if (point.x > face[2].x || point.y > face[2].y)
			return false;

		if (point.x < face[3].x || point.y > face[3].y)
			return false;

		return true;
	}

	// Currently 2D only.
	private Vector2[] GetFace (Bounds bounds)
	{
		return new Vector2[]
		{
			new Vector2(bounds.min.x, bounds.min.y),
			new Vector2(bounds.max.x, bounds.min.y),
			new Vector2(bounds.max.x, bounds.max.y),
			new Vector2(bounds.min.x, bounds.max.y)
		};
	}
}