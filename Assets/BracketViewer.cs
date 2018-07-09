using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System;
using System.Xml.Serialization;
using System.Xml;

public class BracketViewer : MonoBehaviour
{
	Vector2 RoundsScrollPos = new Vector2();
	Vector2 TeamsScrollPos = new Vector2();
	int CurDivIndex = -1;
	int CurRoundIndex = -1;
	ECategoryView CurCat = ECategoryView.Diff;
	public ComboBox CatCombo = new ComboBox();
	public GUIContent[] CatComboList;
	public GUIStyle CatComboStyle = new GUIStyle();
	//string ExelPath = @"C:\Program Files\Microsoft Office 15\root\office15\EXCEL.EXE";
	string ExcelPath = @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE";
	public bool bInEditingMode = false;
	public bool bIsEditing = false;
	int EditPoolIndex = -1;
	int EditTeamIndex = -1;
	int EditJudgeIndex = -1;
	AIData EditAIData = null;
	ExData EditExData = null;
	DiffData EditDiffData = null;
	string EditJudgeName = "";
	bool bEditAddNewData = false;
	int CatPickingFrameUpdate = 0;
	bool bCatPicking = false;

	// Use this for initialization
	void Start()
	{
		CatComboList = new GUIContent[4];
		CatComboList[0] = new GUIContent("AI");
		CatComboList[1] = new GUIContent("Execution");
		CatComboList[2] = new GUIContent("Difficulty");
		CatComboList[3] = new GUIContent("Overall");

		CatComboStyle.normal.textColor = Color.white;
		CatComboStyle.onHover.background =
		CatComboStyle.hover.background = new Texture2D(2, 2);
		CatComboStyle.padding.left =
		CatComboStyle.padding.right =
		CatComboStyle.padding.top =
		CatComboStyle.padding.bottom = 4;

		CatCombo.SetSelectedItemIndex((int)ECategoryView.Overview);

		Global.LoadTournamentData();
	}

	// Update is called once per frame
	void Update()
	{
		CurCat = (ECategoryView)CatCombo.GetSelectedItemIndex();

		HeadJudger hj = Global.GetHeadJudger();
		if (hj)
		{
			hj.Update();
		}
	}

	void ExportScheduleToExcel()
	{
		if (CurDivIndex != -1 && CurRoundIndex != -1 && File.Exists(ExcelPath))
		{
			StringWriter OutStr = new StringWriter();

			OutStr.WriteLine((EDivision)(CurDivIndex) + " - " + (ERound)(CurRoundIndex) + "\t\tJudges");
			List<PoolData> Pools = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools;
            int PoolIndex = 0;
			foreach (PoolData pd in Pools)
			{
				OutStr.WriteLine("Pool " + pd.PoolName + "\t\tAI\tEx\tDiff");
                ResultsData JData = TournamentData.FindResultsData((EDivision)CurDivIndex, (ERound)CurRoundIndex, (EPool)PoolIndex);

				int TeamNum = 0;
				foreach (TeamDataDisplay tdd in pd.Teams)
				{
					++TeamNum;
					OutStr.Write(TeamNum + ": " + tdd.Data.PlayerNames + "\t\t");

                    if (JData != null)
                    {
                        int NameId = JData.GetNameId(ECategory.AI, TeamNum - 1);
                        if (NameId != -1)
                            OutStr.Write(NameDatabase.FindInDatabase(NameId).DisplayName);
                        OutStr.Write("\t");

                        NameId = JData.GetNameId(ECategory.Ex, TeamNum - 1);
                        if (NameId != -1)
                            OutStr.Write(NameDatabase.FindInDatabase(NameId).DisplayName);
                        OutStr.Write("\t");

                        NameId = JData.GetNameId(ECategory.Diff, TeamNum - 1);
                        if (NameId != -1)
                            OutStr.Write(NameDatabase.FindInDatabase(NameId).DisplayName);
                        OutStr.WriteLine("\t");
                    }
				}

				OutStr.WriteLine();

                ++PoolIndex;
			}

			int TempNum = 0;
			string TempFilePath = Application.persistentDataPath + "/TempExcelStr.txt";
			if (File.Exists(TempFilePath))
			{
				bool bDeletedFile = false;
				while (!bDeletedFile)
				{
					try
					{
						File.Delete(TempFilePath);

						bDeletedFile = true;
					}
					catch
					{
						TempFilePath = Application.persistentDataPath + "/TempExcelStr-" + TempNum++ + ".txt";
					}
				}
			}
			StreamWriter TempFile = new StreamWriter(TempFilePath);
			TempFile.Write(OutStr.ToString());
			TempFile.Close();
			OutStr.Close();

			TempFilePath = TempFilePath.Replace('/', '\\');
			Process.Start(ExcelPath, "\"" + TempFilePath + "\"");
		}
	}

	int GetTeamPlace(List<TeamDataDisplay> InList, TeamDataDisplay InData)
	{
		float TotalPoints = InData.Data.RoutineScores.GetTotalPoints();
		int Place = 1;
		foreach (TeamDataDisplay tdd in InList)
		{
			if (tdd.Data.RoutineScores.GetTotalPoints() > TotalPoints)
				++Place;
		}

		return Place;
	}

	void WriteRawResults()
	{
		StringWriter OutStr = new StringWriter();

		OutStr.WriteLine((EDivision)(CurDivIndex) + " " + (ERound)(CurRoundIndex) + " Results");
		List<PoolData> Pools = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools;
		foreach (PoolData pd in Pools)
		{
			OutStr.WriteLine("Pool " + pd.PoolName);
			string CatHeaders = "\t";
			string JudgeHeaders = "\t";
			if (pd.Teams.Count > 0)
			{
				for (int i = 0; i < pd.Teams[0].Data.RoutineScores.AIResults.Count; ++i)
				{
					if (i == 0)
						CatHeaders += "AI";
					CatHeaders += "\t";
					JudgeHeaders += NameDatabase.FindInDatabase(pd.Teams[0].Data.RoutineScores.AIResults[i].JudgeNameId).DisplayName + "\t";
				}
				for (int i = 0; i < pd.Teams[0].Data.RoutineScores.ExResults.Count; ++i)
				{
					if (i == 0)
						CatHeaders += "Ex";
					CatHeaders += "\t";
					JudgeHeaders += NameDatabase.FindInDatabase(pd.Teams[0].Data.RoutineScores.ExResults[i].JudgeNameId).DisplayName + "\t";
				}
				for (int i = 0; i < pd.Teams[0].Data.RoutineScores.DiffResults.Count; ++i)
				{
					if (i == 0)
						CatHeaders += "Diff";
					CatHeaders += "\t";
					JudgeHeaders += NameDatabase.FindInDatabase(pd.Teams[0].Data.RoutineScores.DiffResults[i].JudgeNameId).DisplayName + "\t";
				}
			}
			OutStr.WriteLine(CatHeaders);
			OutStr.WriteLine(JudgeHeaders + "\tTotal\tRank");

			int TeamNum = 1;
			foreach (TeamDataDisplay tdd in pd.Teams)
			{
				string TeamStr = TeamNum + ": " + tdd.Data.PlayerNames + "\t";
				foreach (AIData aid in tdd.Data.RoutineScores.AIResults)
					TeamStr += aid.GetTotalPoints() + "\t";
				foreach (ExData ed in tdd.Data.RoutineScores.ExResults)
					TeamStr += ed.GetTotalPoints() + "\t";
				foreach (DiffData dd in tdd.Data.RoutineScores.DiffResults)
					TeamStr += dd.GetTotalPoints() + "\t";
				OutStr.WriteLine(TeamStr + "\t" + tdd.Data.RoutineScores.GetTotalPoints() + "\t" + GetTeamPlace(pd.Teams, tdd));
				++TeamNum;
			}

			OutStr.WriteLine();
			OutStr.WriteLine();

			if (pd.Teams.Count > 0)
			{
				int ResultsIndex = 0;
				foreach (AIData aid in pd.Teams[0].Data.RoutineScores.AIResults)
				{
					string AIHeader = (EDivision)(CurDivIndex) + " " + (ERound)(CurRoundIndex) + " Results\tArtistic Impression\t" +
						"Pool " + pd.PoolName + "\t" + NameDatabase.FindInDatabase(aid.JudgeNameId).DisplayName;
					OutStr.WriteLine(AIHeader);
					OutStr.WriteLine("\tVariety\tTeamwork\tMusic\tFlow\tForm\tGeneral Impression\tTotal\tRank");
					int TeamIndex = 1;
					foreach (TeamDataDisplay tdd in pd.Teams)
					{
						if (ResultsIndex < tdd.Data.RoutineScores.AIResults.Count)
						{
							AIData Data = tdd.Data.RoutineScores.AIResults[ResultsIndex];
							int Rank = 1;
							foreach (TeamDataDisplay OtherTdd in pd.Teams)
							{
								if (ResultsIndex < OtherTdd.Data.RoutineScores.AIResults.Count &&
									OtherTdd.Data.RoutineScores.AIResults[ResultsIndex].GetTotalPoints() > Data.GetTotalPoints())
								{
									++Rank;
								}
							}

							string TeamStr = TeamIndex + ": " + tdd.Data.PlayerNames + "\t" + Data.Variety + "\t" + Data.Teamwork + "\t" +
								Data.Music + "\t" + Data.Flow + "\t" + Data.Form + "\t" + Data.General + "\t" + Data.GetTotalPoints() + "\t" + Rank;

							OutStr.WriteLine(TeamStr);
							++TeamIndex;
						}
					}

					++ResultsIndex;
					OutStr.WriteLine();
				}

				OutStr.WriteLine();

				ResultsIndex = 0;
				foreach (ExData ed in pd.Teams[0].Data.RoutineScores.ExResults)
				{
					string ExHeader = (EDivision)(CurDivIndex) + " " + (ERound)(CurRoundIndex) + " Results\tExecution\t" +
						"Pool " + pd.PoolName + "\t" + NameDatabase.FindInDatabase(ed.JudgeNameId).DisplayName;
					OutStr.WriteLine(ExHeader);
					OutStr.WriteLine("\t.1\t.2\t.3\t.5\tTotal\tRank");
					int TeamIndex = 1;
					foreach (TeamDataDisplay tdd in pd.Teams)
					{
						if (ResultsIndex < tdd.Data.RoutineScores.ExResults.Count)
						{
							ExData Data = tdd.Data.RoutineScores.ExResults[ResultsIndex];
							int Rank = 1;
							foreach (TeamDataDisplay OtherTdd in pd.Teams)
							{
								if (ResultsIndex < OtherTdd.Data.RoutineScores.ExResults.Count &&
									OtherTdd.Data.RoutineScores.ExResults[ResultsIndex].GetTotalPoints() > Data.GetTotalPoints())
								{
									++Rank;
								}
							}

							string TeamStr = TeamIndex + ": " + tdd.Data.PlayerNames + "\t" + Data.Point1Count + "\t" + Data.Point2Count + "\t" +
								Data.Point3Count + "\t" + Data.Point5Count + "\t" + Data.GetTotalPoints() + "\t" + Rank;

							OutStr.WriteLine(TeamStr);
							++TeamIndex;
						}
					}

					++ResultsIndex;
					OutStr.WriteLine();
				}

				OutStr.WriteLine();

				ResultsIndex = 0;
				foreach (DiffData dd in pd.Teams[0].Data.RoutineScores.DiffResults)
				{
					string ExHeader = (EDivision)(CurDivIndex) + " " + (ERound)(CurRoundIndex) + " Results\tDifficulty\t" +
						"Pool " + pd.PoolName + "\t" + NameDatabase.FindInDatabase(dd.JudgeNameId).DisplayName;
					OutStr.WriteLine(ExHeader);
					string BlockHeader = "\t";
					for (int i = 0; i < dd.NumScores; ++i)
					{
						BlockHeader += (i + 1) + "\t";
					}
					OutStr.WriteLine(BlockHeader + "Total\tRank");
					int TeamIndex = 1;
					foreach (TeamDataDisplay tdd in pd.Teams)
					{
						if (ResultsIndex < tdd.Data.RoutineScores.DiffResults.Count)
						{
							DiffData Data = tdd.Data.RoutineScores.DiffResults[ResultsIndex];
							int Rank = 1;
							foreach (TeamDataDisplay OtherTdd in pd.Teams)
							{
								if (ResultsIndex < OtherTdd.Data.RoutineScores.DiffResults.Count &&
									OtherTdd.Data.RoutineScores.DiffResults[ResultsIndex].GetTotalPoints() > Data.GetTotalPoints())
								{
									++Rank;
								}
							}

							string TeamStr = TeamIndex + ": " + tdd.Data.PlayerNames + "\t";
							for (int i = 0; i < Data.NumScores; ++i)
								TeamStr += Data.DiffScores[i] + "\t";
							TeamStr += Data.GetTotalPoints() + "\t" + Rank;
							OutStr.WriteLine(TeamStr);
							++TeamIndex;
						}
					}

					++ResultsIndex;
					OutStr.WriteLine();
				}
			}
		}

		string TempFilePath = Application.persistentDataPath + "/TempExcelStr.txt";
		if (File.Exists(TempFilePath))
			File.Delete(TempFilePath);
		StreamWriter TempFile = new StreamWriter(TempFilePath);
		TempFile.Write(OutStr.ToString());
		TempFile.Close();
		OutStr.Close();

		TempFilePath = TempFilePath.Replace('/', '\\');

		if (File.Exists(ExcelPath))
			Process.Start(ExcelPath, "\"" + TempFilePath + "\"");
	}

	void WriteRoundSettingsXml(XmlWriter writer, string PoolName)
	{
		writer.WriteStartElement("ns2:RoundSettings");
		{
			writer.WriteElementString("ns2:EventTitle", "FPAW 2017");
			writer.WriteElementString("ns2:EventSubtitle", "Udine, IT");
			writer.WriteElementString("ns2:Division", ((EDivision)CurDivIndex).ToString());
			writer.WriteElementString("ns2:Round", ((ERound)CurRoundIndex).ToString());
			writer.WriteElementString("ns2:Pool", PoolName);
			writer.WriteElementString("ns2:Minutes", Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].RoutineLengthMinutes.ToString());
		}
		writer.WriteEndElement();
	}

	void WritePlayerNamesXml(XmlWriter writer, List<TeamDataDisplay> TeamList)
	{
		for (int TeamIndex = 0; TeamIndex < TeamList.Count; ++TeamIndex)
		{
			TeamDataDisplay tdd = TeamList[TeamIndex];
			for (int PlayerIndex = 0; PlayerIndex < tdd.Data.Players.Count; ++PlayerIndex)
			{
				string PlayerName = tdd.Data.Players[PlayerIndex].Fullname;
				writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Player" + (PlayerIndex + 1), PlayerName);
			}
		}
	}

	void WriteJudgeNamesXml(XmlWriter writer, int PoolIndex)
	{
		ResultsData RData = TournamentData.FindResultsData((EDivision)CurDivIndex, (ERound)CurRoundIndex, (EPool)PoolIndex);

		int ExCount = RData.ExJudgeIds.Count;
		for (int ExIndex = 0; ExIndex < ExCount; ++ExIndex)
		{
			string JudgeName = NameDatabase.FindInDatabase(RData.ExJudgeIds[ExIndex]).DisplayName;
			writer.WriteElementString("ns2:Ex" + (ExIndex + 1), JudgeName);
		}

		int AiCount = RData.AIJudgeIds.Count;
		for (int AiIndex = 0; AiIndex < AiCount; ++AiIndex)
		{
			string JudgeName = NameDatabase.FindInDatabase(RData.AIJudgeIds[AiIndex]).DisplayName;
			writer.WriteElementString("ns2:Ai" + (AiIndex + 1), JudgeName);
		}

		int DiffCount = RData.DiffJudgeIds.Count;
		for (int DiffIndex = 0; DiffIndex < DiffCount; ++DiffIndex)
		{
			string JudgeName = NameDatabase.FindInDatabase(RData.DiffJudgeIds[DiffIndex]).DisplayName;
			writer.WriteElementString("ns2:Diff" + (DiffIndex + 1), JudgeName);
		}
	}

	void WriteExXml(XmlWriter writer, List<TeamDataDisplay> TeamList, int JudgeIndex)
	{
		if (TeamList.Count > 0 && TeamList[0].Data.RoutineScores.ExResults.Count > JudgeIndex)
		{
			writer.WriteStartElement("ns2:Ex" + (JudgeIndex + 1));
			for (int TeamIndex = 0; TeamIndex < TeamList.Count; ++TeamIndex)
			{
				TeamDataDisplay tdd = TeamList[TeamIndex];
				if (JudgeIndex < tdd.Data.RoutineScores.ExResults.Count)
				{
					ExData ed = tdd.Data.RoutineScores.ExResults[JudgeIndex];

					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Point1", ed.Point1Count.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Point2", ed.Point2Count.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Point3", ed.Point3Count.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Point5", ed.Point5Count.ToString());
				}
			}
			writer.WriteEndElement();
		}
	}

	void WriteAiXml(XmlWriter writer, List<TeamDataDisplay> TeamList, int JudgeIndex)
	{
		if (TeamList.Count > 0 && TeamList[0].Data.RoutineScores.AIResults.Count > JudgeIndex)
		{
			writer.WriteStartElement("ns2:Ai" + (JudgeIndex + 1));
			for (int TeamIndex = 0; TeamIndex < TeamList.Count; ++TeamIndex)
			{
				TeamDataDisplay tdd = TeamList[TeamIndex];
				if (JudgeIndex < tdd.Data.RoutineScores.AIResults.Count)
				{
					AIData ad = tdd.Data.RoutineScores.AIResults[JudgeIndex];

					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Variety", ad.Variety.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Teamwork", ad.Teamwork.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Music", ad.Music.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Flow", ad.Flow.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Form", ad.Form.ToString());
					writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "General", ad.General.ToString());
				}
			}
			writer.WriteEndElement();
		}
	}

	void WriteDiffXml(XmlWriter writer, List<TeamDataDisplay> TeamList, int JudgeIndex)
	{
		if (TeamList.Count > 0 && TeamList[0].Data.RoutineScores.DiffResults.Count > JudgeIndex)
		{
			writer.WriteStartElement("ns2:Diff" + (JudgeIndex + 1));
			for (int TeamIndex = 0; TeamIndex < TeamList.Count; ++TeamIndex)
			{
				TeamDataDisplay tdd = TeamList[TeamIndex];
				if (JudgeIndex < tdd.Data.RoutineScores.DiffResults.Count)
				{
					DiffData dd = tdd.Data.RoutineScores.DiffResults[JudgeIndex];
                    int DiffScoreCount = (int)(Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].RoutineLengthMinutes * 4f);
                    for (int ScoreIndex = 0; ScoreIndex < DiffScoreCount; ++ScoreIndex)
					{
						float DiffScore = dd.DiffScores[ScoreIndex];
                        writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Diff" + (ScoreIndex + 1), DiffScore.ToString());
						string ConsecValue = dd.ConsecScores[ScoreIndex] == 1 ? "+" : "";
						writer.WriteElementString("ns2:Team" + (TeamIndex + 1) + "Consec" + (ScoreIndex + 1), ConsecValue);
					}
				}
			}
			writer.WriteEndElement();
		}
	}

	void WriteExcelRoundXml(string ExportPath, PoolData pd, int PoolIndex)
	{
		StreamWriter stream = new StreamWriter(ExportPath + Path.DirectorySeparatorChar + "ExportData.xml");
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.Indent = true;
		settings.IndentChars = ("\t");
		settings.OmitXmlDeclaration = true;
		XmlWriter writer = XmlWriter.Create(stream, settings);

		writer.WriteStartElement("ns2:ScoreSheet");
		{
			writer.WriteAttributeString("xmlns:ns2", "http://freestyledisc.org/FPAScoresheets.xsd");

			WriteRoundSettingsXml(writer, pd.PoolName);

			writer.WriteStartElement("ns2:Players");
			{
				WritePlayerNamesXml(writer, pd.Teams);
			}
			writer.WriteEndElement();

			writer.WriteStartElement("ns2:Judges");
			{
				WriteJudgeNamesXml(writer, PoolIndex);
			}
			writer.WriteEndElement();
			
			WriteExXml(writer, pd.Teams, 0);
			WriteExXml(writer, pd.Teams, 1);
			WriteExXml(writer, pd.Teams, 2);

			WriteAiXml(writer, pd.Teams, 0);
			WriteAiXml(writer, pd.Teams, 1);
			WriteAiXml(writer, pd.Teams, 2);

			WriteDiffXml(writer, pd.Teams, 0);
			WriteDiffXml(writer, pd.Teams, 1);
			WriteDiffXml(writer, pd.Teams, 2);
		}
		writer.WriteEndElement();

		writer.Flush();
		stream.Close();
	}

	void ExportResultsToExcel()
	{
		if (CurDivIndex != -1 && CurRoundIndex != -1)
		{
			List<PoolData> Pools = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools;
			for (int PoolIndex = 0; PoolIndex < Pools.Count; ++PoolIndex)
			{
				try
				{
					string EJudgeDataPath = Environment.CurrentDirectory + "/EJudgeDataExports";
					string PoolName = (Pools.Count > 1 ? "/" + Pools[PoolIndex].PoolName : "");
					string ExportPath = "/" + ((EDivision)CurDivIndex).ToString() + "/" + ((ERound)CurRoundIndex).ToString() + PoolName;
					string FullExportPath = EJudgeDataPath + ExportPath;

					FullExportPath = FullExportPath.Replace('/', Path.DirectorySeparatorChar);
					if (Directory.Exists(FullExportPath))
					{
						foreach (string file in Directory.GetFiles(FullExportPath))
						{
							File.Delete(file);
						}

						Directory.Delete(FullExportPath);
					}

					Directory.CreateDirectory(FullExportPath);

					WriteExcelRoundXml(FullExportPath, Pools[PoolIndex], PoolIndex);

					string ScoreSheetTemplate = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "SpreadSheets" +
						Path.DirectorySeparatorChar + "AutoImportScoresheets.xlsm";
					string ScoreSheetTarget = FullExportPath + Path.DirectorySeparatorChar +
						((EDivision)CurDivIndex).ToString() + "-" +
						((ERound)CurRoundIndex).ToString() + "-" + (Pools.Count > 1 ? Pools[PoolIndex].PoolName : "") + ".xlsm";
					ScoreSheetTemplate = ScoreSheetTemplate.Replace('/', Path.DirectorySeparatorChar);
					ScoreSheetTarget = ScoreSheetTarget.Replace('/', Path.DirectorySeparatorChar);
					File.Copy(ScoreSheetTemplate, ScoreSheetTarget);

					Process.Start(ExcelPath, ScoreSheetTarget);
				}
				catch { }
			}
		}
	}

	void SendRestMessageAsync(LiveStream.TeamList teamList)
	{
		StartCoroutine(Global.SendRestMessage(teamList));
	}

	void OnGUI()
	{
		if (!bIsEditing)
		{
			CatCombo.List(new Rect(20, Screen.height * .03f, Screen.width * .18f, Screen.height * .07f),
				CatComboList[CatCombo.GetSelectedItemIndex()], CatComboList, CatComboStyle);

			if (CatPickingFrameUpdate != Time.frameCount)
			{
				CatPickingFrameUpdate = Time.frameCount;
				bCatPicking = CatCombo.IsPicking;
			}
		}

		if (bInEditingMode && CatCombo.GetSelectedItemIndex() == 3)
		{
			bInEditingMode = false;
			bIsEditing = false;
			EditAIData = null;
			EditExData = null;
			EditDiffData = null;
			bEditAddNewData = false;
		}

		if (!bIsEditing && GUI.Button(new Rect(Screen.width * .3f, Screen.height * .04f, Screen.width * .25f, Screen.height * .07f), "Export SCHEDULE to Excel"))
		{
			ExportScheduleToExcel();
		}

		if (!bIsEditing && GUI.Button(new Rect(Screen.width * .56f, Screen.height * .04f, Screen.width * .23f, Screen.height * .07f), "Export RESULTS to Excel"))
		{
			ExportResultsToExcel();
		}

		if (GUI.Button(new Rect(Screen.width * .83f, Screen.height * .04f, Screen.width * .17f - 20, Screen.height * .07f), "Send All Teams to Livestream"))
		{
			LiveStream.TeamList teamList = new LiveStream.TeamList();
			EDivision division = EDivision.Open;
			foreach (DivisionData dd in Global.AllData.AllDivisions)
			{
				ERound round = ERound.Finals;
				foreach (RoundData rd in dd.Rounds)
				{
					EPool pool = EPool.A;
					foreach (PoolData pd in rd.Pools)
					{
						int teamNumber = 0;
						foreach (TeamDataDisplay td in pd.Teams)
						{
							LiveStream.Team team = new LiveStream.Team(
								LiveStream.TeamStates.Inited,
								division.ToString(),
								round.ToString(),
								pool.ToString(),
								teamNumber
								);

							foreach (PlayerData playerData in td.Data.Players)
							{
								team.Players.Add(new LiveStream.Player(playerData));
							}

							teamList.Teams.Add(team);

							++teamNumber;
						}

						++pool;
					}

					++round;
				}

				++division;
			}

			SendRestMessageAsync(teamList);
		}

		#region Round Buttons
		GUIStyle RoundStyle = new GUIStyle("button");
		string LongestRoundStr = "Women - Quarterfinals";
		Rect RoundRect = new Rect(20, Screen.height * .15f, RoundStyle.CalcSize(new GUIContent(LongestRoundStr)).x + 40, Screen.height * .75f - 20);
		if (!bIsEditing && !bCatPicking)
		{
			GUILayout.BeginArea(RoundRect);

			RoundsScrollPos = GUILayout.BeginScrollView(RoundsScrollPos, GUILayout.MaxHeight(RoundRect.height / 2f));
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
							if (GUILayout.Button(RoundContent, RoundButtonStyle, GUILayout.Width(RoundRect.width * .9f), GUILayout.Height(RoundTextSize.y)))
							{
								CurDivIndex = DivIndex;
								CurRoundIndex = RoundIndex;
							}
						}
					}
				}
			}
			GUILayout.EndScrollView();

			GUILayout.EndArea();
		}
		#endregion

		if (CurDivIndex != -1 && CurRoundIndex != -1)
		{
			float StartX = RoundRect.x + RoundRect.width + 20;
			Rect TeamRect = new Rect(StartX, RoundRect.y, Screen.width - StartX - 20, Screen.height - RoundRect.y - 20);
			GUILayout.BeginArea(TeamRect);
			GUILayout.BeginVertical();
			TeamsScrollPos = GUILayout.BeginScrollView(TeamsScrollPos);
			GUILayout.BeginVertical();

			if (!bIsEditing)
			{
				List<PoolData> Pools = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools;
				int PoolIndex = 0;
				foreach (PoolData pd in Pools)
				{
					GUILayout.Label("Pool " + pd.PoolName);

					int TeamNum = 0;
					foreach (TeamDataDisplay tdd in pd.Teams)
					{
						++TeamNum;
						GUILayout.Label(TeamNum + ": " + tdd.Data.PlayerNames + ":");

						for (int ScoresIndex = 0; ScoresIndex < (CurCat == ECategoryView.Overview ? 1 : 3); ++ScoresIndex)
						{
							GUIStyle EditStyle = new GUIStyle("button");
							EditStyle.alignment = TextAnchor.MiddleLeft;
							string ResultsStr = tdd.Data.RoutineScores.GetResultsString(ScoresIndex, CurCat, true);
							if (ResultsStr.Length > 0)
							{
								if (bInEditingMode)
								{
									if (GUILayout.Button("    " + ResultsStr, EditStyle))
									{
										bIsEditing = true;
										EditPoolIndex = PoolIndex;
										EditTeamIndex = TeamNum - 1;
										EditJudgeIndex = ScoresIndex;
									}
								}
								else
									GUILayout.Label("    " + ResultsStr);
							}
							else if (bInEditingMode && (ScoresIndex == 0 || tdd.Data.RoutineScores.GetResultsString(ScoresIndex - 1, CurCat, true).Length > 0))
							{
								if (GUILayout.Button("    Enter New Scores", EditStyle))
								{
									bIsEditing = true;
									EditPoolIndex = PoolIndex;
									EditTeamIndex = TeamNum - 1;
									EditJudgeIndex = ScoresIndex;
								}
							}
						}
					}

					++PoolIndex;
				}
			}
			else
			{
				TeamData Data = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools[EditPoolIndex].Teams[EditTeamIndex].Data;
				if (CatCombo.GetSelectedItemIndex() == 0)
				{
					if (EditAIData == null)
					{
						AIData JData = null;
						if (EditJudgeIndex >= Data.RoutineScores.AIResults.Count)
						{
							EditAIData = new AIData((EDivision)CurDivIndex, (ERound)CurRoundIndex, (EPool)EditPoolIndex, EditTeamIndex);
							bEditAddNewData = true;
							EditJudgeName = "Judge Name";
						}
						else
						{
							JData = Data.RoutineScores.AIResults[EditJudgeIndex];
							EditAIData = new AIData(JData);
						}
					}

					GUILayout.BeginHorizontal();

					if (EditAIData.JudgeNameId == -1)
					{
						GUILayout.Label("Judge:");
						EditJudgeName = GUILayout.TextField(EditJudgeName);

						char[] Seperators = new char[] {',', ' '};
						string[] Splits = EditJudgeName.Split(Seperators, System.StringSplitOptions.RemoveEmptyEntries);
						NameData JudgeName = null;
						if (Splits.Length == 2)
						{
							JudgeName = NameDatabase.FindInDatabase(Splits[0], Splits[1]);

							if (JudgeName == null)
								JudgeName = NameDatabase.FindInDatabase(Splits[1], Splits[0]);
						}

						if (JudgeName != null)
							EditAIData.JudgeNameId = JudgeName.Id;
					}
					else
						GUILayout.Label(NameDatabase.FindInDatabase(EditAIData.JudgeNameId).DisplayName + ": ");
					GUILayout.Label("V: ");
					float.TryParse(GUILayout.TextField(EditAIData.Variety.ToString()), out EditAIData.Variety);
					GUILayout.Label("T: ");
					float.TryParse(GUILayout.TextField(EditAIData.Teamwork.ToString()), out EditAIData.Teamwork);
					GUILayout.Label("M: ");
					float.TryParse(GUILayout.TextField(EditAIData.Music.ToString()), out EditAIData.Music);
					GUILayout.Label("Fm: ");
					float.TryParse(GUILayout.TextField(EditAIData.Form.ToString()), out EditAIData.Form);
					GUILayout.Label("Fw: ");
					float.TryParse(GUILayout.TextField(EditAIData.Flow.ToString()), out EditAIData.Flow);
                    GUILayout.Label("G: ");
                    float.TryParse(GUILayout.TextField(EditAIData.General.ToString()), out EditAIData.General);
					GUILayout.Label("Total: " + EditAIData.GetTotalPoints().ToString());

					GUILayout.EndHorizontal();
				}
				else if (CatCombo.GetSelectedItemIndex() == 1)
				{
					if (EditExData == null)
					{
						ExData JData = null;
						if (EditJudgeIndex >= Data.RoutineScores.ExResults.Count)
						{
							EditExData = new ExData((EDivision)CurDivIndex, (ERound)CurRoundIndex, (EPool)EditPoolIndex, EditTeamIndex);
							bEditAddNewData = true;
							EditJudgeName = "Judge Name";
						}
						else
						{
							JData = Data.RoutineScores.ExResults[EditJudgeIndex];
							EditExData = new ExData(JData);
						}
					}

					GUILayout.BeginHorizontal();

					if (EditExData.JudgeNameId == -1)
					{
						GUILayout.Label("Judge:");
						EditJudgeName = GUILayout.TextField(EditJudgeName);

						char[] Seperators = new char[] { ',', ' ' };
						string[] Splits = EditJudgeName.Split(Seperators, System.StringSplitOptions.RemoveEmptyEntries);
						NameData JudgeName = null;
						if (Splits.Length == 2)
						{
							JudgeName = NameDatabase.FindInDatabase(Splits[0], Splits[1]);

							if (JudgeName == null)
								JudgeName = NameDatabase.FindInDatabase(Splits[1], Splits[0]);
						}

						if (JudgeName != null)
							EditExData.JudgeNameId = JudgeName.Id;
					}
					else
						GUILayout.Label(NameDatabase.FindInDatabase(EditExData.JudgeNameId).DisplayName + ": ");
					GUILayout.Label(".1: ");
					int.TryParse(GUILayout.TextField(EditExData.Point1Count.ToString()), out EditExData.Point1Count);
					GUILayout.Label(".2: ");
					int.TryParse(GUILayout.TextField(EditExData.Point2Count.ToString()), out EditExData.Point2Count);
					GUILayout.Label(".3: ");
					int.TryParse(GUILayout.TextField(EditExData.Point3Count.ToString()), out EditExData.Point3Count);
					GUILayout.Label(".5: ");
					int.TryParse(GUILayout.TextField(EditExData.Point5Count.ToString()), out EditExData.Point5Count);
					GUILayout.Label("Total: " + EditExData.GetTotalPoints().ToString());

					GUILayout.EndHorizontal();
				}
				else if (CatCombo.GetSelectedItemIndex() == 2)
				{
					if (EditDiffData == null)
					{
						DiffData JData = null;
						if (EditJudgeIndex >= Data.RoutineScores.DiffResults.Count)
						{
							EditDiffData = new DiffData(20, (EDivision)CurDivIndex, (ERound)CurRoundIndex, (EPool)EditPoolIndex, EditTeamIndex);
							bEditAddNewData = true;
							EditJudgeName = "Judge Name";
						}
						else
						{
							JData = Data.RoutineScores.DiffResults[EditJudgeIndex];
							EditDiffData = new DiffData(JData);
						}
					}

					GUILayout.BeginVertical();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Number of Scores: ");
					int.TryParse(GUILayout.TextField(EditDiffData.NumScores.ToString()), out EditDiffData.NumScores);
					EditDiffData.NumScores = Mathf.Clamp(EditDiffData.NumScores, 0, 20);
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();

					if (EditDiffData.JudgeNameId == -1)
					{
						GUILayout.Label("Judge:");
						EditJudgeName = GUILayout.TextField(EditJudgeName);

						char[] Seperators = new char[] { ',', ' ' };
						string[] Splits = EditJudgeName.Split(Seperators, System.StringSplitOptions.RemoveEmptyEntries);
						NameData JudgeName = null;
						if (Splits.Length == 2)
						{
							JudgeName = NameDatabase.FindInDatabase(Splits[0], Splits[1]);

							if (JudgeName == null)
								JudgeName = NameDatabase.FindInDatabase(Splits[1], Splits[0]);
						}

						if (JudgeName != null)
							EditDiffData.JudgeNameId = JudgeName.Id;
					}
					else
						GUILayout.Label(NameDatabase.FindInDatabase(EditDiffData.JudgeNameId).DisplayName + ": ");

					for (int i = 0; i < EditDiffData.NumScores; ++i)
					{
						string DiffStr = GUILayout.TextField(EditDiffData.DiffScores[i].ToString() + EditDiffData.GetConsecString(i));
						if (DiffStr.Contains("-"))
						{
							DiffStr = DiffStr.Replace("-", "");
							EditDiffData.ConsecScores[i] = -1;
						}
						else if (DiffStr.Contains("+"))
						{
							DiffStr = DiffStr.Replace("+", "");
							EditDiffData.ConsecScores[i] = 1;
						}
						else
						{
							EditDiffData.ConsecScores[i] = 0;
						}
						float.TryParse(DiffStr, out EditDiffData.DiffScores[i]);
					}

					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
				}
			}

			GUILayout.EndVertical();
			GUILayout.EndScrollView();

			if (CatCombo.GetSelectedItemIndex() != 3)
			{
				if (!bInEditingMode && GUILayout.Button("Enter Edit Mode", GUILayout.Height(Screen.height * .1f)))
					bInEditingMode = true;
				else if (bInEditingMode)
				{
					if (bIsEditing)
					{
						GUILayout.BeginHorizontal();

						if (GUILayout.Button("Delete Score", GUILayout.Height(Screen.height * .1f)))
						{
							TeamData Data = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools[EditPoolIndex].Teams[EditTeamIndex].Data;
							if (CatCombo.GetSelectedItemIndex() == 0)
							{
								if (!bEditAddNewData && EditJudgeIndex < Data.RoutineScores.AIResults.Count)
									Data.RoutineScores.AIResults[EditJudgeIndex] = new AIData();
								EditAIData = null;
							}
							else if (CatCombo.GetSelectedItemIndex() == 1)
							{
								if (!bEditAddNewData && EditJudgeIndex < Data.RoutineScores.ExResults.Count)
									Data.RoutineScores.ExResults[EditJudgeIndex] = new ExData();
								EditExData = null;
							}
							else if (CatCombo.GetSelectedItemIndex() == 2)
							{
								if (!bEditAddNewData && EditJudgeIndex < Data.RoutineScores.DiffResults.Count)
									Data.RoutineScores.DiffResults[EditJudgeIndex] = new DiffData();
								EditDiffData = null;
							}

							bIsEditing = false;
							EditAIData = null;
							EditExData = null;
							EditDiffData = null;
							bEditAddNewData = false;

							Global.AllData.Save();
						}

						if (GUILayout.Button("Discard Changes", GUILayout.Height(Screen.height * .1f)))
						{
							bIsEditing = false;
							EditAIData = null;
							EditExData = null;
							EditDiffData = null;
							bEditAddNewData = false;
						}

						if (GUILayout.Button("Save Changes", GUILayout.Height(Screen.height * .1f)))
						{
							TeamData Data = Global.AllData.AllDivisions[CurDivIndex].Rounds[CurRoundIndex].Pools[EditPoolIndex].Teams[EditTeamIndex].Data;
							if (CatCombo.GetSelectedItemIndex() == 0)
							{
								if (bEditAddNewData)
									Data.RoutineScores.AIResults.Add(new AIData(EditAIData));
								else
									Data.RoutineScores.AIResults[EditJudgeIndex] = new AIData(EditAIData);
								EditAIData = null;
							}
							else if (CatCombo.GetSelectedItemIndex() == 1)
							{
								if (bEditAddNewData)
									Data.RoutineScores.ExResults.Add(new ExData(EditExData));
								else
									Data.RoutineScores.ExResults[EditJudgeIndex] = new ExData(EditExData);
								EditExData = null;
							}
							else if (CatCombo.GetSelectedItemIndex() == 2)
							{
								if (bEditAddNewData)
									Data.RoutineScores.DiffResults.Add(new DiffData(EditDiffData));
								else
									Data.RoutineScores.DiffResults[EditJudgeIndex] = new DiffData(EditDiffData);
								EditDiffData = null;
							}

							bIsEditing = false;
							bEditAddNewData = false;

							Global.AllData.Save();
						}
						GUILayout.EndHorizontal();
					}
					else if (GUILayout.Button("Exit Edit Mode", GUILayout.Height(Screen.height * .1f)))
						bInEditingMode = false;
				}
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}

public enum ECategoryView
{
	AI,
	Ex,
	Diff,
	Overview
}
