using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataPlotter))]
public class DataPlotterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DataPlotter myScript = (DataPlotter)target;
        if(GUILayout.Button("Create"))
        {
            myScript.initGraph();
        }
    }
}
