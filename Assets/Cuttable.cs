using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

using VectorList = System.Collections.Generic.List<UnityEngine.Vector3>;

class Polygon
{
    public VectorList pts = new VectorList(3);

    public Polygon()
    {
    }

    public Polygon(Vector3 pt1, Vector3 pt2, Vector3 pt3)
    {
        pts.Add(pt1); pts.Add(pt2); pts.Add(pt3);
    }
    public Polygon(Vector3 pt1)
    {
        pts.Add(pt1);
    }

    public void Add(Vector3 pt1)
    {
        pts.Add(pt1);
    }

    // Returns -1 if there is no point in ptlist that is close to input vertex
    public static int FindClosestPointIndex(Vector3 input, VectorList ptList)
    {
        for (int j = 0; j < ptList.Count; ++j)
        {
            // bail once we find a vertex that's not in the list
            var delta = input - ptList[j];
            if (delta.sqrMagnitude < 0.001f)
            {
                return j;
            }
        }
        return -1;
    }

    // check if polygon uses ONLY the points passed as parameter
    public bool UsesPoints(VectorList excludedPts)
    {
        for (int i=0; i<pts.Count; ++i)
        {
            if (FindClosestPointIndex(pts[i],excludedPts)<0)                
                return false;// found a point that's not on the list!
        }
        return true;
    }

    // Make a triangle fan
    public List<Polygon> Triangulate()
    {
        var triList = new List<Polygon>();
        if (pts.Count <= 3)
        {
            triList.Add(this);
            return triList;
        }
        for (int i = 1; i <= pts.Count - 2; ++i)
        {
            int j = (i + 1);
            triList.Add(new Polygon(pts[0], pts[i], pts[j]));
        }
        return triList;
    }


    // split into one or two new polygons
    public List<Polygon> Split(Plane plane)
    {
        var polyList = new List<Polygon>();

        // Quick check to see if we are entirely on one side of the plane
        bool onSameSide = true;
        bool initialSide = plane.GetSide(pts[0]);
        for (int i = 1; i < pts.Count; ++i)
        {
            if (plane.GetSide(pts[i]) != initialSide)
            {
                onSameSide = false;
                break;
            }
        }
        // Simple case - keep everything
        if (onSameSide)
        {
            polyList.Add(this);
            return polyList;
        }

        // This polygon needs to be split into two halves.
        // Create two new polys and add vertices as needed
        Polygon first = new Polygon();
        Polygon second = new Polygon();
        polyList.Add(first);
        polyList.Add(second);

        Polygon current = first; // start adding vertices to first poly
        for (int i = 0; i < pts.Count; ++i)
        {
            // inspect edge from i to i+1
            int j = (i + 1) % pts.Count;
            Vector3 edge = pts[j] - pts[i];
            Ray ray = new Ray(pts[i], edge);
            float dist;
            if (plane.Raycast(ray, out dist) && dist < edge.magnitude)
            {
                // plane was hit, create new vertex
                Vector3 newVert = ray.GetPoint(dist);
                current.Add(pts[i]);
                current.Add(newVert);
                // now toggle, adding to other polygon
                current = (current == first) ? second : first;
                current.Add(newVert);
            } else {
                // No plane intersection- just keep adding points
                current.Add(pts[i]);
            }
        }

        return polyList;
    }
}


// Assign this to a cube!
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Collider))]
public class Cuttable : MonoBehaviour
{
    public GameObject debugObject;

    private Collider coll;
    void Awake()
    {
        coll = GetComponent<Collider>();
    }

    // We don't want polygons that only use this list of vertices
    bool ShouldExcludePoly(Polygon poly, VectorList badPoints)
    {
        return poly.UsesPoints(badPoints);
    }

    public void CutQuad(List<Vector3> ptList)
    {
        // convert from world to model space
        for (int cutpoint = 0; cutpoint < ptList.Count; ++cutpoint)
        {
            Vector3 model = transform.InverseTransformPoint(ptList[cutpoint]);
            model.z = -0.5f;
            ptList[cutpoint] = model;
        }
        ptList= ConvexHull.CH2(ptList);
        CutConvexPolygon(ptList);
    }

    //Create a quad and then cut a hole out of it given the provided points
    void CutConvexPolygon(List<Vector3> ptList)
    {
        // We start with a quad to cut the polygon into
        // quad matches the front face of a default unit cube
        var quad = new Polygon();
        quad.Add(new Vector3(-0.5f, 0.5f, -0.5f));
        quad.Add(new Vector3( 0.5f, 0.5f, -0.5f));
        quad.Add(new Vector3(0.5f, -0.5f, -0.5f));
        quad.Add(new Vector3(-0.5f,-0.5f, -0.5f));

        var polys = new LinkedList<Polygon>();
        polys.AddFirst(quad);

        Vector3 quadNormal = new Vector3(0, 0, -1);
        // Foreach point in cutting list
        for (int cutpoint = 0; cutpoint < ptList.Count; ++cutpoint)
        {
            var firstPoint = ptList[cutpoint];
            var secondPoint = ptList[(cutpoint+1) % ptList.Count];
            // Create plane going through cutting edge that is perpendicular to quad
            Plane plane = new Plane(firstPoint, secondPoint, firstPoint + quadNormal); 

            // Foreach polygon in our mesh
            var polyCount = polys.Count; // this will change as we process them, cache the value now
            for (int pnum = 0; pnum < polyCount; ++pnum)
            {
                // pop first entry and process it appending to end
                var poly = polys.First.Value;
                polys.RemoveFirst();
#if DEBUG_POLY
                if (0 == debugPoly--)
                {
                    visualize(poly);
                    
                }
#endif
                // cut polygon and append new halves back into the list
                List<Polygon> newPolys = poly.Split(plane);
                for (int i = 0; i < newPolys.Count; ++i)
                {
                    var newPoly = newPolys[i];
                    if (ShouldExcludePoly(newPoly, ptList))
                        continue;

                    polys.AddLast(newPoly);
                }
            }
        }

        var triList = new List<Polygon>(polys.Count * 3);

        // Convert polygons into triangles
        // foreach poly in polys
        var currPolyCount = polys.Count; // this will change as we process them, cache the value now
        for (int pnum = 0; pnum < currPolyCount; ++pnum)
        {
            // pop first entry and process it appending to end
            var poly = polys.First.Value;
            polys.RemoveFirst();
#if DEBUG_POLY
            if (pnum==debugPoly)
            {
                visualize(poly);
                return;
            }
#endif
            // convert into triangles and append unless it's in the hole
            List<Polygon> newTris = poly.Triangulate();
            for (int i = 0; i < newTris.Count; ++i)
            {
                triList.Add(newTris[i]);
            }
        }
#if DEBUG_POLY
        {
            var temp = new List<Polygon>(1);
            temp.Add(triList[debugTriangle]);
            GenerateMesh(temp);
            return;
        }
#endif
        GenerateMesh(triList);
    }

    public int debugTriangle = 12;
    public int debugPoly = 2;

    void GenerateMesh(List<Polygon> triList)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        // Just a lazy copy of all vertices- this should be optimized
        var newVertices = new VectorList(triList.Count*3);
        for (int t=0; t<triList.Count; ++t)
        {
            newVertices.Add(triList[t].pts[0]);
            newVertices.Add(triList[t].pts[1]);
            newVertices.Add(triList[t].pts[2]);
        }
        mesh.vertices = newVertices.ToArray();
        // For triangles we try to find the closest vertex and store its index
        var newTris = new int[triList.Count * 3];
        int index = 0;
        for (int t = 0; t < triList.Count; ++t)
        {
            newTris[index++] = index - 1;//Polygon.FindClosestPointIndex(triList[t].pts[0], newVertices);
            newTris[index++] = index - 1;//Polygon.FindClosestPointIndex(triList[t].pts[1], newVertices);
            newTris[index++] = index - 1;//Polygon.FindClosestPointIndex(triList[t].pts[2], newVertices);
        }
        mesh.triangles = newTris;
        mesh.RecalculateNormals();
    }

    void visualize(List<Polygon> tris)
    {
        for (int tri = 0; tri < tris.Count; ++tri)
        {
            visualize(tris[tri]);
        }
    }

    void visualize(Polygon poly)
    {
            for (int i = 0; i < poly.pts.Count; ++i)
            {
                Vector3 one = transform.TransformPoint(poly.pts[i]);
                Vector3 two = transform.TransformPoint(poly.pts[(i + 1) % poly.pts.Count]);
                Debug.DrawLine(one, two, Color.cyan, 100f, false);
            }
    }

    public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
    {
        // forward to sibling component
        return coll.Raycast(ray, out hitInfo, maxDistance);
    }
}
