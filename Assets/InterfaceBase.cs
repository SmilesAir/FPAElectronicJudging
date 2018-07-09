using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class InterfaceBase : MonoBehaviour
{
    public bool bIsJudging = false;
    public EDivision CurDivision = EDivision.Open;
    public ERound CurRound = ERound.Prelims;
    public EPool CurPool = EPool.None;
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

	public virtual void ResetRoutineJudging()
	{
		bIsJudging = false;
	}

	public float GetRoutineLengthMinutes()
    {
        if (Global.AllData == null || CurPool == EPool.None || (int)CurPool >= Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].Pools.Count)
            return 0;

        return Global.AllData.AllDivisions[(int)CurDivision].Rounds[(int)CurRound].RoutineLengthMinutes;
    }
}
