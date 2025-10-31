using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public partial class Global : Node
{
    public static string name;
    public static string desc;
    public static int faceIndex = 0;
    public static int swordIndex = 0;
    public static int shieldIndex = 0;
    public static int fistIndex = 0;


    public static int wonGames;
    public static int lostGames;

    public static int points;

    public static float[] volSliderValues;
    public static string savePath = "user://savegame.save";

    public const int sfxNum = 12;

    public static AudioStream[] sfx = new AudioStream[sfxNum];
    public static string[] sfxPaths = new string[sfxNum] { "click", "coinEnd", "coinStart",
    "megahit1", "megahit2", "punch1", "punch2", "shieldBlock", "swordClash", "swordClash2",
    "swordClash3", "swordHit"}; //NOTE: DOES NOT WORK FOR NON-MP3s!!!


    public static Color[] colors = new Color[4] { new(1, 0, 0), new(0, 0, 1), new(1, 1, 0), new(0, 1, 1) };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        for (int i = 0; i < sfx.Length; i++)
        {
            sfx[i] = (AudioStream)GD.Load("res://SFX/" + sfxPaths[i] + ".mp3");
        }
    }

    public static void LoadCharData()
    {
        if (FileAccess.FileExists(savePath))
        {
            FileAccess saveGame = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);

            string jsonString = saveGame.GetLine();
            Godot.Collections.Dictionary<string, Variant> data = new((Godot.Collections.Dictionary)Json.ParseString(jsonString));

            //NOTE: make this a method or something
            name = (string)GetSavedData("name", data);
            desc = (string)GetSavedData("desc", data);
            faceIndex = (int)GetSavedData("faceIndex", data);
            swordIndex = (int)GetSavedData("swordIndex", data);
            shieldIndex = (int)GetSavedData("shieldIndex", data);
            fistIndex = (int)GetSavedData("fistIndex", data);

            wonGames = (int)GetSavedData("wonGames", data);
            lostGames = (int)GetSavedData("lostGames", data);

            points = (int)GetSavedData("points", data);

            volSliderValues = (float[])GetSavedData("volSliderValues", data);

        }
        else
        {
            GD.Print("No file loaded, doofus! (Global @ LoadCharData)");

            name = "Player";
            desc = "A player of Potato Warriors";
            faceIndex = 0;
            swordIndex = 0;
            shieldIndex = 0;
            fistIndex = 0;

            wonGames = 0;
            lostGames = 0;

            points = 0;

            volSliderValues = Array.Empty<float>();
        }
    }
    /// <summary>
    /// returns a JSON-esque dictionary of character data
    /// </summary>
    /// <returns></returns>
    private static Godot.Collections.Dictionary<string, Variant> SaveCharData()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
        {
            { "name", name },
            { "desc", desc },
            { "faceIndex", faceIndex },
            { "swordIndex", swordIndex },
            { "shieldIndex", shieldIndex },
            { "fistIndex", fistIndex},
            { "wonGames", wonGames },
            { "lostGames", lostGames },
            { "points", points },
            { "volSliderValues", volSliderValues },
        };
    }
    /// <summary>
    /// Safer way to get a value from a just-loaded character info dictionary
    /// </summary>
    /// <param name="key"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    private static Variant GetSavedData(string key, Godot.Collections.Dictionary<string, Variant> d)
    {
        Variant ret;
        bool success;
        success = d.TryGetValue(key, out ret);
        if (!success)
        {
            GD.Print("somethin' went wrong (Global.cs @ GetSavedData | " + key + ")");
        }
        return ret;
    }
    /// <summary>
    /// take the current info in Global, and write it to the local save file
    /// </summary>
    public static void StoreData()
    {
        FileAccess file = FileAccess.Open(Global.savePath, FileAccess.ModeFlags.Write);

        string jsonString = Json.Stringify(SaveCharData());

        file.StoreLine(jsonString);
        //VERY IMPORTANT: DO NOT DELETE THE LINE BELOW
        //I spent 3 FREAKING HOURS on this because you apparently need to MANUALLY CLOSE
        //the file before reading from it, because nobody thought to automate that, apparently.
        file.Close();
    }
    //because, for multiplayer, the local files and stuff have to be accessed through each individual script
    //as such, this method can be called on the player (not the server) to get the properties
    public static Variant? GetLocalPlayerProperty(string prop)
    {
        LoadCharData();
        switch (prop)
        {
            case "wonGames":
                return wonGames;
            case "faceIndex":
                return faceIndex;
            case "swordIndex":
                return swordIndex;
            case "shieldIndex":
                return shieldIndex;
            case "fistIndex":
                return fistIndex;
            case "name":
                return name;
            case "desc":
                return desc;
            default:
                GD.Print("ERROR: No good property given! (Global.cs @ GetLocalPlayerProperty())");
                return null;
        }
    }
    public static Vector2 GetScreenCoordinates(Node2D node)
    {
        return node.GetGlobalTransformWithCanvas().Origin;
    }

    //get multicast boradcast address
    //This is where i used to literally "encyrpt/decrpyt" ips (scramble them into a code) lol, probably shouldn't have done that
    //probably wouldn't have even worked
    public static UnicastIPAddressInformationCollection GetUnicastIPInfos()
    {
        List<UnicastIPAddressInformationCollection> ipCollections = new();
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                ni.GetIPProperties().UnicastAddresses.Count > 0 &&
                ni.GetIPProperties().GatewayAddresses.Count > 0) //ideally picks only the active network interface
            {
                //WARNING: If connected to multiple different networks (Ethernet to one, Wi-fi to another, etc.)
                //then this will only give the broadcast address for one of them! I think
                ipCollections.Add(ni.GetIPProperties().UnicastAddresses);
            }
        }
        return ipCollections[0]; // fallback
    }
    public static IPAddress[] GetLocalIPAddresses()
    {
        var ipList = new List<IPAddress>();
        var infos = GetUnicastIPInfos();
        if (infos != null)
        {
            foreach (UnicastIPAddressInformation ip in infos)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork) //should only be one, should be the LAN
                {
                    ipList.Add(ip.Address);
                }
                else if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    //ipList.Add(ip.Address); //<-- uncomment when supported elsewhere (i. e., multicast formulated)
                    //NOTE: IPv6 may already be supported, due to how this function is used to get the local address for other clients on the LAN, and thus might not need to be IPv4 only
                } else
                {
                    GD.Print("Found Local Address of Unsupported Family: " + ip.Address.ToString() + " (Family: " + ip.Address.AddressFamily.ToString() + ")");
                }
            }
        }
        return ipList.Count > 0 ? ipList.ToArray() : new IPAddress[] { IPAddress.Loopback };
    }
    public static IPAddress GetBroadcastAddress()
    {
        foreach (UnicastIPAddressInformation ip in GetUnicastIPInfos())
        {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] ipBytes = ip.Address.GetAddressBytes();
                byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();
                byte[] broadcastBytes = new byte[ipBytes.Length];

                for (int i = 0; i < ipBytes.Length; i++)
                {
                    broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
                }
                GD.Print("Broadcast Address: " + new IPAddress(broadcastBytes).ToString() + " (from " + ip.ToString() + ")");
                return new IPAddress(broadcastBytes);
            } else if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                //TODO: Implement multicast?
            }
        }
        return IPAddress.Broadcast; // fallback
    }

    public static bool IsLocalIP(string check, bool strictNotLoopback = false, bool strictNotSameDevice = false)
    {
        bool isLocal = false;
        foreach (UnicastIPAddressInformation ip in GetUnicastIPInfos())
        {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                IPAddress convertedCheck = IPAddress.Parse(check);
                if ((convertedCheck.Equals(ip.Address) && !strictNotSameDevice) || (convertedCheck.Equals(IPAddress.Loopback) && !strictNotLoopback))
                {
                    return true;
                }

                byte[] checkBytes = IPAddress.Parse(check).GetAddressBytes();
                byte[] localIPBytes = ip.Address.GetAddressBytes();
                byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();

                bool same = true;
                for (int i = 0; i < checkBytes.Length; i++)
                {
                    same &= ((checkBytes[i] ^ localIPBytes[i]) & maskBytes[i]) == 0;
                }
                isLocal |= same;
            }
        }
        return isLocal;
    }

    public static float RandFloat(float min, float max)
    {
        Random ra = new();
        return (float)(min + (ra.NextDouble() * (max - min)));
    }
}
