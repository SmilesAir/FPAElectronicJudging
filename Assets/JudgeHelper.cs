using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class JudgeHelper : MonoBehaviour
{
	public RoundComboBox RoundCombo = new RoundComboBox();
	public DivisionComboBox DivisionCombo = new DivisionComboBox();
	public int CurPoolButtonIndex = -1;
	Vector2 RightScroll = new Vector2();
	Rect[] JudgeRects = new Rect[3];
	int MovingNameId = -1;
	string JudgeFilterStr = "";
	List<NameData> AvailableJudges = new List<NameData>();
	string AllJudgesString = "";
	Vector2 AllJudgesTextScrollPos = new Vector2();

	// Use this for initialization
	void Start()
	{
		RoundCombo.SetOnSelectionChangedDelegate(OnComboChanged);
		DivisionCombo.SetOnSelectionChangedDelegate(OnComboChanged);

		Global.LoadTournamentData();
	}

	void OnComboChanged()
	{
		CurPoolButtonIndex = -1;
	}

	void MoveTeamToJudgeCategory(int PlayerId, int CategoryIndex)
	{
		ResultsData Data = TournamentData.FindResultsData((EDivision)DivisionCombo.GetSelectedItemIndex(), (ERound)RoundCombo.GetSelectedItemIndex(), (EPool)CurPoolButtonIndex);
		if (Data != null)
		{
			Data.AIJudgeIds.Remove(PlayerId);
			Data.ExJudgeIds.Remove(PlayerId);
			Data.DiffJudgeIds.Remove(PlayerId);

			switch ((ECategory)CategoryIndex)
			{
				case ECategory.AI:
					Data.AIJudgeIds.Add(PlayerId);
					break;
				case ECategory.Ex:
					Data.ExJudgeIds.Add(PlayerId);
					break;
				case ECategory.Diff:
					Data.DiffJudgeIds.Add(PlayerId);
					break;
			}
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (!Input.GetMouseButton(0))
		{
			if (MovingNameId != -1)
			{
				bool bMovedSuccess = false;
				Vector3 NewMousePos = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);
				for (int CatIndex = 0; CatIndex < 3; ++CatIndex)
				{
					if (JudgeRects[CatIndex].Contains(NewMousePos))
					{
						bMovedSuccess = true;
						MoveTeamToJudgeCategory(MovingNameId, CatIndex);
						break;
					}
				}

				if (!bMovedSuccess)
				{
					ResultsData Data = TournamentData.FindResultsData((EDivision)DivisionCombo.GetSelectedItemIndex(), (ERound)RoundCombo.GetSelectedItemIndex(), (EPool)CurPoolButtonIndex);
					if (Data != null)
					{
						Data.AIJudgeIds.Remove(MovingNameId);
						Data.ExJudgeIds.Remove(MovingNameId);
						Data.DiffJudgeIds.Remove(MovingNameId);
					}
				}

				MovingNameId = -1;
			}
		}
	}

	void DrawPoolButton(string InStr, int InButtonIndex, float InHeight)
	{
		GUIStyle PoolStyle = new GUIStyle("button");
		if (InButtonIndex == CurPoolButtonIndex)
		{
			PoolStyle.normal.textColor = Color.green;
			PoolStyle.hover.textColor = Color.green;
			PoolStyle.fontStyle = FontStyle.Bold;
		}
		GUIContent ButCont = new GUIContent(InStr);
		Vector2 PoolSize = PoolStyle.CalcSize(ButCont);
		if (GUILayout.Button(ButCont, PoolStyle, GUILayout.Width(PoolSize.x * 1.3f), GUILayout.Height(InHeight)))
			CurPoolButtonIndex = InButtonIndex;
	}

	void FilterAvailableJudges(string InFilterStr, ref List<NameData> OutFilteredJudges)
	{
		if (InFilterStr.Trim().Length == 0)
			return;

		char[] Seperators = new char[] {',', ' ', '-'};
		string[] Names = InFilterStr.Split(Seperators);
		List<MatchData> CloseNames = NameDatabase.GetCloseNames(Names.Length > 0 ? Names[0].Trim() : "", Names.Length > 1 ? Names[1].Trim() : "");
		CloseNames.AddRange(NameDatabase.GetCloseNames(Names.Length > 1 ? Names[1].Trim() : "", Names.Length > 0 ? Names[0].Trim() : ""));
		for (int JudgeIndex = 0; JudgeIndex < OutFilteredJudges.Count; ++JudgeIndex)
		{
			NameData nd = OutFilteredJudges[JudgeIndex];
			bool bFound = false;
			foreach (MatchData md in CloseNames)
			{
				if (nd == md.Name)
					bFound = true;
			}

			if (!bFound)
			{
				OutFilteredJudges.RemoveAt(JudgeIndex);
				--JudgeIndex;
			}
		}
	}

	void OnGUI()
	{
		bool bRepaintUpdate = Event.current.ToString() == "Repaint";

		if (GUI.Button(new Rect(Screen.width - 150, Screen.height - 150, 150, 150), "Save"))
		{
			Global.AllData.Save();
		}

		float StartSelectY = 30;
		float SelectHeight = 30;
		DivisionCombo.Draw(new Rect(20, StartSelectY, Screen.width * .18f, SelectHeight));
		RoundCombo.Draw(new Rect(20 + Screen.width * .2f, StartSelectY, Screen.width * .18f, SelectHeight));

		GUILayout.BeginArea(new Rect(20 + Screen.width * .4f, StartSelectY, Screen.width - Screen.width * .4f - 40, SelectHeight));
		GUILayout.BeginHorizontal();

		List<PoolData> Pools = Global.AllData.AllDivisions[DivisionCombo.GetSelectedItemIndex()].Rounds[RoundCombo.GetSelectedItemIndex()].Pools;
		for (int PoolIndex = 0; PoolIndex < Pools.Count; ++PoolIndex)
			DrawPoolButton("Pool: " + Pools[PoolIndex].PoolName, PoolIndex, SelectHeight);

		GUILayout.EndHorizontal();
		GUILayout.EndArea();

		float RightRectStartY = Screen.height * .02f + SelectHeight + StartSelectY;
		Rect RightRect = new Rect(20, RightRectStartY, Screen.width * .3f, Screen.height - RightRectStartY - 20);
		Rect LeftRect = new Rect(RightRect.x + RightRect.width + Screen.width * .1f, RightRect.y, RightRect.width, Screen.height * .5f);

		if (CurPoolButtonIndex != -1)
		{
			ResultsData RData = TournamentData.FindResultsData((EDivision)DivisionCombo.GetSelectedItemIndex(), (ERound)RoundCombo.GetSelectedItemIndex(), (EPool)CurPoolButtonIndex);
			if (RData == null)
			{
				RData = new ResultsData((EDivision)DivisionCombo.GetSelectedItemIndex(), (ERound)RoundCombo.GetSelectedItemIndex(), (EPool)CurPoolButtonIndex);
				Pools[CurPoolButtonIndex].JudgersId = RData.Id;

				Global.AllData.ResultsList.Add(RData);
			}
			else if (Pools[CurPoolButtonIndex].JudgersId == -1)
			{
				if (RData.Id == -1)
					RData.Id = ResultsData.GetUniqueId();

				Pools[CurPoolButtonIndex].JudgersId = RData.Id;
			}


			if (RData != null && Global.AllNameData.AllJudges != null)
			{
				GUILayout.BeginArea(RightRect);
				RightScroll = GUILayout.BeginScrollView(RightScroll);
				GUILayout.BeginVertical();

				string NewFilterStr = GUILayout.TextField(JudgeFilterStr);

				if (NewFilterStr != JudgeFilterStr || AvailableJudges.Count == 0)
				{
					JudgeFilterStr = NewFilterStr;

					AvailableJudges.Clear();
					foreach (NameData pd in Global.AllNameData.AllJudges)
					{
						if (!IsPlayingInPool(pd, RData) && !RData.ContainsPlayer(pd))
						{
							AvailableJudges.Add(pd);
						}
					}

					FilterAvailableJudges(JudgeFilterStr, ref AvailableJudges);
				}

				GUILayout.Space(20);

				GUIStyle LabelStyle = new GUIStyle("label");
				GUIStyle PlayerStyle = new GUIStyle("label");
				PlayerStyle.alignment = TextAnchor.MiddleLeft;
				foreach (NameData nd in AvailableJudges)
				{
					bool bMoving = MovingNameId == nd.Id;
					PlayerStyle.normal.textColor = bMoving ? Color.grey : LabelStyle.normal.textColor;

					GUIContent PlayerCont = new GUIContent(nd.DisplayName);
					Vector2 PlayerSize = PlayerStyle.CalcSize(PlayerCont);
					if (GUILayout.RepeatButton(PlayerCont, PlayerStyle, GUILayout.Width(RightRect.width * .9f), GUILayout.Height(PlayerSize.y)))
						MovingNameId = nd.Id;
				}

				GUILayout.EndVertical();
				GUILayout.EndScrollView();
				GUILayout.EndArea();


				// Left Rect ------------
				GUILayout.BeginArea(LeftRect);
				GUILayout.BeginVertical();
				GUIStyle JudgeStyle = new GUIStyle("label");
				JudgeStyle.alignment = TextAnchor.MiddleLeft;

				GUILayout.BeginVertical();
				GUILayout.Label("AI Judges:", JudgeStyle);
				for (int PlayerIndex = 0; PlayerIndex < Mathf.Max(RData.AIJudgeIds.Count, 3); ++PlayerIndex)
				{
					int PlayerId = -1;
					bool bMoving = false;
					string PlayerStr = PlayerIndex < 3 ? (PlayerIndex + 1) + ". " : "";
					if (PlayerIndex < RData.AIJudgeIds.Count)
					{
						PlayerId = RData.AIJudgeIds[PlayerIndex];
						bMoving = PlayerId == MovingNameId;

						PlayerStr += NameDatabase.FindInDatabase(PlayerId).DisplayName;
					}

					JudgeStyle.normal.textColor = bMoving ? Color.grey : LabelStyle.normal.textColor;

					GUIContent JudgeCont = new GUIContent(PlayerStr);
					Vector2 JudgeSize = JudgeStyle.CalcSize(JudgeCont);
					if (GUILayout.RepeatButton(JudgeCont, JudgeStyle, GUILayout.Width(LeftRect.width * .6f), GUILayout.Height(JudgeSize.y)) && PlayerIndex < RData.AIJudgeIds.Count)
						MovingNameId = PlayerId;
				}
				GUILayout.EndVertical();
				if (bRepaintUpdate)
				{
					JudgeRects[0] = GUILayoutUtility.GetLastRect();
					JudgeRects[0].x = LeftRect.x;
					JudgeRects[0].y = LeftRect.y;
				}

				GUILayout.BeginVertical();
				GUILayout.Label("Ex Judges:", JudgeStyle);
				for (int PlayerIndex = 0; PlayerIndex < Mathf.Max(RData.ExJudgeIds.Count, 3); ++PlayerIndex)
				{
					int PlayerId = -1;
					bool bMoving = false;
					string PlayerStr = PlayerIndex < 3 ? (PlayerIndex + 1) + ". " : "";
					if (PlayerIndex < RData.ExJudgeIds.Count)
					{
						PlayerId = RData.ExJudgeIds[PlayerIndex];
						bMoving = PlayerId == MovingNameId;

						PlayerStr += NameDatabase.FindInDatabase(PlayerId).DisplayName;
					}

					JudgeStyle.normal.textColor = bMoving ? Color.grey : LabelStyle.normal.textColor;

					GUIContent JudgeCont = new GUIContent(PlayerStr);
					Vector2 JudgeSize = JudgeStyle.CalcSize(JudgeCont);
					if (GUILayout.RepeatButton(JudgeCont, JudgeStyle, GUILayout.Width(LeftRect.width * .6f), GUILayout.Height(JudgeSize.y)) && PlayerIndex < RData.ExJudgeIds.Count)
						MovingNameId = PlayerId;
				}
				GUILayout.EndVertical();
				if (bRepaintUpdate)
				{
					JudgeRects[1] = GUILayoutUtility.GetLastRect();
					JudgeRects[1].x += LeftRect.x;
					JudgeRects[1].y += LeftRect.y;
				}

				GUILayout.BeginVertical();
				GUILayout.Label("Diff Judges:", JudgeStyle);
				for (int PlayerIndex = 0; PlayerIndex < Mathf.Max(RData.DiffJudgeIds.Count, 3); ++PlayerIndex)
				{
					int PlayerId = -1;
					bool bMoving = false;
					string PlayerStr = PlayerIndex < 3 ? (PlayerIndex + 1) + ". " : "";
					if (PlayerIndex < RData.DiffJudgeIds.Count)
					{
						PlayerId = RData.DiffJudgeIds[PlayerIndex];
						bMoving = PlayerId == MovingNameId;

						PlayerStr += NameDatabase.FindInDatabase(PlayerId).DisplayName;
					}

					JudgeStyle.normal.textColor = bMoving ? Color.grey : LabelStyle.normal.textColor;

					GUIContent JudgeCont = new GUIContent(PlayerStr);
					Vector2 JudgeSize = JudgeStyle.CalcSize(JudgeCont);
					if (GUILayout.RepeatButton(JudgeCont, JudgeStyle, GUILayout.Width(LeftRect.width * .6f), GUILayout.Height(JudgeSize.y)) && PlayerIndex < RData.DiffJudgeIds.Count)
						MovingNameId = PlayerId;
				}
				GUILayout.EndVertical();
				if (bRepaintUpdate)
				{
					JudgeRects[2] = GUILayoutUtility.GetLastRect();
					JudgeRects[2].x += LeftRect.x;
					JudgeRects[2].y += LeftRect.y;
				}

				GUILayout.EndVertical();
				GUILayout.EndArea();

				if (MovingNameId != -1)
					GUI.Label(new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 500, 30), NameDatabase.FindInDatabase(MovingNameId).DisplayName);
			}
		}

		float BottomRectX = RightRect.x + RightRect.width + 20;
		float BottomRectY = LeftRect.y + LeftRect.height;
		GUILayout.BeginArea(new Rect(BottomRectX, BottomRectY, Screen.width - BottomRectX - 170, Screen.height - BottomRectY - 20));
		GUILayout.Label("All Judges Names");
		AllJudgesTextScrollPos = GUILayout.BeginScrollView(AllJudgesTextScrollPos);
		string NewAllJudgesString = GUILayout.TextArea(AllJudgesString);
		if (NewAllJudgesString != AllJudgesString)
		{
			AllJudgesString = NewAllJudgesString;
			AvailableJudges.Clear();
			Global.AllNameData.AllJudges.Clear();

			StringReader reader = new StringReader(AllJudgesString);
			string line = null;
			while ((line = reader.ReadLine()) != null)
			{
				AddJudgersFromString(line);
			}

			Global.AllNameData.Save();
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	void AddJudgersFromString(string InLine)
	{
		foreach (NameData nd in Global.AllNameData.AllNames)
		{
			if (nd.IsInLine(InLine))
				Global.AllNameData.AllJudges.Add(nd);
		}
	}

	public bool IsPlayingInPool(NameData InPlayer, ResultsData InResultsData)
	{
		List<PoolData> Pools = Global.AllData.AllDivisions[DivisionCombo.GetSelectedItemIndex()].Rounds[RoundCombo.GetSelectedItemIndex()].Pools;

		return Pools[(int)InResultsData.Pool].ContainsPlayer(InPlayer);
	}
}

public class ResultsData
{
	public int Id = -1;
	public EDivision Division = EDivision.Open;
	public ERound Round = ERound.Finals;
	public EPool Pool = EPool.None;
	public List<int> AIJudgeIds = new List<int>();
	public List<int> ExJudgeIds = new List<int>();
	public List<int> DiffJudgeIds = new List<int>();

	public static int GetUniqueId()
	{
		if (Global.AllData == null)
			return -1;

		int LargestId = -1;
		foreach (ResultsData jd in Global.AllData.ResultsList)
			LargestId = Mathf.Max(LargestId, jd.Id);

		return ++LargestId;
	}

	public ResultsData()
	{
		Id = ResultsData.GetUniqueId();
	}

	public ResultsData(EDivision InDiv, ERound InRound, EPool InPool)
	{
		Division = InDiv;
		Round = InRound;

		Pool = InPool;
	}

	public bool ContainsPlayer(NameData InPlayer)
	{
		foreach (int Id in AIJudgeIds)
		{
			if (Id == InPlayer.Id)
				return true;
		}

		foreach (int Id in ExJudgeIds)
		{
			if (Id == InPlayer.Id)
				return true;
		}

		foreach (int Id in DiffJudgeIds)
		{
			if (Id == InPlayer.Id)
				return true;
		}

		return false;
	}

	public int GetNameId(ECategory InCat, int InIndex)
	{
		if (InIndex < 0)
			return -1;

		switch (InCat)
		{
			case ECategory.AI:
				if (InIndex < AIJudgeIds.Count)
					return AIJudgeIds[InIndex];
				break;
			case ECategory.Ex:
				if (InIndex < ExJudgeIds.Count)
					return ExJudgeIds[InIndex];
				break;
			case ECategory.Diff:
				if (InIndex < DiffJudgeIds.Count)
					return DiffJudgeIds[InIndex];
				break;
		}

		return -1;
	}

	public int GetNameId(int InIndex)
	{
		if (InIndex < 0)
			return -1;

		if (InIndex < AIJudgeIds.Count)
			return AIJudgeIds[InIndex];
		else
			InIndex -= AIJudgeIds.Count;

		if (InIndex < ExJudgeIds.Count)
			return ExJudgeIds[InIndex];
		else
			InIndex -= ExJudgeIds.Count;

		if (InIndex < DiffJudgeIds.Count)
			return DiffJudgeIds[InIndex];

		return -1;
	}
}

public enum ECategory
{
	AI,
	Ex,
	Diff
}
