using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor {

    private Mesh hexMesh;
    private Material gridMaterial;
    private Map map;

    private bool editing;

    private void OnEnable()
    {
        map = target as Map;

        gridMaterial = new Material(Shader.Find("Hidden/AdVd/GridShader"));
        gridMaterial.hideFlags = HideFlags.HideAndDontSave;

        hexMesh = new Mesh();
        hexMesh.hideFlags = HideFlags.HideAndDontSave;
        Vector3[] vertices = new Vector3[6];
        float cos30 = Mathf.Sqrt(3) / 2;
        vertices[0] = new Vector3(1, 0, 0); vertices[1] = new Vector3(0.5f, 0, -cos30); vertices[2] = new Vector3(-0.5f, 0, -cos30);
        vertices[3] = new Vector3(-1, 0, 0); vertices[4] = new Vector3(-0.5f, 0, cos30); vertices[5] = new Vector3(0.5f, 0, cos30);
        int[] lineIndices = new int[7];
        for (int i = 0; i < 6; ++i) lineIndices[i] = i;//Last index remains 0
        int[] areaIndices = new int[8];
        for (int i = 0; i < 8; ++i) areaIndices[i] = (i - (i & 4) / 4) % 6;//TODO unwrap, you silly
        hexMesh.subMeshCount = 2;
        hexMesh.vertices = vertices;
        hexMesh.SetIndices(lineIndices, MeshTopology.LineStrip, 0);
        hexMesh.SetIndices(areaIndices, MeshTopology.Quads, 1);
    }

    private void OnDisable()
    {
        if (gridMaterial != null) DestroyImmediate(gridMaterial, false);

    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        editing = GUILayout.Toggle(editing, new GUIContent("Edit"), EditorStyles.miniButton);
        Tools.hidden = editing;

        if (GUILayout.Button(new GUIContent("Solve"), EditorStyles.miniButton))
        {
            if (!EditorApplication.isPlaying) map.InitializeState();
            Solver solver = new Solver(map);
            Stack<Vector2Int> path = solver.FindSolution(60);
            if (path == null)
            {
                solution = null;
                Debug.LogError("No path within 60 moves");
            }
            else
            {
                solution = new Vector3[path.Count];
                int i = 0;
                foreach (Vector2Int cell in path)
                {
                    solution[i] = map.GetCellWorldPosition(cell);
                    ++i;
                }
                Debug.Log("Solution in " + solution.Length + " moves.");
            }
        }
        if (GUI.changed) SceneView.RepaintAll();
    }

    Ray ray;
    Vector2Int cell;
    Vector3[] solution;

    private void OnSceneGUI()
    {
        Matrix4x4 matrix = map.transform.localToWorldMatrix;
        Matrix4x4 invMatrix = map.transform.worldToLocalMatrix;

        Handles.matrix = matrix;

        if (editing && Event.current.type == EventType.MouseMove)
        {
            Vector2 screenPoint = Event.current.mousePosition;
            Ray worldRay = HandleUtility.GUIPointToWorldRay(screenPoint);
            ray = new Ray(invMatrix.MultiplyPoint(worldRay.origin),
                invMatrix.MultiplyVector(worldRay.direction));
            Event.current.Use();//TODO check LC prefab placer test
        }


        if (editing)
        {
            Vector3 intersection = ray.GetPoint(ray.origin.y / -ray.direction.y);
            //Debug.Log(intersection + " " + ray);

            cell = Map.GetCellIndices(new Vector2(intersection.x, intersection.z));

            int controlId = GUIUtility.GetControlID(new GUIContent("MapEditor"), FocusType.Passive);
            if (Event.current.type == EventType.Layout)
            {//This will allow us to eat the click
                HandleUtility.AddDefaultControl(controlId);
            }
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                if (map.CheckInBounds(cell))
                {
                    Undo.RecordObject(map, "Map Changed");
                    Map.CellType value = map[cell.y, cell.x];
                    int index = map.V2ToIndex(cell);
                    if (Event.current.control)
                    {
                        if (value == Map.CellType.Switch)
                        {
                            if (Event.current.shift)
                            {
                                map.StartState[index] = (byte)(map.StartState[index] > 0 ? 0 : 1);
                                if (index >= 0 && index < map.CurrentState.Length) map.CurrentState[index] = map.StartState[index];
                            }
                            else
                            {
                                map.TargetState[index] = (byte)(map.TargetState[index] > 0 ? 0 : 1);
                            }
                        }
                    }
                    else
                    {
                        value = (Map.CellType)(((int)value + 1) % Map.CELL_COUNT);
                        map[cell.y, cell.x] = value;
                        map.StartState[index] = 0;
                        map.TargetState[index] = 0;
                        /*if (index >= 0 && index < map.CurrentState.Length) */map.CurrentState[index] = 0;
                    }
                }
                Event.current.Use();
            }
        }

        Matrix4x4 cellMatrix;
        gridMaterial.color = Color.blue;
        gridMaterial.SetPass(1);
        if (editing && map.CheckInBounds(cell))
        {
            Vector2 cellPos = map.GetCellPosition(cell);
            cellMatrix = matrix * Matrix4x4.TRS(new Vector3(cellPos.x, 0f, cellPos.y), Quaternion.identity, Vector3.one);
            Graphics.DrawMeshNow(hexMesh, cellMatrix, 1);// area
        }

        gridMaterial.color = Color.green;
        gridMaterial.SetPass(1);
        Vector2 startPos = map.GetCellPosition(map.StartingCell);
        cellMatrix = matrix * Matrix4x4.TRS(new Vector3(startPos.x, 0f, startPos.y), Quaternion.identity, Vector3.one);
        Graphics.DrawMeshNow(hexMesh, cellMatrix, 1);// area

        gridMaterial.color = Color.red;
        gridMaterial.SetPass(1);
        Vector2 endPos = map.GetCellPosition(map.EndingCell);
        cellMatrix = matrix * Matrix4x4.TRS(new Vector3(endPos.x, 0f, endPos.y), Quaternion.identity, Vector3.one);
        Graphics.DrawMeshNow(hexMesh, cellMatrix, 1);// area

        gridMaterial.color = Color.white;
        gridMaterial.SetPass(0);
        for (int i = 0; i < map.Height; ++i)
        {
            for (int j = 0; j < map.Width; ++j)
            {
                Map.CellType cell = map[i, j];

                Vector2 cp = map.GetCellPosition(i, j);
                cellMatrix = matrix * Matrix4x4.TRS(new Vector3(cp.x, 0f, cp.y), Quaternion.identity, Vector3.one);

                Graphics.DrawMeshNow(hexMesh, cellMatrix, 0);// line
                cellMatrix = matrix * Matrix4x4.TRS(new Vector3(cp.x, 0f, cp.y), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f));
                switch (cell)
                {
                    case Map.CellType.Block:
                        Graphics.DrawMeshNow(hexMesh, cellMatrix, 1);// area
                        break;
                    case Map.CellType.Switch:
                        Graphics.DrawMeshNow(hexMesh, cellMatrix, 0);// line
                        break;
                }
            }
        }

        gridMaterial.color = Color.cyan;
        gridMaterial.SetPass(0);
        for (int i = 0; i < map.Height; ++i)
        {
            for (int j = 0; j < map.Width; ++j)
            {
                bool state = map.TargetState[map.RCToIndex(i, j)] > 0;

                Vector2 cp = map.GetCellPosition(i, j);

                cellMatrix = matrix * Matrix4x4.TRS(new Vector3(cp.x, 0f, cp.y), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f));
                if (state)
                {
                    Graphics.DrawMeshNow(hexMesh, cellMatrix, 0);// line
                }
            }
        }

        gridMaterial.color = Color.cyan;
        gridMaterial.SetPass(1);
        for (int i = 0; i < map.Height; ++i)
        {
            for (int j = 0; j < map.Width; ++j)
            {
                int index = map.RCToIndex(i, j);
                bool state = map.StartState[index] > 0;
                if (EditorApplication.isPlaying)
                {
                    //if (index >= 0 && index < map.CurrentState.Length)//currentState size can't be assumed
                    state = map.CurrentState[index] > 0;
                }
                

                Vector2 cp = map.GetCellPosition(i, j);

                cellMatrix = matrix * Matrix4x4.TRS(new Vector3(cp.x, 0f, cp.y), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f));
                if (state)
                {
                    Graphics.DrawMeshNow(hexMesh, cellMatrix, 1);// area
                }
            }
        }

        if (solution != null)
        {
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(5f, solution);
        }
    }

}
