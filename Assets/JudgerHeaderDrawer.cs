using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class JudgerHeaderDrawer : MonoBehaviour
{
	public string JudgeName = "Judge Name";
	public string DivisionString = "Division Name";
    public string TeamName = "Team Name";
	public float RoutineTime = 122f;
	public delegate void HoldForReadyDelegate();
	public HoldForReadyDelegate PressedHoldForReady;
	public HoldForReadyDelegate ReleasedHoldForReady;
	public delegate void FinishedJudgingDelegate();
	public FinishedJudgingDelegate OnFinishedJudging;
	public delegate void OnRecoverAutosaveDelegate();
	public OnRecoverAutosaveDelegate OnRecoverAutosave;
    public delegate void OnEditTeamsButtonClickDelegate();
    public OnEditTeamsButtonClickDelegate OnEditTeamsClick;
    public delegate void OnFinishEditTeamsButtonClickDelegate();
    public OnFinishEditTeamsButtonClickDelegate OnFinishEditTeamsClick;
	bool bIsHoldForReady = false;
	float JudgeNotReadyDelayTimer = .5f;
	public bool bShowReadyButton = false;
	public bool bShowFinishedButton = false;
	public bool bShowWaitingForJudges = false;
	public int WaitingForJudgesCount = 0;
	int ShowBackupTapCount = 0;
	float ShowBackupTapCooldown = 0f;

	public GameObject CanvasGO;
    public GameObject JudgeNameUI;
    public GameObject CategoryUI;
    public GameObject TimeUI;
    public Button ReadyButtonUI;
    public Button FinishedButtonUI;
    public Button EditTeamsButtonUI;
    public Button FinishEditTeamsButtonUI;
    public GameObject TeamNameUI;

    new void Start()
    {
        ReadyButtonUI.onClick.AddListener(() =>
            {
                OnReadyButtonClick();
            });

        FinishedButtonUI.onClick.AddListener(() =>
            {
                OnFinishedButtonClick();
            });
        EditTeamsButtonUI.onClick.AddListener(() =>
            {
                OnEditTeamsButtonClick();
            });
        FinishEditTeamsButtonUI.onClick.AddListener(() =>
            {
                OnFinishEditTeamsButtonClick();
            });

        CanvasGO.SetActive(true);
    }

    void OnReadyButtonClick()
    {
        bIsHoldForReady = true;

        if (PressedHoldForReady != null)
            PressedHoldForReady();
    }

    void OnFinishedButtonClick()
    {
        OnFinishedJudging();

        bIsHoldForReady = false;
        JudgeNotReadyDelayTimer = .5f;
    }

    void OnEditTeamsButtonClick()
    {
        if (OnEditTeamsClick != null)
            OnEditTeamsClick();
    }

    void OnFinishEditTeamsButtonClick()
    {
        if (OnFinishEditTeamsClick != null)
        {
            OnFinishEditTeamsClick();
        }
    }

	void Update()
	{
		if (ShowBackupTapCount > 0)
		{
			ShowBackupTapCooldown += Time.deltaTime;

			if (ShowBackupTapCooldown > 1f)
			{
				ShowBackupTapCooldown = 0f;
				ShowBackupTapCount = 0;
			}
		}
	}

    new void OnGUI()
	{
        JudgeNameUI.GetComponent<Text>().text = JudgeName;
        CategoryUI.GetComponent<Text>().text = DivisionString;
        TeamNameUI.GetComponent<Text>().text = TeamName;

		if (bShowReadyButton)
		{
            ReadyButtonUI.gameObject.SetActive(true);
            FinishedButtonUI.gameObject.SetActive(false);
            TimeUI.SetActive(false);

			Vector3 MousePos = Input.mousePosition;
			MousePos.y = Screen.height - MousePos.y;
			Rect ShowBackupRect = new Rect(0, 0, 200f, 200f);
			if (Input.GetMouseButtonDown(0) && ShowBackupRect.Contains(MousePos))
			{
				ShowBackupTapCooldown = 0f;
				++ShowBackupTapCount;

				if (ShowBackupTapCount > 4)
				{
					ShowBackupTapCount = 0;
					if (OnRecoverAutosave != null)
						OnRecoverAutosave();
				}
			}
		}
        else if (bShowFinishedButton)
        {
            FinishedButtonUI.gameObject.SetActive(true);
            ReadyButtonUI.gameObject.SetActive(false);
            TimeUI.SetActive(false);

            float AnimScale = 1f - Mathf.Abs(Mathf.Abs(Mathf.Cos(Time.time * 5f))) * .1f;

		}
		else if (bShowWaitingForJudges)
		{
			FinishedButtonUI.gameObject.SetActive(false);
			ReadyButtonUI.gameObject.SetActive(false);
			TimeUI.SetActive(true);

			string MessageString = "Waiting for " + WaitingForJudgesCount + " Judges";
			TimeUI.GetComponent<Text>().text = MessageString;
			TimeUI.GetComponent<Text>().fontSize = 31;
		}
		else
		{
			FinishedButtonUI.gameObject.SetActive(false);
			ReadyButtonUI.gameObject.SetActive(false);
			TimeUI.SetActive(true);

			int Minutes = Mathf.FloorToInt(RoutineTime / 60f);
			int Seconds = Mathf.FloorToInt(RoutineTime) % 60;
			string TimeString = bShowFinishedButton ? "Finished" : Minutes.ToString("0") + ":" + Seconds.ToString("00");
			TimeUI.GetComponent<Text>().text = TimeString;
			TimeUI.GetComponent<Text>().fontSize = 60;
		}
	}
}
