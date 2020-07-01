using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PaperRig))]
public class PaperRigEditor : Editor
{
    public static readonly Color SolidColor = new Color(0.0f, 0.6f, 0.95f, 0.8f);
    public static readonly Color SemiTransparentColor = new Color(0.0f, 0.3f, 0.6f, 0.3f);

    private PaperRig _obj;


    public override void OnInspectorGUI()
    {
	    //EditorGUILayout.BeginHorizontal();
	    //this._showHandles = EditorGUILayout.Toggle(this._showHandles, GUILayout.MaxWidth(20));
	    //EditorGUILayout.LabelField("Show Handles");
     //   EditorGUILayout.EndHorizontal();
        base.OnInspectorGUI();
    }

    private void OnEnable()
    {
        this._obj = (PaperRig)target;
    }

    private void OnSceneGUI()
    {
	    this.DrawHandles();
    }

    private void DrawHandles()
    {
	    var data = this._obj.GetHandlesData();
	    if (data.ShowHandles == false)
	    {
			return;
	    }

	    Handles.color = SemiTransparentColor;
	    Handles.DrawSolidDisc(data.Center, data.Normal, data.Radius);

	    Handles.color = SolidColor;
	    Handles.DrawWireDisc(data.Center, data.Normal, data.Radius);

	    float size = 0.3f;
	    Handles.SphereHandleCap("CenterGizmo".GetHashCode(), data.Center, Quaternion.identity, size, EventType.Repaint);
	    Handles.DrawDottedLine(data.PtA, data.PtB, 3);

	    if (data.boundingPointsList != null)
	    {
		    var points = data.boundingPointsList.ToArray();
		    Handles.DrawSolidRectangleWithOutline(points, SemiTransparentColor, SolidColor);
	    }
    }
}
