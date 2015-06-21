using UnityEngine;
using System.Collections.Generic;

public class DecalPolygon
{
	public List<Vector3> vertices = new List<Vector3>(9);

	public DecalPolygon (params Vector3[] verts)
	{
		vertices.AddRange(verts);
	}

	public static DecalPolygon ClipPolygon (DecalPolygon poly, Plane plane)
	{
		bool[] positive = new bool[9];
		int positiveCount = 0;

		// Iterate through every vertex of the polygon <poly>.
		for (int i = 0; i < poly.vertices.Count; i++)
		{
			// Set if the current vertex is in the positive side of the plane.
			positive[i] = !plane.GetSide(poly.vertices[i]);

			if (positive[i])
				positiveCount++;
		}

		// If there are no vertices on the positive side of the plane, then
		// return null and cancel out the whole triangle polygon.
		if (positiveCount == 0)
			return null;

		// If all the vertices of the this triangle polygon are on the positive
		// side of the plane, just return the polygon as is. Nothing needs to be clipped.
		if (positiveCount == poly.vertices.Count)
			return poly;

		DecalPolygon tempPoly = new DecalPolygon();

		for (int i = 0; i < poly.vertices.Count; i++)
		{
			// <next> will start from the last vertex index and move towards the starting index.
			int next = i + 1;
			next %= poly.vertices.Count;

			// If the current vertex is within the positive side of the plane, then no clipping is needed.
			// So we just add it back to <tempPoly>.
			if (positive[i])
				tempPoly.vertices.Add(poly.vertices[i]);
			
			// If the current vertex and the next vertex are on oposite sides of the plane, then clipe them
			// so the plane and add the new clipped vertex.
			if (positive[i] != positive[next])
			{
				Vector3 v1 = poly.vertices[next];
				Vector3 v2 = poly.vertices[i];

				///Vector3 v = DecalBuilder.LineCast(plane, v1, v2);
				Vector3 v = DecalBuilder.LineCast(plane, v1, v2);

				// Add the new clipped vertex to <tempPoly>.
				tempPoly.vertices.Add(v);
			}
		}

		return tempPoly;
	}
}