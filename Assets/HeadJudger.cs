using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Timers;

public class HeadJudger : MonoBehaviour
{
	public EDivision CurDivision = EDivision.Open;
	public ERound CurRound = ERound.Prelims;
	public EPool CurPool = EPool.None;
	public bool IsJudgingSecondaryPool { get { return bFestivalJudging && CurPool == CurFestivalPool; } }
	public int CurTeam
	{
		get { return IsJudgingSecondaryPool ? CurPrivateTeam2 : CurPrivateTeam; }
		set { if (IsJudgingSecondaryPool) CurPrivateTeam2 = value; else CurPrivateTeam = value; }
	}
	int CurPrivateTeam = -1;
	int CurPrivateTeam2 = -1;
	public bool bFestivalJudging = false;
	public EPool CurFestivalPool = EPool.None;
	public DivisionComboBox DivisionCombo;
	public RoundComboBox RoundCombo;
	public Vector2 PoolsScrollPos = new Vector2();
	List<NetworkJudgeData> AIJudges = new List<NetworkJudgeData>();
	List<NetworkJudgeData> ExJudges = new List<NetworkJudgeData>();
	List<NetworkJudgeData> DiffJudges = new List<NetworkJudgeData>();
	public bool bLockedForJudging = false;
	DateTime RoutineStartTime;
	public bool bJudging;
	int CancelClickCount = 0;
	float CancelClickTimer = -1f;
	float ResetRoutineStartTime = 20f;
	float CurrentResetRoutineTime = 0f;
	float ResetStartTimeCooldown = 1.5f;
	float CurrentResetStartTimeCooldown = 0;
	public int ActiveJudgingJudgers = 0;
	bool bRoutineTimeElapsed = false;

	// Use this for initialization
	void Start()
	{
		Global.LoadTournamentData();

		DivisionCombo = new DivisionComboBox();
		RoundCombo = new RoundComboBox();

		Debug.Log(Network.InitializeServer(32, 8765, false));

		Global.NetObj.InitUdpListener(true);

		Global.DivDataState = 0;
		Global.CurDataState = 0;
	}

	// Update is called once per frame
	public void Update()
	{
		Global.NetObj.UpdateUdpListener();

		Global.NetObj.UpdateHeadJudgeState();

		CurDivision = (EDivision)DivisionCombo.GetSelectedItemIndex();
		CurRound = (ERound)RoundCombo.GetSelectedItemIndex();

		UpdateConnectedJudgeList(ref AIJudges);
		UpdateConnectedJudgeList(ref ExJudges);
		UpdateConnectedJudgeList(ref DiffJudges);

		UpdateActiveJudgingJudgersCount();
	}

	void UpdateConnectedJudgeList(ref List<NetworkJudgeData> InList)
	{
		for (int JudgeIndex = 0; JudgeIndex < InList.Count; ++JudgeIndex)
		{
			if (InList[JudgeIndex] != null)
			{
				string JudgeGuid = InList[JudgeIndex].Guid;
				if (!Global.NetObj.ContainsConnectedGuid(JudgeGuid))
				{
					OnDisconnectedJudger(JudgeGuid);
					InList[JudgeIndex] = null;
				}
			}
		}
	}

	int GetIndexOfGuid(List<NetworkJudgeData> InList, string InGuid)
	{
		for (int i = 0; i < InList.Count; ++i)
		{
			if (InList[i] != null && InList[i].Guid == InGuid)
				return i;
		}

		return -1;
	}

	NetworkJudgeData GetJudgeDataFromGuid(List<NetworkJudgeData> InList, string InGuid)
	{
		for (int i = 0; i < InList.Count; ++i)
		{
			if (InList[i] != null && InList[i].Guid == InGuid)
				return InList[i];
		}

		return null;
	}

	public NetworkJudgeData GetJudgeData(string InGuid)
	{
		NetworkJudgeData Data = GetJudgeDataFromGuid(AIJudges, InGuid);
		if (Data == null)
			Data = GetJudgeDataFromGuid(ExJudges, InGuid);
		if (Data == null)
			Data = GetJudgeDataFromGuid(DiffJudges, InGuid);

		return Data;
	}

	public int GetJudgeIndexRaw(string InGuid)
	{
		int Ret = -1;
		Ret = GetIndexOfGuid(AIJudges, InGuid);
		if (Ret < 0)
			Ret = GetIndexOfGuid(ExJudges, InGuid);
		if (Ret < 0)
			Ret = GetIndexOfGuid(DiffJudges, InGuid);

		return Ret;
	}

	public EPool GetJudgePool(string InGuid)
	{
		return CurPool;
	}

	public int GetJudgeTeam(string InGuid)
	{
        return CurTeam;
	}

	public int GetActiveJudgingJudgers(string InGuid)
	{
		return ActiveJudgingJudgers;
	}

	public void SetJudgeReady(string InGuid, bool bReady)
	{
		NetworkJudgeData Data = GetJudgeData(InGuid);
		if (Data != null)
		{
			Data.bIsHoldReady = bReady;
		}
	}

	public void SetJudgeLocked(string InGuid, bool bLocked)
	{
		NetworkJudgeData Data = GetJudgeData(InGuid);
		if (Data != null)
		{
			Data.bLocked = bLocked;
		}
	}

	public void SetJudgeJudging(string InGuid, bool bJudging)
	{
		NetworkJudgeData Data = GetJudgeData(InGuid);
		if (Data != null)
		{
			Data.bJudging = bJudging;
		}
	}

	public void SetJudgeEditing(string InGuid, bool bEditing)
	{
		NetworkJudgeData Data = GetJudgeData(InGuid);
		if (Data != null)
		{
			Data.bEditing = bEditing;
		}
	}

	public void SetJudgeNameId(string InGuid, int InNameId)
	{
		NetworkJudgeData Data = GetJudgeData(InGuid);
		if (Data != null)
		{
			Data.NameId = InNameId;
		}
	}

    public void SetCurrentPool(EPool NewCurPool)
    {
        CurPool = NewCurPool;

        if (NewCurPool != EPool.None && Global.IsValid(CurDivision, CurRound, NewCurPool, 0))
        {
            CurTeam = 0;
        }
    }

	public NetworkJudgeData GetJudgeDataFromNameId(ResultsData InJudgeData, int InId)
	{
		foreach (NetworkJudgeData njd in AIJudges)
		{
			if (njd != null && njd.NameId == InId)
			{
				return njd;
			}
		}

		foreach (NetworkJudgeData njd in ExJudges)
		{
			if (njd != null && njd.NameId == InId)
			{
				return njd;
			}
		}

		foreach (NetworkJudgeData njd in DiffJudges)
		{
			if (njd != null && njd.NameId == InId)
			{
				return njd;
			}
		}

		return null;
	}

	public bool IsInSecondaryPool(string InGuid)
	{
		if (bFestivalJudging)
		{
			ResultsData JData = TournamentData.FindResultsData(CurDivision, CurRound, CurFestivalPool);
			if (JData != null)
			{
				for (int JudgeIndex = 0; JudgeIndex < AIJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = AIJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < JData.AIJudgeIds.Count)
							return false;
						else
							return true;
					}
				}
				for (int JudgeIndex = 0; JudgeIndex < ExJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = ExJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < JData.ExJudgeIds.Count)
							return false;
						else
							return true;
					}
				}
				for (int JudgeIndex = 0; JudgeIndex < DiffJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = DiffJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < JData.DiffJudgeIds.Count)
							return false;
						else
							return true;
					}
				}
			}
		}

		return false;
	}

	public int GetCatIndexForGuid(string InGuid)
	{
		int InOrgIndex = GetJudgeIndexRaw(InGuid);

		if (bFestivalJudging)
		{
			ResultsData RData = TournamentData.FindResultsData(CurDivision, CurRound, CurFestivalPool);
			if (RData != null)
			{
				for (int JudgeIndex = 0; JudgeIndex < AIJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = AIJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < RData.AIJudgeIds.Count)
							return InOrgIndex;
						else
							return InOrgIndex - RData.AIJudgeIds.Count;
					}
				}
				for (int JudgeIndex = 0; JudgeIndex < ExJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = ExJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < RData.ExJudgeIds.Count)
							return InOrgIndex;
						else
							return InOrgIndex - RData.ExJudgeIds.Count;
					}
				}
				for (int JudgeIndex = 0; JudgeIndex < DiffJudges.Count; ++JudgeIndex)
				{
					NetworkJudgeData njd = DiffJudges[JudgeIndex];
					if (njd != null && njd.Guid == InGuid)
					{
						if (JudgeIndex < RData.DiffJudgeIds.Count)
							return InOrgIndex;
						else
							return InOrgIndex - RData.DiffJudgeIds.Count;
					}
				}
			}
		}

		return InOrgIndex;
	}

	void InitJudgersNameIds()
	{
		ResultsData RData = TournamentData.FindResultsData(CurDivision, CurRound, CurPool);

		if (RData != null)
		{
			for (int JudgeIndex = 0; JudgeIndex < AIJudges.Count; ++JudgeIndex)
			{
				if (AIJudges[JudgeIndex] != null && JudgeIndex < RData.AIJudgeIds.Count)
				{
					AIJudges[JudgeIndex].NameId = RData.AIJudgeIds[JudgeIndex];
				}
			}

			for (int JudgeIndex = 0; JudgeIndex < ExJudges.Count; ++JudgeIndex)
			{
				if (ExJudges[JudgeIndex] != null && JudgeIndex < RData.ExJudgeIds.Count)
				{
					ExJudges[JudgeIndex].NameId = RData.ExJudgeIds[JudgeIndex];
				}
			}

			for (int JudgeIndex = 0; JudgeIndex < DiffJudges.Count; ++JudgeIndex)
			{
				if (DiffJudges[JudgeIndex] != null && JudgeIndex < RData.DiffJudgeIds.Count)
				{
					DiffJudges[JudgeIndex].NameId = RData.DiffJudgeIds[JudgeIndex];
				}
			}
		}
	}

	void AddNewJudger(ref List<NetworkJudgeData> InList, string InGuid)
	{
		//if (InList.Count < 3)
		{
			int InsertIndex = -1;
			for (int JudgeIndex = 0; JudgeIndex < InList.Count; ++JudgeIndex)
			{
				if (InList[JudgeIndex] == null)
				{
					InsertIndex = JudgeIndex;
					break;
				}
			}

			if (InsertIndex >= 0)
			{
				InList[InsertIndex] = new NetworkJudgeData(InGuid);
				Global.NetObj.SendJudgeInfo(InGuid, GetCatIndexForGuid(InGuid));
			}
			else
			{
				InList.Add(new NetworkJudgeData(InGuid));
				InsertIndex = InList.Count - 1;
				Global.NetObj.SendJudgeInfo(InGuid, GetCatIndexForGuid(InGuid));
			}

			InitJudgersNameIds();
		}

		Debug.Log(" add judger " + InGuid + "   " + InList.Count);
	}

	public void OnConnectedJudger(string InGuid, int InJudgeType)
	{
		switch ((ECategory)InJudgeType)
		{
			case ECategory.AI:
				AddNewJudger(ref AIJudges, InGuid);
				break;
			case ECategory.Ex:
				AddNewJudger(ref ExJudges, InGuid);
				break;
			case ECategory.Diff:
				AddNewJudger(ref DiffJudges, InGuid);
				break;
		}
	}

	public void OnReconnectedJudger(string InGuid, int InJudgeType, int InCatJudgeIndex, int InNameId, bool bInIsHoldReady, bool bInLocked, bool bInJudging, bool bInEditing)
	{
		Debug.Log(" on reconnected judger " + InJudgeType);

		int AdjustedCatJudgeIndex = GetCatIndexForGuid(InGuid);

		switch ((ECategory)InJudgeType)
		{
			case ECategory.AI:
				{
					if (AdjustedCatJudgeIndex == -1)
						AdjustedCatJudgeIndex = AIJudges.Count;

					while (AIJudges.Count <= AdjustedCatJudgeIndex)
						AIJudges.Add(new NetworkJudgeData());

					AIJudges[AdjustedCatJudgeIndex] = new NetworkJudgeData(InGuid, InNameId, bInIsHoldReady, bInLocked, bInJudging, bInEditing);

					break;
				}
			case ECategory.Ex:
				{
					if (AdjustedCatJudgeIndex == -1)
						AdjustedCatJudgeIndex = ExJudges.Count;

					while (ExJudges.Count <= AdjustedCatJudgeIndex)
						ExJudges.Add(new NetworkJudgeData());

					ExJudges[AdjustedCatJudgeIndex] = new NetworkJudgeData(InGuid, InNameId, bInIsHoldReady, bInLocked, bInJudging, bInEditing);
					break;
				}
			case ECategory.Diff:
				{
					if (AdjustedCatJudgeIndex == -1)
						AdjustedCatJudgeIndex = DiffJudges.Count;

					while (DiffJudges.Count <= AdjustedCatJudgeIndex)
						DiffJudges.Add(new NetworkJudgeData());

					DiffJudges[AdjustedCatJudgeIndex] = new NetworkJudgeData(InGuid, InNameId, bInIsHoldReady, bInLocked, bInJudging, bInEditing);
					break;
				}
		}
	}

	public void OnDisconnectedJudger(string InGuid)
	{
	}

	bool IsAllJudgesReady()
	{
		int LockedCount = 0;
		foreach (NetworkJudgeData njd in AIJudges)
		{
			if (njd != null && njd.bIsHoldReady)
				++LockedCount;
		}
		foreach (NetworkJudgeData njd in ExJudges)
		{
			if (njd != null && njd.bIsHoldReady)
				++LockedCount;
		}
		foreach (NetworkJudgeData njd in DiffJudges)
		{
			if (njd != null && njd.bIsHoldReady)
				++LockedCount;
		}

		return LockedCount > 0;
	}

	void DrawControlButtons()
	{
		Rect BotRect = new Rect(20, Screen.height * .78f, Screen.width - 40, Screen.height * .17f);
		GUILayout.BeginArea(BotRect);
		GUILayout.BeginHorizontal();

		GUIStyle ActiveStyle = new GUIStyle("button");
		GUIStyle InActiveStyle = new GUIStyle("button");
		InActiveStyle.normal.textColor = Color.grey;
		InActiveStyle.hover = InActiveStyle.active = InActiveStyle.normal;

		if (ActiveJudgingJudgers == 0)
		{
			if (GUILayout.Button("Call for Judges Ready", ActiveStyle, GUILayout.Width(Screen.width * .23f), GUILayout.Height(BotRect.height)))
				Global.NetObj.CallForJudgesReady();
		}
		else
		{
			if (GUILayout.Button("Call for Judges Ready", InActiveStyle, GUILayout.Width(Screen.width * .23f), GUILayout.Height(BotRect.height)))
				Global.NetObj.CallForJudgesReady();
		}

		if (IsAllJudgesReady() && !bJudging)
		{
            RoundData Round = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound];
            int TotalSeconds = (int)(DateTime.Now - RoutineStartTime).TotalSeconds;
            int RoundMinutes = Mathf.FloorToInt(Round.RoutineLengthMinutes);
            int RoundSeconds = Mathf.FloorToInt(Round.RoutineLengthMinutes * 60) % 60;

            bool bDuringRoutine = bJudging && (TotalSeconds <= (int)(Round.RoutineLengthMinutes * 60f));

            if (GUILayout.Button("Ready For Routine Start", bDuringRoutine ? InActiveStyle : ActiveStyle, GUILayout.Width(Screen.width * .23f), GUILayout.Height(BotRect.height)) && !bDuringRoutine)
			{
				Global.NetObj.LockJudgesToJudge();
				bLockedForJudging = true;
                bJudging = false;

				// Send ready to livestream
				TeamData readyTeam = Global.GetTeamData(CurDivision, CurRound, CurPool, CurTeam);
				SendRestMessageAsync(readyTeam, LiveStream.TeamStates.JudgesReady);
			}
		}
		else
			GUILayout.Button("Ready For Routine Start", InActiveStyle, GUILayout.Width(Screen.width * .23f), GUILayout.Height(BotRect.height));

		GUILayout.Space(Screen.width * .01f);

		if (bLockedForJudging && !bJudging)
		{
			CurrentResetStartTimeCooldown -= Time.deltaTime;

			if (GUILayout.Button("Click on First Throw", CurrentResetStartTimeCooldown > 0f ? InActiveStyle : ActiveStyle, GUILayout.Width(Screen.width * .45f), GUILayout.Height(BotRect.height)))
			{
				if (CurrentResetStartTimeCooldown <= 0f)
				{
					StartRoutine();
				}
			}
		}
		else if (bJudging)
		{
			CancelClickTimer -= Time.deltaTime;
			CurrentResetRoutineTime -= Time.deltaTime;

			float cancelWidth = Screen.width * .45f;
			float cancelHeight = BotRect.height;

			if (CurrentResetRoutineTime > 0)
			{
				cancelWidth /= 2f;
				cancelWidth -= 10f;

				if (GUILayout.Button("Triple Click to Reset", ActiveStyle, GUILayout.Width(cancelWidth), GUILayout.Height(cancelHeight)))
				{
					++CancelClickCount;
					if (CancelClickCount >= 2)
					{
						CancelClickCount = 0;
						CancelClickTimer = -1f;

						ResetRoutine();
					}
					else if (CancelClickTimer < 0)
					{
						CancelClickCount = 0;
					}

					CancelClickTimer = .5f;
				}
			}

			if (GUILayout.Button("Triple Click to Stop Judging", ActiveStyle, GUILayout.Width(cancelWidth), GUILayout.Height(cancelHeight)))
			{
				++CancelClickCount;
				if (CancelClickCount >= 2)
				{
					CancelClickCount = 0;
					CancelClickTimer = -1f;

					StopRoutine(true);
				}
				else if (CancelClickTimer < 0)
				{
					CancelClickCount = 0;
				}

				CancelClickTimer = .5f;
			}
		}
		else
			GUILayout.Button("Click on First Throw", InActiveStyle, GUILayout.Width(Screen.width * .45f), GUILayout.Height(BotRect.height));

		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	public void StartRoutine()
	{
		if (bLockedForJudging)
		{
			Global.NetObj.ServerSendStartRoutine();

			RoutineStartTime = DateTime.Now;
			bJudging = true;

			// Send start to livestream
			TeamData readyTeam = Global.GetTeamData(CurDivision, CurRound, CurPool, CurTeam);
			SendRestMessageAsync(readyTeam, LiveStream.TeamStates.Begin);

			CurrentResetRoutineTime = ResetRoutineStartTime;

			bRoutineTimeElapsed = false;
		}
	}

	public void StopRoutine()
	{
		StopRoutine(false);
	}

	public void StopRoutine(bool bCancelled)
	{
		if (bJudging)
		{
			Global.NetObj.ServerSendStopRoutine();

			bJudging = false;
			bLockedForJudging = false;

			// Send message to livestream
			TeamData stoppedTeam = Global.GetTeamData(CurDivision, CurRound, CurPool, CurTeam);
			if (bCancelled && !bRoutineTimeElapsed)
			{
				SendRestMessageAsync(stoppedTeam, LiveStream.TeamStates.Stopped);
			}
			else
			{
				LiveStream.Team finishedTeam = new LiveStream.Team(
					LiveStream.TeamStates.ScoresRecorded,
					CurDivision.ToString(),
					CurRound.ToString(),
					((EPool)CurPool).ToString(),
					CurTeam);

				foreach (PlayerData pd in stoppedTeam.Players)
				{
					finishedTeam.Players.Add(new LiveStream.Player(pd));
				}

				finishedTeam.DifficultyScore = stoppedTeam.RoutineScores.GetDiffPoints();
				finishedTeam.ArtisticImpressionScore = stoppedTeam.RoutineScores.GetAIPoints();
				finishedTeam.ExecutionScore = stoppedTeam.RoutineScores.GetExPoints();

				SendRestMessageAsync(finishedTeam);
			}
		}
	}

	public void ResetRoutine()
	{
		if (bJudging)
		{
			bJudging = false;

			CurrentResetStartTimeCooldown = ResetStartTimeCooldown;

			Global.NetObj.ServerSendResetRoutine();
		}
	}

	void DrawJudgeLabel(ResultsData InRData, int InId)
	{
		GUIStyle JudgeStyle = new GUIStyle("label");
		JudgeStyle.fontSize = 15;
		GUIStyle LabelStyle = new GUIStyle("label");
		NameData NData = NameDatabase.FindInDatabase(InId);
		if (NData != null)
		{
			string Str = NData.DisplayName;
			NetworkJudgeData NJData = GetJudgeDataFromNameId(InRData, NData.Id);
			//Debug.Log(" asdf: " + InRData.AIJudgeIds[0] + "   " + NData.Id + "  " + InId);
			if (NJData != null)
			{
				if (NJData.bJudging)
				{
					JudgeStyle.normal.textColor = Color.yellow;
					Str += " - JUDGING";
				}
				else if (NJData.bLocked)
				{
					JudgeStyle.normal.textColor = Color.green;
					Str += " - LOCKED";
				}
				else if (NJData.bIsHoldReady)
				{
					JudgeStyle.normal.textColor = Color.green;
					Str += " - READY";
				}
				else if (NJData.bEditing)
				{
					JudgeStyle.normal.textColor = Color.yellow;
					Str += " - EDITING";
				}
				else
				{
					JudgeStyle.normal.textColor = Color.red;
					Str += " - NOT READY";
				}
			}
			else
			{
				JudgeStyle.normal.textColor = LabelStyle.normal.textColor;
				Str += " - NOT CONNECTED";
			}
			GUILayout.Label(Str, JudgeStyle);
		}
	}

	void OnGUI()
	{
		float SelectButWidth = Screen.width * .25f;
		float SelectButHeight = Screen.height * .08f;
		float SelectY = Screen.height * .04f;
		DivisionCombo.Draw(new Rect(20, SelectY, SelectButWidth, SelectButHeight));
		RoundCombo.Draw(new Rect(20 + SelectButWidth + Screen.width * .02f, SelectY, SelectButWidth, SelectButHeight));

		ResultsData RData = TournamentData.FindResultsData(CurDivision, CurRound, CurPool);

		float InfoY = Screen.height * .02f + SelectY + SelectButHeight;
		GUIStyle InfoStyle = new GUIStyle("label");
		InfoStyle.fontSize = 30;
		GUIContent TimeDate = new GUIContent(DateTime.Now.ToString());
		Vector2 TimeDateSize = InfoStyle.CalcSize(TimeDate);
		GUI.Label(new Rect(20, InfoY, TimeDateSize.x, TimeDateSize.y), TimeDate, InfoStyle);

		bool bValidDivisionRoundSettings = Global.AllData.AllDivisions.Length > (int)CurDivision &&
			Global.AllData.AllDivisions[(int)CurDivision].Rounds.Length > (int)CurRound;

		if (bValidDivisionRoundSettings)
		{
			RoundData Round = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound];
			int TotalSeconds = bJudging ? (int)(DateTime.Now - RoutineStartTime).TotalSeconds : 0;
			int Minutes = Mathf.FloorToInt(TotalSeconds / 60f);
			int Seconds = Mathf.FloorToInt(TotalSeconds) % 60;
			int RoundMinutes = Mathf.FloorToInt(Round.RoutineLengthMinutes);
			int RoundSeconds = Mathf.FloorToInt(Round.RoutineLengthMinutes * 60) % 60;
			GUIContent RoutineTime = new GUIContent(String.Format("{0}:{1:00} / {2}:{3:00}", Minutes, Seconds, RoundMinutes, RoundSeconds));
			Vector2 TimeSize = InfoStyle.CalcSize(RoutineTime);
			GUI.Label(new Rect(Screen.width - 20 - TimeSize.x, InfoY, TimeSize.x, TimeSize.y), RoutineTime, InfoStyle);

			if (TotalSeconds > Round.RoutineLengthMinutes * 60 && !bRoutineTimeElapsed)
			{
				bRoutineTimeElapsed = true;

				// Send ready to livestream
				TeamData finishedTeam = Global.GetTeamData(CurDivision, CurRound, CurPool, CurTeam);
				SendRestMessageAsync(finishedTeam, LiveStream.TeamStates.Finished);
			}
		}

		if (bFestivalJudging)
		{
			float PoolButWidth = Screen.width * .09f;
			RoundData Round = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound];
			GUIStyle SelectedStyle = new GUIStyle("button");
			SelectedStyle.normal.textColor = Color.green;
			SelectedStyle.hover.textColor = Color.green;
			SelectedStyle.fontStyle = FontStyle.Bold;
			GUIStyle ButtonStyle = new GUIStyle("button");
			if (GUI.Button(new Rect(20 + 2f * SelectButWidth + Screen.width * .1f, SelectY, PoolButWidth, SelectButHeight),
				"Pool: " + Round.Pools[(int)CurFestivalPool].PoolName, CurPool == CurFestivalPool ? SelectedStyle : ButtonStyle))
			{
                SetCurrentPool(CurFestivalPool);
				InitJudgersNameIds();
				++Global.CurDataState;
			}

			if (GUI.Button(new Rect(20 + 2f * SelectButWidth + Screen.width * .12f + PoolButWidth, SelectY, PoolButWidth, SelectButHeight),
				"Pool: " + Round.Pools[(int)CurFestivalPool + 2].PoolName, CurPool == CurFestivalPool + 2 ? SelectedStyle : ButtonStyle))
			{
                SetCurrentPool(CurFestivalPool + 2);
				InitJudgersNameIds();
				++Global.CurDataState;
			}

			if (GUI.Button(new Rect(20 + 2f * SelectButWidth + Screen.width * .14f + 2f * PoolButWidth, SelectY, PoolButWidth, SelectButHeight),
				"AI Scores", (CurPool > EPool.Max) ? SelectedStyle : ButtonStyle))
			{
				EPool newPool = EPool.None;
				if (CurPool == EPool.A || CurPool == EPool.C)
				{
					newPool = EPool.PostScoresAC;
				}
				else if (CurPool == EPool.B || CurPool == EPool.D)
				{
					newPool = EPool.PostScoresBD;
				}
				else
				{
					switch (CurPool)
					{
						case EPool.PostScoresAC:
							newPool = EPool.PostScoresCA;
							break;
						case EPool.PostScoresCA:
							newPool = EPool.PostScoresAC;
							break;
						case EPool.PostScoresBD:
							newPool = EPool.PostScoresDB;
							break;
						case EPool.PostScoresDB:
							newPool = EPool.PostScoresBD;
							break;
					}
				}

				SetCurrentPool(newPool);
				InitJudgersNameIds();
				++Global.CurDataState;
			}
		}

		#region Teams
		if (!DivisionCombo.IsPicking && !RoundCombo.IsPicking && bValidDivisionRoundSettings)
		{
			Rect LeftRect = new Rect(20, Screen.height * .22f, Screen.width / 2 - 40, Screen.height * .5f);
			if (CurPool == EPool.None && !bFestivalJudging)
			{
				GUILayout.BeginArea(LeftRect);
				PoolsScrollPos = GUILayout.BeginScrollView(PoolsScrollPos);
				GUILayout.BeginVertical();

				RoundData Round = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound];
				if (Round.Pools.Count <= 2)
				{
					for (int PoolIndex = 0; PoolIndex < Round.Pools.Count; ++PoolIndex)
					{
						PoolData Pool = Round.Pools[PoolIndex];
						string PoolText = "";
						PoolText += Pool.PoolName + "\n";
						for (int TeamIndex = 0; TeamIndex < Pool.Teams.Count; ++TeamIndex)
						{
							TeamData Team = Pool.Teams[TeamIndex].Data;
							PoolText += (TeamIndex + 1) + ". " + Team.PlayerNames + "\n";
						}

						GUIStyle ButtonStyle = new GUIStyle("button");
						GUIStyle PoolStyle = new GUIStyle("button");
						PoolStyle.alignment = TextAnchor.UpperLeft;
						PoolStyle.normal.textColor = (EPool)PoolIndex == CurPool ? Color.green : ButtonStyle.normal.textColor;
						PoolStyle.hover.textColor = (EPool)PoolIndex == CurPool ? Color.green : ButtonStyle.hover.textColor;
						PoolStyle.fontStyle = (EPool)PoolIndex == CurPool ? FontStyle.Bold : ButtonStyle.fontStyle;
						Vector2 PoolTextSize = PoolStyle.CalcSize(new GUIContent(PoolText));
						if (GUILayout.Button(PoolText, PoolStyle, GUILayout.Width(LeftRect.width * .9f), GUILayout.Height(PoolTextSize.y)))
						{
                            SetCurrentPool((EPool)PoolIndex);
							InitJudgersNameIds();
							++Global.CurDataState;
							bFestivalJudging = false;
						}
					}
				}
				else if (Round.Pools.Count == 4)
				{
					for (int ButIndex = 0; ButIndex < 2; ++ButIndex)
					{
						string PoolText = "";

						for (int PoolIndex = 0; PoolIndex < 2; ++PoolIndex)
						{
							PoolData Pool = Round.Pools[2 * PoolIndex + ButIndex];
							PoolText += Pool.PoolName + "\n";
							for (int TeamIndex = 0; TeamIndex < Pool.Teams.Count; ++TeamIndex)
							{
								TeamData Team = Pool.Teams[TeamIndex].Data;
								PoolText += (TeamIndex + 1) + ". " + Team.PlayerNames + "\n";
							}

							PoolText += "\n";
						}

						GUIStyle ButtonStyle = new GUIStyle("button");
						GUIStyle PoolStyle = new GUIStyle("button");
						PoolStyle.alignment = TextAnchor.UpperLeft;
						PoolStyle.normal.textColor = ButIndex == (int)CurPool ? Color.green : ButtonStyle.normal.textColor;
						PoolStyle.hover.textColor = ButIndex == (int)CurPool ? Color.green : ButtonStyle.hover.textColor;
						PoolStyle.fontStyle = ButIndex == (int)CurPool ? FontStyle.Bold : ButtonStyle.fontStyle;
						Vector2 PoolTextSize = PoolStyle.CalcSize(new GUIContent(PoolText));
						if (GUILayout.Button(PoolText, PoolStyle, GUILayout.Width(LeftRect.width * .9f), GUILayout.Height(PoolTextSize.y)))
						{
							CurFestivalPool = (EPool)ButIndex;
							bFestivalJudging = true;
						}
					}
				}

				GUILayout.EndVertical();
				GUILayout.EndScrollView();
				GUILayout.EndArea();
			}
			else if (!bFestivalJudging || CurPool != EPool.None)
			{
				float LeftRectWidth = Screen.width * .45f;
				//new Rect(Screen.width - RightRectWidth - 20, AreaRect.y, RightRectWidth, Screen.height - AreaRect.y - 20);
				GUILayout.BeginArea(LeftRect);
				GUILayout.BeginVertical();

				GUIStyle TeamStyle = new GUIStyle("button");
				TeamStyle.fontSize = 17;
				TeamStyle.alignment = TextAnchor.MiddleLeft;
				GUIStyle BackStyle = new GUIStyle(TeamStyle);
				List<PoolData> Pools = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools;
				if (CurPool >= 0 && (int)CurPool < Pools.Count)
				{
					for (int TeamIndex = 0; TeamIndex < Pools[(int)CurPool].Teams.Count; ++TeamIndex)
					{
						TeamData Data = Pools[(int)CurPool].Teams[TeamIndex].Data;
						if (Data != null)
						{
							GUIContent TeamContent = new GUIContent((TeamIndex + 1) + ". " + Data.PlayerNames);
							Vector2 TeamSize = TeamStyle.CalcSize(TeamContent);
							TeamStyle.fontStyle = TeamIndex == CurTeam ? FontStyle.Bold : FontStyle.Normal;

                            if (!bLockedForJudging)
                            {
                                TeamStyle.normal.textColor = TeamIndex == CurTeam ? Color.green : new GUIStyle("button").normal.textColor;
                                TeamStyle.hover.textColor = TeamIndex == CurTeam ? Color.green : new GUIStyle("button").hover.textColor;
                            }
                            else
                            {
                                TeamStyle.normal.textColor = TeamIndex == CurTeam ? Color.green : Color.gray;
                                TeamStyle.hover.textColor = TeamIndex == CurTeam ? Color.green : Color.gray;
								TeamStyle.active.textColor = TeamIndex == CurTeam ? Color.green : Color.gray;
							}

							if (GUILayout.Button(TeamContent, TeamStyle, GUILayout.Width(LeftRectWidth), GUILayout.Height(TeamSize.y)))
							{
                                if (!bLockedForJudging)
                                {
                                    CurTeam = TeamIndex;
                                    ++Global.CurDataState;
                                }
							}
						}
					}
				}

				GUILayout.Space(Screen.height * .03f);

				GUIContent BackContent = new GUIContent("<- Back To Pool Selection");
				Vector2 BackSize = BackStyle.CalcSize(BackContent);
				if (GUILayout.Button(BackContent, BackStyle, GUILayout.Width(LeftRectWidth), GUILayout.Height(BackSize.y)))
				{
					SetCurrentPool(EPool.None);
					CurTeam = -1;
					bFestivalJudging = false;
				}

				GUILayout.EndVertical();
				GUILayout.EndArea();

				// Ready Buttons
				if (CurTeam >= 0)
					DrawControlButtons();
			}
		}
		else
		{
            SetCurrentPool(EPool.None);
			bFestivalJudging = false;
		}
		#endregion

		#region Judges
		if (CurPool != EPool.None && RData != null)
		{
			Rect RightRect = new Rect(Screen.width * .5f + 20, Screen.height * .22f, Screen.width / 2 - 40, Screen.height * .88f - 20);
			GUILayout.BeginArea(RightRect);
			GUILayout.BeginVertical();

			GUIStyle CatHeaderStyle = new GUIStyle("label");
			CatHeaderStyle.fontSize = 17;
			CatHeaderStyle.fontStyle = FontStyle.Bold;
			

			GUILayout.Label("AI Judges:", CatHeaderStyle);
			foreach (int id in RData.AIJudgeIds)
				DrawJudgeLabel(RData, id);
			GUILayout.Label("Ex Judges:", CatHeaderStyle);
			foreach (int id in RData.ExJudgeIds)
				DrawJudgeLabel(RData, id);
			GUILayout.Label("Diff Judges:", CatHeaderStyle);
			foreach (int id in RData.DiffJudgeIds)
				DrawJudgeLabel(RData, id);

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
		#endregion
	}

	void UpdateActiveJudgingJudgersCount()
	{
		int Count = 0;
		ResultsData RData = TournamentData.FindResultsData(CurDivision, CurRound, CurPool);
		if (RData != null)
		{
			foreach (int id in RData.AIJudgeIds)
			{
				NetworkJudgeData NJData = GetJudgeDataFromNameId(RData, id);
				if (NJData != null && (NJData.bJudging || NJData.bEditing))
				{
					++Count;
				}
			}
			foreach (int id in RData.ExJudgeIds)
			{
				NetworkJudgeData NJData = GetJudgeDataFromNameId(RData, id);
				if (NJData != null && (NJData.bJudging || NJData.bEditing))
				{
					++Count;
				}
			}
			foreach (int id in RData.DiffJudgeIds)
			{
				NetworkJudgeData NJData = GetJudgeDataFromNameId(RData, id);
				if (NJData != null && (NJData.bJudging || NJData.bEditing))
				{
					++Count;
				}
			}

			if (Count != ActiveJudgingJudgers)
			{
				ActiveJudgingJudgers = Count;
				++Global.CurDataState;

				UpdateFinishRoutineAndGoToNextTeam();
			}
		}
	}

	public void ReceiveDiffData(string JudgeGuid, string InData)
	{
		DiffData NewDiffData = DiffData.Load(Global.GenerateStreamFromString(InData));
		if (NewDiffData != null)
		{
			TeamData TData = Global.GetTeamData(NewDiffData.Division, NewDiffData.Round, NewDiffData.Pool, NewDiffData.Team);
			if (TData != null)
			{
				bool bNewScore = TData.RoutineScores.SetDiffResults(NewDiffData);

				OnRecievedResultsData(bNewScore);
			}
		}
	}

	public void ReceiveExData(string JudgeGuid, string InData)
	{
		ExData NewExData = ExData.Load(Global.GenerateStreamFromString(InData));
		if (NewExData != null)
		{
			TeamData TData = Global.GetTeamData(NewExData.Division, NewExData.Round, NewExData.Pool, NewExData.Team);
			if (TData != null)
			{
				bool bNewScore = TData.RoutineScores.SetExResults(NewExData);

				OnRecievedResultsData(bNewScore);
			}
		}
	}

	public void ReceiveAIData(string JudgeGuid, string InData)
	{
		AIData NewAiData = AIData.Load(Global.GenerateStreamFromString(InData));
		if (NewAiData != null)
		{
			TeamData TData = Global.GetTeamData(NewAiData.Division, NewAiData.Round, NewAiData.Pool, NewAiData.Team);
			if (TData != null)
			{
				bool bNewScore = TData.RoutineScores.SetAIResults(NewAiData);

				OnRecievedResultsData(bNewScore);
			}
		}
	}

	private void UpdateFinishRoutineAndGoToNextTeam()
	{
		if (bJudging && ActiveJudgingJudgers == 0)
		{
			ResultsData RData = TournamentData.FindResultsData(CurDivision, CurRound, CurPool);
			if (RData != null)
			{
				int JudgesCount = 0;
				foreach (int id in RData.AIJudgeIds)
					++JudgesCount;
				foreach (int id in RData.ExJudgeIds)
					++JudgesCount;
				foreach (int id in RData.DiffJudgeIds)
					++JudgesCount;

				TeamData TData = Global.GetTeamData(CurDivision, CurRound, CurPool, CurTeam);
				if (TData.RoutineScores.GetTotalValidScores() >= JudgesCount)
				{
					// If we got all results
					StopRoutine();

					if (Global.IsValid(CurDivision, CurRound, CurPool, CurTeam))
					{
						int curPoolTeamCount = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams.Count;

						if (bFestivalJudging)
						{
							int cachedTeamIndex = CurTeam;
							int newPool = (int)CurPool + 2;

							if (newPool >= 4)
							{
								newPool = newPool % 4;
								if (newPool < Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count)
								{
									int newPoolTeamCount = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[newPool].Teams.Count;

									if (cachedTeamIndex < newPoolTeamCount - 1)
									{
										CurPool = (EPool)newPool;
										InitJudgersNameIds();
										CurTeam = cachedTeamIndex + 1;
										++Global.CurDataState;
									}
									else if (cachedTeamIndex < curPoolTeamCount - 1)
									{
										CurTeam = cachedTeamIndex + 1;
										++Global.CurDataState;
									}
									else
									{
										// End of pools
									}
								}
							}
							else
							{
								int newPoolTeamCount = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[newPool].Teams.Count;

								if (cachedTeamIndex >= newPoolTeamCount && cachedTeamIndex < curPoolTeamCount - 1)
								{
									CurTeam = cachedTeamIndex + 1;
									++Global.CurDataState;
								}
								else if (cachedTeamIndex < newPoolTeamCount)
								{
									CurPool = (EPool)newPool;
									CurTeam = cachedTeamIndex;
									InitJudgersNameIds();
									++Global.CurDataState;
								}
								else
								{
									// End of Pools
								}
							}
						}
						else
						{
							if (CurTeam < curPoolTeamCount - 1)
							{
								++CurTeam;
								++Global.CurDataState;
							}
						}
					}
				}
			}
		}
	}

	public void OnRecievedResultsData(bool bFirstScore)
	{
		Global.AllData.Save();

		UpdateActiveJudgingJudgersCount();

		UpdateFinishRoutineAndGoToNextTeam();
	}

	void TestRestApi()
	{
		LiveStream.Player playerA = new LiveStream.Player();
		playerA.Name = "Ryan";
		LiveStream.Player playerB = new LiveStream.Player();
		playerB.Name = "James";
		LiveStream.Player playerC = new LiveStream.Player();
		playerC.Name = "Jake";
		LiveStream.Player playerD = new LiveStream.Player();
		playerD.Name = "Randy";

		LiveStream.Team teamA = new LiveStream.Team();
		teamA.State = LiveStream.TeamStates.JudgesReady;
		teamA.Players.Add(playerA);
		teamA.Players.Add(playerB);

		LiveStream.Team teamB = new LiveStream.Team();
		teamB.State = LiveStream.TeamStates.JudgesReady;
		teamB.Players.Add(playerB);
		teamB.Players.Add(playerC);

		LiveStream.Team teamC = new LiveStream.Team();
		teamC.State = LiveStream.TeamStates.JudgesReady;
		teamC.Players.Add(playerB);
		teamC.Players.Add(playerC);
		teamC.Players.Add(playerD);

		LiveStream.Team teamD = new LiveStream.Team();
		teamD.State = LiveStream.TeamStates.JudgesReady;
		teamD.Players.Add(playerC);
		teamD.Players.Add(playerD);

		SendRestMessageAsync(teamA);

		LiveStream.TeamList teamList = new LiveStream.TeamList();
		teamList.Teams.Add(teamA);
		teamList.Teams.Add(teamB);
		teamList.Teams.Add(teamC);
		teamList.Teams.Add(teamD);

		//SendRestMessageAsync(teamList);

		teamD.DifficultyScore = 10f;
		SendRestMessageAsync(teamD);
	}

	void SendRestMessageAsync(TeamData team, LiveStream.TeamStates teamState)
	{
		LiveStream.Team newTeam = new LiveStream.Team(
			teamState,
			CurDivision.ToString(),
			CurRound.ToString(),
			((EPool)CurPool).ToString(),
			CurTeam);

		foreach (PlayerData pd in team.Players)
		{
			newTeam.Players.Add(new LiveStream.Player(pd));
		}

		SendRestMessageAsync(newTeam);
	}

	void SendRestMessageAsync(LiveStream.Team team)
	{
		StartCoroutine(Global.SendRestMessage(team));
	}
}

class CastleDownloadHandler : DownloadHandlerScript
{
	public delegate void Finished();
	public event Finished onFinished;

	protected override void CompleteContent()
	{
		UnityEngine.Debug.Log("CompleteContent()");
		base.CompleteContent();
		if (onFinished != null)
		{
			onFinished();
		}
	}
}

namespace LiveStream
{
	public enum TeamStates
	{
		None,
		Inited,
		JudgesReady,
		Begin,
		Stopped,
		Finished,
		ScoresRecorded
	}

	[Serializable]
	public class Player
	{
		public string Name;
		public int Rank;
		public string HomeCity;
		public string HomeCountry;

		public Player()
		{
		}

		public Player(PlayerData pd)
		{
			Name = pd.Fullname;
			Rank = pd.Rank;
		}
	}

	[Serializable]
	public class Team
	{
		public TeamStates State;
		public string Event;
		public string Division;
		public string Pool;
		public string Round;
		public string TeamName; // Optional, in case the team has a name
		public int TeamNumber; // Play Order Number
		public List<Player> Players = new List<Player>();

		public float ArtisticImpressionScore;
		public float ExecutionScore;
		public float DifficultyScore;

		public Team()
		{
		}

		public Team(TeamStates state, string div, string round, string pool, int teamNumber)
		{
			State = state;
			Division = div;
			Round = round;
			Pool = pool;
			TeamNumber = teamNumber;
		}
	}

	[Serializable]
	public class TeamList
	{
		public List<Team> Teams = new List<Team>();
	}
}

public class NetworkJudgeData
{
	public string Guid = "";
	public int NameId = -1;
	public bool bIsHoldReady = false;
	public bool bLocked = false;
	public bool bJudging = false;
	public bool bEditing = false;

	public NetworkJudgeData()
	{
	}

	public NetworkJudgeData(string InGuid)
	{
		Guid = InGuid;
	}

	public NetworkJudgeData(string InGuid, int InNameId, bool bInIsHoldReady, bool bInLocked, bool bInJudging, bool bInEditing)
	{
		Guid = InGuid;
		NameId = InNameId;
		bIsHoldReady = bInIsHoldReady;
		bLocked = bInLocked;
		bJudging = bInJudging;
		bEditing = bInEditing;
	}
}
