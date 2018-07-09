using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class ArtisticImpressionJudger : JudgerBase
{
	NumberScroll VarietyNS = new NumberScroll();
	NumberScroll TeamworkNS = new NumberScroll();
	NumberScroll MusicNS = new NumberScroll();
	NumberScroll FlowNS = new NumberScroll();
	NumberScroll FormNS = new NumberScroll();
    NumberScroll GeneralNS = new NumberScroll();
	public AIData CurData = new AIData();
	public AIData CachedData = new AIData();
	public float CurrentTotalScore = 0;
	float CurBackupScore = 0;
	float BackupTimer = -1f;
	List<BackupAIData> BackupList = new List<BackupAIData>();
	public BackupAIData CurBackupData = null;

    public GameObject VarietyNumberBox;
    public GameObject TeamworkNumberBox;
    public GameObject MusicNumberBox;
    public GameObject FlowNumberBox;
    public GameObject FormNumberBox;
    public GameObject GeneralNumberBox;
    public GameObject TotalScoreUI;

	// Use this for initialization
	new void Start()
	{
		base.Start();

		JudgerCategory = ECategory.AI;
	}

	// Update is called once per frame
	new void Update()
	{
		base.Update();

		VarietyNS.Update();
		TeamworkNS.Update();
		MusicNS.Update();
		FlowNS.Update();
		FormNS.Update();
        GeneralNS.Update();

		CurrentTotalScore = VarietyNS.GetRoundedNumber() + TeamworkNS.GetRoundedNumber() + MusicNS.GetRoundedNumber() + FlowNS.GetRoundedNumber() +
            FormNS.GetRoundedNumber() + GeneralNS.GetRoundedNumber();
		CurrentTotalScore /= 6f;

		if (CurrentTotalScore != CurBackupScore)
		{
			BackupTimer -= Time.deltaTime;
			if (BackupTimer < 0)
			{
				BackupTimer = 5f;

				CurBackupScore = CurrentTotalScore;

				BackupCurrentData();
			}
		}

		CurData.Variety = VarietyNS.NumberValue;
		CurData.Teamwork = TeamworkNS.NumberValue;
		CurData.Music = MusicNS.NumberValue;
		CurData.Flow = FlowNS.NumberValue;
		CurData.Form = FormNS.NumberValue;
        CurData.General = GeneralNS.NumberValue;
	}

	string GetBackupDisplayString(BackupAIData bd)
	{
		TeamData td = Global.GetTeamData(bd.Data.Division, bd.Data.Round, bd.Data.Pool, bd.Data.Team);
		string TeamName = td != null ? td.PlayerNames : "Missing Team";
		NameData JudgeNameData = NameDatabase.FindInDatabase(bd.Data.JudgeNameId);
		string JudgeName = JudgeNameData != null ? JudgeNameData.DisplayName : "Missing Judge";
		string BackupStr = JudgeName + "  " + bd.Data.Division.ToString() + " " + bd.Data.Round.ToString() + " " +
			(char)(bd.Data.Pool + 'A') + " | " + TeamName + " | " + bd.WrittenTime.ToString();

		return BackupStr;
	}

	void DrawBackupList()
	{
		Rect BackupArea = new Rect(20, 100, Screen.width - 40, Screen.height - 150);
		GUILayout.BeginArea(BackupArea);
		GUILayout.BeginVertical();
		BackupAreaScrollPos = GUILayout.BeginScrollView(BackupAreaScrollPos);

		foreach (BackupAIData bd in BackupList)
		{
			GUILayout.BeginHorizontal();
			string BackupStr = GetBackupDisplayString(bd) + " | V: " + bd.Data.Variety + "  T: " + bd.Data.Teamwork + "  M: " + bd.Data.Music +
				"  Fw: " + bd.Data.Flow + "  Fm: " + bd.Data.Form;
			GUIStyle LabelStyle = new GUIStyle("label");
			GUIContent BackupContent = new GUIContent(BackupStr);
			GUILayout.Label(BackupContent, GUILayout.MaxWidth(LabelStyle.CalcSize(BackupContent).x + 20));
			if (GUILayout.Button("Load"))
			{
				bIsChoosingBackup = false;
				//bBackupLoaded = true;
				CurData = bd.Data;
				//CurBackupData = bd;

				HeaderDrawer.CanvasGO.SetActive(true);
				JudgerCanvasUI.SetActive(true);

				VarietyNS.NumberValue = CurData.Variety;
				TeamworkNS.NumberValue = CurData.Teamwork;
				MusicNS.NumberValue = CurData.Music;
				FormNS.NumberValue = CurData.Form;
				FlowNS.NumberValue = CurData.Flow;
                GeneralNS.NumberValue = CurData.General;
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	new void OnGUI()
	{
		if (bIsChoosingBackup)
		{
			DrawBackupList();
		}
		else
		{
			if (bBackupLoaded)
			{
				if (CurBackupData != null)
				{
					GUIStyle BackupTextStyle = new GUIStyle("label");
					BackupTextStyle.fontSize = 30;
					GUI.Label(new Rect(Screen.width * .07f, Screen.height * .11f, Screen.width * .86f, Screen.height * .3f),
						"Backup data for: " + GetBackupDisplayString(CurBackupData), BackupTextStyle);

					if (GUI.Button(new Rect(Screen.width * .07f, Screen.height * .35f, Screen.width * .3f, Screen.height * .2f), "Send Backup Data"))
					{
						SendResultsToHeadJudger((int)CurData.Division, (int)CurData.Round, (int)CurData.Pool, CurData.Team);
					}
					if (GUI.Button(new Rect(Screen.width * .42f, Screen.height * .35f, Screen.width * .51f, Screen.height * .2f), "Exit Backup mode and discard changes"))
					{
						bBackupLoaded = false;
						CurBackupData = null;
					}
				}
			}
			else
			{
				base.OnGUI();
			}

            if (!bIsDrawingEditingTeams)
            {
                VarietyNS.DrawRect = Global.GetScreenSpaceRectTransform(VarietyNumberBox);
                TeamworkNS.DrawRect = Global.GetScreenSpaceRectTransform(TeamworkNumberBox);
                MusicNS.DrawRect = Global.GetScreenSpaceRectTransform(MusicNumberBox);
                FlowNS.DrawRect = Global.GetScreenSpaceRectTransform(FlowNumberBox);
                FormNS.DrawRect = Global.GetScreenSpaceRectTransform(FormNumberBox);
                GeneralNS.DrawRect = Global.GetScreenSpaceRectTransform(GeneralNumberBox);


                VarietyNS.Draw();
                TeamworkNS.Draw();
                MusicNS.Draw();
                FlowNS.Draw();
                FormNS.Draw();
                GeneralNS.Draw();

                TotalScoreUI.GetComponent<Text>().text = CurrentTotalScore.ToString("0.00");
            }
		}
	}

	public override void StartRoutineJudging()
	{
		base.StartRoutineJudging();

		bBackupLoaded = false;
	}

	public override void SendResultsToHeadJudger(int InDiv, int InRound, int InPool, int InTeam)
	{
		base.SendResultsToHeadJudger(InDiv, InRound, InPool, InTeam);

		RoutineScoreData SData = Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[InPool].Teams[InTeam].Data.RoutineScores;
		CurData.Division = (EDivision)InDiv;
		CurData.Round = (ERound)InRound;
		CurData.Pool = (EPool)InPool;
		CurData.Team = InTeam;
		CurData.JudgeNameId = GetJudgeNameId();
		SData.SetAIResults(CurData);

		if (Networking.IsConnectedToServer)
			Global.NetObj.ClientSendFinishJudgingAI(CurData.SerializeToString());
		else
		{
			CachedData = new AIData(CurData);
			Networking.bNeedSendCachedResults = true;
		}
	}

	public override void SendCachedResultsToHeadJudger()
	{
		base.SendCachedResultsToHeadJudger();

		if (Networking.IsConnectedToServer)
		{
			Global.NetObj.ClientSendFinishJudgingAI(CachedData.SerializeToString());
			Networking.bNeedSendCachedResults = false;
		}
	}

	public override void StartEditingTeam(int InTeamIndex)
	{
		if (Global.AllData == null || CurPool == EPool.None || (int)CurPool >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count ||
			InTeamIndex < 0 || InTeamIndex >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams.Count)
		{
			return;
		}

		RoutineScoreData SData = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams[InTeamIndex].Data.RoutineScores;

		if (SData != null && SData.DiffResults != null)
		{
			CurData = null;

			base.StartEditingTeam(InTeamIndex);

			foreach (AIData aid in SData.AIResults)
			{
				if (aid.JudgeNameId == GetJudgeNameId())
				{
					CurData = aid;
					break;
				}
			}

			if (CurData == null)
				CurData = new AIData(CurDivision, CurRound, CurPool, InTeamIndex);

			VarietyNS.NumberValue = CurData.Variety;
			TeamworkNS.NumberValue = CurData.Teamwork;
			MusicNS.NumberValue = CurData.Music;
			FlowNS.NumberValue = CurData.Flow;
			FormNS.NumberValue = CurData.Form;
            GeneralNS.NumberValue = CurData.General;
		}
	}

	void BackupCurrentData()
	{
		try
		{
			if (CurData != null)
			{
				CurData.JudgeNameId = GetJudgeNameId();

				string BackupPath = Application.persistentDataPath + "/Backup";
				if (!Directory.Exists(BackupPath))
					Directory.CreateDirectory(BackupPath);

				StreamWriter file = new StreamWriter(BackupPath + "/AIBackup-" + DateTime.Now.Ticks + ".xml");
				file.WriteLine(CurData.SerializeToString());
				file.Close();
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Backup exception: " + e.Message);
		}
	}

	public override void RecoverAutosave()
	{
		base.RecoverAutosave();

		BackupList.Clear();

		string BackupPath = Application.persistentDataPath + "/Backup";
		string[] Files = Directory.GetFiles(BackupPath);
		foreach (string filename in Files)
		{
			if (filename.Contains("AIBackup"))
			{
				BackupAIData backup = new BackupAIData();
				try
				{
					FileStream BackupFile = new FileStream(filename, FileMode.Open);
					backup.Data = AIData.Load(BackupFile);
					backup.Filename = filename;
					BackupFile.Close();
				}
				catch (System.Exception e)
				{
					Debug.Log("Load autosave exception: " + e.Message);
				}

				backup.WrittenTime = File.GetLastWriteTime(filename);
				BackupList.Add(backup);
			}
		}

		BackupList.Sort(
			delegate(BackupAIData b1, BackupAIData b2)
			{
				if (b1 == b2)
					return 0;
				else if (b1.WrittenTime < b2.WrittenTime)
					return 1;
				else
					return -1;
			});

		// Delete old backup files
		if (BackupList.Count > Global.MaxBackupFileCount)
		{
			for (int FileIndex = Global.MaxBackupFileCount; FileIndex < BackupList.Count; ++FileIndex)
			{
				try
				{
					File.Delete(BackupList[FileIndex].Filename);
				}
				catch (System.Exception e)
				{
					Debug.Log("Delete old backup files exception: " + e.Message);
				}
			}

			while (BackupList.Count > Global.MaxBackupFileCount)
			{
				BackupList.RemoveAt(Global.MaxBackupFileCount);
			}
		}
	}

	public void LoadAutosave(string InFilename)
	{
		try
		{
			FileStream File = new FileStream(InFilename, FileMode.Open);
			CurData = AIData.Load(File);
			File.Close();
		}
		catch (System.Exception e)
		{
			Debug.Log("Load autosave exception: " + e.Message);
		}
	}

	public override void ResetScoreData()
	{
		CurData = new AIData(CurDivision, CurRound, CurPool, CurTeam);

		VarietyNS.NumberValue = 0;
		TeamworkNS.NumberValue = 0;
		MusicNS.NumberValue = 0;
		FlowNS.NumberValue = 0;
		FormNS.NumberValue = 0;
        GeneralNS.NumberValue = 0;
	}
}

public class BackupAIData
{
	public AIData Data = null;
	public DateTime WrittenTime;
	public string Filename = null;
}

public class AIData
{
	public int JudgeNameId = -1;
	public EDivision Division;
	public ERound Round;
	public EPool Pool;
	public int Team;
	public float Variety = 0;
	public float Teamwork = 0;
	public float Music = 0;
	public float Flow = 0;
	public float Form = 0;
    public float General = 0;

	public AIData()
	{
	}

	public AIData(EDivision InDiv, ERound InRound, EPool InPool, int InTeam)
	{
		Division = InDiv;
		Round = InRound;
		Pool = InPool;
		Team = InTeam;
	}

	public AIData(AIData InData)
	{
		JudgeNameId = InData.JudgeNameId;
		Division = InData.Division;
		Round = InData.Round;
		Pool = InData.Pool;
		Team = InData.Team;
		Variety = InData.Variety;
		Teamwork = InData.Teamwork;
		Music = InData.Music;
		Flow = InData.Flow;
		Form = InData.Form;
        General = InData.General;
	}

	public string SerializeToString()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(AIData));
		MemoryStream stream = new MemoryStream();
		serializer.Serialize(stream, this);
		stream.Position = 0;
		StreamReader Reader = new StreamReader(stream);
		string WholeString = Reader.ReadToEnd();
		Reader.Close();

		stream.Close();

		return WholeString;
	}

	public static AIData Load(Stream InStream)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(AIData));
		AIData LoadedData = serializer.Deserialize(InStream) as AIData;

		return LoadedData;
	}

	public float GetTotalPoints()
	{
		return (Variety + Teamwork + Music + Flow + Form + General) / 6f;
	}

	public bool IsValid()
	{
		return JudgeNameId != -1;
	}
}
