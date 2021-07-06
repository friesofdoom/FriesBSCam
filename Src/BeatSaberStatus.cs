using System;
using System.Collections.Generic;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace FriesBSCameraPlugin
{
	public class BeatMap
	{
		[JsonProperty("songCover")] public string SongCover { get; set; }
		[JsonProperty("songName")] public string SongName { get; set; }
		[JsonProperty("songSubName")] public string SongSubName { get; set; }
		[JsonProperty("songAuthorName")] public string SongAuthorName { get; set; }
		[JsonProperty("levelAuthorName")] public string LevelAuthorName { get; set; }
		[JsonProperty("songHash")] public string SongHash { get; set; }
		[JsonProperty("levelId")] public string LevelId { get; set; }
	}

	internal enum EventType
	{
		songStart,
		noteMissed,
		scoreChanged,
		menu,
		pause,
		resume
	}

	public class BeatSaberStatus
	{
		public int score;//CurrentScore
		public bool menu = true;//In-menu?
		public bool paused;
		public bool connected;
		public BeatMap map;

		//public List<string> received = new List<string>();
		public readonly List<string> debug = new List<string>(); //debug messages. 
		private readonly WebSocket _ws = new WebSocket("ws://localhost:6557/socket");

		private static Timer _reconnectTimer;


		private void ScoreUpdate(JToken perf)
		{
			score = (int)perf["score"];
			//debug.Add("Score: " + score + " CurrentMaxScore: " + currentMaxScore + " ");
		}

		public BeatSaberStatus()
		{
			_ws.OnOpen += (sender, e) => debug.Add("Should have connected to BS");
		
			_ws.OnMessage += (sender, e) =>
			{
				connected = true;
				
				// Convert incoming data
				var received = JObject.Parse(e.Data);
				if (received["status"]?["beatmap"] == null || received["event"] == null) return;
				if (Enum.TryParse(received["event"].ToString(), out EventType eventType)) return;

				switch (eventType)
				{
					case EventType.songStart:
						debug.Add("SongStart");
						score = 0;
						map = received["status"]["beatmap"].ToObject<BeatMap>();
						menu = false;
						paused = false;
						debug.Add("Song name is " + map?.SongName);
						break;
					
					case EventType.noteMissed:
					case EventType.scoreChanged:
						menu = false;
						paused = false;
						ScoreUpdate(received["status"]["performance"]);
						break;
					
					case EventType.menu:
						paused = false;
						menu = true;
						break;
					
					case EventType.pause:
						paused = true;
						break;
					
					case EventType.resume:
						paused = false;
						break;
					
					default:
						return;
				}
			};
			
			_ws.OnClose += (sender, e) =>
			{
				// Closed
				debug.Add("Closed");
				_reconnectTimer.Start();
			};
			
			_ws.OnError += (sender, e) =>
			{
				//Some error. 
				debug.Add(e.Message);
				if (e.Message.Contains("OnMessage event"))
				{
					debug.Add("Beat Saber websocket error: OnMessage event");
					debug.Add("Sender is: " + sender);
					debug.Add("Verbose error is: " + e.Message.ToString());
					return;
				}

				if (e.Message.Contains("occurred in closing the connection"))
					debug.Add("Websocket error in closing");
				else
				{
					if (!_ws.IsAlive) return;
					
					debug.Add(
						"Error wasn't about something closing. So I'm attempting to close it so it can restart");
					_ws.CloseAsync();
				}
			};

			_reconnectTimer = new Timer(500) {AutoReset = true};
			_reconnectTimer.Elapsed += WsConnect;
			_reconnectTimer.Enabled = true;
		}

		private void WsConnect(object source, ElapsedEventArgs t)
		{
			debug.Add("Timer Fired. " + t.SignalTime);
			if (_ws.IsAlive)
				debug.Add("IsAlive = true. Should be connected.");
			else
			{
				debug.Add("IsAlive = false. Try to connect.");
				_ws.ConnectAsync();
				_reconnectTimer.Stop();
			}
		}
		
		public void ShutDown()
		{
			_reconnectTimer.Dispose();
			_ws.CloseAsync();
		}
	}
}