using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class WindowsVoice : MonoBehaviour
{
	[DllImport("WindowsVoice")]
	public static extern void initSpeech();
	[DllImport("WindowsVoice")]
	public static extern void destroySpeech();
	[DllImport("WindowsVoice")]
	public static extern void addToSpeechQueue(string s);

	public static WindowsVoice theVoice = null;
	// Use this for initialization
	void Start()
	{
		try
		{
			if (theVoice == null)
			{
				theVoice = this;
				DontDestroyOnLoad(gameObject);
				initSpeech();
			}
			//else
			//Destroy(gameObject);
		}
		catch
		{
		}
	}
	public void speak(string msg)
	{
		try
		{
			addToSpeechQueue(msg);
		}
		catch
		{
		}
	}
	void OnDestroy()
	{
		try
		{
			if (theVoice == this)
			{
				Debug.Log("Destroying speech");
				destroySpeech();
				theVoice = null;
			}
		}
		catch
		{
		}
	}
}
