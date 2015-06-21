using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class DecalBuilder
{
	private static List<Vector3> bufVertices = new List<Vector3>();
	private static List<Vector3> bufNormals = new List<Vector3>();

	private static List<Vector2> bufUVCoords = new List<Vector2>();

	private static List<int> bufIndices = new List<int>();
 
	public static void BuildObjectDecal (Decal decal, GameObject affectedObject)
	{
		Mesh affectedMesh = affectedObject.GetComponent<MeshFilter>().sharedMesh;

		// If there is not mesh for <affectedObject>, just return.
		if (affectedMesh == null)
			return;

		Vector3[] vertices = affectedMesh.vertices;
		int[] triangles = affectedMesh.triangles;

		// TODO: Perform a linecast from the affectedobject to all other objects based on their
		// bounds and return new vertices on the edges of the linecast.

		Plane right = new Plane(Vector3.right, Vector3.right / 2f);
		Plane left = new Plane(-Vector3.right, -Vector3.right / 2f);

		Plane top = new Plane(Vector3.up, Vector3.up / 2f);
		Plane bottom = new Plane(-Vector3.up, -Vector3.up / 2f);

		Plane front = new Plane(Vector3.forward, Vector3.forward / 2f);
		Plane back = new Plane(-Vector3.forward, -Vector3.forward / 2f);

		Matrix4x4 matrix = decal.transform.worldToLocalMatrix * affectedObject.transform.localToWorldMatrix;

		// Used to keep track of our vertex count when dealing with multiple GameObjects.
		// NO NEED FOR THIS, SINCE IT WILL ALWAYS START AT 0 WHEN WE SEPERATE THE GENERATED MESHES (MAYBE)
		int startVertexCount = bufVertices.Count;

		// Iterate through all the triangles in the <affectedMesh>.
		for (int i = 0; i < triangles.Length; i += 3)
		{
			// Get all the vertices based on their respective triangle indices.
			// TODO: Figure out what MultiplyPoint() does.
			Vector3 v1 = matrix.MultiplyPoint(vertices[triangles[i]]);
			Vector3 v2 = matrix.MultiplyPoint(vertices[triangles[i + 1]]);
			Vector3 v3 = matrix.MultiplyPoint(vertices[triangles[i + 2]]);

			// Calculate the normal for this triangle.
			Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

			// Create a new polygon based on the vertices from the current triangle.
			DecalPolygon poly = new DecalPolygon(v1, v2, v3);

			// Clip the current polygon (triangle) using the Sutherland-Hodgman clipping algorithm
			// applied using the Devide and Conquer simplification method.

			if ((poly = DecalPolygon.ClipPolygon(poly, right)) == null)
				continue;

			if ((poly = DecalPolygon.ClipPolygon(poly, left)) == null)
				continue;

			if ((poly = DecalPolygon.ClipPolygon(poly, top)) == null)
				continue;

			if ((poly = DecalPolygon.ClipPolygon(poly, bottom)) == null)
				continue;

			if ((poly = DecalPolygon.ClipPolygon(poly, front)) == null)
				continue;

			if ((poly = DecalPolygon.ClipPolygon(poly, back)) == null)
				continue;

			// Add the information from this modified triangle polygon to their respective buffers.
			AddPolygon(poly, normal);
		}

		GenerateUVCoords(startVertexCount);
	}
		
	private static void AddPolygon (DecalPolygon poly, Vector3 normal)
	{
		// Get the index for the first index, which will be a constant).
		int index1 = AddVertex(poly.vertices[0], normal);

		for (int i = 1; i < poly.vertices.Count - 1; i++)
		{
			// Get the other indexes for the proceding vertices.
			int index2 = AddVertex(poly.vertices[i], normal);
			int index3 = AddVertex(poly.vertices[i + 1], normal);

			// Add all the collected triangle polygon indices.
			bufIndices.Add(index1);
			bufIndices.Add(index2);
			bufIndices.Add(index3);
		}
	}

	private static int AddVertex (Vector3 vertex, Vector3 normal)
	{
		int index = FindVertex(vertex);

		if (index == -1)
		{
			// Add the vertex and its normal to their respective buffer lists.
			bufVertices.Add(vertex);
			bufNormals.Add(normal);

			// Set the index for the newly added vertex.
			index = bufNormals.Count - 1;
		}
		else
		{
			// Add up the normals for this vertex in common.
			Vector3 total = bufNormals[index] + normal;

			// Normalize the summed normal value.
			bufNormals[index] = total.normalized;
		}

		return index;
	}

	private static int FindVertex (Vector3 vertex)
	{
		for (int i = 0; i < bufVertices.Count; i++)
		{
			if (Vector3.Distance (bufVertices[i], vertex) < 0.01f)
			{
				return i;
			}
		}

		return -1;
	}

	private static void GenerateUVCoords (int start)
	{
		for (int i = start; i < bufVertices.Count; i++)
		{
			Vector3 vertex = bufVertices[i];
			Vector2 uv = new Vector2(vertex.x, vertex.y);

			bufUVCoords.Add(uv);
		}
	}

	public static void Push (float amount)
	{
		for (int i = 0; i < bufNormals.Count; i++)
		{
			bufVertices[i] += bufNormals[i] * amount;
		}
	}

	public static Mesh BuildMesh ()
	{
		// If there are no indices, return null.
		if (bufIndices.Count == 0)
			return null;

		Mesh mesh = new Mesh();

		mesh.vertices = bufVertices.ToArray();
		mesh.normals = bufNormals.ToArray();
		mesh.uv = bufUVCoords.ToArray();
		mesh.uv2 = bufUVCoords.ToArray();
		mesh.triangles = bufIndices.ToArray();

		return mesh;
	}

	// Returns the intersecting vertex point between a ray (from vertex <a> to <b>) on a plane <plane>.
	public static Vector3 LineCast (Plane plane, Vector3 a, Vector3 b)
	{
		float distance;
		Ray ray = new Ray(a, b - a);

		// Cast a ray between vertex <a> and <b> and get the distance towards the intersection
		// point of the ray on the plane.
		plane.Raycast(ray, out distance);

		return ray.GetPoint(distance);
	}
}