using System;
using System.Diagnostics.CodeAnalysis;
using Boo.Lang;
using UnityEngine;

public struct HandlesData
{
	public bool ShowHandles;
    public Vector3 Center;
    public Vector3 Normal;
    public Vector3 PtA;
    public Vector3 PtB;
    public float Radius;

    public List<Vector3> boundingPointsList;
}

[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
[ExecuteInEditMode]
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
    private static readonly int PtA = Shader.PropertyToID("_PtA");
    private static readonly int PtB = Shader.PropertyToID("_PtB");

    /// <summary>
    /// GameObjects and state fields 
    /// </summary>
    [SerializeField] private bool _showHandles = true;
    [SerializeField] private GameObject _paperObj = null;
    [SerializeField] [HideInInspector]
    private GameObject _bendTransformObj = null;
    [SerializeField] private Vector2 _paperSize = new Vector2(10f, 10f);
    [SerializeField] private float _bendAxisRotation = 0;
    [SerializeField] private float _bendRadius = 10;
    [SerializeField] private float _bendPhase = 0;

    private Vector3 _bendCenter;
    private Vector3 _pointA;
    private Vector3 _pointB;
    private Vector3 _worldPointA;
    private Vector3 _worldPointB;
    private List<Vector3> _originalBounds;
    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    public bool IsValid => this.Validate();

    public HandlesData GetHandlesData()
    {
        HandlesData data = new HandlesData();

        if (this.IsValid == true)
        {
	        data.ShowHandles = this._showHandles;
            data.Center = this._bendCenter;
            data.Normal = this._bendTransformObj.transform.forward;
            data.PtA = this._worldPointA;
            data.PtB = this._worldPointB;
            data.Radius = this._bendRadius;
            data.boundingPointsList = this._originalBounds;
        }

        return data;
    }

    public void GetRenderer()
    {
        if (this.IsValid == false)
        {
            Debug.LogWarning("Target object and its bend transform should be assigned first!");
            return;
        }

        var obj = this._paperObj;

        this._renderer = obj.GetComponent<Renderer>();

        if (this._renderer == null)
        {
            Debug.LogException(new NullReferenceException(), this);
        }
    }
    public void SetBendAxisRotation(float angle)
    {
	    if (this._bendTransformObj == null)
	    {
			return;
	    }

	    this._bendTransformObj.transform.localEulerAngles = new Vector3(0, angle, 0);
    }

    public void GetPointsAB()
    {
        if (this.IsValid == false)
        {
            return;
        }

        var objTransform = this._paperObj.transform;
        var bendNormal = this._bendTransformObj.transform.forward;
        var bendPosition = this._bendTransformObj.transform.position;

        var pos = this._paperObj.transform.position;
        var width = this._paperSize.x * 0.5f;
        var length = this._paperSize.y * 0.5f;

        // Since we are going to use absolutely flat rectangular mesh as source before band. It's boundary points will lay on same plane
        var planePts = new[] { new Vector3(-width,0, -length), new Vector3(-width,0, length), new Vector3(width, 0,-length), new Vector3(width,0, length) };
        for (int i = 0; i < planePts.Length; i++)
        {
            var pt = objTransform.TransformPoint(planePts[i]);
            planePts[i] = pt;
        }
        this._originalBounds = new List<Vector3>() { planePts[0], planePts[1], planePts[3], planePts[2] };

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
        var localPt1 = this._bendTransformObj.transform.InverseTransformPoint(projPts[indexB]);
        if (localPt1.x < 0)
        {
            var a = indexA;
            var b = indexB;
            indexA = b;
            indexB = a;
        }

        if (indexA == -1 || indexB == -1)
        {
            return;
        }

        // Assign values
        this._worldPointA = projPts[indexA];
        this._worldPointB = projPts[indexB];
        this._pointA = objTransform.InverseTransformPoint(projPts[indexA]);
        this._pointB = objTransform.InverseTransformPoint(projPts[indexB]);
    }

    public void GetBendCenter()
    {
        if (this.IsValid == false)
        {
            return;
        }

        this._bendCenter = this._worldPointB + (this._bendTransformObj.transform.up * this._bendRadius);
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

        var localPos = this._bendTransformObj.transform.InverseTransformPoint(this._bendCenter);

        this._propertyBlock.SetMatrix(BendMatrix, this._bendTransformObj.transform.worldToLocalMatrix);
        this._propertyBlock.SetMatrix(InvBendMatrix, this._bendTransformObj.transform.localToWorldMatrix);
        this._propertyBlock.SetVector(PtA, this._pointA);
        this._propertyBlock.SetVector(PtB, this._pointB);
        this._propertyBlock.SetVector(Center, localPos);
        this._propertyBlock.SetFloat(Radius, this._bendRadius);
        this._propertyBlock.SetFloat(Offset, this._bendPhase);

        this._renderer.SetPropertyBlock(this._propertyBlock);
    }

    public void BendRenderer()
    {
        this.GetPointsAB();
        this.GetBendCenter();
        this.MaterialPropertyBlockUpdate();
    }

    private void OnEnable()
    {
        this.GetRenderer();
    }

    private bool Validate()
    {

	    if (this._paperObj == null)
	    {
		    return false;
	    }

	    if (this._bendTransformObj == null)
	    {
		    this._bendTransformObj = new GameObject {name = "BendTransformObj"};
		    this._bendTransformObj.transform.parent = this._paperObj.transform;
		    this._bendTransformObj.transform.localPosition = Vector3.zero;
		    this._bendTransformObj.transform.localRotation = Quaternion.identity;
		    this._bendTransformObj.hideFlags = HideFlags.HideInHierarchy;
	    }

        return true;
    }

    private void LateUpdate()
    {
        this.BendRenderer();
        this.SetBendAxisRotation(this._bendAxisRotation);
    }

    private void OnValidate()
    {
        this.GetRenderer();
        this.BendRenderer();
		this.SetBendAxisRotation(this._bendAxisRotation);
    }
}
