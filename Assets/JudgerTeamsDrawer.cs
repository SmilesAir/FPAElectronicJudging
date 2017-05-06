using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class JudgerTeamsDrawer : MonoBehaviour
{
    public GameObject CanvasGO;
    public GameObject[] TeamButtons;
    public delegate void OnTeamButtonClickDelegate(int teamIndex);
    public OnTeamButtonClickDelegate OnTeamButtonClick;

    void Start()
    {
        // CanvasGO.SetActive(true);
    }

    void TeamButtonClicked(int buttonIndex)
    {
        if (OnTeamButtonClick != null)
        {
            OnTeamButtonClick(buttonIndex);
        }
    }

    public void InitButtonTeamsText(int InDiv, int InRound, int InPool, int InJudgeCategoryIndex, ECategoryView InCategoryView)
    {
        CanvasGO.SetActive(true);

        if (InDiv >= 0 && InDiv < Global.AllData.AllDivisions.Length &&
            InRound >= 0 && InRound < Global.AllData.AllDivisions[InDiv].Rounds.Length &&
            InPool >= 0 && InPool < Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools.Count)
        {
            int buttonIndex = 0;
            foreach (GameObject go in TeamButtons)
            {
                Button button = go.GetComponent<Button>();
                button.onClick.RemoveAllListeners();

                if (buttonIndex < Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[InPool].Teams.Count)
                {
                    go.SetActive(true);
                    int localButtonIndex = buttonIndex;
                    button.onClick.AddListener(() => { TeamButtonClicked(localButtonIndex); });

                    RoutineScoreData SData = Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[InPool].Teams[buttonIndex].Data.RoutineScores;
                    string PlayerNames = Global.AllData.AllDivisions[InDiv].Rounds[InRound].Pools[InPool].Teams[buttonIndex].Data.PlayerNames;
                    if (SData != null)
                    {
                        foreach (Text buttonText in go.GetComponentsInChildren<Text>())
                        {
                            string buttonString = PlayerNames;
                            if (SData.ContainsJudgeScores())
                            {
                                buttonString += ": " + SData.GetResultsString(InJudgeCategoryIndex, InCategoryView, false);
                            }
                            else
                            {
                                buttonString += ": No Data";
                            }

                            buttonText.text = buttonString;
                        }
                    }
                }
                else
                {
                    go.SetActive(false);
                }

                ++buttonIndex;
            }
        }
    }
}
