using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BendRig : MonoBehaviour
{
    public GameObject Target;
    public Transform BendTransform;

    public Transform BendCenter;

    public float radius = 10;
    public float offset = 0;

    // Used only for drawing circle gizmo
    private const int CircleSegments = 120;

    private Material targetMat;
    private bool validRig;
    private Vector3 planeDirection;
    

    private Renderer targetRenderer;

    private void DrawCircle(Transform transform, Color color)
    {

        float step = (2.0f * Mathf.PI) / CircleSegments;
        float theta = 0.0f;

        Gizmos.color = color;
        Vector3 oldPos = new Vector3(0.0f, this.radius, 0.0f);
        for (int i = 0; i < CircleSegments + 1; i++)
        {
            Vector3 pos = new Vector3(this.radius * Mathf.Sin(theta), this.radius * Mathf.Cos(theta));
            Gizmos.DrawLine(transform.TransformPoint(oldPos), transform.TransformPoint(pos));
            oldPos = pos;

            theta += step;
        }
    }

    private void DrawPlaneProjection()
    {
        if (this.Target == null)
        {
            return;
        }

        var t = this.Target.transform;
        var bendNormal = this.BendTransform.forward;
        var bendPos = this.BendTransform.position;
        // var bounds = this.Target.GetComponent<MeshFilter>().sharedMesh.bounds;
        var bounds = this.Target.GetComponent<Collider>().bounds;
        //var planePts = new[] { t.TransformPoint(bounds.min), t.TransformPoint(bounds.max), t.TransformPoint(new Vector3(bounds.min.x, 0, bounds.max.z)), t.TransformPoint(new Vector3(bounds.max.x, 0, bounds.min.z)) };
        var planePts = new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, 0, bounds.max.z), new Vector3(bounds.max.x, 0, bounds.min.z) };

        // array of original plane points projected onto bend-transform XY plane
        Vector3[] projPts = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            var pt = planePts[i];
            var v = pt - bendPos;
            var dist = Vector3.Dot(v, bendNormal);
            projPts[i] = pt - (dist * bendNormal);
        }
        
        // start & end points of projection line
        int pt0 = -1;
        int pt1 = -1;

        float min = 0;
        float max = 0;

        // find start & end points of projection line
        for (int i = 0; i < 4; i++)
        {
            var localPt = t.InverseTransformPoint(projPts[i]);
            if (min > localPt.x)
            {
                min = localPt.x;
                pt0 = i;
            }

            if (max < localPt.x)
            {
                max = localPt.x;
                pt1 = i;
            }
        }

        // fix for start/end points flip
        var localPt1 = this.BendTransform.InverseTransformPoint(projPts[pt1]);
        if (localPt1.x < 0)
        {
            var a = pt0;
            var b = pt1;
            pt0 = b;
            pt1 = a;
            Debug.Log("Swap start/end");
        }


        var center = projPts[pt1] + (this.BendTransform.up * this.radius);
        this.BendCenter.position = center;

        // draw bend center
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.3f);
        Gizmos.color = new Color(1.0f, 0,0,0.5f);
        Gizmos.DrawLine(center, projPts[pt1]);

        this.DrawCircle(this.BendCenter, Color.red);

        // draw gizmos to show line segment of plane bounds projected onto bend-transform XY plane
        Gizmos.color = new Color(1, 1, 0, 0.8f);
        if (pt0 != -1 && pt1 != -1)
        {
            var a = projPts[pt0];
            var b = projPts[pt1];
            Gizmos.DrawSphere(a, 0.15f);
            Gizmos.DrawSphere(b, 0.15f);
            Gizmos.DrawLine(a, b);
        }

        if (this.targetMat == null)
        {
            this.targetMat = Target.GetComponent<Renderer>().sharedMaterial;
        }

        // set shader values
        var localPosition = this.BendCenter.localPosition;

        this.targetMat.SetMatrix("_BendMatrix", this.BendTransform.worldToLocalMatrix);
        this.targetMat.SetMatrix("_InvBendMatrix", this.BendTransform.localToWorldMatrix);
        this.targetMat.SetVector("_startPt", t.InverseTransformPoint(projPts[pt0]));
        this.targetMat.SetVector("_endPt", t.InverseTransformPoint(projPts[pt1]));
        this.targetMat.SetVector("_center", localPosition);
        this.targetMat.SetFloat("_radius", this.radius);
        this.targetMat.SetFloat("_offset", this.offset);
    }

    private void OnDrawGizmos()
    {
        DrawPlaneProjection();
    }
}
