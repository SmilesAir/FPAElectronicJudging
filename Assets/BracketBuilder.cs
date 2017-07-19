using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text; 

public class BracketBuilder : MonoBehaviour
{
    string RankingURL = "http://koljah.de/freestylediscrankings/currentopen.htm";
	bool bGetRankings = true;
	bool bWaitingForRankings = false;
	WWW RankingsRequest;
	string RankingsText = "";
	string FetchStatus = "";
	//string InputTeamsText = "Strunz, Bianca Schiller, Dave\nWiseman, James  - 	Gauthier, Jake\nKenny, Paul  Cesari, Manuel\nSanna, Fabio  Lamred, Christian\nMarconi, Serge Daniels, Lori\nThoma, Niki Edelmann, Benjamin\nTrevino, Johnny Titcomb, John \nAhlstrom, Bjorn  Burzan, Tobias\nWhitlock, Glen Inoue, Yohei\nSchwarze, Lasse Weiss, Jiri\nPatris, Konrad Boulware, Bob\nZoila, Emlila Olsson, Tomas\nLowry, Mary Esterbrook, Mike\nMarron, Pat Lacina, Lukas\nLahm, Randy  Borghesi, Roberto";
	string InputTeamsText = "";
	TextEditor InputTeamsEditor = null;
	string InputTeamsAutoCompleteStr = "";
	Vector2 TeamsScrollPos;
	Vector2 ErrorsScrollPos;
	Vector2 CloseScrollPos;
	string ParseTeamsText = "";
	Vector2 ParsedScrollPos;
	Vector2 RoundsScrollPos;
	string PoolCountString = "1";
	string PoolCutString = "0";
	string RoutineMinutesString = "4";
	bool bSortTeams = true;
	int PoolCount = -1;
	int PoolCut = 0;
	bool InputTeamsTextChanged = false;
	bool InputTeamsTextReduced = false;
	TeamDataDisplay MovingTeam = null;
	public RoundComboBox RoundCombo = new RoundComboBox();
	public DivisionComboBox DivisionCombo = new DivisionComboBox();
	List<char> BreakChars = new List<char>();
	List<ErrorLine> ErrorList = new List<ErrorLine>();
	bool bFixingNameError = false;
	string ErrorFirstSearchStr = "";
	string ErrorLastSearchStr = "";
	ErrorData EditingErrorData = null;

	List<PlayerData> AllPlayers = new List<PlayerData>();
	List<TeamData> AllTeams = new List<TeamData>();
	List<PoolDataDisplay> AllPools = new List<PoolDataDisplay>();

	// Use this for initialization
	void Start()
	{
		Global.LoadTournamentData();

		//BreakChars.Add(' ');
		BreakChars.Add('-');
		BreakChars.Add(',');
		BreakChars.Add('\n');
	}

	string AutoCompleteString(TextEditor InEditor, ref string InStr)
	{
		string Ret = "";

		if (InEditor != null)
		{
            int CurPos = InEditor.cursorIndex;
			if (CurPos > 0)
			{
				string PrevStr = "";
				int PrevPos = CurPos - 1;
				while (PrevPos >= 0)
				{
					char PrevC = InStr[PrevPos];
					if (BreakChars.Contains(PrevC))
					{
						break;
					}

					PrevStr = PrevC + PrevStr;
					--PrevPos;
				}

				char CurC = CurPos < InStr.Length ? InStr[CurPos] : '?';
				if (PrevStr.Length > 0 && (CurPos == InStr.Length || BreakChars.Contains(CurC)))
				{
					foreach (NameData nd in Global.AllNameData.AllNames)
					{
						string AutoStr = nd.GetAutoComplete(PrevStr);
						if (AutoStr.Length > 0)
						{
							CurPos -= PrevStr.Length;
							InStr = InStr.Remove(CurPos, PrevStr.Length);
							InStr = InStr.Insert(CurPos, AutoStr);

							Ret = AutoStr.Substring(PrevStr.Length);
                            InEditor.selectIndex = CurPos + AutoStr.Length;
							break;
						}
					}
				}
			}
		}

		return Ret;
	}

	// Update is called once per frame
	void Update()
	{
		if (bGetRankings)
		{
			RankingsRequest = new WWW(RankingURL);
			bWaitingForRankings = true;
			bGetRankings = false;
			FetchStatus = "Querying Shrednow.com";
		}
		else if (bWaitingForRankings && RankingsRequest.isDone)
		{
			if (RankingsRequest.error != null)
				FetchStatus = RankingsText = RankingsRequest.error;
			else
			{
				RankingsText = Encoding.UTF7.GetString(RankingsRequest.bytes, 0, RankingsRequest.bytes.Length);

				//int RankStartIndex = "<td align=\"center\">".Length;
				string line = null;
				StringReader AllText = new StringReader(RankingsText);
				AllPlayers.Clear();
				while ((line = AllText.ReadLine()) != null)
				{
                    // Old Arthur parsing
                    //if (line.StartsWith("<td align=\"center\">"))
                    //{
                    //    PlayerData NewData = new PlayerData();
                    //    int.TryParse(line.Substring(RankStartIndex, line.IndexOf('<', RankStartIndex) - RankStartIndex), out NewData.Rank);
                    //    int StartNameIndex = line.IndexOf("</td><td>") + "</td><td>".Length;
                    //    string LastName = line.Substring(StartNameIndex, line.IndexOf(',', StartNameIndex) - StartNameIndex);
                    //    string FirstName = line.Substring(line.IndexOf(',', StartNameIndex) + 2, line.IndexOf('<', StartNameIndex) - line.IndexOf(',', StartNameIndex) - 2);
                    //    int StartPointsIndex = line.IndexOf("</td><td align=\"center\">", StartNameIndex + FirstName.Length + LastName.Length + 10) +
                    //        "</td><td align=\"center\">".Length;
                    //    float.TryParse(line.Substring(StartPointsIndex, line.IndexOf('<', StartPointsIndex) - StartPointsIndex), out NewData.RankingPoints);

                    //    NameData NewName = NameDatabase.TryAddNewName(FirstName, LastName);
                    //    NewData.NameId = NewName.Id;

                    //    AllPlayers.Add(NewData);
                    //}

                    line = line.Trim();

                    string RankTd = "<td height=19 class=xl6322297 style='height:14.5pt'>";

                    string NameClass = "xl1522297";
                    string PointsClass = "xl6322297";

                    // New Kolja parsing
                    if (line.StartsWith(RankTd))
                    {
                        PlayerData NewData = new PlayerData();
                        string rankStr = line.Trim().Replace(RankTd, "");
                        rankStr = rankStr.Replace("</td>", "");
                        int.TryParse(rankStr, out NewData.Rank);
                        AllText.ReadLine();

                        string NameLine = AllText.ReadLine().Trim().Replace("<td class=" + NameClass + ">", "");
                        int NameLineCommaIndex = NameLine.IndexOf(',');
                        if (NameLineCommaIndex == -1)
                        {
                            continue;
                        }
                        string LastName = NameLine.Substring(0, NameLineCommaIndex);
                        string FirstName = NameLine.Substring(NameLineCommaIndex + 2, NameLine.Length - LastName.Length - 7);
                        NameData NewName = NameDatabase.TryAddNewName(FirstName, LastName);
                        NewData.NameId = NewName.Id;

                        AllText.ReadLine();
                        AllText.ReadLine();
                        string PointsLine = AllText.ReadLine().Trim();
						PointsLine = PointsLine.Replace("<td class=" + PointsClass + ">", "").Replace("</td>", "").Replace(",", ".");
                        float.TryParse(PointsLine, out NewData.RankingPoints);

                        AllPlayers.Add(NewData);
                    }
				}

				RankingsText = "";
				for (int i = 0; i < AllPlayers.Count; ++i)
				{
					PlayerData Data = AllPlayers[i];
					RankingsText += NameDatabase.FindInDatabase(Data.NameId).DisplayName + "   " + Data.Rank + "   " + Data.RankingPoints + "\n";
				}

				FetchStatus = "Got " + AllPlayers.Count + " Players Rankings";

				InputTeamsTextChanged = true;
			}

			bWaitingForRankings = false;
		}

		if (InputTeamsTextChanged)
		{
			InputTeamsTextChanged = false;

			ParseTeamsText = "";
			StringReader TeamText = new StringReader(InputTeamsText);
			string line = null;
			AllTeams.Clear();
			ErrorList.Clear();
			while ((line = TeamText.ReadLine()) != null)
			{
				TeamData NewTeam = GetTeamFromString(line);
				if (NewTeam != null)
				{
					AllTeams.Add(NewTeam);
				}
				else
				{
					char[] BreakCharArray = new char[10];
					BreakChars.CopyTo(BreakCharArray, 0);
					string[] NameArray = line.Split(BreakCharArray);
					ErrorLine NewError = new ErrorLine();
					NewError.OriginalLine = line;
					foreach (string s in NameArray)
					{
						string NameStr = s.Trim();
						List<PlayerData> OutPlayers = new List<PlayerData>();
						GetPlayersFromString(NameStr, ref OutPlayers);
						NewError.ErrorList.Add(new ErrorData(NameStr, OutPlayers.Count == 0));
					}
					ErrorList.Add(NewError);
				}
			}

			if (bSortTeams)
			{
				AllTeams.Sort(TeamSorter);
			}

			for (int TeamIndex = 0; TeamIndex < AllTeams.Count; ++TeamIndex)
			{
				ParseTeamsText += (TeamIndex + 1) + ". " + AllTeams[TeamIndex].PlayerNames + " : " + AllTeams[TeamIndex].TotalRankPoints + "\n";
			}

			AllPools.Clear();
			if (PoolCount > 0)
			{
				char PoolName = 'A';
				for (int TeamIndex = 0; TeamIndex < AllTeams.Count; ++TeamIndex)
				{
					if (bSortTeams)
					{
						int PoolIndex = TeamIndex % PoolCount;
						if (PoolIndex >= AllPools.Count)
							AllPools.Add(new PoolDataDisplay((PoolName++).ToString()));

						AllPools[PoolIndex].Data.Teams.Add(new TeamDataDisplay(AllTeams[TeamIndex]));
					}
					else
					{
						int TeamsPerPool = AllTeams.Count / PoolCount;
						int PoolIndex = TeamIndex / TeamsPerPool;
						if (PoolIndex >= AllPools.Count)
							AllPools.Add(new PoolDataDisplay((PoolName++).ToString()));

						AllPools[PoolIndex].Data.Teams.Add(new TeamDataDisplay(AllTeams[TeamIndex]));
					}
				}
			}

			InputTeamsTextReduced = false;

			//Debug.Log("Parsing pools. Teams: " + AllTeams.Count + "  Pools: " + AllPools.Count);
		}

		if (Input.GetMouseButton(0))
		{
			if (MovingTeam != null)
			{
			}
		}
		else
		{
			if (MovingTeam != null)
			{
				for (int PoolIndex = 0; PoolIndex < AllPools.Count; ++PoolIndex)
				{
					Vector3 NewMousePos = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y + ParsedScrollPos.y, 0);
					if (AllPools[PoolIndex].DisplayRect.Contains(NewMousePos))
					{
						MovedTeamBetweenPools(MovingTeam, PoolIndex);
						break;
					}
				}
			}

			for (int PoolIndex = 0; PoolIndex < AllPools.Count; ++PoolIndex)
			{
				PoolData PData = AllPools[PoolIndex].Data;
				for (int TeamIndex = 0; TeamIndex < PData.Teams.Count; ++TeamIndex)
				{
					PData.Teams[TeamIndex].bClickedOn = false;
				}
			}

			MovingTeam = null;
		}
	}

	int TeamSorter(TeamData t1, TeamData t2)
	{
		if (t1.TotalRankPoints == t2.TotalRankPoints)
			return 0;
		else if (t1.TotalRankPoints < t2.TotalRankPoints)
			return -1;
		else
			return 1;
	}

	int TeamSorter(TeamDataDisplay t1, TeamDataDisplay t2)
	{
		return TeamSorter(t1.Data, t2.Data);
	}

	int TeamResultsSorter(TeamData t1, TeamData t2)
	{
		float t1Points = t1.RoutineScores.GetTotalPoints();
		float t2Points = t2.RoutineScores.GetTotalPoints();

		if (t1Points == t2Points)
			return 0;
		else if (t1Points < t2Points)
			return -1;
		else
			return 1;
	}

	int TeamResultsSorter(TeamDataDisplay t1, TeamDataDisplay t2)
	{
		return TeamResultsSorter(t1.Data, t2.Data);
	}

	void MovedTeamBetweenPools(TeamDataDisplay InTeam, int NewPoolIndex)
	{
		for (int PoolIndex = 0; PoolIndex < AllPools.Count; ++PoolIndex)
		{
			PoolData PData = AllPools[PoolIndex].Data;
			for (int TeamIndex = 0; TeamIndex < PData.Teams.Count; ++TeamIndex)
			{
				TeamDataDisplay TData = PData.Teams[TeamIndex];
				if (TData == InTeam)
				{
					PData.Teams.Remove(TData);
					PoolIndex = AllPools.Count;
					break;
				}
			}
		}

		AllPools[NewPoolIndex].Data.Teams.Add(InTeam);

		AllPools[NewPoolIndex].Data.Teams.Sort(TeamSorter);
	}

	void FillInputWithSavedRound(int InDivIndex, int InRoundIndex)
	{
		InputTeamsText = "";
		List<PoolData> Pools = Global.AllData.AllDivisions[InDivIndex].Rounds[InRoundIndex].Pools;
		for (int PoolIndex = 0; PoolIndex < Pools.Count; ++PoolIndex)
		{
			PoolData Pool = Pools[PoolIndex];
			for (int TeamIndex = 0; TeamIndex < Pool.Teams.Count; ++TeamIndex)
			{
				InputTeamsText += Pool.Teams[TeamIndex].Data.PlayerNames + "\n";
			}
		}

		PoolCount = Pools.Count;
		PoolCountString = Pools.Count.ToString();

		DivisionCombo.SetSelectedItemIndex(InDivIndex);
		RoundCombo.SetSelectedItemIndex(InRoundIndex);

		bool bHasResults = Global.AllData.AllDivisions[InDivIndex].Rounds[InRoundIndex].ContainsJudgeScores();

		AllPools.Clear();
		foreach (PoolData pd in Pools)
		{
			AllPools.Add(new PoolDataDisplay(pd));

			if (bHasResults)
				AllPools[AllPools.Count - 1].Data.Teams.Sort(TeamResultsSorter);
		}

		InputTeamsTextChanged = false;
	}

	void CutPools()
	{
		foreach (PoolDataDisplay pdd in AllPools)
		{
			if (pdd.Data.Teams.Count > PoolCut)
				pdd.Data.Teams.RemoveRange(PoolCut, pdd.Data.Teams.Count - PoolCut);
		}

		InputTeamsText = "";
		for (int PoolIndex = 0; PoolIndex < AllPools.Count; ++PoolIndex)
		{
			PoolData Pool = AllPools[PoolIndex].Data;
			for (int TeamIndex = 0; TeamIndex < Pool.Teams.Count; ++TeamIndex)
			{
				InputTeamsText += Pool.Teams[TeamIndex].Data.PlayerNames + "\n";
			}
		}

		InputTeamsTextChanged = false;
	}

	void UpdateInputTeamsText(string NewStr)
	{
		if (NewStr != InputTeamsText)
		{
			InputTeamsTextChanged = true;

			//Debug.Log(" changed: " + InputTeamsText.Length + " - " + InputTeamsAutoCompleteStr.Length + " >= " + NewStr.Length);

			if (InputTeamsAutoCompleteStr.Length > 0)
			{
                int PrePos = InputTeamsEditor.cursorIndex - 1;
				char PreC = PrePos >= 0 ? NewStr[PrePos] : '?';
				if (InputTeamsAutoCompleteStr.Length > 0 && (PreC == '\n' || PreC == '\t'))
				{
					int SavedAutoStrLen = InputTeamsAutoCompleteStr.Length;
					InputTeamsAutoCompleteStr = "";
					NewStr = InputTeamsText;
                    InputTeamsEditor.cursorIndex += SavedAutoStrLen;
					InputTeamsEditor.SelectNone();
				}
			}

			if (InputTeamsText.Length - InputTeamsAutoCompleteStr.Length >= NewStr.Length)
			{
				//Debug.Log(" reduced str");
				InputTeamsAutoCompleteStr = "";
				InputTeamsTextReduced = true;
			}

			InputTeamsText = NewStr;
		}

		if (InputTeamsTextChanged)
		{
			if (InputTeamsEditor != null && !InputTeamsTextReduced)
			{
				InputTeamsAutoCompleteStr = AutoCompleteString(InputTeamsEditor, ref InputTeamsText);

				//Debug.Log(" auto complted");
			}
		}
	}

	void DrawLeftPane()
	{
		Rect LeftRect = new Rect(20, 110, Screen.width / 2 - 40, Screen.height * .75f - 20);
		GUILayout.BeginArea(LeftRect);
		GUILayout.BeginVertical();
		TeamsScrollPos = GUILayout.BeginScrollView(TeamsScrollPos, GUILayout.MaxHeight(LeftRect.height / 2f));
		string NewInputTeamsText = GUILayout.TextArea(InputTeamsText, GUILayout.MinHeight(LeftRect.height / 2.2f));
		InputTeamsEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
		UpdateInputTeamsText(NewInputTeamsText);
		GUILayout.EndScrollView();

		ErrorsScrollPos = GUILayout.BeginScrollView(ErrorsScrollPos, GUILayout.MaxHeight(LeftRect.height * .25f));
		GUILayout.BeginVertical();
		GUILayout.Label("Errors: " + ErrorList.Count);

		foreach (ErrorLine el in ErrorList)
		{
			//GUIStyle ErrorStyle = new GUIStyle();
			GUILayout.BeginHorizontal();

			foreach (ErrorData ed in el.ErrorList)
			{
				if (ed.bIsError)
				{
					if (GUILayout.Button(ed.PlayerName))
					{
						bFixingNameError = true;
						EditingErrorData = ed;

						char[] Seperators = new char[]{',', '-', ' ', '.'};
						string[] ErrorNames = ed.PlayerName.Split(Seperators);
						if (ed.PlayerName.Contains(","))
						{
							if (ErrorNames.Length > 0)
								ErrorLastSearchStr = ErrorNames[0].Trim();
							if (ErrorNames.Length > 1)
								ErrorFirstSearchStr = ErrorNames[1].Trim();
						}
						else
						{
							if (ErrorNames.Length > 0)
								ErrorFirstSearchStr = ErrorNames[0].Trim();
							if (ErrorNames.Length > 1)
								ErrorLastSearchStr = ErrorNames[1].Trim();
						}
					}
				}
				else
					GUILayout.Label(ed.PlayerName + " - ");
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();
		GUILayout.EndScrollView();

		RoundsScrollPos = GUILayout.BeginScrollView(RoundsScrollPos, GUILayout.MaxHeight(LeftRect.height / 2f));
		GUIStyle RoundButtonStyle = new GUIStyle("button");
		RoundButtonStyle.alignment = TextAnchor.MiddleLeft;
		for (int DivIndex = 0; DivIndex < Global.AllData.AllDivisions.Length; ++DivIndex)
		{
			DivisionData DivData = Global.AllData.AllDivisions[DivIndex];
			if (DivData.HasAnyPoolData())
			{
				for (int RoundIndex = 0; RoundIndex < DivData.Rounds.Length; ++RoundIndex)
				{
					RoundData RData = DivData.Rounds[RoundIndex];
					if (RData.HasAnyPoolData())
					{
						GUIContent RoundContent = new GUIContent(((EDivision)DivIndex) + " - " + ((ERound)RoundIndex) + (RData.ContainsJudgeScores() ? " - Results" : ""));
						Vector2 RoundTextSize = RoundButtonStyle.CalcSize(RoundContent);
						if (GUILayout.Button(RoundContent, RoundButtonStyle, GUILayout.Width(LeftRect.width * .9f), GUILayout.Height(RoundTextSize.y)))
						{
							FillInputWithSavedRound(DivIndex, RoundIndex);
						}
					}
				}
			}
		}
		GUILayout.EndScrollView();

		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	void DrawRightPane(bool bLayoutUpdate, bool bRepaintUpdate)
	{
		Rect AreaRect = new Rect(Screen.width / 2 + 20, 130, Screen.width / 2 - 40, Screen.height - 150 - 50);
		GUILayout.BeginArea(AreaRect);
		ParsedScrollPos = GUILayout.BeginScrollView(ParsedScrollPos);
		//ParseTeamsText = GUILayout.TextArea(ParseTeamsText);
		GUILayout.BeginVertical();
		for (int PoolIndex = 0; PoolIndex < AllPools.Count; ++PoolIndex)
		{
			GUILayout.BeginVertical();
			PoolData PData = AllPools[PoolIndex].Data;
			GUILayout.Label("Pool " + PData.PoolName + ":");
			for (int TeamIndex = 0; TeamIndex < PData.Teams.Count; ++TeamIndex)
			{
				TeamData TData = PData.Teams[TeamIndex].Data;
				GUIStyle LabelStyle = new GUIStyle();
				if (AllPools[PoolIndex].Data.Teams[TeamIndex].bClickedOn)
				{
					LabelStyle.normal.textColor = Color.grey;
				}
				else
				{
					LabelStyle.normal.textColor = Color.white;
				}

				if (DivisionCombo.IsPicking || RoundCombo.IsPicking)
				{
				}
				else
				{
					string TeamStr = (TeamIndex + 1) + ". " + TData.PlayerNames + " : " +
						(TData.ContainsJudgeScores() ? TData.RoutineScores.GetTotalPoints() : TData.TotalRankPoints);
					bool bTeamClicked = GUILayout.RepeatButton(TeamStr, LabelStyle);
					AllPools[PoolIndex].Data.Teams[TeamIndex].bClickedOn |= !bLayoutUpdate ? bTeamClicked : AllPools[PoolIndex].Data.Teams[TeamIndex].bClickedOn;
					if (bTeamClicked)
						MovingTeam = PData.Teams[TeamIndex];
				}
			}
			GUILayout.EndVertical();

			if (bRepaintUpdate)
			{
				AllPools[PoolIndex].DisplayRect = GUILayoutUtility.GetLastRect();
				AllPools[PoolIndex].DisplayRect.x += AreaRect.x;
				AllPools[PoolIndex].DisplayRect.y += AreaRect.y;
			}
		}

		GUILayout.EndVertical();
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	void OnGUI()
	{
		bool bLayoutUpdate = Event.current.ToString() == "Layout";
		bool bRepaintUpdate = Event.current.ToString() == "Repaint";
		RankingURL = GUI.TextField(new Rect(20, 20, Screen.width - 270, 30), RankingURL);
		//if (!bGetRankings && !bWaitingForRankings)
		//    bGetRankings = GUI.Button(new Rect(20, 70, 200, 40), "Fetch Rankings");
		GUILayout.BeginArea(new Rect(20, 70, 500, 40));
		GUILayout.BeginHorizontal();

		GUILayout.Label("Number of Pools:");
		PoolCountString = GUILayout.TextField(PoolCountString);
		int NewPoolCount = PoolCount;
		if (int.TryParse(PoolCountString, out NewPoolCount) && NewPoolCount != PoolCount)
		{
			PoolCount = NewPoolCount;
			InputTeamsTextChanged = true;
		}

		GUILayout.Label("Pool Cut:");
		PoolCutString = GUILayout.TextField(PoolCutString);
		if (int.TryParse(PoolCutString, out PoolCut) && GUILayout.Button("Cut"))
		{
			CutPools();
		}

		GUILayout.Label("Routine Minutes:");
		RoutineMinutesString = GUILayout.TextField(RoutineMinutesString);

		GUILayout.Space(20);

		bool bNewSortTeams = GUILayout.Toggle(bSortTeams, "Sort Teams");
		InputTeamsTextChanged |= bNewSortTeams != bSortTeams;
		bSortTeams = bNewSortTeams;

		GUILayout.EndHorizontal();
		GUILayout.EndArea();

		if (FetchStatus.Length > 0)
			GUI.Label(new Rect(Screen.width - 240, 20, 200, 40), FetchStatus);

		DrawLeftPane();

		if (bFixingNameError)
		{
			Rect AreaRect = new Rect(Screen.width / 2 + 20, 130, Screen.width / 2 - 40, Screen.height - 150 - 50);
			GUILayout.BeginArea(AreaRect);
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();

			ErrorFirstSearchStr = GUILayout.TextField(ErrorFirstSearchStr);
			ErrorLastSearchStr = GUILayout.TextField(ErrorLastSearchStr);
			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Add New Player Name"))
			{
				PlayerData NewData = new PlayerData();
				NewData.RankingPoints = 0;
				NewData.Rank = -1;
				NameData NewName = NameDatabase.TryAddNewName(ErrorFirstSearchStr, ErrorLastSearchStr);
				NewData.NameId = NewName.Id;

				AllPlayers.Add(NewData);
			}

			if (GUILayout.Button("Cancel"))
			{
				bFixingNameError = false;
			}

			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			GUILayout.BeginVertical();
			CloseScrollPos = GUILayout.BeginScrollView(CloseScrollPos);

			List<MatchData> CloseNames = NameDatabase.GetCloseNames(ErrorFirstSearchStr, ErrorLastSearchStr);
			foreach (MatchData md in CloseNames)
			{
				if (GUILayout.Button(md.Name.DisplayName))
				{
					StringReader InputReader = new StringReader(InputTeamsText);
					string InputLine = null;
					string NewInputText = "";
					while ((InputLine = InputReader.ReadLine()) != null)
					{
						foreach (ErrorLine el in ErrorList)
						{
							if (InputLine == el.OriginalLine)
							{
								foreach (ErrorData ed in el.ErrorList)
								{
									if (EditingErrorData == ed)
									{
										InputLine = ReplaceWholeWord(el.OriginalLine, EditingErrorData.PlayerName, md.Name.DisplayName);
										InputTeamsTextChanged = true;

										bFixingNameError = false;
										break;
									}
								}
							}
						}

						NewInputText += InputLine + "\n";
					}
					InputReader.Close();

					InputTeamsText = NewInputText;
				}
			}

			GUILayout.EndScrollView();
			GUILayout.EndVertical();

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
		else
			DrawRightPane(bLayoutUpdate, bRepaintUpdate);

		if (MovingTeam != null)
		{
			GUI.Label(new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 500, 30), MovingTeam.Data.PlayerNames + " : " + MovingTeam.Data.TotalRankPoints);
		}

		DivisionCombo.Draw(new Rect(Screen.width * .57f, 70, Screen.width * .18f, 30));
		RoundCombo.Draw(new Rect(Screen.width * .77f, 70, Screen.width * .18f, 30));

		if (GUI.Button(new Rect(Screen.width / 2 + 20, Screen.height - 50, Screen.width / 2 - 40, 30), "Create Round"))
			CreateRoundData();
	}

	bool IsWhitespace(char InChar)
	{
		return InChar == ' ' || InChar == '\n' || InChar == ',';
	}

	string ReplaceWholeWord(string OrgStr, string OldStr, string NewStr)
	{
		int CurIndex = -1;
		while ((CurIndex = OrgStr.IndexOf(OldStr, CurIndex + 1)) != -1)
		{
			bool bBefore = false;
			if (CurIndex == 0)
			{
				bBefore = true;
			}
			else if (IsWhitespace(OrgStr[CurIndex - 1]))
			{
				bBefore = true;
			}

			bool bAfter = false;
			if (CurIndex + OldStr.Length == OrgStr.Length)
			{
				bAfter = true;
			}
			else if (IsWhitespace(OrgStr[CurIndex + OldStr.Length]))
			{
				bAfter = true;
			}

			if (bBefore && bAfter)
			{
				OrgStr = OrgStr.Remove(CurIndex, OldStr.Length);
				OrgStr = OrgStr.Insert(CurIndex, NewStr);
			}
		}

		return OrgStr;
	}

	void GetPlayersFromString(string InLine, ref List<PlayerData> OutPlayers)
	{
		for (int PlayerIndex = 0; PlayerIndex < AllPlayers.Count; ++PlayerIndex)
		{
			PlayerData Data = AllPlayers[PlayerIndex];
			if (NameDatabase.FindInDatabase(Data.NameId).IsInLine(InLine))
				OutPlayers.Add(Data);
		}
	}

	TeamData GetTeamFromString(string InLine)
	{
		List<PlayerData> Players = new List<PlayerData>();
		GetPlayersFromString(InLine, ref Players);

		TeamData NewTeam = new TeamData();
		NewTeam.Players = Players;
		for (int PlayerIndex = 0; PlayerIndex < Players.Count; ++PlayerIndex)
		{
			NewTeam.TotalRankPoints += Players[PlayerIndex].RankingPoints;
		}

		if (NewTeam.Players.Count < (DivisionCombo.GetSelectedItemIndex() == 2 ? 3 : 2))
		{
			return null;
		}

		return NewTeam;
	}

	void CreateRoundData()
	{
		DivisionData DivData = Global.AllData.AllDivisions[DivisionCombo.GetSelectedItemIndex()];
		DivData.Rounds[RoundCombo.GetSelectedItemIndex()].Pools = new List<PoolData>(AllPools.Count);
		float.TryParse(RoutineMinutesString, out DivData.Rounds[RoundCombo.GetSelectedItemIndex()].RoutineLengthMinutes);

		foreach (PoolDataDisplay pdd in AllPools)
		{
			DivData.Rounds[RoundCombo.GetSelectedItemIndex()].Pools.Add(new PoolData(pdd.Data));
		}

		Debug.Log(Global.AllData.Save());
	}
}

public class MatchData
{
	public NameData Name = null;
	public float Match = 0;

	public MatchData(NameData InName, float InMatch)
	{
		Name = InName;
		Match = InMatch;
	}
}

public class ErrorData
{
	public string PlayerName = "";
	public bool bIsError = false;

	public ErrorData()
	{
	}

	public ErrorData(string InPlayerStr, bool bInIsError)
	{
		PlayerName = InPlayerStr;
		bIsError = bInIsError;
	}
}

public class ErrorLine
{
	public string OriginalLine = "";
	public List<ErrorData> ErrorList = new List<ErrorData>();
}

public class PlayerData
{
	public int NameId;
	public float RankingPoints;
	public int Rank = -1;

	public string Fullname { get { return NameDatabase.FindInDatabase(NameId).DisplayName; } }
}

public class TeamData
{
	public List<PlayerData> Players = new List<PlayerData>();
	public float TotalRankPoints = 0;
	public RoutineScoreData RoutineScores = new RoutineScoreData();

	public string PlayerNames
	{
		get
		{
			string Ret = "";
			for (int PlayerIndex = 0; PlayerIndex < Players.Count; ++PlayerIndex)
			{
				Ret += Players[PlayerIndex].Fullname;
				if (PlayerIndex < Players.Count - 1)
					Ret += " - ";
			}

			return Ret;
		}
	}

    public string PlayerNamesWithRank
    {
        get
        {
            string Ret = "";
            for (int PlayerIndex = 0; PlayerIndex < Players.Count; ++PlayerIndex)
            {
                Ret += Players[PlayerIndex].Fullname;

                int Rank = Players[PlayerIndex].Rank;
                if (Rank > 0)
                    Ret += " (#" + Rank + ")";

                if (PlayerIndex < Players.Count - 1)
                    Ret += " - ";
            }

            return Ret;
        }
    }

	public bool ContainsPlayer(NameData InPlayer)
	{
		foreach (PlayerData pd in Players)
		{
			if (pd.NameId == InPlayer.Id)
				return true;
		}

		return false;
	}

	public bool ContainsJudgeScores()
	{
		return RoutineScores.ContainsJudgeScores();
	}
}

public class TeamDataDisplay
{
	public TeamData Data;
	public bool bClickedOn = false;

	public TeamDataDisplay() { }

	public TeamDataDisplay(TeamData InData)
	{
		Data = InData;
	}
}

public class PoolDataDisplay
{
	public PoolData Data;
	public Rect DisplayRect = new Rect();

	public PoolDataDisplay() { Data = new PoolData(); }
	public PoolDataDisplay(string InName) { Data = new PoolData(); Data.PoolName = InName; }
	public PoolDataDisplay(PoolData InData)
	{
		Data = new PoolData(InData);
	}
}

public class PoolData
{
	public string PoolName = "No Name";
	public List<TeamDataDisplay> Teams = new List<TeamDataDisplay>();
	public int JudgersId = -1;
	public List<int> ResultsByTeamIndex = new List<int>();

	public PoolData() { }

	public PoolData(PoolData InData)
	{
		PoolName = InData.PoolName;
		Teams = InData.Teams;
	}

	public bool ContainsPlayer(NameData InPlayer)
	{
		foreach (TeamDataDisplay tdd in Teams)
		{
			if (tdd.Data.ContainsPlayer(InPlayer))
				return true;
		}

		return false;
	}
}

public class RoundData
{
	public List<PoolData> Pools = new List<PoolData>();
	public float RoutineLengthMinutes = 4f;

	public bool HasAnyPoolData()
	{
		return Pools.Count > 0;
	}

	public bool ContainsJudgeScores()
	{
		foreach (PoolData pd in Pools)
		{
			foreach(TeamDataDisplay tdd in pd.Teams)
			{
				if (tdd.Data.ContainsJudgeScores())
					return true;
			}
		}

		return false;
	}
}

public class DivisionData
{
	public RoundData[] Rounds = new RoundData[4];

	public DivisionData()
	{
		for (int i = 0; i < Rounds.Length; ++i)
		{
			Rounds[i] = new RoundData();
		}
	}

	public bool HasAnyPoolData()
	{
		for (int RoundIndex = 0; RoundIndex < Rounds.Length; ++RoundIndex)
		{
			if (Rounds[RoundIndex].HasAnyPoolData())
				return true;
		}

		return false;
	}
}

[XmlRoot("TournamentRootData")]
public class TournamentData
{
	public DivisionData[] AllDivisions = new DivisionData[4];

	[XmlArray("JudgeList")]
	[XmlArrayItem("JudgeData")]
	public List<ResultsData> ResultsList { get; set; }

	public TournamentData()
	{
		for (int i = 0; i < 4; ++i)
		{
			AllDivisions[i] = new DivisionData();
		}
	}

	public string Save()
	{
		CalculateResultOrder();

		for (int TryIndex = 0; TryIndex < 5; ++TryIndex)
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(TournamentData));
				StreamWriter stream = new StreamWriter(Application.persistentDataPath + "/save.xml");
				serializer.Serialize(stream, this);
				stream.Close();
			}
			catch
			{
				System.Threading.Thread.Sleep(1000);
			}
		}

		return "Save Tourney Data";
	}

	public string SerializeToString()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(TournamentData));
		MemoryStream stream = new MemoryStream();
		serializer.Serialize(stream, this);
		stream.Position = 0;
		StreamReader Reader = new StreamReader(stream);
		string WholeString = Reader.ReadToEnd();
		Reader.Close();

		stream.Close();

		return WholeString;
	}

	public static TournamentData Load(string Filename)
	{
		TournamentData LoadedData = null;

		try
		{
			if (File.Exists(Filename))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(TournamentData));
				FileStream stream = new FileStream(Filename, FileMode.Open);
				LoadedData = serializer.Deserialize(stream) as TournamentData;
				stream.Close();
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Loading exception: " + e.Message);
		}

		return LoadedData;
	}

	public static TournamentData Load(Stream InStream)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(TournamentData));
		TournamentData LoadedData = serializer.Deserialize(InStream) as TournamentData;

		return LoadedData;
	}

	public static ResultsData FindJudgeData(int InId)
	{
		if (Global.AllData.ResultsList == null)
			return null;

		foreach (ResultsData jd in Global.AllData.ResultsList)
		{
			if (jd.Id == InId)
				return jd;
		}

		return null;
	}

	public static ResultsData FindResultsData(EDivision InDiv, ERound InRound, int InPool)
	{
		if (Global.AllData.ResultsList == null)
			return null;

		foreach (ResultsData rd in Global.AllData.ResultsList)
		{
			if (rd.Division == InDiv && rd.Round == InRound && rd.Pool == InPool)
				return rd;
		}

		return null;
	}

	private void CalculateResultOrder()
	{
		foreach (DivisionData dd in AllDivisions)
		{
			foreach (RoundData rd in dd.Rounds)
			{
				foreach (PoolData pd in rd.Pools)
				{
					List<KeyValuePair<int, float>> sortedTeams = new List<KeyValuePair<int, float>>();
					int teamIndex = 0;
					foreach (TeamDataDisplay td in pd.Teams)
					{
						float points = td.Data.RoutineScores.GetTotalPoints();
						if (sortedTeams.Count == 0)
						{
							sortedTeams.Add(new KeyValuePair<int, float>(teamIndex, points));
						}
						else
						{
							bool bInserted = false;
							for (int i = 0; i < sortedTeams.Count; ++i)
							{
								if (points > sortedTeams[i].Value)
								{
									sortedTeams.Insert(i, new KeyValuePair<int, float>(teamIndex, points));

									bInserted = true;
									break;
								}
							}

							if (!bInserted)
							{
								sortedTeams.Add(new KeyValuePair<int, float>(teamIndex, points));
							}
						}

						++teamIndex;
					}

					pd.ResultsByTeamIndex.Clear();

					foreach (KeyValuePair<int, float> team in sortedTeams)
					{
						pd.ResultsByTeamIndex.Add(team.Key);
					}
				}
			}
		}
	}

	public static TeamData GetTeamData(EDivision InDiv, ERound InRound, int InPool, int InTeam)
	{
		if ((int)InDiv < Global.AllData.AllDivisions.Length &&
			(int)InRound < Global.AllData.AllDivisions[(int)InDiv].Rounds.Length &&
			InPool < Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools.Count &&
			InTeam < Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools[InPool].Teams.Count)
		{
			return Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools[InPool].Teams[InTeam].Data;
		}

		return null;
	}
}

public class RoundComboBox
{
	public ComboBox RoundCombo = new ComboBox();
	public GUIContent[] RoundComboList;
	public GUIStyle RoundComboStyle = new GUIStyle();
	public bool IsPicking { get { return RoundCombo.IsPicking; } }
	bool bInited = false;

	public void Init()
	{
		RoundComboList = new GUIContent[4];
		RoundComboList[0] = new GUIContent("Finals");
		RoundComboList[1] = new GUIContent("Semifinals");
		RoundComboList[2] = new GUIContent("Quaterfinals");
		RoundComboList[3] = new GUIContent("Prelims");

		RoundComboStyle.normal.textColor = Color.white;
		RoundComboStyle.onHover.background =
		RoundComboStyle.hover.background = new Texture2D(2, 2);
		RoundComboStyle.padding.left =
		RoundComboStyle.padding.right =
		RoundComboStyle.padding.top =
		RoundComboStyle.padding.bottom = 4;

		bInited = true;
	}

	public void SetOnSelectionChangedDelegate(ComboBox.OnSelectionChangedDelegate InDelegate)
	{
		RoundCombo.OnSelectionChanged = InDelegate;
	}

	public void Draw(Rect InRect)
	{
		if (!bInited)
			Init();

		RoundCombo.List(InRect, RoundComboList[RoundCombo.GetSelectedItemIndex()], RoundComboList, RoundComboStyle);
	}

	public int GetSelectedItemIndex()
	{
		return RoundCombo.GetSelectedItemIndex();
	}

	public void SetSelectedItemIndex(int InIndex)
	{
		RoundCombo.SetSelectedItemIndex(InIndex);
	}
}

public class DivisionComboBox
{
	public ComboBox DivCombo = new ComboBox();
	public GUIContent[] DivComboList;
	public GUIStyle DivComboStyle = new GUIStyle();
	public bool IsPicking { get { return DivCombo.IsPicking; } }
	bool bInited = false;

	public void Init()
	{
		DivComboList = new GUIContent[4];
		DivComboList[0] = new GUIContent("Open");
		DivComboList[1] = new GUIContent("Mixed");
		DivComboList[2] = new GUIContent("Coop");
		DivComboList[3] = new GUIContent("Women");

		DivComboStyle.normal.textColor = Color.white;
		DivComboStyle.onHover.background =
		DivComboStyle.hover.background = new Texture2D(2, 2);
		DivComboStyle.padding.left =
		DivComboStyle.padding.right =
		DivComboStyle.padding.top =
		DivComboStyle.padding.bottom = 4;

		bInited = true;
	}

	public void SetOnSelectionChangedDelegate(ComboBox.OnSelectionChangedDelegate InDelegate)
	{
		DivCombo.OnSelectionChanged = InDelegate;
	}

	public void Draw(Rect InRect)
	{
		if (!bInited)
			Init();

		DivCombo.List(InRect, DivComboList[DivCombo.GetSelectedItemIndex()], DivComboList, DivComboStyle);
	}

	public int GetSelectedItemIndex()
	{
		return DivCombo.GetSelectedItemIndex();
	}

	public void SetSelectedItemIndex(int InIndex)
	{
		DivCombo.SetSelectedItemIndex(InIndex);
	}
}

