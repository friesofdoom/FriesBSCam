using System;
using System.Collections.Generic;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Timers;

public class BeatSaberStatus
{
	public int score = 0;//CurrentScore
	public int currentMaxScore = 0;//currentMaxScore possible
	public string rank = "SSS";//Rank
	public int combo = 0;//Combo
	public bool menu = true;//In-menu?
	public bool paused = false;
	public bool connected = false;
	public string type = "";//game type
	public string songName = "";
	public string songSubName = "";
	public string songAuthorName = "";
	public string levelAuthorName = "";
	public string songHash = "";
	public string levelId = "";

	public string cover = "empty";

	//public List<string> received = new List<string>();
	public List<string> debug = new List<string>(); //debug messages. 
	private WebSocket ws;

	private static Timer reconnectTimer;


	public void ScoreUpdate(JToken perf)
	{
		score = (int)perf["score"];
		currentMaxScore = (int)perf["currentMaxScore"];
		rank = perf["rank"].ToString();
		combo = (int)perf["combo"];
		//debug.Add("Score: " + score + " CurrentMaxScore: " + currentMaxScore + " ");
	}

	public BeatSaberStatus()
	{

		//the constructor, I think. 
		ws = new WebSocket("ws://localhost:6557/socket");
		ws.OnOpen += (sender, e) =>
		{
			//socket open. 
			debug.Add("Should have connected to BS");
		};
		ws.OnMessage += (sender, e) =>
		{
			//debug.Add(e.Data);
			connected = true;
			JObject received = JObject.Parse(e.Data);



			if (received["event"].ToString() == "songStart")
			{
				debug.Add("SongStart");
				score = 0;
				currentMaxScore = 0;
				rank = "SSS";
				combo = 0;
				cover = received["status"]["beatmap"]["songCover"].ToString();
				songName = received["status"]["beatmap"]["songName"].ToString();
				songSubName = received["status"]["beatmap"]["songSubName"].ToString();
				songAuthorName = received["status"]["beatmap"]["songAuthorName"].ToString();
				levelAuthorName = received["status"]["beatmap"]["levelAuthorName"].ToString();
				songHash = received["status"]["beatmap"]["songHash"].ToString();
				levelId = received["status"]["beatmap"]["levelId"].ToString();
				menu = false;
				paused = false;
				debug.Add("Songname is " + songName);
			}

			if (
				received["event"].ToString() == "noteMissed"
				||
				received["event"].ToString() == "scoreChanged"
				)
			{
				menu = false;
				paused = false;
				ScoreUpdate(received["status"]["performance"]);
			}
			if (received["event"].ToString() == "menu")
			{
				paused = false;
				menu = true;
			}

			if (received["event"].ToString() == "pause")
            {
				paused = true;
            }
			if (received["event"].ToString() == "resume")
            {
				paused = false;
            }

			//received.Add(e.Data);
			//message. Just add this to the bottom of the responses list. 
		};
		ws.OnClose += (sender, e) =>
		{
			debug.Add("Closed");
			//closed
			reconnectTimer.Start();
		};
		ws.OnError += (sender, e) =>
		{
			//Some error. 
			debug.Add(e.Message);
			if (e.Message.Contains("OnMessage event"))
			{
				debug.Add("Beatsaber websocket error: OnMessage event");
				debug.Add("Sender is: " + sender.ToString());
				debug.Add("Verbose error is: " + e.Message.ToString());
			}
			else
			{


				if (e.Message.Contains("occurred in closing the connection"))
				{
					debug.Add("Webwsocket error in closing");
				}
				else
				{
					if (ws.IsAlive)
					{
						debug.Add("Error wasn't about something closing. So I'm attempting to close it so it can restart");
						ws.CloseAsync();
					}
				}
			}
		};

		reconnectTimer = new Timer(2000);
		reconnectTimer.AutoReset = true;
		reconnectTimer.Elapsed += wsConnect;
		reconnectTimer.Enabled = true;

	}

	private void wsConnect(Object source, ElapsedEventArgs t)
	{
		debug.Add("Timer Fired. " + t.SignalTime);
		if (ws.IsAlive)
		{
			debug.Add("IsAlive = true. Should be connected.");
		}
		else
		{
			debug.Add("IsAlive = false. Try to connect.");
			ws.ConnectAsync();
			reconnectTimer.Stop();
		}
	}
	public void shutDown()
	{
		reconnectTimer.Dispose();
		ws.CloseAsync();

	}
}
