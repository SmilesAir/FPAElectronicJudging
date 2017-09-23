using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using UnityEngine.Networking;

public class Global : MonoBehaviour
{
	public GameObject BracketBuilder;
	public GameObject ExJudger;
	public GameObject AIJudger;
	public GameObject DiffJudger;
	public GameObject JudgeHelper;
	public GameObject HeadJudgerGo;
	public GameObject NetworkObj;
	public GameObject BracketViewer;
	public GameObject OverlayGo;
	public static Networking NetObj;
	public Texture2D NumberTexture;
	public Texture2D NumberBorderTexture;
	public Texture2D NumberBorderGreenTexture;
	public Texture2D NumberLineTexture;
	public static Texture2D NumberTex;
	public static Texture2D NumberBorderTex;
	public static Texture2D NumberBorderGreenTex;
	public static Texture2D NumberLineTex;
	public static Global Obj;
	public static TournamentData AllData = null;
	public static NameDatabase AllNameData = null;
    static WindowsVoice VoiceObj = null;
	public static int MaxBackupFileCount = 40;
	public static AudioSource GlobalAudioSource = null;

	public static EDivision CurrentDivision = EDivision.Open;

	public static EInterface LastActiveInterface = EInterface.Startup;
	public static EInterface ActiveInterface = EInterface.Startup;
	public static bool bDrawAllInterfaceButtons = false; // Debug

	public static int DivDataState = -1;
	public static int CurDataState = -1;
	int FastFramesCount = 0;

	// http://localhost:9000
	public static string LivestreamCompUrl = "http://192.168.8.101:9000";

	// Use this for initialization
	void Start()
	{
		Debug.Log(Application.persistentDataPath);

		NumberTex = NumberTexture;
		NumberBorderTex = NumberBorderTexture;
		NumberBorderGreenTex = NumberBorderGreenTexture;
		NumberLineTex = NumberLineTexture;

		NetObj = NetworkObj.GetComponent<Networking>();

		Obj = GetComponent<Global>();

        VoiceObj = GetComponent<WindowsVoice>();

		GlobalAudioSource = GetComponent<AudioSource>();

		QualitySettings.vSyncCount = 0;
	}

	// Update is called once per frame
	void Update()
	{
		--FastFramesCount;

		if (Input.GetMouseButton(0) || Input.touchCount > 0)
		{
			Application.targetFrameRate = 20;

			FastFramesCount = 100;
		}
		else if (FastFramesCount > 0)
		{
			Application.targetFrameRate = 20;
		}
		else
		{
			Application.targetFrameRate = 7;
		}

		if (LastActiveInterface != ActiveInterface)
		{
			BracketBuilder.SetActive(false);
			DiffJudger.SetActive(false);
			AIJudger.SetActive(false);
			ExJudger.SetActive(false);
			JudgeHelper.SetActive(false);
			HeadJudgerGo.SetActive(false);
			BracketViewer.SetActive(false);

			LastActiveInterface = ActiveInterface;
		}

		switch (ActiveInterface)
		{
			case EInterface.BracketBuilder:
				BracketBuilder.SetActive(true);
				break;
			case EInterface.DiffJudger:
				DiffJudger.SetActive(true);
				break;
			case EInterface.AIJudger:
				AIJudger.SetActive(true);
				break;
			case EInterface.ExeJudger:
				ExJudger.SetActive(true);
				break;
			case EInterface.HeadJudger:
				HeadJudgerGo.SetActive(true);
				break;
			case EInterface.JudgeHelper:
				JudgeHelper.SetActive(true);
				break;
			case EInterface.BracketViewer:
				BracketViewer.SetActive(true);
				break;
			case EInterface.Overlay:
				OverlayGo.SetActive(true);
				break;
		}
	}

	void OnGUI()
	{
		if (bDrawAllInterfaceButtons && ActiveInterface != EInterface.Overlay)
		{
			float PaddingWidth = 10f;
			float ButtonWidth = (Screen.width - 20 - 7 * PaddingWidth) / 8;
			float StartX = 10;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Brack Builder"))
				ActiveInterface = EInterface.BracketBuilder;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Diff Judger"))
				ActiveInterface = EInterface.DiffJudger;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "AI Judger"))
				ActiveInterface = EInterface.AIJudger;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Exe Judger"))
				ActiveInterface = EInterface.ExeJudger;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Head Judger"))
				ActiveInterface = EInterface.HeadJudger;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Judger Picker"))
				ActiveInterface = EInterface.JudgeHelper;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Bracket Viewer"))
				ActiveInterface = EInterface.BracketViewer;
			StartX += ButtonWidth + PaddingWidth;
			if (GUI.Button(new Rect(StartX, 0, ButtonWidth, 20), "Overlay"))
				ActiveInterface = EInterface.Overlay;
		}
		else if (ActiveInterface == EInterface.Startup)
		{
			GUILayout.BeginArea(new Rect(30, 30, Screen.width - 60, Screen.height - 60));
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Diff Judger", GUILayout.Height(Screen.height - 60)))
				ActiveInterface = EInterface.DiffJudger;
			if (GUILayout.Button("AI Judger", GUILayout.Height(Screen.height - 60)))
				ActiveInterface = EInterface.AIJudger;
			if (GUILayout.Button("Exe Judger", GUILayout.Height(Screen.height - 60)))
				ActiveInterface = EInterface.ExeJudger;
			if (GUILayout.Button("All Interfaces (Head Judge)\nVersion: 2.01", GUILayout.Height(Screen.height - 60)))
				bDrawAllInterfaceButtons = true;


			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}

    public static JudgerBase GetActiveJudger()
    {
        if (Obj != null)
        {
            if (Obj.AIJudger.activeSelf)
                return Obj.AIJudger.GetComponent<ArtisticImpressionJudger>();
            else if (Obj.ExJudger.activeSelf)
                return Obj.ExJudger.GetComponent<ExecutionJudger>();
            else if (Obj.DiffJudger.activeSelf)
                return Obj.DiffJudger.GetComponent<DifficultyJudger>();
        }

        return null;
    }

	public static InterfaceBase GetActiveInterface()
	{
		if (Obj != null)
		{
			if (Obj.AIJudger.activeSelf)
				return Obj.AIJudger.GetComponent<ArtisticImpressionJudger>();
			else if (Obj.ExJudger.activeSelf)
				return Obj.ExJudger.GetComponent<ExecutionJudger>();
			else if (Obj.DiffJudger.activeSelf)
				return Obj.DiffJudger.GetComponent<DifficultyJudger>();
            else if (Obj.OverlayGo.activeSelf)
                return Obj.OverlayGo.GetComponent<Overlay>();
		}

		return null;
	}

	public static ECategory GetActiveJudgerType()
	{
		if (Obj != null)
		{
			if (Obj.AIJudger.activeSelf)
				return ECategory.AI;
			else if (Obj.ExJudger.activeSelf)
				return ECategory.Ex;
			else if (Obj.DiffJudger.activeSelf)
				return ECategory.Diff;
		}

		return ECategory.AI;
	}

	public static bool IsHeadJudger()
	{
		return Global.Obj.HeadJudgerGo.activeSelf;
	}

	public static HeadJudger GetHeadJudger()
	{
		return Global.Obj.HeadJudgerGo.GetComponent<HeadJudger>();
	}

	public static void LoadTournamentData()
	{
		if (Global.AllData == null)
		{
			Global.AllData = TournamentData.Load(Application.persistentDataPath + "/save.xml");
			if (Global.AllData == null)
				Global.AllData = new TournamentData();
		}

		LoadNameData();
	}

	public static void LoadNameData()
	{
		if (Global.AllNameData == null)
		{
			Global.AllNameData = NameDatabase.Load(Application.persistentDataPath + "/names.xml");
			if (Global.AllNameData == null)
				Global.AllNameData = new NameDatabase();
		}
	}

	public static Stream GenerateStreamFromString(string s)
	{
		MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.GetEncoding("iso-8859-1"));
		writer.Write(s);
		writer.Flush();
		stream.Position = 0;
		return stream;
	}

	public static bool DataExists(EDivision InDiv, ERound InRound, int InPool, int InTeam)
	{
		if (Global.AllData == null || InPool < 0 || InPool >= Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools.Count ||
			InTeam < 0 || InTeam >= Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools[InPool].Teams.Count)
			return false;

		return true;
	}

	public static TeamData GetTeamData(EDivision InDiv, ERound InRound, int InPool, int InTeam)
	{
		if (!DataExists(InDiv, InRound, InPool, InTeam))
			return null;

		return Global.AllData.AllDivisions[(int)InDiv].Rounds[(int)InRound].Pools[InPool].Teams[InTeam].Data;
	}

    public static Rect GetScreenSpaceRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        Vector3 scale = rt.localScale;
        return new Rect(rt.rect.xMin * scale.x + rt.position.x, rt.rect.yMin * scale.y + Screen.height - rt.position.y,
            rt.rect.width * scale.x, rt.rect.height * scale.y);
    }

    public static ECategoryView CategoryToCategoryView(ECategory InCategory)
    {
        switch (InCategory)
        {
            case ECategory.AI:
                return ECategoryView.AI;
            case ECategory.Diff:
                return ECategoryView.Diff;
            case ECategory.Ex:
                return ECategoryView.Ex;
            default:
                Debug.LogError("Trying to covert invalid category");
                return ECategoryView.AI;
        }
    }

    public static bool IsValid(EDivision InDivision, ERound InRound, int InPool, int InTeam)
    {
        if ((int)InDivision >= 0 && (int)InRound >= 0 && InPool >= 0 && InTeam >= 0 &&
            (int)InDivision < Global.AllData.AllDivisions.Length &&
            (int)InRound < Global.AllData.AllDivisions[(int)InDivision].Rounds.Length &&
            InPool < Global.AllData.AllDivisions[(int)InDivision].Rounds[(int)InRound].Pools.Count &&
            InTeam < Global.AllData.AllDivisions[(int)InDivision].Rounds[(int)InRound].Pools[InPool].Teams.Count)
        {
            return true;
        }

        return false;
    }

    public static void PlayTextToSpeech(string text)
    {
        VoiceObj.speak(text);
    }

	public static IEnumerator SendRestMessage(LiveStream.Team team)
	{
		// http://localhost:9000
		using (UnityWebRequest www = CreateUnityWebRequest(LivestreamCompUrl + "/api/teams", team))
		{
			yield return www.Send();

			if (www.isError)
			{
				Debug.Log("Rest Api: " + www.error);
			}
			else
			{
				Debug.Log("Rest Api: " + www.downloadHandler.text);
			}
		}
	}

	public static IEnumerator SendRestMessage(LiveStream.TeamList teamList)
	{
		using (UnityWebRequest www = CreateUnityWebRequest(LivestreamCompUrl + "/api/teamlist", teamList))
		{
			yield return www.Send();

			if (www.isError)
			{
				Debug.Log(www.error);
			}
			else
			{
				Debug.Log(www.downloadHandler.text);
			}
		}
	}
	public static UnityWebRequest CreateUnityWebRequest<Type>(string url, Type team)
	{
		return CreateUnityWebRequest(url, JsonUtility.ToJson(team));
	}

	public static UnityWebRequest CreateUnityWebRequest(string url, string param)
	{
		UnityWebRequest requestU = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
		byte[] bytes = GetBytes(param);
		UploadHandlerRaw uH = new UploadHandlerRaw(bytes);
		uH.contentType = "application/json";
		requestU.uploadHandler = uH;
		requestU.SetRequestHeader("Content-Type", "application/json");
		CastleDownloadHandler dH = new CastleDownloadHandler();
		requestU.downloadHandler = dH; //need a download handler so that I can read response data
		return requestU;
	}

	protected static byte[] GetBytes(string str)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		return bytes;
	}
}

public enum EInterface
{
	Startup,
	BracketBuilder,
	DiffJudger,
	AIJudger,
	ExeJudger,
	HeadJudger,
	JudgeHelper,
	BracketViewer,
	Overlay
}

public enum EDivision
{
	Open,
	Mixed,
	Coop,
	Women
}

public enum ERound
{
	Finals,
	Semifinals,
	Quaterfinals,
	Prelims
}

public enum EPool
{
	A,
	B,
	C,
	D
}

public class RoutineScoreData
{
	public EDivision Division;
	public ERound Round;

	public List<ExData> ExResults = new List<ExData>();
	public List<AIData> AIResults = new List<AIData>();
	public List<DiffData> DiffResults = new List<DiffData>();

	public string GetResultsString(int InResultIndex, ECategoryView InCat, bool bInIncludeName)
	{
		switch (InCat)
		{
			case ECategoryView.AI:
				if (InResultIndex < AIResults.Count)
                    return System.String.Format((bInIncludeName ? "{6}:  " : "") + "V: {0:0.0}  T: {1:0.0}  M: {2:0.0}  Fw: {3:0.0}  Fm: {4:0.0}  G: {5:0.0}  Total: {7:0.00}",
						AIResults[InResultIndex].Variety, AIResults[InResultIndex].Teamwork, AIResults[InResultIndex].Music, AIResults[InResultIndex].Flow,
                        AIResults[InResultIndex].Form, AIResults[InResultIndex].General,
                        NameDatabase.FindInDatabase(AIResults[InResultIndex].JudgeNameId).DisplayName, AIResults[InResultIndex].GetTotalPoints());
				break;
			case ECategoryView.Ex:
				if (InResultIndex < ExResults.Count)
					return System.String.Format((bInIncludeName ? "{4}:  " : "") + ".1: {0}  .2: {1}  .3: {2}  .5: {3}  Total: {5:0.00}", ExResults[InResultIndex].Point1Count, ExResults[InResultIndex].Point2Count,
						ExResults[InResultIndex].Point3Count, ExResults[InResultIndex].Point5Count,
						NameDatabase.FindInDatabase(ExResults[InResultIndex].JudgeNameId).DisplayName, ExResults[InResultIndex].GetTotalPoints());
				break;
			case ECategoryView.Diff:
				if (InResultIndex < DiffResults.Count)
				{
					string Ret = bInIncludeName ? NameDatabase.FindInDatabase(DiffResults[InResultIndex].JudgeNameId).DisplayName + ":  " : "";
					for (int i = 0; i < DiffResults[InResultIndex].NumScores; ++i)
					{
						Ret += DiffResults[InResultIndex].DiffScores[i].ToString("0.0") + DiffResults[InResultIndex].GetConsecString(i) +
							((i < DiffResults[InResultIndex].NumScores - 1) ? ", " : "");
					}
					Ret += "  Total: " + DiffResults[InResultIndex].GetTotalPoints().ToString("0.00");
					return Ret;
				}
				break;
			case ECategoryView.Overview:
				{
					string Ret = "";
					float AIPoints = 0;
					foreach (AIData aid in AIResults)
					{
						AIPoints += aid.GetTotalPoints();
						Ret += "  AI: " + aid.GetTotalPoints().ToString("0.00");
					}
					float ExPoints = 0;
					foreach (ExData exd in ExResults)
					{
						ExPoints += exd.GetTotalPoints();
						Ret += "  Ex: " + exd.GetTotalPoints().ToString("0.00");
					}
					float DiffPoints = 0;
					foreach (DiffData dd in DiffResults)
					{
						DiffPoints += dd.GetTotalPoints();
						Ret += "  Diff: " + dd.GetTotalPoints().ToString("0.00");
					}
					float TotalPoints = AIPoints + ExPoints + DiffPoints;
					if (TotalPoints > 0)
						Ret += "    Total: " + TotalPoints.ToString("0.00");
					return Ret;
				}
		}

		return "";
	}

	public bool SetDiffResults(DiffData InData)
	{
		if (InData.JudgeNameId == -1)
			return false;

		ResultsData rd = TournamentData.FindResultsData(InData.Division, InData.Round, InData.Pool);
		int ResultIndex = -1;
		for (int i = 0; i < rd.DiffJudgeIds.Count; ++i)
		{
			if (InData.JudgeNameId == rd.DiffJudgeIds[i])
			{
				ResultIndex = i;
				break;
			}
		}

		bool bNewScore = false;
		if (ResultIndex >= 0)
		{
			for (int DataIndex = 0; DataIndex <= ResultIndex; ++DataIndex)
			{
				if (DataIndex >= DiffResults.Count)
					DiffResults.Add(new DiffData());
			}

			if (!DiffResults[ResultIndex].IsValid())
			{
				bNewScore = true;
			}

			DiffResults[ResultIndex] = InData;
		}

		return bNewScore;
	}

	public bool SetExResults(ExData InData)
	{
		if (InData.JudgeNameId == -1)
			return false;

		ResultsData rd = TournamentData.FindResultsData(InData.Division, InData.Round, InData.Pool);
		int ResultIndex = -1;
		for (int i = 0; i < rd.ExJudgeIds.Count; ++i)
		{
			if (InData.JudgeNameId == rd.ExJudgeIds[i])
			{
				ResultIndex = i;
				break;
			}
		}

		bool bNewScore = false;
		if (ResultIndex >= 0)
		{
			for (int DataIndex = 0; DataIndex <= ResultIndex; ++DataIndex)
			{
				if (DataIndex >= ExResults.Count)
					ExResults.Add(new ExData());
			}

			if (!ExResults[ResultIndex].IsValid())
			{
				bNewScore = true;
			}

			ExResults[ResultIndex] = InData;
		}

		return bNewScore;
	}

	public bool SetAIResults(AIData InData)
	{
		if (InData.JudgeNameId == -1)
			return false;

		ResultsData rd = TournamentData.FindResultsData(InData.Division, InData.Round, InData.Pool);
		int ResultIndex = -1;
		for (int i = 0; i < rd.AIJudgeIds.Count; ++i)
		{
			if (InData.JudgeNameId == rd.AIJudgeIds[i])
			{
				ResultIndex = i;
				break;
			}
		}

		bool bNewScore = false;
		if (ResultIndex >= 0)
		{
			for (int DataIndex = 0; DataIndex <= ResultIndex; ++DataIndex)
			{
				if (DataIndex >= AIResults.Count)
					AIResults.Add(new AIData());
			}

			if (!AIResults[ResultIndex].IsValid())
			{
				bNewScore = true;
			}

			AIResults[ResultIndex] = InData;
		}

		return bNewScore;
	}

	public float GetTotalPoints()
	{
		return GetExPoints() + GetAIPoints() + GetDiffPoints();
	}

	public float GetExPoints()
	{
		float TotalPoints = 0;

		foreach (ExData ed in ExResults)
			TotalPoints += ed.GetTotalPoints();

		return TotalPoints;
	}

	public float GetAIPoints()
	{
		float TotalPoints = 0;

		foreach (AIData ad in AIResults)
			TotalPoints += ad.GetTotalPoints();

		return TotalPoints;
	}

	public float GetDiffPoints()
	{
		float TotalPoints = 0;

		foreach (DiffData dd in DiffResults)
			TotalPoints += dd.GetTotalPoints();

		return TotalPoints;
	}

	public bool ContainsJudgeScores()
	{
		return ExResults.Count > 0 || AIResults.Count > 0 || DiffResults.Count > 0;
	}

	public int GetTotalValidScores()
	{
		int Count = 0;
		foreach (DiffData dd in DiffResults)
		{
			if (dd.IsValid())
			{
				++Count;
			}
		}
		foreach (ExData ed in ExResults)
		{
			if (ed.IsValid())
			{
				++Count;
			}
		}
		foreach (AIData ad in AIResults)
		{
			if (ad.IsValid())
			{
				++Count;
			}
		}

		return Count;
	}
}

[XmlRoot("NameDatabase")]
public class NameDatabase
{
	[XmlArray("AllNames")]
	[XmlArrayItem("NameData")]
	public List<NameData> AllNames { get; set; }

	[XmlArray("AllJudges")]
	[XmlArrayItem("NameData")]
	public List<NameData> AllJudges { get; set; }

	public NameDatabase()
	{
		AllNames = new List<NameData>();
		AllJudges = new List<NameData>();
	}

	public static NameData FindInDatabase(string InFirstName, string InLastName)
	{
		return FindInDatabase(new NameData(InFirstName, InLastName));
	}

	public static NameData FindInDatabase(NameData InName)
	{
		if (Global.AllNameData == null)
			return new NameData("No", "Name");

		foreach (NameData nd in Global.AllNameData.AllNames)
		{
			if (nd == InName)
			{
				return nd;
			}
		}

		return null;
	}

	public static NameData FindInDatabase(int InId)
	{
		if (Global.AllNameData == null || InId < 0)
			return new NameData("No", "Name");

		foreach (NameData nd in Global.AllNameData.AllNames)
		{
			if (nd.Id == InId)
				return nd;
		}

		return null;
	}

	public string Save()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(NameDatabase));
		try
		{
			StreamWriter stream = new StreamWriter(Application.persistentDataPath + "/names.xml");
			serializer.Serialize(stream, this);
			stream.Close();
		}
		catch (System.Exception e)
		{
			Debug.LogWarning(e);
		}

		return "Save Name Data";
	}

	public string SerializeToString()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(NameDatabase));
		MemoryStream stream = new MemoryStream();
		serializer.Serialize(stream, this);
		stream.Position = 0;
        StreamReader Reader = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"));
		string WholeString = Reader.ReadToEnd();
		Reader.Close();

		stream.Close();

		return WholeString;
	}

	public static NameDatabase Load(string Filename)
	{
        if (!File.Exists(Filename))
        {
            return null;
        }

		NameDatabase LoadedData = null;

		try
		{
			XmlSerializer serializer = new XmlSerializer(typeof(NameDatabase));
			FileStream stream = new FileStream(Filename, FileMode.Open);
			LoadedData = serializer.Deserialize(stream) as NameDatabase;
			stream.Close();
		}
		catch (IOException e)
		{
			Debug.Log("Loading exception: " + e.Message);
		}

		return LoadedData;
	}

	public static NameDatabase Load(Stream InStream)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(NameDatabase));
		NameDatabase LoadedData = serializer.Deserialize(InStream) as NameDatabase;

		return LoadedData;
	}

	public static NameData TryAddNewName(string InFirstName, string InLastName)
	{
		NameData NewName = NameDatabase.FindInDatabase(InFirstName, InLastName);
		if (NewName == null)
		{
			NewName = new NameData(InFirstName, InLastName);
			Global.AllNameData.AllNames.Add(NewName);

			Global.AllNameData.Save();
		}

		return NewName;
	}

	public static List<MatchData> GetCloseNames(string InFirstName, string InLastName)
	{
		List<MatchData> Ret = new List<MatchData>();
		if (Global.AllNameData != null)
		{
			foreach (NameData nd in Global.AllNameData.AllNames)
			{
				float FirstMatch = 0;
				float LastMatch = 0;
				foreach (string fa in nd.FirstAliases)
				{
					FirstMatch = Mathf.Max(FirstMatch, IsCloseMatch(fa, InFirstName));
				}

				foreach (string la in nd.LastAliases)
				{
					LastMatch = Mathf.Max(LastMatch, IsCloseMatch(la, InLastName));
				}

				if (FirstMatch + LastMatch > 0)
					Ret.Add(new MatchData(nd, FirstMatch + LastMatch));
			}
		}

		Ret.Sort(MatchSorter);
		if (Ret.Count > 5)
		{
			float HighestMatch = Ret[0].Match;
			for (int i = 0; i < Ret.Count; ++i)
			{
				if (HighestMatch - Ret[i].Match > .25f)
				{
					Ret.RemoveAt(i);
					--i;
				}
			}
		}

		return Ret;
	}

	public static int MatchSorter(MatchData m1, MatchData m2)
	{
		if (m1.Match == m2.Match)
			return 0;
		else if (m1.Match < m2.Match)
			return 1;
		else
			return -1;
	}

	public static float IsCloseMatch(string In1, string In2)
	{
		In1 = In1.ToLower();
		In2 = In2.ToLower();

		int LongestStreak = 0;
		for (int Char1Index = 0; Char1Index < In1.Length; ++Char1Index)
		{
			int CurStreak = 0;
			char c1 = In1[Char1Index];

			foreach (char c2 in In2)
			{
				if (c1 == c2)
				{
					++CurStreak;

					if (Char1Index + CurStreak >= In1.Length - 1)
						break;

					c1 = In1[Char1Index + CurStreak];
				}
				else if (CurStreak > 0)
					break;
			}

			LongestStreak = Mathf.Max(LongestStreak, CurStreak);
		}

		return ((float)LongestStreak);
	}
}

public class NameData
{
	public int Id = -1;
	public string FirstName;
	public string LastName;
	public List<string> FirstAliases = new List<string>();
	public List<string> LastAliases = new List<string>();

	public string DisplayName { get { return FirstName + " " + LastName; } }

	public static int GetUniqueId()
	{
		if (Global.AllNameData == null)
			return -1;

		int LargestId = -1;
		foreach (NameData nd in Global.AllNameData.AllNames)
			LargestId = Mathf.Max(LargestId, nd.Id);

		return ++LargestId;
	}

	public NameData()
	{
		FirstName = "None";
		LastName = "None";

		Id = NameData.GetUniqueId();
	}

	public NameData(string InFirstName, string InLastName)
	{
		FirstName = InFirstName;
		if (FirstName.Length > 0)
		{
			StringBuilder FormattedFirstName = new StringBuilder();
			FormattedFirstName.Append(FirstName.Substring(0, 1).ToUpper());
			FormattedFirstName.Append(FirstName.Substring(1).ToLower());
			FirstName = FormattedFirstName.ToString();
		}
		LastName = InLastName;
		if (LastName.Length > 0)
		{
			StringBuilder FormattedLastName = new StringBuilder();
			FormattedLastName.Append(LastName.Substring(0, 1).ToUpper());
			FormattedLastName.Append(LastName.Substring(1).ToLower());
			LastName = FormattedLastName.ToString();
		}

		FirstAliases.Add(InFirstName);
		LastAliases.Add(InLastName);

		Id = NameData.GetUniqueId();
	}

	public bool AddFirstAlias(string InFirstName)
	{
		if (!FirstAliases.Contains(InFirstName))
		{
			if (InFirstName.Length > 0)
			{
				char[] NameArray = InFirstName.ToCharArray();
				NameArray[0] = InFirstName.ToUpper()[0];
				InFirstName = NameArray.ToString();
			}
			FirstAliases.Add(InFirstName);

			return true;
		}
		else
			return false;
	}

	public bool AddLastAlias(string InLastName)
	{
		if (!LastAliases.Contains(InLastName))
		{
			if (InLastName.Length > 0)
			{
				char[] NameArray = InLastName.ToCharArray();
				NameArray[0] = InLastName.ToUpper()[0];
				InLastName = NameArray.ToString();
			}

			LastAliases.Add(InLastName);

			return true;
		}
		else
			return false;
	}

	public static bool operator ==(NameData a, NameData b)
	{
		if (System.Object.ReferenceEquals(a, b))
			return true;

		if (((System.Object)a) == null || ((System.Object)b) == null)
			return false;

		if (a.DisplayName.ToLower() == b.DisplayName.ToLower())
			return true;
		for (int aIndex = 0; aIndex < a.FirstAliases.Count; ++aIndex)
		{
			string aName = a.FirstAliases[aIndex] + " " + a.LastAliases[aIndex];
			for (int bIndex = 0; bIndex < b.FirstAliases.Count; ++bIndex)
			{
				string bName = b.FirstAliases[bIndex] + " " + b.LastAliases[bIndex];
				if (aName.ToLower() == bName.ToLower())
				{
					return true;
				}
			}
		}

		return false;
	}

	public static bool operator !=(NameData a, NameData b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public bool IsInLine(string InLine)
	{
		string InLineLower = InLine.ToLower();
		foreach (string n in FirstAliases)
		{
            int FirstNameIndex = InLineLower.IndexOf(n.ToLower());
            if (FirstNameIndex != -1)
			{
                // Hack for now: Assume '-' is the name deliminator
                int NextDeliminatorIndex = InLineLower.IndexOf('-', FirstNameIndex);
                foreach (string ln in LastAliases)
			    {
                    int NextLastNameIndex = InLineLower.IndexOf(ln.ToLower(), FirstNameIndex);
                    if (NextLastNameIndex != -1 &&
                        (NextDeliminatorIndex == -1 || NextLastNameIndex < NextDeliminatorIndex))
                    {
                        return true;
                    }
                }
			}
		}

		return false;
	}

	public string GetAutoComplete(string InPrefix)
	{
		string TrimPrefix = InPrefix.TrimStart();
		string SavedTrim = InPrefix.Substring(0, InPrefix.Length - TrimPrefix.Length);

		if (TrimPrefix.Length == 0)
			return "";

		bool bMatchFirst = false;
		string FirstPrefix = "";
		foreach (string s in FirstAliases)
		{
			if (s.ToLower().StartsWith(TrimPrefix.ToLower()))
			{
				return SavedTrim + s + " " + LastName;
			}
			else if (TrimPrefix.StartsWith(s + " "))
			{
				FirstPrefix = s + " ";
				bMatchFirst = true;
				break;
			}
		}

		if (bMatchFirst)
		{
			string LastPrefix = TrimPrefix.Replace(FirstPrefix, "");

			foreach (string s in LastAliases)
			{
				if (s.ToLower().StartsWith(LastPrefix.ToLower()))
				{
					return SavedTrim + FirstPrefix + s;
				}
			}
		}

		return "";
	}
}
