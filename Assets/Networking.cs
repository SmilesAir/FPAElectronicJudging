using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.IO;

public class Networking : MonoBehaviour
{
	public bool bClientInited = false;
	public UdpClient Client = null;
	public static string PlayerIp = "";
	public static bool bIsServer = false;
	static IPEndPoint ReceiveEndPoint;
	static NetData StateData;
	static bool bGotMessage = true;
	static float PingServerTimer = -1f;
	public static string ServerIp = "";
	static Socket BroadcastSocket;
	public static bool IsConnectedToServer = false;
	public static bool HasConnection
	{
		get { return Network.connections.Length > 0; }
	}
	public static bool bGettingDivision = false;
	public static bool bGettingNames = false;
	public static string AllDataBuilderString = "";
	public static string NamesBuilderString = "";
	public static bool bNeedSendCachedResults = false;
	public int LastSyncedDivState = -1;
	public int LastSyncedCurState = -1;
	float ClientQueryTimer = -1;
	bool bSentConnectedMessage = false;
	public static int ServerPort = 8901;
	public static int ClientPort = 8123;

	// Use this for initialization
	void Start()
	{
		PlayerIp = Network.player.ipAddress;

		BroadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		BroadcastSocket.EnableBroadcast = true;
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	void OnDestroy()
	{
		if (Network.isClient)
		{
			Network.Disconnect();
		}
	}

	public bool ContainsConnectedGuid(string InGuid)
	{
		foreach (NetworkPlayer np in Network.connections)
		{
			if (np.guid == InGuid)
				return true;
		}

		return false;
	}

	public void InitUdpListener(bool bInIsServer)
	{
		if (Client == null)
		{
			bIsServer = bInIsServer;
			if (bIsServer)
			{
				Client = new UdpClient(ServerPort);
				ReceiveEndPoint = new IPEndPoint(IPAddress.Any, ServerPort);
			}
			else
			{
				bool bKeepConnecting = true;
				while (bKeepConnecting)
				{
					try
					{
						Client = new UdpClient(ClientPort);
						ReceiveEndPoint = new IPEndPoint(IPAddress.Any, ClientPort);

						bKeepConnecting = false;
					}
					catch
					{
						++ClientPort;
					}
				}
			}

			StateData = new NetData();
			StateData.e = ReceiveEndPoint;
			StateData.u = Client;
		}
	}

	public static void ReceiveCallback(IAsyncResult ar)
	{
		UdpClient u = (UdpClient)((NetData)(ar.AsyncState)).u;
		IPEndPoint e = (IPEndPoint)((NetData)(ar.AsyncState)).e;

		Byte[] ReceiveBytes = u.EndReceive(ar, ref e);
		string ReceiveString = Encoding.ASCII.GetString(ReceiveBytes);

		Debug.Log("received: " + ReceiveString);

		bGotMessage = true;

		// ping out this ip address
		if (ReceiveString.StartsWith("PingServer"))
		{
			IPEndPoint Group = new IPEndPoint(IPAddress.Broadcast, int.Parse(ReceiveString.Replace("PingServer","")));
			byte[] Bytes = Encoding.ASCII.GetBytes(PlayerIp);
			BroadcastSocket.SendTo(Bytes, Group);
		}
		else
		{
			ServerIp = ReceiveString;

			Debug.Log("got sever ip: " + ReceiveString);
		}
	}

	public void UpdateUdpListener()
	{
		if (Client != null)
		{
			if (bGotMessage)
			{
				bGotMessage = false;

				Client.BeginReceive(ReceiveCallback, StateData);
			}

			if (!bIsServer && ServerIp.Length == 0)
			{
				PingServerTimer -= Time.deltaTime;
				if (PingServerTimer < 0)
				{
					PingServerTimer = 3f;

					string PingString = "PingServer" + ClientPort;
					IPEndPoint Group = new IPEndPoint(IPAddress.Broadcast, ServerPort);
					byte[] Bytes = Encoding.ASCII.GetBytes(PingString);
					BroadcastSocket.SendTo(Bytes, Group);
				}
			}

			if (!IsConnectedToServer && ServerIp.Length > 0)
			{
				if (Network.Connect(ServerIp, 8765) == NetworkConnectionError.NoError)
				{
					IsConnectedToServer = true;
					bSentConnectedMessage = false;

					Debug.Log("Connected to: " + ServerIp);
				}
			}

			if (!bSentConnectedMessage && Network.isClient)
			{
				bSentConnectedMessage = true;

				JudgerBase JudgeBase = Global.GetActiveJudger();
				if (JudgeBase)
				{
					JudgeBase.JudgeGuid = Network.player.guid;

					//Debug.Log(" send connect message: " + bClientInited);

					if (bClientInited)
						SendJudgeReconnected(Network.player.guid, (int)Global.GetActiveJudgerType(), JudgeBase.JudgeCategoryIndex, JudgeBase.GetJudgeNameId(),
							JudgeBase.bClientReadyToBeLocked, JudgeBase.bLockedForJudging, JudgeBase.bIsJudging, JudgeBase.bIsEditing);
					else
						SendJudgeConnected(Network.player.guid, (int)Global.GetActiveJudgerType());
				}
			}

			if (Networking.bNeedSendCachedResults && bSentConnectedMessage)
			{
				JudgerBase JudgeBase = Global.GetActiveJudger();
				if (JudgeBase)
					JudgeBase.SendCachedResultsToHeadJudger();
			}
		}
	}

	void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		Debug.Log(" disconnected network: " + ServerIp);

		ServerIp = "";
		IsConnectedToServer = false;
	}

	void OnFailedToConnect(NetworkConnectionError error)
	{
		Debug.Log("Could not connect to server: " + error);

		ServerIp = "";
		IsConnectedToServer = false;
	}

	public void SendDataToJudgers(string InDataString, string InNameString)
	{
		Debug.Log(" sending to judgers: " + InDataString.Length + "  : " + InDataString);

		GetComponent<NetworkView>().RPC("StartSendDivisionRPC", RPCMode.Others);

		int NumDivStrs = Mathf.CeilToInt(InDataString.Length / 2048f);
		for (int StrIndex = 0; StrIndex < NumDivStrs; ++StrIndex)
		{
			int StartIndex = StrIndex * 2048;
			int Len = Mathf.Min(2048, InDataString.Length - StartIndex);
			string Str = InDataString.Substring(StartIndex, Len);
			GetComponent<NetworkView>().RPC("SendPartialDivisionRCP", RPCMode.Others, Str);
		}

		GetComponent<NetworkView>().RPC("FinishSendDivisionRPC", RPCMode.Others, Global.DivDataState);



		GetComponent<NetworkView>().RPC("StartSendNamesRPC", RPCMode.Others);

		int NumNameStrs = Mathf.CeilToInt(InNameString.Length / 2048f);
		for (int StrIndex = 0; StrIndex < NumNameStrs; ++StrIndex)
		{
			int StartIndex = StrIndex * 2048;
			int Len = Mathf.Min(2048, InNameString.Length - StartIndex);
			string Str = InNameString.Substring(StartIndex, Len);
			GetComponent<NetworkView>().RPC("SendPartialNamesRCP", RPCMode.Others, Str);
		}

		GetComponent<NetworkView>().RPC("FinishSendNamesRPC", RPCMode.Others);
	}

	public void UpdateClientJudgeState()
	{
		ClientQueryTimer -= Time.deltaTime;

		if (ClientQueryTimer < 0)
		{
			ClientQueryTimer = 3f;

			if (Network.isClient)
			{
				GetComponent<NetworkView>().RPC("QueryHeadJudgeForState", RPCMode.Others);
			}
		}

		if (LastSyncedCurState != Global.CurDataState)
		{
			LastSyncedCurState = Global.CurDataState;

			GetComponent<NetworkView>().RPC("QueryHeadJudgeForCurData", RPCMode.Others);
		}

		if (LastSyncedDivState != Global.DivDataState)
		{
			LastSyncedDivState = Global.DivDataState;

			GetComponent<NetworkView>().RPC("QueryHeadJudgeForDivData", RPCMode.Others);
		}
	}

	public void UpdateHeadJudgeState()
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (LastSyncedCurState != Global.CurDataState)
		{
			LastSyncedCurState = Global.CurDataState;

			foreach (NetworkPlayer np in Network.connections)
			{
				int JudgeIndex = HJudge.GetCatIndexForGuid(np.guid);
				if (JudgeIndex >= 0)
				{
					GetComponent<NetworkView>().RPC("SendJudgerInfoRPC", np, np.guid, JudgeIndex);
				}

				GetComponent<NetworkView>().RPC("SendCurDataRPC", np, Global.CurDataState, (int)HJudge.CurDivision, (int)HJudge.CurRound,
					HJudge.GetJudgePool(np.guid), HJudge.GetJudgeTeam(np.guid), HJudge.GetActiveJudgingJudgers(np.guid));

				Debug.Log(" send data to judge team: " + HJudge.GetJudgePool(np.guid));
			}
		}

		if (LastSyncedDivState != Global.DivDataState)
		{
			LastSyncedDivState = Global.DivDataState;

			SendDataToJudgers(Global.AllData.SerializeToString(), Global.AllNameData.SerializeToString());
		}
	}

	public void CallForJudgesReady()
	{
		GetComponent<NetworkView>().RPC("CallForJudgesReadyRPC", RPCMode.Others);
	}

	public void LockJudgesToJudge()
	{
		GetComponent<NetworkView>().RPC("SendLockJudgesRPC", RPCMode.Others);
	}

	public void SendJudgeReady(string InGuid, bool bInReady)
	{
		if (IsConnectedToServer)
			GetComponent<NetworkView>().RPC("SendJudgeReadyRPC", RPCMode.Others, InGuid, bInReady);
	}

	public void SendJudgeLocked(string InGuid, bool bInLocked)
	{
		if (IsConnectedToServer)
			GetComponent<NetworkView>().RPC("SendJudgeLockedRPC", RPCMode.Others, InGuid, bInLocked);
	}

	public void SendJudgeJudging(string InGuid, bool bInJudging)
	{
		if (IsConnectedToServer)
			GetComponent<NetworkView>().RPC("SendJudgeJudgingRPC", RPCMode.Others, InGuid, bInJudging);
	}
	
	public void SendJudgeEditing(string InGuid, bool bInEditing)
	{
		if (IsConnectedToServer)
			GetComponent<NetworkView>().RPC("SendJudgeEditingRPC", RPCMode.Server, InGuid, bInEditing);
	}

	public void SendJudgeNameId(string InGuid, int InNameId)
	{
		if (IsConnectedToServer)
			GetComponent<NetworkView>().RPC("SendJudgeNameIdRPC", RPCMode.Others, InGuid, InNameId);
	}

	public void SendJudgeConnected(string InGuid, int InJudgeType)
	{
		GetComponent<NetworkView>().RPC("SendJudgeConnectedRPC", RPCMode.Server, InGuid, InJudgeType);
	}

	public void SendJudgeReconnected(string InGuid, int InJudgeType, int InCatJudgeIndex, int InNameId, bool bInIsHoldReady, bool bInLocked, bool bInJudging, bool bInEditing)
	{
		GetComponent<NetworkView>().RPC("SendJudgeReconnectedRPC", RPCMode.Server, InGuid, InJudgeType, InCatJudgeIndex, InNameId, bInIsHoldReady, bInLocked, bInJudging, bInEditing);
	}

	public void SendJudgeInfo(string InGuid, int InJudgeCategoryIndex)
	{
		foreach (NetworkPlayer np in Network.connections)
		{
			if (np.guid == InGuid)
			{
				GetComponent<NetworkView>().RPC("SendJudgerInfoRPC", np, InGuid, InJudgeCategoryIndex);
				break;
			}
		}
	}

	public void ServerSendStartRoutine()
	{
		GetComponent<NetworkView>().RPC("ServerSendStartRoutineRPC", RPCMode.Others);
	}

	public void ClientSendFinishJudgingDiff(string InDiffData)
	{
		JudgerBase JudgeBase = Global.GetActiveJudger();
		if (JudgeBase)
			GetComponent<NetworkView>().RPC("ClientSendFinishJudgingDiffRPC", RPCMode.Others, JudgeBase.JudgeGuid, InDiffData);
	}

	public void ServerSendStopRoutine()
	{
		GetComponent<NetworkView>().RPC("ServerSendStopRoutineRPC", RPCMode.Others);
	}

	public void ClientSendFinishJudgingEx(string InExData)
	{
		JudgerBase JudgeBase = Global.GetActiveJudger();
		if (JudgeBase)
			GetComponent<NetworkView>().RPC("ClientSendFinishJudgingExRPC", RPCMode.Others, JudgeBase.JudgeGuid, InExData);
	}

	public void ClientSendFinishJudgingAI(string InAIData)
	{
		JudgerBase JudgeBase = Global.GetActiveJudger();
		if (JudgeBase)
			GetComponent<NetworkView>().RPC("ClientSendFinishJudgingAIRPC", RPCMode.Others, JudgeBase.JudgeGuid, InAIData);
	}

	[RPC]
	void ClientSendFinishJudgingAIRPC(string InGuid, string InAIData)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.ReceiveAIData(InGuid, InAIData);
	}

	[RPC]
	void ClientSendFinishJudgingExRPC(string InGuid, string InExData)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.ReceiveExData(InGuid, InExData);
	}

	[RPC]
	void ClientSendFinishJudgingDiffRPC(string InGuid, string InDiffData)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.ReceiveDiffData(InGuid, InDiffData);
	}

	[RPC]
	void ServerSendStartRoutineRPC()
	{
		InterfaceBase JudgeBase = Global.GetActiveInterface();
		if (JudgeBase)
			JudgeBase.StartRoutineJudging();
	}

	[RPC]
	void ServerSendStopRoutineRPC()
	{
		InterfaceBase JudgeBase = Global.GetActiveInterface();
		if (JudgeBase)
			JudgeBase.StopRoutineJudging();
	}

	[RPC]
	void SendJudgerInfoRPC(string InGuid, int InJudgeCategoryIndex)
	{
		JudgerBase JudgeBase = Global.GetActiveJudger();
		if (JudgeBase && JudgeBase.JudgeGuid == InGuid)
		{
			JudgeBase.JudgeCategoryIndex = InJudgeCategoryIndex;
			JudgeBase.bIsDataDirty = true;

			bClientInited = true;
		}
	}

	[RPC]
	void SendJudgeConnectedRPC(string InGuid, int InJudgeType)
	{
		HeadJudger HJudger = Global.GetHeadJudger();
		if (HJudger)
			HJudger.OnConnectedJudger(InGuid, InJudgeType);
	}

	[RPC]
	void SendJudgeReconnectedRPC(string InGuid, int InJudgeType, int InCatJudgeIndex, int InNameId, bool bInIsHoldReady, bool bInLocked, bool bInJudging, bool bInEditing)
	{
		Debug.Log("Got reconnecte rpc");

		HeadJudger HJudger = Global.GetHeadJudger();
		if (HJudger)
			HJudger.OnReconnectedJudger(InGuid, InJudgeType, InCatJudgeIndex, InNameId, bInIsHoldReady, bInLocked, bInJudging, bInEditing);
	}

	[RPC]
	void SendJudgeReadyRPC(string InJudgeGuid, bool bInReady)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.SetJudgeReady(InJudgeGuid, bInReady);
	}

	[RPC]
	void SendJudgeLockedRPC(string InJudgeGuid, bool bInLocked)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.SetJudgeLocked(InJudgeGuid, bInLocked);

		Debug.Log(" got locked judge: " + InJudgeGuid + "  " + bInLocked);
	}

	[RPC]
	void SendJudgeJudgingRPC(string InJudgeGuid, bool bInJudging)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.SetJudgeJudging(InJudgeGuid, bInJudging);
	}

	[RPC]
	void SendJudgeEditingRPC(string InJudgeGuid, bool bInEditing)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.SetJudgeEditing(InJudgeGuid, bInEditing);
	}

	[RPC]
	void SendJudgeNameIdRPC(string InJudgeGuid, int InNameId)
	{
		HeadJudger HJudge = Global.GetHeadJudger();
		if (HJudge)
			HJudge.SetJudgeNameId(InJudgeGuid, InNameId);
	}

	[RPC]
	void CallForJudgesReadyRPC()
	{
		if (Network.isClient)
		{
			JudgerBase Judger = Global.GetActiveJudger();
			if (Judger && !Judger.bIsJudging && !Judger.bClientReadyToBeLocked)
			{
				Judger.bHeadJudgeRequestingReady = true;
			}
		}
	}

	[RPC]
	void SendLockJudgesRPC()
	{
		if (Network.isClient)
		{
			JudgerBase Judger = Global.GetActiveJudger();
			if (Judger && !Judger.bIsJudging && Judger.bClientReadyToBeLocked)
			{
                Judger.LockForJudging();

				SendJudgeLocked(Judger.JudgeGuid, true);
			}
		}
	}

	[RPC]
	void QueryHeadJudgeForCurData()
	{
		if (Network.isServer)
		{
			HeadJudger HJudge = Global.GetHeadJudger();
			GetComponent<NetworkView>().RPC("SendCurDataRPC", RPCMode.Others, Global.CurDataState, (int)HJudge.CurDivision, (int)HJudge.CurRound, HJudge.CurPool, HJudge.CurTeam, HJudge.ActiveJudgingJudgers);
		}
	}

	[RPC]
	void QueryHeadJudgeForDivData()
	{
		if (Network.isServer)
			SendDataToJudgers(Global.AllData.SerializeToString(), Global.AllNameData.SerializeToString());
	}

	[RPC]
	void QueryHeadJudgeForState()
	{
		if (Network.isServer)
		{
			GetComponent<NetworkView>().RPC("SendHeadJudgeState", RPCMode.Others, Global.DivDataState, Global.CurDataState);
		}
	}

	[RPC]
	void SendHeadJudgeState(int InDivState, int InCurState)
	{
		JudgerBase Judger = Global.GetActiveJudger();
		if (!Judger || !Judger.bIsJudging)
		{
			Global.CurDataState = InCurState;
			Global.DivDataState = InDivState;
		}
	}

	[RPC]
	void SendCurDataRPC(int InCurState, int InDivision, int InRound, int InPool, int InTeam, int InActiveJudgingJudgers)
	{
		JudgerBase Judger = Global.GetActiveJudger();
		if (Judger)
		{
			Debug.Log(" got SendCurDataRPC " + Judger.bIsJudging + "  " + Judger.bLockedForJudging + "  " + Judger.bClientReadyToBeLocked +
				" " + Judger.bHeadJudgeRequestingReady + " " + Judger.bIsEditing + " " + Judger.bRoutineTimeEnded);
		}
		if (Judger && !Judger.bIsJudging)
		{
			Judger.CurDivision = (EDivision)InDivision;
			Judger.CurRound = (ERound)InRound;
			Judger.CurPool = InPool;
			Judger.CurTeam = InTeam;
			Judger.WaitingForJudgesCount = InActiveJudgingJudgers;

			Global.CurDataState = InCurState;
			LastSyncedCurState = InCurState;

			Debug.Log("!!!!!!!!!!!!!! set judge data");
		}

		if (Global.Obj.OverlayGo && Global.Obj.OverlayGo.activeSelf)
		{
			Overlay OverlayScript = Global.Obj.OverlayGo.GetComponent<Overlay>();
			if (OverlayScript)
			{
				OverlayScript.CurDivision = (EDivision)InDivision;
				OverlayScript.CurRound = (ERound)InRound;
				OverlayScript.CurPool = InPool;
				OverlayScript.CurTeam = InTeam;
			}
		}
	}

	[RPC]
	void StartSendDivisionRPC()
	{
		bGettingDivision = true;

		AllDataBuilderString = "";

		Debug.Log(" Start receive ");
	}

	[RPC]
	void SendPartialDivisionRCP(string InString)
	{
		if (bGettingDivision)
		{
			AllDataBuilderString += InString;

			Debug.Log(" parial: " + InString);
		}
	}

	[RPC]
	void FinishSendDivisionRPC(int InDivState)
	{
		bGettingDivision = false;

		try
		{
			using (Stream stream = Global.GenerateStreamFromString(AllDataBuilderString))
				Global.AllData = TournamentData.Load(stream);
		}
		catch (Exception e)
		{
			Debug.Log("Exception in FinishSendDivisionRPC: " + e.Message);
		}
		Global.DivDataState = InDivState;
		LastSyncedDivState = InDivState;

		JudgerBase Judger = Global.GetActiveJudger();
		if (Judger)
		{
			SendJudgeNameId(Judger.JudgeGuid, Judger.GetJudgeNameId());
		}

		//Debug.Log(" finished: ");
	}

	[RPC]
	void StartSendNamesRPC()
	{
		bGettingNames = true;

		NamesBuilderString = "";

		Debug.Log(" Start receive ");
	}

	[RPC]
	void SendPartialNamesRCP(string InString)
	{
		if (bGettingNames)
		{
			NamesBuilderString += InString;

			//Debug.Log(" parial: " + InString);
		}
	}

	[RPC]
	void FinishSendNamesRPC()
	{
		bGettingNames = false;
		try
		{
			using (Stream stream = Global.GenerateStreamFromString(NamesBuilderString))
				Global.AllNameData = NameDatabase.Load(stream);
		}
		catch (Exception e)
		{
			Debug.Log("Exception in FinishSendNamesRPC: " + e.Message);
		}

		//Debug.Log(" finished: ");
	}
}

public class NetData
{
	public UdpClient u;
	public IPEndPoint e;
}
