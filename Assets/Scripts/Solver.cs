using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Solver {

    private Map map;
    private Vector2Int playerCell;
    private byte[] virtualState;

    public Solver(Map map)
    {
        this.map = map;
        this.playerCell = map.StartingCell;
        this.virtualState = new byte[map.StartState.Length];
    }

    public Vector2Int PlayerCell
    {
        get { return playerCell; }
        set { playerCell = value; }
    }

    private class Node
    {
        public Vector2Int cell;
        public int dist;
        public int heuristic;
        public Node prev;
        public Node(Vector2Int cell, int dist, int heuristic, Node prev) {
            this.cell = cell;
            this.dist = dist;
            this.heuristic = heuristic;
            this.prev = prev; }
    }

    public Stack<Vector2Int> FindSolution(int maxMoves)
    {
        int bestDist = int.MaxValue;

        System.Array.Copy(map.CurrentState, virtualState, virtualState.Length);

        int heuristic = GetDistance();

        List<Node> nodeList = new List<Node>();
        nodeList.Add(new Node(playerCell, 0, heuristic, null));

        //A* with break cost
        int nodeListIndex;
        for (nodeListIndex = 0; nodeListIndex < nodeList.Count; ++nodeListIndex)
        {
            bestDist = nodeList[nodeListIndex].dist + nodeList[nodeListIndex].heuristic;
            int minDistNode = nodeListIndex;

            // Find node with minimum estimated cost
            for (int li = nodeListIndex + 1; li < nodeList.Count; li++)
            {
                int dist = nodeList[li].dist + nodeList[li].heuristic;
                if (dist < bestDist) { bestDist = dist; minDistNode = li; }
            }
            //Swap positions
            Node node = nodeList[minDistNode];
            nodeList[minDistNode] = nodeList[nodeListIndex];
            nodeList[nodeListIndex] = node;

            if (bestDist > maxMoves) return null;//Too far
            //TODO check path complete and break?
            if (node.heuristic == 0) break;

            SetVirtualState(node);
            Vector2Int currentCell = node.cell;
            // Find more nodes
            for (int i = 0; i < 6; ++i)
            {
                Game.Direction direction = (Game.Direction)i;
                Vector2Int neighbourCell = Map.GetNextCell(currentCell, direction);
                //Check legal move
                if (!CheckLegalMove(neighbourCell)) continue;

                int totalDist = node.dist + 1;
                //TODO dont add nodes with high heuristic?
                
                heuristic = node.heuristic + GetStepDistanceChange(neighbourCell);
                nodeList.Add(new Node(neighbourCell, totalDist, heuristic, node));
            }
        }

        if (nodeListIndex == nodeList.Count) return null;
        else
        {
            Node node = nodeList[nodeListIndex];
            int nodeDist = node.dist;
            Stack<Vector2Int> path = new Stack<Vector2Int>(nodeDist);

            while (node != null && nodeDist > 0)
            {
                path.Push(node.cell);

                nodeDist--;
                node = node.prev;
            }
            return path;
        }
    }

    private void SetVirtualState(Node node)
    {
        int distance = node.dist;

        System.Array.Copy(map.CurrentState, virtualState, virtualState.Length);

        while (node != null && distance > 0)
        {
            Step(node.cell);

            distance--;
            node = node.prev;
        }
    }

    private int GetDistance()
    {
        int switchDistance = 0;
        byte[] targetState = map.TargetState;
        for (int index = 0; index < virtualState.Length; ++index)
        {
            if (virtualState[index] != targetState[index]) switchDistance++;
        }

        return switchDistance;
    }

    private bool CheckLegalMove(Vector2Int coords)//Might need to do special checks in the future that depend on virtual state
    {
        return map.CheckLegalMove(coords);
    }
    
    private void Step(Vector2Int coords)//TODO solve duplicity!
    {
        int currentIndex = map.V2ToIndex(coords);
        Map.CellType cell = map.GetCell(coords);//TODO optimize to use index
        if (cell == Map.CellType.Switch)
        {
            byte state = virtualState[currentIndex];
            state = (byte)(state > 0 ? 0 : 1);//Switch
            virtualState[currentIndex] = state;
        }
    }

    private int GetStepDistanceChange(Vector2Int coords)
    {
        int currentIndex = map.V2ToIndex(coords);
        Map.CellType cell = map.GetCell(coords);//TODO optimize to use index
        if (cell == Map.CellType.Switch)
        {
            byte state = virtualState[currentIndex];
            state = (byte)(state > 0 ? 0 : 1);//Switch

            if (map.TargetState[currentIndex] == state) return -1;//Closer
            else return +1;//Further
        }
        return 0;
    }
}
