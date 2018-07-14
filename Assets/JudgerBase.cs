using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class JudgerBase : InterfaceBase
{
	int JudgeNameId = -1;
	public string JudgeGuid = null;
	public int JudgeCategoryIndex = -1;
	public bool bIsEditing = false;
	public bool bRoutineTimeEnded = false;
	public int EditingTeamIndex = -1;
	public ECategory JudgerCategory;
	public DateTime RoutineStartTime;
	Vector2 TeamsScrollPos = new Vector2();
	public bool bIsChoosingBackup = false;
	public bool bBackupLoaded = false;
	public Vector2 BackupAreaScrollPos = new Vector2();
	public bool bIsDataDirty = false;

	public bool bHeadJudgeRequestingReady = false;
	public bool bLockedForJudging = false;
	public bool bClientReadyToBeLocked = false;
    public bool bIsDrawingEditingTeams = false;

    public GameObject JudgerCanvasUI;
    public JudgerHeaderDrawer HeaderDrawer;
    public JudgerTeamsDrawer TeamsDrawer;

	float TestReadyTimer = 1f;
	public bool TestReady = false;
	public int QuitGestureSuccessCount = 0;

	// Use this for initialization
	public void Start()
	{
		Global.NetObj.InitUdpListener(false);

		JudgerCanvasUI.SetActive(true);
	}

	public void OnEnable()
	{
		HeaderDrawer = GetComponent<JudgerHeaderDrawer>();

		HeaderDrawer.PressedHoldForReady = PressedHoldForReady;
		HeaderDrawer.ReleasedHoldForReady = ReleasedHoldForReady;
		HeaderDrawer.OnFinishedJudging = JudgePressedFinishedJudging;
		HeaderDrawer.OnRecoverAutosave = RecoverAutosave;
		HeaderDrawer.OnEditTeamsClick = OnEditTeamButtonClickCallback;
		HeaderDrawer.OnFinishEditTeamsClick = OnFinishEditTeamButtonClickCallback;

		TeamsDrawer = GetComponent<JudgerTeamsDrawer>();
		TeamsDrawer.OnTeamButtonClick = OnEditTeamSelected;

		if (JudgerCanvasUI)
		{
			JudgerCanvasUI.SetActive(true);
		}
	}

	public void OnDisable()
	{
		HeaderDrawer.PressedHoldForReady = null;
		HeaderDrawer.ReleasedHoldForReady = null;
		HeaderDrawer.OnFinishedJudging = null;
		HeaderDrawer.OnRecoverAutosave = null;
		HeaderDrawer.OnEditTeamsClick = null;
		HeaderDrawer.OnFinishEditTeamsClick = null;

		TeamsDrawer = GetComponent<JudgerTeamsDrawer>();
		TeamsDrawer.OnTeamButtonClick = null;

		if (JudgerCanvasUI)
		{
			JudgerCanvasUI.SetActive(false);
		}
	}

	public virtual void RecoverAutosave()
	{
		bIsChoosingBackup = true;

		JudgerCanvasUI.SetActive(false);
		HeaderDrawer.CanvasGO.SetActive(false);
		TeamsDrawer.CanvasGO.SetActive(false);
	}

	void JudgePressedFinishedJudging()
	{
		bRoutineTimeEnded = false;

		FinishRoutineJudging();
	}

    public void OnEditTeamButtonClickCallback()
    {
        if (!bIsJudging && !bLockedForJudging && !bClientReadyToBeLocked)
        {
            OnEditTeamsButtonClick();
        }
    }

    public virtual void OnEditTeamsButtonClick()
    {
        bIsDrawingEditingTeams = true;

        JudgerCanvasUI.SetActive(false);
        HeaderDrawer.EditTeamsButtonUI.gameObject.SetActive(false);
        HeaderDrawer.FinishEditTeamsButtonUI.gameObject.SetActive(true);

        TeamsDrawer.InitButtonTeamsText((int)CurDivision, (int)CurRound, (int)CurPool, JudgeCategoryIndex, Global.CategoryToCategoryView(JudgerCategory));
    }

    public void OnFinishEditTeamButtonClickCallback()
    {
        OnFinishEditTeamsButtonClick();
    }

    public virtual void OnFinishEditTeamsButtonClick()
    {
        FinishEditTeams();
    }

    void FinishEditTeams()
    {
        bIsDrawingEditingTeams = false;

        HeaderDrawer.FinishEditTeamsButtonUI.gameObject.SetActive(false);
        HeaderDrawer.EditTeamsButtonUI.gameObject.SetActive(true);
        TeamsDrawer.CanvasGO.SetActive(false);

		if (bIsEditing)
		{
			FinishEditingTeam(EditingTeamIndex);
		}

		JudgerCanvasUI.SetActive(true);

        ResetScoreData();
    }

	public virtual void PressedHoldForReady()
	{
		if (bHeadJudgeRequestingReady && !bIsJudging && !bLockedForJudging && !bClientReadyToBeLocked)
		{
			bHeadJudgeRequestingReady = false;
			bClientReadyToBeLocked = true;

			Global.NetObj.SendJudgeReady(JudgeGuid, true);

			TestReadyTimer = -1f;

			if (bIsEditing)
				FinishEditingTeam(EditingTeamIndex);

            FinishEditTeams();

			bBackupLoaded = false;

			ResetScoreData();
		}
	}

	void ReleasedHoldForReady()
	{
		//if (TestReadyTimer < 0)
		//    TestReady = true;

		//bClientReadyToBeLocked = false || TestReady;

		//Global.NetObj.SendJudgeReady(JudgeGuid, bClientReadyToBeLocked);
	}

	// Update is called once per frame
	public void Update()
	{
		UpdateQuitGesture();

		Global.NetObj.UpdateUdpListener();

		Global.NetObj.UpdateClientJudgeState();

		if (bIsJudging && HeaderDrawer.RoutineTime / 60f >= GetRoutineLengthMinutes())
			bRoutineTimeEnded = true;

		TestReadyTimer -= Time.deltaTime;

		if (Global.AllData == null || Global.AllNameData == null)
			return;

		if (CurPool != EPool.None)
		{
			int NameId = GetJudgeNameId();
			if (NameId != -1)
				HeaderDrawer.JudgeName = NameDatabase.FindInDatabase(NameId).DisplayName;
		}

		HeaderDrawer.DivisionString = CurDivision.ToString();
		HeaderDrawer.RoutineTime = bIsJudging ? (float)(DateTime.Now - RoutineStartTime).TotalSeconds : 0;
		HeaderDrawer.bShowReadyButton = bHeadJudgeRequestingReady && !bLockedForJudging && !bRoutineTimeEnded && !bClientReadyToBeLocked;
		HeaderDrawer.bShowFinishedButton = bRoutineTimeEnded;
		HeaderDrawer.WaitingForJudgesCount = WaitingForJudgesCount;
		HeaderDrawer.bShowWaitingForJudges = WaitingForJudgesCount > 0 && !bLockedForJudging &&
			!bIsJudging && !HeaderDrawer.bShowFinishedButton && !HeaderDrawer.bShowReadyButton && !bIsEditing;

		bIsDataDirty = false;
	}

	public int GetJudgeNameId()
	{
		if (JudgeNameId != -1 && !bIsDataDirty)
			return JudgeNameId;

		if (CurPool != EPool.None)
		{
			ResultsData Data = TournamentData.FindResultsData(CurDivision, CurRound, CurPool);
			JudgeNameId = Data.GetNameId(JudgerCategory, JudgeCategoryIndex);
		}

		return JudgeNameId;
	}

	public void ResetJudgeNameId()
	{
		JudgeNameId = -1;
	}

    public virtual void OnEditTeamSelected(int teamIndex)
    {
        StartEditingTeam(teamIndex);

        JudgerCanvasUI.SetActive(true);
    }

	public void OnGUI()
	{
        HeaderDrawer.TeamName = GetTeamNameString();
	}

    public virtual string GetTeamNameString()
    {
        if (Global.AllData == null || CurPool == EPool.None || (int)CurPool >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count)
            return "";

        List<TeamDataDisplay> Teams = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams;
		if (CurTeam == -1)
		{
			return "No Team Set";
		}
		else if (!bIsEditing)
        {
             return Teams[CurTeam].Data.PlayerNames;
        }
        else if (EditingTeamIndex >= 0)
        {
            return Teams[EditingTeamIndex].Data.PlayerNames;
        }

        return "";
    }

    public override void StartRoutineJudging()
	{
        base.StartRoutineJudging();

		if (bLockedForJudging)
		{
			bIsJudging = true;
			bRoutineTimeEnded = false;
			CurJudgingTeam = CurTeam;

			RoutineStartTime = DateTime.Now;

			Global.NetObj.SendJudgeJudging(JudgeGuid, true);
		}
	}

	public virtual void FinishRoutineJudging()
	{
        StopRoutineJudging();

		SendResultsToHeadJudger((int)CurDivision, (int)CurRound, (int)CurPool, CurJudgingTeam);

		CurJudgingTeam = -1;
	}

	public override void StopRoutineJudging()
	{
        base.StopRoutineJudging();

        bLockedForJudging = false;
        TestReady = false;
        bClientReadyToBeLocked = false;

		Global.NetObj.SendJudgeReady(JudgeGuid, false);
		Global.NetObj.SendJudgeJudging(JudgeGuid, false);
		Global.NetObj.SendJudgeLocked(JudgeGuid, false);
	}

	public override void ResetRoutineJudging()
	{
		base.ResetRoutineJudging();

		Global.NetObj.SendJudgeJudging(JudgeGuid, false);
	}

	public virtual void SendResultsToHeadJudger(int InDiv, int InRound, int InPool, int InTeam)
	{
	}

	public virtual void SendCachedResultsToHeadJudger()
	{
	}

	public virtual void StartEditingTeam(int InTeamIndex)
	{
        bIsDrawingEditingTeams = false;
		bIsEditing = true;
		EditingTeamIndex = InTeamIndex;

		Global.NetObj.SendJudgeEditing(JudgeGuid, true);
	}

	public virtual void FinishEditingTeam(int InTeamIndex)
	{
		SendResultsToHeadJudger((int)CurDivision, (int)CurRound, (int)CurPool, EditingTeamIndex);

		EditingTeamIndex = -1;
		bIsEditing = false;

		Global.NetObj.SendJudgeEditing(JudgeGuid, false);
	}

	public virtual void ResetScoreData()
	{
	}

    public virtual void LockForJudging()
    {
        bLockedForJudging = true;
    }

	public void UpdateQuitGesture()
	{
		Vector3 mousePos = Input.mousePosition;
		mousePos.y = Screen.height - mousePos.y;
		bool bMatching = false;
		if (Input.GetMouseButton(0))
		{
			Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f);
			mousePos -= screenCenter;
			if (mousePos.magnitude > Screen.height * .15f)
			{
				int curQuadrent = 0;
				if (mousePos.x > 0)
				{
					if (mousePos.y > 0)
					{
						curQuadrent = 0;
					}
					else
					{
						curQuadrent = 1;
					}
				}
				else
				{
					if (mousePos.y > 0)
					{
						curQuadrent = 3;
					}
					else
					{
						curQuadrent = 2;
					}
				}

				if (curQuadrent == (QuitGestureSuccessCount % 4))
				{
					bMatching = true;
					++QuitGestureSuccessCount;
				}
				else if (curQuadrent == ((QuitGestureSuccessCount - 1) % 4))
				{
					bMatching = true;
				}

				if (QuitGestureSuccessCount >= 10)
				{
					Application.Quit();
				}
			}
		}

		if (!bMatching)
		{
			QuitGestureSuccessCount = 0;
		}
	}
}
