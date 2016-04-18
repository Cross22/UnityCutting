using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class Cutter : MonoBehaviour {
    public Cuttable objectToCut;
    public float minVertexDistance= 0.2f; // how closely spaced cut points should be generated
    public GameObject debugObject;


    List<Vector3> cutPoints = new List<Vector3>();
    float minDistSq;
    void Awake()
    {
        Assert.IsNotNull(objectToCut);
        minDistSq = minVertexDistance * minVertexDistance; // for faster calculations
    }

    void FinishCutting()
    {
        this.enabled = false;
        objectToCut.CutQuad(cutPoints);
        cutPoints.Clear();
    }

    public void ProcessPoint(RaycastHit pt)
    {
        var ptCoord = pt.point;
        for (int i = 0; i < cutPoints.Count; ++i)
        {
            // far enough away for new point?
            if ((cutPoints[i]- ptCoord).sqrMagnitude < minDistSq)
            {
                // Special case: close to origin again
                if (i==0 && cutPoints.Count>3)
                {
                    FinishCutting();   
                }
                return;
            }
        }
        cutPoints.Add(pt.point);

        var go = Instantiate(debugObject, ptCoord, Quaternion.identity) as GameObject;
        go.transform.SetParent(transform);
    }

    void Update()
    {
#if DEBUG_CUTTING
        if (Input.GetMouseButton(0))
        {
            cutPoints.Add(new Vector3(0, 0.5f, -0.1f));
            cutPoints.Add(new Vector3(0.3f, 0.3f, -0.1f));
            cutPoints.Add(new Vector3(-0.3f, 0.3f, -0.1f));
            objectToCut.CutConvexPolygon(cutPoints);
            cutPoints.Clear();

        }
#endif        
        if (Input.GetMouseButton(0))
        {
            // Ray from mouse position in screen XXX units into the world
            // only check for collision with the object specified in
            // objectToCut
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (objectToCut.Raycast(ray, out hit, 10.0F))
            {
                ProcessPoint(hit);
            }

        }
    }
}
