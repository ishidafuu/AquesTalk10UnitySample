using UnityEngine;
using UnityEngine.UI;

public class LogDraw : MonoBehaviour
{
	public Text text_ = null;

	private void Awake()
	{
		Application.logMessageReceived += OnLogMessage;
	}

	private void OnDestroy()
	{
		Application.logMessageReceived += OnLogMessage;
	}

	private void OnLogMessage(string logString, string stackTrace, LogType logType)
	{
		if (string.IsNullOrEmpty(logString))
		{
			return;
		}

		text_.text += logString + "\n";
	}

} 