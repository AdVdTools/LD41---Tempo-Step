using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [System.NonSerialized]
    public Vector2Int currentCell;
    private Vector2Int targetCell;

    public float walkSmoothTime = 0.8f;
    public float cellThreshold = 0.25f;
    
    private Vector3 target;
    private Vector3 velocity;
    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //Lerp to next
        Vector3 position = transform.position;
        position = Vector3.SmoothDamp(position, target, ref velocity, walkSmoothTime);
        transform.position = position;

        //Check if within threshold to Pop cell, reset lerp and message the map (OnPlayerMove delegate on Game?)
        if ((position - target).sqrMagnitude < cellThreshold * cellThreshold)
        {
            if (currentCell != targetCell)
            {
                currentCell = targetCell;
                Game.CurrentGame.OnPlayerMove();
                //TODO
            }
        }
	}

    public void SetTargetCell(Vector2Int targetCell) {
        this.targetCell = targetCell;
        
        this.target = Game.CurrentGame.map.GetCellWorldPosition(targetCell.y, targetCell.x);
    }
}
