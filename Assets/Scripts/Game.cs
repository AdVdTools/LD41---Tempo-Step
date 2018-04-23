using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {

    public enum Direction {
        None = -1,
        NW = 0, North = 1, NE = 2,
        SW = 3, South = 4, SE = 5,
    }

    public class InputEvent
    {
        public Direction direction;
        public float ttl;

        public InputEvent(Direction i, float v)
        {
            this.direction = i;
            this.ttl = v;
        }
    }

    public readonly KeyCode[] controls = new KeyCode[6] {
        KeyCode.Q, KeyCode.W, KeyCode.E,
        KeyCode.A, KeyCode.S, KeyCode.D,
    };

    private const int MAX_INPUTS = 5;
    
    static private Game currentGame;
    static public Game CurrentGame { get { return currentGame; } }

    public Map map;
    public Player player;
    public SoundPlayer soundPlayer;
    public UIController uiController;

    [Space]
    public float inputBufferTime = 0.4f;

    private bool ongoing = false;
    private bool acceptingInput = false;
    private Queue<InputEvent> inputQueue = new Queue<InputEvent>();
    private int stepCount;

    private int currentNote;
    private int tuneScore;
    private float nextNoteTime;
    private float tuneTime;
    [Space]
    public float tuneDelay = 2f;
    public float timeTolerance = 0.2f;

    private Solver solver;

    public bool GameOngoing { get { return ongoing; } }

    private void Awake()
    {
        currentGame = this;
    }

    // Use this for initialization
    void Start() {
        player.gameObject.SetActive(false);

        LoadControls();
    }

    public void LoadControls()
    {
        controls[0] = (KeyCode)PlayerPrefs.GetInt("NW", (int)controls[0]);
        controls[1] = (KeyCode)PlayerPrefs.GetInt("North", (int)controls[1]);
        controls[2] = (KeyCode)PlayerPrefs.GetInt("NE", (int)controls[2]);
        controls[3] = (KeyCode)PlayerPrefs.GetInt("SW", (int)controls[3]);
        controls[4] = (KeyCode)PlayerPrefs.GetInt("South", (int)controls[4]);
        controls[5] = (KeyCode)PlayerPrefs.GetInt("SE", (int)controls[5]);
    }

    public void SaveControls()
    {
        PlayerPrefs.SetInt("NW", (int)controls[0]);
        PlayerPrefs.SetInt("North", (int)controls[1]);
        PlayerPrefs.SetInt("NE", (int)controls[2]);
        PlayerPrefs.SetInt("SW", (int)controls[3]);
        PlayerPrefs.SetInt("South", (int)controls[4]);
        PlayerPrefs.SetInt("SE", (int)controls[5]);
    }

    public void SetLevel(Map newMap)
    {
        if (map != null)
        {
            map.DestroyMap();
            Destroy(map);
        }

        this.map = Instantiate(newMap);
        this.solver = new Solver(map);

        Vector3 startingPosition = map.GetCellWorldPosition(map.StartingCell);
        player.SetTargetCell(map.StartingCell);
        player.currentCell = map.StartingCell;
        player.transform.position = startingPosition;
        player.gameObject.SetActive(true);

        uiController.uiNotesHolder.gameObject.SetActive(false);

        map.BuildMap();
    }

    public void OnLevelReady()
    { 
        map.ActivateCells();

        ongoing = true;
        acceptingInput = true;
        stepCount = 0;

        currentNote = 0;
        tuneScore = 0;
        tuneTime = -tuneDelay * map.Tune.tempo;
        nextNoteTime = map.Tune.notes[currentNote].delay;
        uiController.uiNotesHolder.gameObject.SetActive(true);
        uiController.SetTuneTime(tuneTime);

        uiController.MovesUpdate(0);
        uiController.ScoreUpdate(0f);
        uiController.RefreshScoreText(false);
    }

    public void Pause()
    {
        acceptingInput = false;
    }

    public void Resume()
    {
        acceptingInput = true;
    }

    private void FinishGame()
    {
        ongoing = false;
        acceptingInput = false;

        //if (map.Tune.notes.Length == stepCount)
        //{
        //    map.Tune.PlayTune();
        //}

        uiController.RecordScore();
        uiController.Play();//Goes to level select
    }
    
	// Update is called once per frame
	void Update () {
        for (int i = 0; i < 6; ++i)
        {
            if (Input.GetKeyDown(controls[i]))
            {
                if (inputQueue.Count < MAX_INPUTS)
                {
                    InputEvent e = new InputEvent((Direction)i, Time.time + inputBufferTime);
                    inputQueue.Enqueue(e);
                }
            }
        }

        if (uiController.uiNotesHolder.gameObject.activeInHierarchy)
        {
            tuneTime += Time.deltaTime * map.Tune.tempo;
            uiController.SetTuneTime(tuneTime);
        }

        while (inputQueue.Count > 0 && acceptingInput)
        {
            InputEvent e = inputQueue.Dequeue();
            if (e.ttl < Time.time) continue;//TOO old
            HandleInput(e.direction);
        }
	}
    
    public void OnPlayerMove()
    {
        stepCount++;
        map.Step(player.currentCell);

        //int distance = GetDistance();//TODO Replace calls
        solver.PlayerCell = player.currentCell;
        Stack<Vector2Int> path = solver.FindSolution(map.Tune.notes.Length * 2);
        int distance = path != null ? path.Count : map.Tune.notes.Length;
        Tune.Note note = map.GetNote(distance);
        soundPlayer.Play(note.key);

        //Debug.Log(tuneTime - nextNoteTime);
        uiController.MovesUpdate(stepCount);
        if (Mathf.Abs(tuneTime - nextNoteTime) < timeTolerance)
        {
            tuneScore++;
            uiController.ScoreUpdate((float)tuneScore / (stepCount + distance));
            uiController.RefreshScoreText(true);
        }
        else
        {
            uiController.RefreshScoreText(false);
        }

        currentNote++;
        if (currentNote < map.Tune.notes.Length)
        {
            nextNoteTime += map.Tune.notes[currentNote].delay;
        }

        acceptingInput = true;

        Debug.Log(path.Count);
        //Check distance, check if finished
        if (path.Count == 0)
        {
            FinishGame();
        }
    }

    private void HandleInput(Direction direction) {
        Vector2Int nextCell = Map.GetNextCell(player.currentCell, direction);
        
        if (!map.CheckLegalMove(nextCell)) return;

        acceptingInput = false;
        player.SetTargetCell(nextCell);
    }
}
