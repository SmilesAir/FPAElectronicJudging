using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using System;
using UnityEngine.UI;

public class DifficultyJudger : JudgerBase
{
	float NumberLineWidth = 0;
	float NumberLineHeight = 0;
	Rect NumberLineRect = new Rect();
	bool bInputingDiffScore = false;
	float CurrentDiffScore = 0;
	Vector3 MousePos = new Vector3();
	float DiffScoreSnap = .5f;
	public DiffData CurData;
	public DiffData CachedData = new DiffData();
	NumberScroll[] NSArray = new NumberScroll[20];
	int[] ConsecScores = new int[20];
	int RecordScoreIndex = -1;
	int RecordScoreConsecIndex = -1;
	public AudioClip[] MarkAudioClips = new AudioClip[20];
	public AudioClip EndAudioClip;
	bool bPlayedEndClip = false;
	bool bInputingConsec = false;
	float InputingConsecCooldown = -1f;
	List<BackupDiffData> BackupList = new List<BackupDiffData>();
	public BackupDiffData CurBackupData = null;

    public GameObject NumberlineUI;
    public Button PlusButtonUI;
    public Button NoneButtonUI;
    public GameObject TotalScoreUI;
    public GameObject[] NumberBoxesList;
    public GameObject[] LabelList;

	// Use this for initialization
	new void Start()
	{
		base.Start();

		NumberLineWidth = Screen.width;
		NumberLineHeight = NumberLineWidth / 4f;
		NumberLineRect = new Rect(0, Screen.height - NumberLineHeight, NumberLineWidth, NumberLineHeight - 20);

		float NSStartX = 20;
		float NSStartY = Screen.height * .42f;
		float TotalWidth = Screen.width - 2f * NSStartX;
		float NSWidth = TotalWidth / NSArray.Length * 2f;
        for (int i = 0; i < 20; ++i)
        {
            NSArray[i] = new NumberScroll();
        }
		
		JudgerCategory = ECategory.Diff;
	}

	// Update is called once per frame
	new void Update()
	{
        if (bIsDrawingEditingTeams)
        {
            return;
        }

		base.Update();

		int JustFinishedIndex = Mathf.Min((int)(HeaderDrawer.RoutineTime / 15f) - 1, GetNumScoreBlocks() - 1);
		if (JustFinishedIndex >= 0 && NSArray[JustFinishedIndex].NumberValue == 0)
		{
			if (RecordScoreIndex != JustFinishedIndex)
			{
				GetComponent<AudioSource>().clip = MarkAudioClips[JustFinishedIndex];
				GetComponent<AudioSource>().Play();
			}

			RecordScoreIndex = JustFinishedIndex;
		}
		else
			RecordScoreIndex = -1;

		if (bIsJudging && !GetComponent<AudioSource>().isPlaying && !bPlayedEndClip && RecordScoreIndex == GetNumScoreBlocks() - 1)
		{
			bPlayedEndClip = true;
			GetComponent<AudioSource>().clip = EndAudioClip;
			GetComponent<AudioSource>().Play();
		}

		foreach (NumberScroll ns in NSArray)
			ns.bAnimating = false;

		if (RecordScoreIndex != -1)
			NSArray[RecordScoreIndex].bAnimating = true;

		InputingConsecCooldown -= Time.deltaTime;
		if (!bInputingConsec && InputingConsecCooldown < 0)
		{
			MousePos = Input.mousePosition;
			MousePos.y = Screen.height - MousePos.y;
			if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && bInputingDiffScore))
			{
				if (NumberLineRect.Contains(MousePos) && MousePos.y > Screen.height * .78f || bInputingDiffScore)
				{
					bInputingDiffScore = true;

					CurrentDiffScore = Mathf.Clamp((MousePos.x - NumberLineRect.x - .0488f * NumberLineWidth) / (NumberLineWidth * .8984f), 0f, 1f) * 10f;
					CurrentDiffScore = Mathf.Round(CurrentDiffScore / DiffScoreSnap) * DiffScoreSnap;
				}
			}
			else
			{
				if (bInputingDiffScore)
				{
					if (RecordScoreIndex != -1)
						NSArray[RecordScoreIndex].NumberValue = CurrentDiffScore;

					RecordScoreConsecIndex = RecordScoreIndex;
					bInputingConsec = true;
				}

				bInputingDiffScore = false;
			}
		}

		for (int i = 0; i < GetNumScoreBlocks(); ++i)
		{
			NSArray[i].Update();

			if (CurData != null)
			{
				if (CurData.DiffScores[i] != NSArray[i].NumberValue && !NSArray[i].IsPicking)
				{
					CurData.DiffScores[i] = NSArray[i].NumberValue;

					BackupCurrentData();
				}
			}

			if (NSArray[i].bEditing)
			{
				bInputingConsec = true;
				RecordScoreConsecIndex = i;
			}
		}

        int NumBlocks = GetNumScoreBlocks();
        for (int i = 0; i < 20; ++i)
        {
            NumberBoxesList[i].SetActive(i < NumBlocks);
            LabelList[i].SetActive(i < NumBlocks);
        }

        if (CurData != null)
        {
            TotalScoreUI.GetComponent<Text>().text = CurData.GetTotalPoints().ToString("0.0");
        }
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
					BackupTextStyle.fontSize = 20;
					GUI.Label(new Rect(Screen.width * .07f, Screen.height * .11f, Screen.width * .86f, Screen.height * .3f),
						"Backup data for: " + GetBackupDisplayString(CurBackupData), BackupTextStyle);

					if (GUI.Button(new Rect(Screen.width * .07f, Screen.height * .25f, Screen.width * .3f, Screen.height * .1f), "Send Backup Data"))
					{
						SendResultsToHeadJudger((int)CurData.Division, (int)CurData.Round, (int)CurData.Pool, CurData.Team);

						bInputingConsec = false;
					}
					if (GUI.Button(new Rect(Screen.width * .42f, Screen.height * .25f, Screen.width * .51f, Screen.height * .1f), "Exit Backup mode and discard changes"))
					{
						bBackupLoaded = false;
						CurBackupData = null;
						bInputingConsec = false;
					}
				}
			}
			else
			{
				base.OnGUI();
			}

            if (!bIsDrawingEditingTeams)
            {
                if (bInputingConsec)
                {
                    NumberlineUI.SetActive(false);
                    NoneButtonUI.gameObject.SetActive(true);
                    PlusButtonUI.gameObject.SetActive(true);

                    GUIStyle ButtonStyle = new GUIStyle("button");
                    ButtonStyle.fontSize = 30;
                }
                else
                {
                    NumberlineUI.SetActive(true);
                    NoneButtonUI.gameObject.SetActive(false);
                    PlusButtonUI.gameObject.SetActive(false);
                }

                for (int i = 0; i < GetNumScoreBlocks(); ++i)
                {
                    string ConsecStr = " / ";
                    switch (ConsecScores[i])
                    {
                        case -1:
                            ConsecStr += "-";
                            break;
                        case 0:
                            ConsecStr += "None";
                            break;
                        case 1:
                            ConsecStr += "+";
                            break;
                    }
                    string BlockLabelString = (i + 1).ToString() + ConsecStr;
                    LabelList[i].GetComponent<Text>().text = BlockLabelString;

                    NSArray[i].DrawRect = Global.GetScreenSpaceRectTransform(NumberBoxesList[i]);
                    NSArray[i].Draw();
                }

                if (bInputingDiffScore)
                {
                    string ScoreString = CurrentDiffScore.ToString("0.0");
                    GUIStyle NewStyle = new GUIStyle("label");
                    NewStyle.fontSize = 50;
                    NewStyle.normal.textColor = Color.red;
                    Vector2 StrSize = NewStyle.CalcSize(new GUIContent(ScoreString));
                    GUI.Label(new Rect(Mathf.Clamp(MousePos.x - StrSize.x / 2f, 0, Screen.width - StrSize.x), MousePos.y - Screen.height * .15f, StrSize.x, StrSize.y), ScoreString, NewStyle);
                }
            }
		}
	}

    public void OnPlusButtonClick()
    {
        SetConsecScore(1);
    }

    public void OnNoneButtonClick()
    {
        SetConsecScore(0);
    }

	void SetConsecScore(int InScore)
	{
		bInputingConsec = false;
		InputingConsecCooldown = .1f;
        if (RecordScoreConsecIndex >= 0 && RecordScoreConsecIndex < ConsecScores.Length)
        {
            ConsecScores[RecordScoreConsecIndex] = InScore;

            if (CurData != null)
            {
                CurData.ConsecScores[RecordScoreConsecIndex] = InScore;
            }
        }

		BackupCurrentData();
	}

	int GetNumScoreBlocks()
	{
		return Mathf.RoundToInt(GetRoutineLengthMinutes() * 4f);
	}

	string GetBackupDisplayString(BackupDiffData bd)
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

		foreach (BackupDiffData bd in BackupList)
		{
			GUILayout.BeginHorizontal();
			string BackupStr = GetBackupDisplayString(bd) + " | ";
			for (int DiffIndex = 0; DiffIndex < bd.Data.NumScores; ++DiffIndex)
			{
				int ConsecScore = bd.Data.ConsecScores[DiffIndex];
				BackupStr += bd.Data.DiffScores[DiffIndex] + (ConsecScore == -1 ? "-" : (ConsecScore == 1 ? "+" : "")) + (DiffIndex == bd.Data.NumScores - 1 ? "" : ", ");
			}
			GUIStyle LabelStyle = new GUIStyle("label");
			GUIContent BackupContent = new GUIContent(BackupStr);
			GUILayout.Label(BackupContent, GUILayout.MaxWidth(LabelStyle.CalcSize(BackupContent).x + 20));
			if (GUILayout.Button("Load"))
			{
				bIsChoosingBackup = false;
				//bBackupLoaded = true;
				bInputingConsec = false;
				CurData = bd.Data;
				//CurBackupData = bd;

				HeaderDrawer.CanvasGO.SetActive(true);
				JudgerCanvasUI.SetActive(true);

				for (int ScoreIndex = 0; ScoreIndex < bd.Data.NumScores; ++ScoreIndex)
				{
					NSArray[ScoreIndex].NumberValue = bd.Data.DiffScores[ScoreIndex];
					ConsecScores[ScoreIndex] = bd.Data.ConsecScores[ScoreIndex];
				}
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	public override void StartRoutineJudging()
	{
		base.StartRoutineJudging();

		bPlayedEndClip = false;
	}

	public override void SendResultsToHeadJudger(int InDiv, int InRound, int InPool, int InTeam)
	{
		base.SendResultsToHeadJudger(InDiv, InRound, InPool, InTeam);

		RoutineScoreData SData = Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[(int)CurPool].Teams[InTeam].Data.RoutineScores;
		CurData.Division = (EDivision)InDiv;
		CurData.Round = (ERound)InRound;
		CurData.Pool = (EPool)InPool;
		CurData.Team = InTeam;
		CurData.JudgeNameId = GetJudgeNameId();
		SData.SetDiffResults(CurData);

		if (Networking.IsConnectedToServer)
			Global.NetObj.ClientSendFinishJudgingDiff(CurData.SerializeToString());
		else
		{
			CachedData = new DiffData(CurData);
			Networking.bNeedSendCachedResults = true;
		}
	}

	public override void SendCachedResultsToHeadJudger()
	{
		base.SendCachedResultsToHeadJudger();

		if (Networking.IsConnectedToServer)
		{
			Global.NetObj.ClientSendFinishJudgingDiff(CachedData.SerializeToString());
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

			foreach (DiffData dd in SData.DiffResults)
			{
				if (dd.JudgeNameId == GetJudgeNameId())
				{
					CurData = dd;
					break;
				}
			}

			if (CurData == null)
				CurData = new DiffData(GetNumScoreBlocks(), CurDivision, CurRound, CurPool, InTeamIndex);

			for (int i = 0; i < GetNumScoreBlocks(); ++i)
			{
				NSArray[i].NumberValue = CurData.DiffScores[i];
				ConsecScores[i] = CurData.ConsecScores[i];
			}
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

				StreamWriter file = new StreamWriter(BackupPath + "/DiffBackup-" + DateTime.Now.Ticks + ".xml");
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
			if (filename.Contains("DiffBackup"))
			{
				BackupDiffData backup = new BackupDiffData();
				try
				{
					FileStream BackupFile = new FileStream(filename, FileMode.Open);
					backup.Data = DiffData.Load(BackupFile);
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
			delegate(BackupDiffData b1, BackupDiffData b2)
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
			CurData = DiffData.Load(File);
			File.Close();
		}
		catch (System.Exception e)
		{
			Debug.Log("Load autosave exception: " + e.Message);
		}
	}

	public override void ResetScoreData()
	{
		CurData = new DiffData(GetNumScoreBlocks(), CurDivision, CurRound, CurPool, CurTeam);

		foreach (NumberScroll ns in NSArray)
			ns.NumberValue = 0;
		for (int ScoreIndex = 0; ScoreIndex < ConsecScores.Length; ++ScoreIndex)
			ConsecScores[ScoreIndex] = 0;
	}

    public override void OnEditTeamsButtonClick()
    {
        base.OnEditTeamsButtonClick();

        
    }

    public override void LockForJudging()
    {
        base.LockForJudging();

        RoundData Round = Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound];

        string SpeechText = "Judging ready for " + GetTeamNameString() + ", " + CurDivision.ToString() + ", " +
            CurRound.ToString() + ", " + ((int)Round.RoutineLengthMinutes).ToString() + " minutes.";

        Global.PlayTextToSpeech(SpeechText);
    }
}

public class BackupDiffData
{
	public DiffData Data = null;
	public DateTime WrittenTime;
	public string Filename = null;
}

public class DiffData
{
	public int JudgeNameId = -1;
	public EDivision Division;
	public ERound Round;
	public EPool Pool;
	public int Team;
	public int NumScores = 16;
	public float[] DiffScores = new float[20];
	public int[] ConsecScores = new int[20];

	public DiffData()
	{
	}

	public DiffData(int InNumScores, EDivision InDiv, ERound InRound, EPool InPool, int InTeam)
	{
		NumScores = InNumScores;
		Division = InDiv;
		Round = InRound;
		Pool = InPool;
		Team = InTeam;
	}

	public DiffData(DiffData InData)
	{
		JudgeNameId = InData.JudgeNameId;
		Division = InData.Division;
		Round = InData.Round;
		Pool = InData.Pool;
		Team = InData.Team;
		NumScores = InData.NumScores;
		for (int i = 0; i < InData.DiffScores.Length; ++i)
		{
			DiffScores[i] = InData.DiffScores[i];
			ConsecScores[i] = InData.ConsecScores[i];
		}
	}

	public string SerializeToString()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(DiffData));
		MemoryStream stream = new MemoryStream();
		serializer.Serialize(stream, this);
		stream.Position = 0;
		StreamReader Reader = new StreamReader(stream);
		string WholeString = Reader.ReadToEnd();
		Reader.Close();

		stream.Close();

		return WholeString;
	}

	public static DiffData Load(Stream InStream)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(DiffData));
		DiffData LoadedData = serializer.Deserialize(InStream) as DiffData;

		return LoadedData;
	}

	public float GetTotalPoints()
	{
		float RetScore = 0;
		float LowestScore = 10f;
		for (int i = 0; i < NumScores; ++i)
		{
			float Score = DiffScores[i];
			LowestScore = Mathf.Min(Score, LowestScore);
			RetScore += ConsecScores[i];

			RetScore += Score;
		}

		RetScore -= LowestScore;
		RetScore /= Math.Max(1, NumScores - 1);
		RetScore *= 1.5f;

		return RetScore;
	}

	public string GetConsecString(int InIndex)
	{
		int ConsecScore = ConsecScores[InIndex];
		return ConsecScore == -1 ? "-" : (ConsecScore == 1 ? "+" : "");
	}

	public bool IsValid()
	{
		return JudgeNameId != -1;
	}
}
