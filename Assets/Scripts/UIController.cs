using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    public GameObject mainPanel;
    public GameObject controlsPanel;
    public GameObject levelSelectPanel;
    public GameObject gamePanel;

    [Space]
    public Text[] controlNames = new Text[6];

    [Space]
    public Button resumeButton;

    [Space]
    public VerticalLayoutGroup layout;
    public Button buttonPrefab;
    public Text levelTooltip;

    [Space]
    public Text uiScoreText;
    public RectTransform uiNotesHolder;
    public RectTransform uiNotePrefab;
    public float xOffset = 100f;
    public float scale = 100f;

    private int waitingForControl = -1;

    public Map[] levels;
    private float[] scores;
    private int[] moves;
    private float currentScore;
    private int currentMoves;
    private int currentLevel = -1;

    private void Start()
    {
        Home();
        LoadScores();

        RectTransform layoutRT = layout.transform as RectTransform;
        layoutRT.sizeDelta = new Vector2(0f, levels.Length * 100);

        int i = 0;
        foreach (Map level in levels)
        {
            Button button = Instantiate(buttonPrefab, layoutRT);
            Text text = button.GetComponentInChildren<Text>();
            int levelIndex = i;
            i++;
            text.text = "Level " + i;
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => StartLevel(levelIndex)));
            EventTrigger trigger = button.GetComponent<EventTrigger>();

            var SetTooltipCallback = new UnityEngine.Events.UnityAction<BaseEventData>((eventData) =>
            {
                button.Select();
                levelTooltip.text = "Level " + (levelIndex + 1) + ": " + GetLevelTooltip(levelIndex);
            });

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener(SetTooltipCallback);
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry selectEntry = new EventTrigger.Entry();
            selectEntry.eventID = EventTriggerType.Select;
            selectEntry.callback.AddListener(SetTooltipCallback);
            trigger.triggers.Add(selectEntry);
        }

        RefreshControls();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gamePanel.activeInHierarchy)
            {
                Pause();
            }
            else if (controlsPanel.activeInHierarchy)
            {
                waitingForControl = -1;
                RefreshControls();
            }
        }
        else if (Input.anyKeyDown && waitingForControl != -1)
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(kcode))//This is just silly
                {
                    Game.CurrentGame.controls[waitingForControl] = kcode;
                    Game.CurrentGame.SaveControls();
                    waitingForControl = -1;
                    RefreshControls();
                    break;
                }
            }
        }
    }

    public void Home()
    {
        mainPanel.SetActive(true);
        controlsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        gamePanel.SetActive(false);

        waitingForControl = -1;
    }

    public void Controls()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        gamePanel.SetActive(false);

        waitingForControl = -1;
        RefreshControls();
    }

    public void LevelSelect()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        gamePanel.SetActive(false);

        resumeButton.gameObject.SetActive(Game.CurrentGame.GameOngoing);
        levelTooltip.text = "";
    }

    public void GameUI()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        gamePanel.SetActive(true);
    }

    private void RefreshControls()
    {
        KeyCode[] controls = Game.CurrentGame.controls;
        for (int i = 0; i < controlNames.Length; ++i)
        {
            controlNames[i].color = new Color(0.9f, 0.9f, 0.9f);
            controlNames[i].fontStyle = FontStyle.Normal;
            controlNames[i].text = controls[i].ToString();
        }
    }

    public void AwaitControl(int controlID)
    {
        waitingForControl = controlID;
        controlNames[controlID].color = new Color(1f, 1f, 0f);
        controlNames[controlID].fontStyle = FontStyle.BoldAndItalic;
    }


    public void Play()
    {
        LevelSelect();
    }

    public void Pause()
    {
        Game.CurrentGame.Pause();
        LevelSelect();
    }

    public void StartLevel(int level)
    {
        currentLevel = level;
        currentScore = -1f;
        Game.CurrentGame.SetLevel(levels[level]);
        PlaceNotes(levels[level].Tune);
        GameUI();
    }

    private void PlaceNotes(Tune tune)
    {
        foreach (Transform child in uiNotesHolder.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        float accumDelay = 0;
        foreach (Tune.Note note in tune.notes)
        {
            accumDelay += note.delay;
            Vector2 position = new Vector2(xOffset + accumDelay * scale, 0f);
            //Debug.Log(position);
            RectTransform noteRT = Instantiate(uiNotePrefab, uiNotesHolder);
            noteRT.localPosition = position;
        }
    }

    public void SetTuneTime(float time)
    {
        RectTransform rt = uiNotesHolder as RectTransform;
        rt.localPosition = new Vector3(-time * scale, rt.localPosition.y, 0f);
    }

    public void MovesUpdate(int moves)
    {
        currentMoves = moves;
    }

    public void ScoreUpdate(float score)//Just a message
    {
        currentScore = score;
    }

    public void RefreshScoreText(bool timed = false)
    {
        uiScoreText.text = "Moves: " + currentMoves;
            //+ "\nScore: " + ((int)(currentScore * 100)) + "%";
        // **** Rythm Score canceled! ****
        //uiScoreText.color = timed ? Color.cyan : new Color(0.8f, 0.8f, 0.8f);
    }

    public void RecordScore()
    {
        if (currentScore > scores[currentLevel])
        {
            scores[currentLevel] = currentScore;
            PlayerPrefs.SetFloat(levels[currentLevel].name + "_Score", currentScore);
            //SaveScores();
        }
        if (moves[currentLevel] <= 0 || currentMoves < moves[currentLevel])
        {
            moves[currentLevel] = currentMoves;
            PlayerPrefs.SetInt(levels[currentLevel].name + "_Moves", currentMoves);
        }

    }

    public void LoadScores()
    {
        scores = new float[levels.Length];
        moves = new int[levels.Length];
        for (int i = 0; i < scores.Length; ++i)
        {
            scores[i] = PlayerPrefs.GetFloat(levels[i].name + "_Score", -1);
            moves[i] = PlayerPrefs.GetInt(levels[i].name + "_Moves", -1);
        }
    }

    //public void SaveScores()
    //{
    //    Debug.LogWarning("TODO");
    //}

    private string GetLevelTooltip(int level)
    {
        float score = scores[level];
        int movesUsed = moves[level];
        if (movesUsed < 0) return "Best: --";
        else
        {
            //return /*((int)(score * 100)) + "% Complete\n" + */"Best: " + movesUsed;
            if (movesUsed == levels[level].Tune.notes.Length)
            {
                return "Best: " + movesUsed + ". Perfect!";
            }
            else
            {
                return "Best: " + movesUsed;
            }
        }
    }

    public void Resume()
    {
        Game.CurrentGame.Resume();

        GameUI();
    }

    public void OnValidate()
    {
        if (controlNames.Length != 6) System.Array.Resize(ref controlNames, 6);
    }
}
