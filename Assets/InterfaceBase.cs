using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class InterfaceBase : MonoBehaviour
{
    public bool bIsJudging = false;
    public EDivision CurDivision = EDivision.Open;
    public ERound CurRound = ERound.Prelims;
    public int CurPool = -1;
    public int CurTeam = -1;
	public int CurJudgingTeam = -1;
	public int WaitingForJudgesCount = 0;

	// Use this for initialization
	public void Start()
	{
	}

	public virtual void StartRoutineJudging()
	{
	}

    public virtual void StopRoutineJudging()
    {
        bIsJudging = false;
    }

    public float GetRoutineLengthMinutes()
    {
        if (Global.AllData == null || CurPool < 0 || CurPool >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count)
            return 0;

        return Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].RoutineLengthMinutes;
    }
}
