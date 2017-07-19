using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

public class Overlay : InterfaceBase
{
	public Texture2D BgTexture;
    public Texture2D ScoreboardTexture;
    public Texture2D ScoreboardSimpleTexture;
    public enum EOverlayMode
    {
        Routine,
        Scoreboard,
        Max
    }
    EOverlayMode OverlayMode = EOverlayMode.Scoreboard;
    public bool bRoutineStarted = false;
    public DateTime RoutineStartTime;
    public DateTime RoutineFinishTime;
    public enum EOverlayAnimState
    {
        In,
        Out,
        Hold,
        Max
    }
    public EOverlayAnimState OverlayAnimState = EOverlayAnimState.In;
    float OverlayAnimInterp = 0f;
    DateTime StartAnimTime;

	// Use this for initialization
	void Start()
	{
		Global.NetObj.InitUdpListener(false);

        RoutineStartTime = DateTime.Now;
        RoutineFinishTime = DateTime.Now;
	}

	// Update is called once per frame
	void Update()
	{
		Global.NetObj.UpdateUdpListener();

		Global.NetObj.UpdateClientJudgeState();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OverlayMode = OverlayMode == EOverlayMode.Routine ? EOverlayMode.Scoreboard : EOverlayMode.Routine;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StartAnimTime = DateTime.Now;
            OverlayAnimInterp = 0f;
            OverlayAnimState = EOverlayAnimState.In;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OverlayAnimInterp = 0f;
            OverlayAnimState = EOverlayAnimState.Out;
            StartAnimTime = DateTime.Now;
        }

        if (bRoutineStarted && (DateTime.Now - RoutineStartTime).TotalMinutes > GetRoutineLengthMinutes())
        {
            StopRoutineJudging();
        }

        const float AnimateTime = 2f;
        if (OverlayAnimState == EOverlayAnimState.Out && DateTime.Now > StartAnimTime)
        {
            double time = (DateTime.Now - StartAnimTime).TotalSeconds;
            OverlayAnimInterp = Math.Min((float)(time / AnimateTime), 1f);
        }

        if (OverlayAnimState == EOverlayAnimState.In && DateTime.Now > StartAnimTime)
        {
            double SecondsAfterRoutine = (DateTime.Now - StartAnimTime).TotalSeconds;
            OverlayAnimInterp = Math.Max(0f, (float)(SecondsAfterRoutine / AnimateTime));
        }
	}

    public override void StartRoutineJudging()
    {
        base.StartRoutineJudging();

        bRoutineStarted = true;
        RoutineStartTime = DateTime.Now;
        OverlayAnimInterp = 0f;
        OverlayAnimState = EOverlayAnimState.Out;
        double WaitToStartTime = 10.0;
        StartAnimTime = (RoutineStartTime.AddSeconds(WaitToStartTime));
    }

    public override void StopRoutineJudging()
    {
        base.StopRoutineJudging();

        if (OverlayAnimInterp > 0)
        {
            RoutineFinishTime = DateTime.Now;
            StartAnimTime = DateTime.Now;
        }

        OverlayAnimInterp = 0f;
        OverlayAnimState = EOverlayAnimState.In;
        bRoutineStarted = false;
    }

    void DrawRoutineOverlay()
    {
        if (HasValidData())
        {
            if (Global.AllData != null && Global.AllData.AllDivisions.Length > ((int)CurDivision) && Global.AllData.AllDivisions[((int)CurDivision)].Rounds.Length > ((int)CurRound) &&
                Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools.Count > CurPool &&
                Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools[CurPool].Teams.Count > CurTeam)
            {
                string PlayerNames = Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools[CurPool].Teams[CurTeam].Data.PlayerNamesWithRank;
                int TotalTeams = Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools[CurPool].Teams.Count;
                //GUI.Label(new Rect(400, 400, 500, 500), ((int)CurDivision) + "  " + ((int)CurRound) + "  " + CurPool + "   " + CurTeam);

                float OverlayHeight = Screen.height * .85f;
                float OverlayOffsetY = 0;
                if (OverlayAnimState == EOverlayAnimState.Out)
                {
                    OverlayOffsetY = Math.Min((float)(OverlayAnimInterp * OverlayHeight), OverlayHeight);
                }

                if (OverlayAnimState == EOverlayAnimState.In)
                {
                    OverlayOffsetY = Math.Max(0, (float)((1.0 - OverlayAnimInterp) * OverlayHeight));
                }

                GUIStyle LineStyle = new GUIStyle("label");
                LineStyle.fontSize = (int)(22 * Screen.height / 720f);
                LineStyle.fontStyle = FontStyle.Bold;

                GUI.DrawTexture(new Rect(0, OverlayHeight + OverlayOffsetY, Screen.width, Screen.height * .15f), BgTexture);

                GUI.contentColor = Color.white;
                GUIContent TopLineContent = new GUIContent(GetDivisionString() + " - " + ((ERound)((int)CurRound)).ToString() + GetPoolString());
                GUI.Label(new Rect(Screen.width * .06f, Screen.height * .88f + OverlayOffsetY, Screen.width, Screen.height * .05f), TopLineContent, LineStyle);
                GUIContent BottomLineContent = new GUIContent(PlayerNames);
                GUI.Label(new Rect(Screen.width * .06f, Screen.height * .93f + OverlayOffsetY, Screen.width, Screen.height * .05f), BottomLineContent, LineStyle);
            }
        }
    }

    void DrawScoreboardOverlay()
    {
        if (HasValidData())
        {
            int TeamsCount = Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools[CurPool].Teams.Count;
            bool bUseSimpleBg = TeamsCount > 8;

            if (!bUseSimpleBg)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), ScoreboardTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), ScoreboardSimpleTexture);
            }

            GUI.contentColor = Color.black;
            GUIStyle CategoryStyle = new GUIStyle("label");
            CategoryStyle.fontSize = (int)(37 * Screen.height / 720f);
            CategoryStyle.fontStyle = FontStyle.Bold;
            CategoryStyle.alignment = TextAnchor.MiddleCenter;

            GUIContent CategoryContent = new GUIContent(GetDivisionString() + " - " + ((ERound)((int)CurRound)).ToString() + GetPoolString());
            GUI.Label(new Rect(0, Screen.height * .04f, Screen.width, Screen.height * .15f), CategoryContent, CategoryStyle);

            GUIStyle PlaceStyle = new GUIStyle("label");
            PlaceStyle.fontSize = (int)(30 * Screen.height / 720f);
            PlaceStyle.fontStyle = FontStyle.Bold;
            PlaceStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle InfoStyle = new GUIStyle("label");
            InfoStyle.fontSize = (int)(30 * Screen.height / 720f);
            InfoStyle.fontStyle = FontStyle.Bold;
            InfoStyle.alignment = TextAnchor.MiddleLeft;

            float LineStartHeight = Screen.height * .22f;
            float LineHeight = Screen.height * .084f;
            if (bUseSimpleBg)
            {
                LineHeight = Screen.height * .657f / TeamsCount;
            }

            List<TeamDataDisplay> SortedTeamList = new List<TeamDataDisplay>();
            List<TeamDataDisplay> NoScoresTeamList = new List<TeamDataDisplay>();
            foreach (TeamDataDisplay tdd in Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools[CurPool].Teams)
            {
                if (tdd.Data.RoutineScores.GetTotalPoints() > 0f)
                {
                    SortedTeamList.Add(tdd);
                }
                else
                {
                    NoScoresTeamList.Add(tdd);
                }
            }

            SortedTeamList.Sort(delegate(TeamDataDisplay tdd1, TeamDataDisplay tdd2)
            {
                float Score1 = tdd1.Data.RoutineScores.GetTotalPoints();
                float Score2 = tdd2.Data.RoutineScores.GetTotalPoints();

                if (Score1 == Score2)
                    return 0;
                else if (Score1 > Score2)
                    return -1;

                return 1;
            });

            SortedTeamList.AddRange(NoScoresTeamList);

            int Rank = 1;
            foreach (TeamDataDisplay tdd in SortedTeamList)
            {
                GUIStyle NameStyle = new GUIStyle("label");
                NameStyle.fontSize = (int)(30 * Screen.height / 720f);
                NameStyle.fontStyle = FontStyle.Bold;
                NameStyle.alignment = TextAnchor.MiddleLeft;

                GUIContent PlaceContent = new GUIContent(Rank.ToString());
                ++Rank;
                GUI.Label(new Rect(Screen.width * .052f, LineStartHeight, Screen.width * .03f, Screen.height * .15f), PlaceContent, PlaceStyle);

                GUIContent NameContent = new GUIContent(tdd.Data.PlayerNamesWithRank);
                float NameWidth = NameStyle.CalcSize(NameContent).x;
                float MaxNameWidth = Screen.width * .54f;
                while (NameWidth > MaxNameWidth)
                {
                    --NameStyle.fontSize;
                    NameWidth = NameStyle.CalcSize(NameContent).x;
                }
                GUI.Label(new Rect(Screen.width * .1f, LineStartHeight, Screen.width, Screen.height * .15f), NameContent, NameStyle);

                float TotalStartWidth = Screen.width * .68f;
                GUIContent TotalContent = new GUIContent(tdd.Data.RoutineScores.GetTotalPoints().ToString("0.0"));
                GUI.Label(new Rect(TotalStartWidth, LineStartHeight, Screen.width, Screen.height * .15f), TotalContent, InfoStyle);

                float NumbersWidth = Screen.width * .07f;
                GUIContent ExContent = new GUIContent(GetTotalExPointsString(tdd));
                GUI.Label(new Rect(TotalStartWidth + NumbersWidth, LineStartHeight, Screen.width, Screen.height * .15f), ExContent, InfoStyle);

                GUIContent AiContent = new GUIContent(GetTotalAiPointsString(tdd));
                GUI.Label(new Rect(TotalStartWidth + 2f * NumbersWidth, LineStartHeight, Screen.width, Screen.height * .15f), AiContent, InfoStyle);

                GUIContent DiffContent = new GUIContent(GetTotalDiffPointsString(tdd));
                GUI.Label(new Rect(TotalStartWidth + 3f * NumbersWidth, LineStartHeight, Screen.width, Screen.height * .15f), DiffContent, InfoStyle);

                LineStartHeight += LineHeight;
            }
        }
    }

    bool HasValidData()
    {
        if (((int)CurDivision) >= 0 && ((int)CurRound) >= 0 && CurPool >= 0 &&
            Global.AllData != null &&
            ((int)CurDivision) < Global.AllData.AllDivisions.Length &&
            ((int)CurRound) < Global.AllData.AllDivisions[((int)CurDivision)].Rounds.Length &&
            CurPool < Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools.Count)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    string GetTotalExPointsString(TeamDataDisplay tdd)
    {
        float ret = 0;
        foreach (ExData ed in tdd.Data.RoutineScores.ExResults)
        {
            ret += ed.GetTotalPoints();
        }

        return ret.ToString("0.0");
    }

    string GetTotalAiPointsString(TeamDataDisplay tdd)
    {
        float ret = 0;
        foreach (AIData ai in tdd.Data.RoutineScores.AIResults)
        {
            ret += ai.GetTotalPoints();
        }

        return ret.ToString("0.0");
    }

    string GetTotalDiffPointsString(TeamDataDisplay tdd)
    {
        float ret = 0;
        foreach (DiffData dd in tdd.Data.RoutineScores.DiffResults)
        {
            ret += dd.GetTotalPoints();
        }

        return ret.ToString("0.0");
    }

    string GetPoolString()
    {
        if (HasValidData())
        {
            if (Global.AllData.AllDivisions[((int)CurDivision)].Rounds[((int)CurRound)].Pools.Count > 1)
            {
                return " - Pool: " + (char)('A' + CurPool);
            }
        }

        return "";
    }

    string GetDivisionString()
    {
        string DivisionName = "";
        switch ((EDivision)((int)CurDivision))
        {
            case EDivision.Open:
                DivisionName = "Open Pairs";
                break;
            case EDivision.Mixed:
                DivisionName = "Mixed Pairs";
                break;
            case EDivision.Coop:
                DivisionName = "Coop";
                break;
            case EDivision.Women:
                DivisionName = "Women Pairs";
                break;
        }

        return DivisionName;
    }

	void OnGUI()
	{
        switch (OverlayMode)
        {
            case EOverlayMode.Routine:
                DrawRoutineOverlay();
                break;
            case EOverlayMode.Scoreboard:
                DrawScoreboardOverlay();
                break;
        }
	}
}
