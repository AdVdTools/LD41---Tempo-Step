using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellPool : MonoBehaviour {

    static private CellPool instance;
    static private object _lock = new object();
    static private bool applicationIsQuitting = false;
    static public CellPool Instance
    {
        get {
            lock (_lock)
            {
                if (instance == null && !applicationIsQuitting)
                {
                    instance = FindObjectOfType<CellPool>();
                    if (instance == null)
                    {
                        instance = new GameObject("CellPool").AddComponent<CellPool>();
                    }
                }
                return instance;
            }
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Debug.LogError("Already assigned an instance, "+(instance == this));
    }

    public Cell floorPrefab;
    public Cell blockPrefab;
    public Cell switchPrefab;

    private Stack<Cell> floorPool = new Stack<Cell>();
    private Stack<Cell> blockPool = new Stack<Cell>();
    private Stack<Cell> switchPool = new Stack<Cell>();

    public Cell GetCell(Map.CellType cellType)
    {
        Cell cell = null;
        switch (cellType)
        {
            case Map.CellType.Empty:
                if (floorPool.Count > 0) cell = floorPool.Pop();
                else cell = Instantiate(floorPrefab);
                break;
            case Map.CellType.Block:
                if (blockPool.Count > 0) cell = blockPool.Pop();
                else cell = Instantiate(blockPrefab);
                break;
            case Map.CellType.Switch:
                if (switchPool.Count > 0) cell = switchPool.Pop();
                else cell = Instantiate(switchPrefab);
                break;
        }
#if UNITY_EDITOR
        cell.hideFlags = HideFlags.DontSave;
#endif
        return cell;
    }

    public void PoolCell(Cell cell, Map.CellType cellType)
    {
        //Disable?
        switch (cellType)
        {
            case Map.CellType.Empty:
                floorPool.Push(cell);
                break;
            case Map.CellType.Block:
                blockPool.Push(cell);
                break;
            case Map.CellType.Switch:
                switchPool.Push(cell);
                break;
        }
    }

    public void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}
