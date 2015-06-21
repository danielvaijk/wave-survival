using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Decal : MonoBehaviour
{
	public float pushDistance = 0.01f;

	[HideInInspector]
	public Bounds decalBounds = new Bounds();

	// Draws the gizmos when this GameObject is selected.
	private void OnDrawGizmosSelected ()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
	}

	private void Start ()
	{
		// Add a mesh filter.
		MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
		MeshRenderer renderer = gameObject.AddComponent<MeshRenderer> ();

		// TODO: Set the decal MeshRenderer material.

		decalBounds = GenerateBounds (transform);

		GameObject[] affectedObjects = GetAffectedObjects (decalBounds);

		// Sort the order of affected objects by distance to the decal object.
		affectedObjects = affectedObjects.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToArray();

		// Build a decal mesh based on all the affected GameObjects.
		foreach (GameObject go in affectedObjects)
		{
			DecalBuilder.BuildObjectDecal(this, go);
		}

		DecalBuilder.Push(pushDistance);

		Mesh mesh = DecalBuilder.BuildMesh();

		if (mesh != null)
		{
			mesh.name = "DecalMesh";
			filter.mesh = mesh;
		}
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

	// Generates and returns the bound box for this decal based on its global scale.
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
}