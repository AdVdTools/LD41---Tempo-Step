using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour {

    public enum CellType { Empty = 0, Switch = 1, Block = 2 }

    public const int CELL_COUNT = 3;

    [SerializeField] private int width = 4, height = 4;

    [SerializeField] private Vector2Int startingCell;
    [SerializeField] private Vector2Int endingCell;

    //TODO serialized stuff should move to scritable object if possible
    [HideInInspector] [SerializeField] private CellType[] grid = new CellType[16];
    private Cell[] cellGrid = new Cell[16];
    [HideInInspector] [SerializeField] private byte[] startState = new byte[16];
    [HideInInspector] [SerializeField] private byte[] targetState = new byte[16];
    private byte[] currentState = new byte[16];

    [SerializeField] private Tune tune;

    public int Width { get { return width; } }
    public int Height { get { return height; } }

    public Vector2Int StartingCell { get { return startingCell; } }
    public Vector2Int EndingCell { get { return endingCell; } }

    public CellType this[int i, int j] {
        get {
            return grid[i * width + j];
        }
        set {
            grid[i * width + j] = value;
        }
    }

    public Cell[] Cells
    {
        get { return cellGrid; }
    }

    public byte[] StartState
    {
        get { return startState; }
    }
    public byte[] TargetState
    {
        get { return targetState; }
    }
    public byte[] CurrentState
    {
        get { return currentState; }
    }

    public Tune Tune {
        get { return tune; }
    }

    public Tune.Note GetNote(int distance)
    {
        int index = tune.notes.Length - 1 - distance;
        if (index < 0) index = Random.Range(0, tune.notes.Length);
        return tune.notes[index];
    }

    public int GetDistance()
    {
        int distance = 0;
        for (int index = 0; index < currentState.Length; ++index)
        {
            if (currentState[index] != targetState[index]) distance++;
        }
        return Mathf.Max(distance, 0/*Path distance?*/);
    }

    private void Awake()
    {
        InitializeState();
    }

    public void InitializeState()
    {
        currentState = new byte[startState.Length];
        System.Array.Copy(startState, currentState, startState.Length);
    }

    public void Step(Vector2Int coords)
    {
        int currentIndex = V2ToIndex(coords);
        CellType cell = grid[currentIndex];
        if (cell == Map.CellType.Switch)
        {
            byte state = currentState[currentIndex];
            state = (byte)(state > 0 ? 0 : 1);//Switch
            currentState[currentIndex] = state;
            cellGrid[currentIndex].OnCellStateChanged(state, targetState[currentIndex]);

        }
    }

    public bool CheckLegalMove(Vector2Int coords)//TODO more complex in the future
    {
        if (!CheckInBounds(coords)) return false;
        if (GetCell(coords) == CellType.Block) return false;
        return true;
    }

    public int RCToIndex(int row, int column)//TODO exceptions?
    {
        return column + width * row;
    }

    public int V2ToIndex(Vector2Int coords)//TODO exceptions?
    {
        return coords.x + width * coords.y;
    }

    public Vector2Int IndexToV2(int index)//TODO exceptions?
    {
        return new Vector2Int(index % width, index / width);
    }

    public Vector2Int GetInBounds(Vector2Int coords)
    {
        coords.x = Mathf.Clamp(coords.x, 0, width - 1);
        coords.y = Mathf.Clamp(coords.y, 0, height - 1);
        return coords;
    }

    public bool CheckInBounds(Vector2Int coords)
    {
        if (coords.x < 0 || coords.x >= width) return false;
        if (coords.y < 0 || coords.y >= height) return false;
        return true;
    }
    
    public CellType GetCell(Vector2Int coords)
    {
        return GetCell(coords.y, coords.x);
    }
    public CellType GetCell(int row, int column)
    {
        return this[row, column];
    }


    public const float sqrt3 = 1.7320508f;
    public const float cos30 = 0.8660254f;

    public Vector2 GetCellPosition(Vector2Int coords)
    {
        return GetCellPosition(coords.y, coords.x);
    }

    public Vector2 GetCellPosition(int row, int column)
    {
        Vector2 coords;
        coords.x = column * 1.5f;
        coords.y = (row * 2f - (column & 1)) * cos30;
        return coords;
    }

    public Vector3 GetCellWorldPosition(Vector2Int coords)
    {
        return GetCellWorldPosition(coords.y, coords.x);
    }

    public Vector3 GetCellWorldPosition(int row, int column)
    {
        Vector2 cellPosition = GetCellPosition(row, column);
        return transform.localToWorldMatrix.MultiplyPoint(new Vector3(cellPosition.x, 0f, cellPosition.y));
    }
    
    public static Vector2Int GetNextCell(Vector2Int currentCell, Game.Direction direction)
    {
        Vector2Int indexOffset = default(Vector2Int);
        int oddColumn = currentCell.x & 1;
        switch (direction)
        {
            case Game.Direction.NW:
                indexOffset.x = -1;
                indexOffset.y = 1 - oddColumn;
                break;
            case Game.Direction.North:
                indexOffset.x = 0;
                indexOffset.y = 1;
                break;
            case Game.Direction.NE:
                indexOffset.x = 1;
                indexOffset.y = 1 - oddColumn;
                break;

            case Game.Direction.SW:
                indexOffset.x = -1;
                indexOffset.y = -oddColumn;
                break;
            case Game.Direction.South:
                indexOffset.x = 0;
                indexOffset.y = -1;
                break;
            case Game.Direction.SE:
                indexOffset.x = 1;
                indexOffset.y = -oddColumn;
                break;
        }
        return currentCell + indexOffset;
    }

    //x is column, y is row in output
    public static Vector2Int GetCellIndices(Vector2 coords)//TODO simplify if possible
    {
        Vector2 normalizedCoords = new Vector2(coords.x / 1.5f, coords.y / sqrt3);
        int oddToEven = Mathf.FloorToInt(normalizedCoords.x) & 1;
        normalizedCoords.x += 0.5f + (oddToEven * 2 - 1) * (1 - Mathf.PingPong(normalizedCoords.y, 0.5f) * 4) / 6;
        normalizedCoords.y += 0.5f + 0.5f * ((Mathf.FloorToInt(normalizedCoords.x) + 0) & 1);
        //Debug.Log(normalizedCoords.x +" "+normalizedCoords.y + " " + ((Mathf.FloorToInt(normalizedCoords.y) + 1) & 1));
        Vector2Int indices = new Vector2Int(
            Mathf.FloorToInt(normalizedCoords.x),
            Mathf.FloorToInt(normalizedCoords.y));
        return indices;
    }

    [ContextMenu("BuildMap")]
    public void BuildMap()
    {
        if (cellGrid.Length != grid.Length) cellGrid = new Cell[grid.Length];
        for (int i = 0; i < Height; ++i)
        {
            for (int j = 0; j < Width; ++j)
            {
                int index = RCToIndex(i, j);
                CellType type = grid[index];

                Cell cell = CellPool.Instance.GetCell(type);
                Vector2 localPos = GetCellPosition(i, j);
                cell.transform.localPosition = new Vector3(localPos.x, 0f, localPos.y); 
                cell.transform.SetParent(this.transform, false);
                cell.gameObject.SetActive(true);
                //TODO cell.On*
                cell.OnCellPlaced();
                cellGrid[index] = cell;//TODO pool cell if != null, but where?
            }
        }
        StartCoroutine(DelayedLevelReady());
    }

    private IEnumerator DelayedLevelReady()
    {
        yield return new WaitForSeconds(1f);
        Game.CurrentGame.OnLevelReady();
    }

    public void ActivateCells()
    {
        for (int index = 0; index < cellGrid.Length; ++index)
        {
            Cell cell = cellGrid[index];
            cell.OnCellActivated();
            cell.OnCellStateChanged(currentState[index], targetState[index]);
        }
        Debug.LogWarning("TODO");
    }

    public void DeactivateCells()
    {
        //TODO cell.On*
        for (int index = 0; index < cellGrid.Length; ++index)
        {
            Cell cell = cellGrid[index];
            cell.OnCellDeactivated();
        }
        Debug.LogWarning("TODO");
    }

    [ContextMenu("DestroyMap")]
    public void DestroyMap()
    {
        for (int index = 0; index < cellGrid.Length; ++index)
        {
            Cell cell = cellGrid[index];
            if (cell == null) continue;
            cell.OnCellRemoved();
            cellGrid[index] = null;
            cell.gameObject.SetActive(false);
            cell.transform.SetParent(CellPool.Instance.transform);
            CellPool.Instance.PoolCell(cell, grid[index]);
        }
        Debug.LogWarning("TODO");
    }

    private void OnValidate()
    {
        int targetLength = width * height;
        if (grid.Length != targetLength) System.Array.Resize(ref grid, targetLength);
        if (cellGrid.Length != targetLength) System.Array.Resize(ref cellGrid, targetLength);

        if (startState.Length != targetLength) System.Array.Resize(ref startState, targetLength);
        if (targetState.Length != targetLength) System.Array.Resize(ref targetState, targetLength);
        if (currentState.Length != targetLength) System.Array.Resize(ref currentState, targetLength);
        
        startingCell = GetInBounds(startingCell);
        endingCell = GetInBounds(endingCell);
    }
}
