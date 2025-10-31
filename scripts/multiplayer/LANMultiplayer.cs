using Godot;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

public partial class LANMultiplayer : Node
{
	[Export]
	public PacketPeerUdp broadcaster = new();
	[Export]
	PacketPeerUdp listener = new();
	[Export]
	int listenPort = 8911;
	[Export]
	int broadcastPort = 8912;
	[Export]
	string broadcastAddress = "192.168.4.255";

	string localIP = "";
	//note: may not be .4 subnet (why Global.GetBroadcastAddress() exists and is being used)
	public ServerInfo SI;

	public Dictionary<string, ServerInfo> foundServersDict = new();
	private List<byte> CodePicker = new();

	public Timer broadcastTimer;
	private const float broadcastInterval = 3f;
	private const float timeoutInterval = 9f; //should be 3x broadcast interval
											  // Called when the node enters the scene tree for the first time.

	public bool canConnectToSelf = false; //for debug
	public void SetCanConnectToSelf(bool val)
	{
		canConnectToSelf = val;
		GD.Print("canConnectToSelf set to " + canConnectToSelf);
	}

	[Export]
	public Label bindStatusLabel;
	string bindStatusL = "_";
	string bindStatusB = "_"; //shouldn't be necessary but oh well

	public override void _Ready()
	{
		broadcastAddress = Global.GetBroadcastAddress().ToString();
		broadcastTimer = new(broadcastInterval, false, this);
		broadcastTimer.TimerFinished += BroadcastOnInterval;

		for (byte i = 0; i < 255; i++)
		{
			CodePicker.Add(i);
		}

		string[] localIPs = new string[Global.GetLocalIPAddresses().Length];
		for (int i = 0; i < Global.GetLocalIPAddresses().Length; i++)
		{
			localIPs[i] = Global.GetLocalIPAddresses()[i].ToString();
		}
		localIP = Global.GetLocalIPAddresses()[0].ToString(); //assume LAN only has one, at least for now

		SetupListener();

		SI = new(); //so ip is empty (to catch self-broadcasts if later needed)

		this.TreeExiting += VanishIntoTheVoid;
	}

	//TODO: make equivalent for listener
	private void SetupListener()
	{
		Error status = listener.Bind(listenPort);
		switch (status)
		{
			case Error.Ok:
				GD.Print("LISTENER: Successfully bound to port " + listenPort);
				break;
			//Below are currenly not implemented in the Error enum
			/*case Error.AddressInUse:
				GD.Print("LISTENER ERROR: Address already in use");
				break;
			case Error.AddressNotAvailable:
				GD.Print("LISTENER ERROR: Address not available");
				break;*/
			case Error.InvalidParameter:
				GD.Print("LISTENER ERROR: Invalid parameter");
				break;
			case Error.Failed:
				GD.Print("LISTENER ERROR: Failed");
				break;
			default:
				GD.Print("LISTENER ERROR: Unknown error (error: " + status.ToString() + ")");
				break;
		}
		SetPortBindStatusMSG(true, status);
	}
	public void SetupBroadcast(string n, string d)
	{
		SI = new(Global.GetLocalIPAddresses()[0].ToString()/*assume LAN only has one, at least for now*/, n, d, 1, true);
		BeginBroadcast();
	}
	public void SetupBroadcast(ServerInfo s)
    {
		SI = s;
		BeginBroadcast();
    }

	private void BeginBroadcast()
	{
		broadcaster.SetBroadcastEnabled(true);
		broadcaster.SetDestAddress(broadcastAddress, listenPort);

		Error status = broadcaster.Bind(broadcastPort);
		switch (status)
		{
			case Error.Ok:
				GD.Print("BROADCASTER: Successfully bound to port " + broadcastPort);
				break;
			//Below are currenly not implemented in the Error enum
			/*case Error.AddressInUse:
				GD.Print("BROADCASTER ERROR: Address already in use");
				break;
			case Error.AddressNotAvailable:
				GD.Print("BROADCASTER ERROR: Address not available");
				break;*/
			case Error.InvalidParameter:
				GD.Print("BROADCASTER ERROR: Invalid parameter");
				break;
			case Error.Failed:
				GD.Print("BROADCASTER ERROR: Failed");
				break;
			default:
				GD.Print("BROADCASTER ERROR: Unknown error (error: " + status.ToString() + ")");
				break;
		}
		SetPortBindStatusMSG(false, status);
		listener.Close();

		AssignRandomCode();

		BroadcastOnInterval();
		broadcastTimer.Start();
	}

	public void StopBroadcast()
	{
		broadcastTimer.Stop();
		broadcaster.Close();
	}
	
	public void StopListening()
    {
		listener.Close();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!SI.setup)
        {
			if (listener.GetAvailablePacketCount() > 0)
			{
				byte[] packetData = listener.GetPacket();
				string serverIP = listener.GetPacketIP();
				int serverPort = listener.GetPacketPort();

				ServerInfo receivedSI = JsonSerializer.Deserialize<ServerInfo>(packetData.GetStringFromUtf16());

				if (IPAddress.TryParse(serverIP, out _))
				{
					//GD.Print("received packet from IP: " + serverIP + ":" + serverPort);
				}
				else
				{
					serverIP = receivedSI.ip;
					//GD.Print("received packet with invalid IP, using embedded IP: " + serverIP + ":" + serverPort + " (original: " + listener.GetPacketIP() + ")");
				}

				/* GD.Print("about to do debug (may crash in print statement)");
				GD.Print("Is local debug: " + Global.IsLocalIP(serverIP)); */
				//GD.Print(serverIP != SI.ip, canConnectToSelf, Global.IsLocalIP(serverIP));
				if ((serverIP != SI.ip || canConnectToSelf) && Global.IsLocalIP(serverIP))
				{
					// Remove any existing server with the same IP
					var existingCode = foundServersDict
						.FirstOrDefault(kvp => kvp.Value.ip == receivedSI.ip).Key;
					if (existingCode != null)
					{
						foundServersDict.Remove(existingCode);
						//GD.Print("(REPLACED)");
						receivedSI.lifetime = 0; //reset lifetime on update
					}

					// Add or update the server info by code
					foundServersDict[receivedSI.code] = receivedSI;
					//GD.Print("Discovered server: " + receivedSI.serverName + " at " + serverIP + ":" + serverPort + " | Code: " + receivedSI.code);
				}
				else if (serverIP == SI.ip)
				{
					GD.Print("Received packet from self, ignoring (canConnectToSelf=" + canConnectToSelf + ")");
				}
				else
				{
					GD.Print("Received packet from non-local IP: " + serverIP + ", ignoring");
				}
			}

			//Clean up expired servers (not heard from in 5 seconds)
			//TODO: implement refresh or delete upon unsuccessful connection attempt
			foreach (var key in foundServersDict.Keys.ToList())
			{
				foundServersDict[key].lifetime += (float)delta;
				if (foundServersDict[key].lifetime > timeoutInterval)
				{
					foundServersDict.Remove(key);
					GD.Print("Removed expired server with code: " + key);
				}
			}
		} else {
            SI.lifetime += (float)delta;
        }
	}

	private void BroadcastOnInterval()
	{
		if (!SI.setup)
		{
			GD.Print("ERROR: ServerInfo not set up, cannot broadcast");
			return;
		} else if (SI.playerCount >= 4)
        {
			GD.Print("Stopped broadcasting: 4 (or more?) players");
			return;
        }

		//GD.Print("Broadcasting game data");
		//assume serverinfo has been correctly set by whatever object
		//owns this LANMultiplayer object

		AssignRandomCode();

		SI.lifetime = 0; //reset (broadcasting again, so freshen up)

		//send it (NOW with code)
		string broadcastJSON = JsonSerializer.Serialize(SI);
		byte[] packet = broadcastJSON.ToUtf16Buffer(); //TODO: figure out the correct encoding and not just guess

		if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:broadcast_info"]) GD.Print("Broadcast info || name: " + SI.serverName + " | Code: " + SI.code);

		broadcaster.PutPacket(packet);

		broadcastTimer.Start();
	}
	private void AssignRandomCode()
	{
		List<byte> unusedCodes = new(CodePicker);
		foreach (string s in foundServersDict.Keys)
		{
			unusedCodes.Remove(byte.Parse(s));
		}
		//asign random unused code (not to be confused with like 20% of this project)
		if (SI.code == "")
		{
			Random rand = new();
			if (unusedCodes.Count == 0)
			{
				GD.Print("ERROR: Somehow 256 servers are broadcasting on the same LAN (???), cannot assign unique code");

				//fallback to random code (may cause conflicts, likely very long, but whtvs)
				SI.code = rand.Next(255, int.MaxValue).ToString();
			}
			else
			{
				SI.code = unusedCodes[rand.Next(0, 255)].ToString();
			}
		}
	}

	public ServerInfo GetSIFromCode(string code)
	{
		return foundServersDict[code];
	}

	public string GetServerIPFromCode(string code)
	{
		try
		{
			ServerInfo s = foundServersDict[code];
			return s.ip;
		}
		catch (KeyNotFoundException)
		{
			GD.Print("ERROR: No server found with code " + code + ". available codes: " + String.Join(", ", foundServersDict.Keys.ToArray()));
			return "";
		}
		catch (Exception ex)
		{
			GD.PrintErr("ERROR: could not server IP for code " + code + ": " + ex.Message);
			return "";
		}
	}
	public string[] GetActiveCodes()
	{
		return foundServersDict.Keys.ToArray();
	}

	private void SetPortBindStatusMSG(bool listener, Error status)
	{
		if (listener)
		{
			bindStatusL = status.ToString();
		}
		else
		{
			bindStatusB = status.ToString();
		}
		bindStatusLabel.Text = "Listener Bind Status: " + bindStatusL + " | Broadcaster Bind Status: " + bindStatusB;
	}

	public void VanishIntoTheVoid()
	{
		broadcastTimer.Stop();
		listener.Close();
		SetPortBindStatusMSG(true, Error.Ok);
		broadcaster.Close();
		SetPortBindStatusMSG(false, Error.Ok);
    }
}
