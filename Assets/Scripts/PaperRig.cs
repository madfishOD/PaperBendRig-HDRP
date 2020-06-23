using System.Diagnostics.CodeAnalysis;
using UnityEngine;

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
    [SerializeField] private GameObject _bendGameObject = null;
    [SerializeField] private Transform _bendTransform = null;
    [SerializeField] private float _bendRadius = 10;
    [SerializeField] private float _bendPhase = 0;

    private Vector3 _bendCenter;
    private Vector3 _pointA;
    private Vector3 _pointB;
    private Renderer _renderer;
    private BoxCollider _collider;
    private MaterialPropertyBlock _propertyBlock;

    public bool IsValid => this.Validate();

    public void Initialize()
    {
        if (this.IsValid == false)
        {
            return;
        }

        this._renderer = this._bendGameObject.GetComponent<Renderer>();
        this._propertyBlock = new MaterialPropertyBlock();
    }

    public void GetPointsAB()
    {
        if (this.IsValid == false)
        {
            return;
        }

        var objTransform = this._bendGameObject.transform;
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
        this._pointA = projPts[indexA];
        this._pointB = projPts[indexB];
    }

    public void MaterialPropertyBlockUpdate()
    {
        this._renderer.SetPropertyBlock(this._propertyBlock);
    }

    private void OnEnable()
    {
        this.Initialize();
    }

    private void Update()
    {
        this.GetPointsAB();
        this.MaterialPropertyBlockUpdate();
    }

    private bool Validate()
    {
        if (this._bendGameObject == null || this._bendTransform == null)
        {
            return false;
        }

        this._collider = this._bendGameObject.GetComponent<BoxCollider>();

        if (this._collider == null)
        {
            this._collider = this._bendGameObject.AddComponent<BoxCollider>();
        }

        return true;
    }
}
