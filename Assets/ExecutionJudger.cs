using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class ExecutionJudger : JudgerBase
{
	public ExData CurData = new ExData();
	public ExData CachedData = new ExData();
	List<BackupExData> BackupList = new List<BackupExData>();
	public BackupExData CurBackupData = null;

    public GameObject TotalScoreUI;
    public GameObject[] AddButtonUIList;
    public GameObject[] SubtractButtonUIList;
    public GameObject[] CountButtonUIList;
    public GameObject[] PointsUIList;

	// Use this for initialization
	new void Start()
	{
		base.Start();

		JudgerCategory = ECategory.Ex;

        UpdatePoints();
	}

	// Update is called once per frame
	new void Update()
	{
		base.Update();
	}

	string GetBackupDisplayString(BackupExData bd)
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

		foreach (BackupExData bd in BackupList)
		{
			GUILayout.BeginHorizontal();
			string BackupStr = GetBackupDisplayString(bd) + " | .1: " + bd.Data.Point1Count + "  .2: " + bd.Data.Point2Count + "  .3: " + bd.Data.Point3Count +
				"  .5: " + bd.Data.Point5Count;
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

				UpdatePoints();
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
		}
	}

	public override void StartRoutineJudging()
	{
		base.StartRoutineJudging();

		bBackupLoaded = false;

        UpdatePoints();
	}

	public override void SendResultsToHeadJudger(int InDiv, int InRound, int InPool, int InTeam)
	{
		base.SendResultsToHeadJudger(InDiv, InRound, InPool, InTeam);

		Debug.Log(" SendResultsToHeadJudger " + InDiv + " " + InRound + " " + InPool + " " + InTeam);

		RoutineScoreData SData = Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[InPool].Teams[InTeam].Data.RoutineScores;
		CurData.Division = (EDivision)InDiv;
		CurData.Round = (ERound)InRound;
		CurData.Pool = (EPool)InPool;
		CurData.Team = InTeam;
		CurData.JudgeNameId = GetJudgeNameId();
		SData.SetExResults(CurData);

		if (Networking.IsConnectedToServer)
		{
			Debug.Log(" send ex data to server " + CurData.Point1Count);

			Global.NetObj.ClientSendFinishJudgingEx(CurData.SerializeToString());
		}
		else
		{
			CachedData = new ExData(CurData);
			Networking.bNeedSendCachedResults = true;
		}
	}

	public override void SendCachedResultsToHeadJudger()
	{
		base.SendCachedResultsToHeadJudger();

		if (Networking.IsConnectedToServer)
		{
			Global.NetObj.ClientSendFinishJudgingEx(CachedData.SerializeToString());
			Networking.bNeedSendCachedResults = false;
		}
	}

	public override void StartEditingTeam(int InTeamIndex)
	{
		if (Global.AllData == null || CurPool == EPool.None || (int)CurPool >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count ||
			InTeamIndex < 0 || InTeamIndex >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams.Count)
			return;

		RoutineScoreData SData = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools[(int)CurPool].Teams[InTeamIndex].Data.RoutineScores;

		if (SData != null && SData.DiffResults != null)
		{
			CurData = null;

			base.StartEditingTeam(InTeamIndex);

			foreach (ExData ed in SData.ExResults)
			{
				if (ed.JudgeNameId == GetJudgeNameId())
				{
					CurData = ed;
					break;
				}
			}

			if (CurData == null)
				CurData = new ExData(CurDivision, CurRound, CurPool, InTeamIndex);
		}

		UpdatePoints();
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

				StreamWriter file = new StreamWriter(BackupPath + "/ExBackup-" + DateTime.Now.Ticks + ".xml");
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
			if (filename.Contains("ExBackup"))
			{
				BackupExData backup = new BackupExData();
				try
				{
					FileStream BackupFile = new FileStream(filename, FileMode.Open);
					backup.Data = ExData.Load(BackupFile);
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
			delegate(BackupExData b1, BackupExData b2)
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
			CurData = ExData.Load(File);
			File.Close();
		}
		catch (System.Exception e)
		{
			Debug.Log("Load autosave exception: " + e.Message);
		}
	}

	public override void ResetScoreData()
	{
		CurData = new ExData(CurDivision, CurRound, CurPool, CurTeam);
	}

    public void UpdatePoints()
    {
        CountButtonUIList[0].GetComponent<Text>().text = CurData.Point1Count.ToString();
        CountButtonUIList[1].GetComponent<Text>().text = CurData.Point2Count.ToString();
        CountButtonUIList[2].GetComponent<Text>().text = CurData.Point3Count.ToString();
        CountButtonUIList[3].GetComponent<Text>().text = CurData.Point5Count.ToString();

        PointsUIList[0].GetComponent<Text>().text = (CurData.Point1Count * .1f).ToString("0.0");
        PointsUIList[1].GetComponent<Text>().text = (CurData.Point2Count * .2f).ToString("0.0");
        PointsUIList[2].GetComponent<Text>().text = (CurData.Point3Count * .3f).ToString("0.0");
        PointsUIList[3].GetComponent<Text>().text = (CurData.Point5Count * .5f).ToString("0.0");

        TotalScoreUI.GetComponent<Text>().text = CurData.GetTotalPoints().ToString("0.0");
    }

    public void OnAddButtonClick(int PointDeduction)
    {
        switch (PointDeduction)
        {
            case 1:
                ++CurData.Point1Count;
                break;
            case 2:
                ++CurData.Point2Count;
                break;
            case 3:
                ++CurData.Point3Count;
                break;
            case 5:
                ++CurData.Point5Count;
                break;
        }

        UpdatePoints();

        BackupCurrentData();
    }

    public void OnSubtractButtonClick(int PointDeduction)
    {
        switch (PointDeduction)
        {
            case 1:
                CurData.Point1Count = Math.Max(0, CurData.Point1Count - 1);
                break;
            case 2:
                CurData.Point2Count = Math.Max(0, CurData.Point2Count - 1);;
                break;
            case 3:
                CurData.Point3Count = Math.Max(0, CurData.Point3Count - 1);;
                break;
            case 5:
                CurData.Point5Count = Math.Max(0, CurData.Point5Count - 1);;
                break;
        }

        UpdatePoints();

        BackupCurrentData();
    }

    public override string GetTeamNameString()
    {
        string NamesString = base.GetTeamNameString();;
        NamesString = NamesString.Replace(" - ", "\n");

        return NamesString;
    }
}

public class BackupExData
{
	public ExData Data = null;
	public DateTime WrittenTime;
	public string Filename = null;
}

public class ExData
{
	public int JudgeNameId = -1;
	public EDivision Division;
	public ERound Round;
	public EPool Pool;
	public int Team;
	public int Point1Count = 0;
	public int Point2Count = 0;
	public int Point3Count = 0;
	public int Point5Count = 0;

	public ExData()
	{
	}

	public ExData(EDivision InDiv, ERound InRound, EPool InPool, int InTeam)
	{
		Division = InDiv;
		Round = InRound;
		Pool = InPool;
		Team = InTeam;
	}

	public ExData(ExData InData)
	{
		JudgeNameId = InData.JudgeNameId;
		Division = InData.Division;
		Round = InData.Round;
		Pool = InData.Pool;
		Team = InData.Team;
		Point1Count = InData.Point1Count;
		Point2Count = InData.Point2Count;
		Point3Count = InData.Point3Count;
		Point5Count = InData.Point5Count;
	}

	public void Reset()
	{
		Point1Count = 0;
		Point2Count = 0;
		Point3Count = 0;
		Point5Count = 0;
	}

	public string SerializeToString()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(ExData));
		MemoryStream stream = new MemoryStream();
		serializer.Serialize(stream, this);
		stream.Position = 0;
		StreamReader Reader = new StreamReader(stream);
		string WholeString = Reader.ReadToEnd();
		Reader.Close();

		stream.Close();

		return WholeString;
	}

	public static ExData Load(Stream InStream)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(ExData));
		ExData LoadedData = serializer.Deserialize(InStream) as ExData;

		return LoadedData;
	}

	public float GetTotalPoints()
	{
		return 10f - Point1Count * .1f - Point2Count * .2f - Point3Count * .3f - Point5Count * .5f;
	}

	public bool IsValid()
	{
		return JudgeNameId != -1;
	}
}
