using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Serialization;

[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class PaperRig : MonoBehaviour
{
    /// <summary>
    /// cached to ID shader properties
    /// </summary>
    private static readonly int InvBendMatrix = Shader.PropertyToID("_InvBendMatrix");
    private static readonly int BendMatrix = Shader.PropertyToID("_BendMatrix");
    private static readonly int Center = Shader.PropertyToID("_center");
    private static readonly int Radius = Shader.PropertyToID("_radius");
    private static readonly int Offset = Shader.PropertyToID("_offset");
    private static readonly int PtA = Shader.PropertyToID("_startPt");
    private static readonly int PtB = Shader.PropertyToID("_endPt");

    /// <summary>
    /// GameObjects and state fields 
    /// </summary>
    [SerializeField] private GameObject _targetObj = null;
    [SerializeField] private Transform _bendTransform = null;
    [SerializeField] private float _bendRadius = 10;
    [SerializeField] private float _bendPhase = 0;

    private Vector3 _bendCenter;
    private Vector3 _pointA;
    private Vector3 _pointB;
    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private BoxCollider _collider;

    public bool IsValid => this.Validate();

    public void GetRenderer()
    {
        if (this.IsValid == false)
        {
            Debug.LogWarning("Target object and its bend transform should be assigned first!");
            return;
        }

        var obj = this._targetObj;

        this._collider = obj.GetComponent<BoxCollider>();

        if (this._collider == null)
        {
            this._collider = obj.AddComponent<BoxCollider>();
        }

        this._renderer = obj.GetComponent<Renderer>();

        if (this._renderer == null)
        {
            Debug.LogException(new NullReferenceException(), this);
        }
    }


    public void GetPointsAB()
    {
        if (this.IsValid == false)
        {
            return;
        }

        var objTransform = this._targetObj.transform;
        var bendNormal = this._bendTransform.forward;
        var bendPosition = this._bendTransform.position;
        var bounds = this._collider.bounds;

        // Since we are going to use absolutely flat rectangular mesh as source before band. It's boundary points will lay on same plane
        var planePts = new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, 0, bounds.max.z), new Vector3(bounds.max.x, 0, bounds.min.z) };

        // Array of original plane points projected to bend-transform XY plane
        Vector3[] projPts = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            var pt = planePts[i];
            var v = pt - bendPosition;
            var dist = Vector3.Dot(v, bendNormal);
            projPts[i] = pt - (dist * bendNormal);
        }

        // Start & end points of projection line array IDs
        int indexA = -1;
        int indexB = -1;

        float min = 0;
        float max = 0;

        // Find start & end points of projection line
        for (int i = 0; i < 4; i++)
        {
            var localPt = objTransform.InverseTransformPoint(projPts[i]);
            if (min > localPt.x)
            {
                min = localPt.x;
                indexA = i;
            }

            if (max < localPt.x)
            {
                max = localPt.x;
                indexB = i;
            }
        }

        // Prevent swapping of start/end points
        var localPt1 = objTransform.InverseTransformPoint(projPts[indexB]);
        if (localPt1.x < 0)
        {
            var a = indexA;
            var b = indexB;
            indexA = b;
            indexB = a;
        }

        // Assign values
        this._pointA = objTransform.InverseTransformPoint(projPts[indexA]);
        this._pointB = objTransform.InverseTransformPoint(projPts[indexB]);
    }

    public void GetBendCenter()
    {
        this._bendCenter = this._pointB + (this._bendTransform.up * this._bendRadius);
    }

    public void MaterialPropertyBlockUpdate()
    {
        if (this._renderer == null)
        {
            Debug.LogWarning("Seems like target object is missing renderer!");
            return;
        }

        if (this._propertyBlock == null)
        {
            this._propertyBlock = new MaterialPropertyBlock();
        }

        this._propertyBlock.SetMatrix(BendMatrix, this._bendTransform.worldToLocalMatrix);
        this._propertyBlock.SetMatrix(InvBendMatrix, this._bendTransform.localToWorldMatrix);
        this._propertyBlock.SetVector(PtA, this._pointA);
        this._propertyBlock.SetVector(PtB, this._pointB);
        this._propertyBlock.SetVector(Center, this._bendCenter);
        this._propertyBlock.SetFloat(Radius, this._bendRadius);

        this._renderer.SetPropertyBlock(this._propertyBlock);
    }

    public void BendRenerer()
    {
        this.GetPointsAB();
        this.GetBendCenter();
        this.MaterialPropertyBlockUpdate();
    }

    private void OnEnable()
    {
        this.GetRenderer();
    }

    private void Update()
    {
        this.BendRenerer();
    }

    private void OnValidate()
    {
        this.GetRenderer();
        this.BendRenerer();
    }

    private bool Validate()
    {
        if (this._targetObj == null || this._bendTransform == null)
        {
            return false;
        }

        return true;
    }
}
