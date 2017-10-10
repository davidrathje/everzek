using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OpenEQ.Network;

public class TestNetwork : MonoBehaviour {

    LoginStream login;
    WorldStream world;
    ZoneStream zone;

    public GameObject spawnPrefab;
    public GameObject spawnPool;

    bool isInitialized = false;

    public CharacterSelectEntry characterSelected;
    public ServerListElement serverSelected;

    public string LoginUrl = "login.eqemulator.net";
    public int LoginPort = 5998;
    public string Username;
    public string Password;
    public string ServerToJoin = "[R] RebuildEQ.com - Check forums to be a tester!";
    //or, [R] Shin's RebuildEQ TEST Server
    public string CharacterName = "";
    

    public Dictionary<uint, GameObject> SpawnPool = new Dictionary<uint, GameObject>();
    public List<Spawn> SpawnQueue = new List<Spawn>();
    public List<DeleteSpawn> SpawnDeleteQueue = new List<DeleteSpawn>();
    public List<SpawnHPUpdate> SpawnHPUpdateQueue = new List<SpawnHPUpdate>();
    public List<SpawnPositionUpdate> SpawnPositionUpdateQueue = new List<SpawnPositionUpdate>();
    public List<PlayerPositionUpdateServer> PlayerPositionUpdateServerQueue = new List<PlayerPositionUpdateServer>();

    // Use this for initialization
    void Start () {

        if (ServerToJoin.Length == 0)
        {
            Debug.LogError("Cannot join empty server! Exiting");
            return;
        }
        login = new LoginStream(LoginUrl, LoginPort);
        Debug.Log("Logging in..");
        
        login.Login(Username, Password);
        login.LoginSuccess += OnLoginSuccess;
        login.ServerList += OnServerList;
        login.PlaySuccess += OnPlaySuccess;
    }

    void OnLoginSuccess(object sender, bool success)
    {
        if (!success)
        {
            Debug.LogError("Login failed.");
            return;
        }
        
        Debug.Log("Login successful. Retrieving server list.");
        login.RequestServerList();
    }

    void OnServerList(object sender, List<ServerListElement> list)
    {
        Debug.Log("Server List");
        //authSection.Visibility = Visibility.Hidden;
        //serverListSection.Visibility = Visibility.Visible;

        // Sort this list first.
        list = list.OrderByDescending(a => a.ServerListID).ThenBy(b => b.Status).ThenByDescending(c => c.PlayersOnline).ThenBy(d => d.Longname).ToList();

        //var i = 0;
       
        foreach (var server in list)
        {
            //Debug.Log(server.Longname + " - " + server.WorldIP);
            if (server.Longname.Equals(ServerToJoin))
            {
                serverSelected = server;
                Debug.Log("Joining " + ServerToJoin + server);
                /*if (serverSelected.WorldIP.Equals("127.0.0.1")) {
                    Debug.Log("Changing world IP since it's localhost");
                    serverSelected.WorldIP = "192.168.1.100";
                }*/
                login.Play(serverSelected);
            }
            //Debug.Log(server.Longname);
            /*serverGrid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 25));
            var namefield = new TextBlock { Text = server.Longname, Font = serverNameHeader.Font, TextSize = serverNameHeader.TextSize, TextColor = serverNameHeader.TextColor };
            namefield.SetGridColumn(0);
            namefield.SetGridRow(i);
            serverGrid.Children.Add(namefield);
            var statusfield = new TextBlock { Text = server.Status == ServerStatus.Up ? server.PlayersOnline.ToString() : "Down", Font = serverNameHeader.Font, TextSize = serverNameHeader.TextSize, TextColor = serverNameHeader.TextColor };
            statusfield.SetGridColumn(1);
            statusfield.SetGridRow(i);
            serverGrid.Children.Add(statusfield);
            var serverButton = new Button { MouseOverImage = loginButton.MouseOverImage, NotPressedImage = loginButton.NotPressedImage, PressedImage = loginButton.PressedImage };
            var buttonLabel = new TextBlock { Text = "Play", Font = serverNameHeader.Font, TextSize = 8, TextColor = serverNameHeader.TextColor, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            serverButton.Content = buttonLabel;
            serverButton.SetGridColumn(2);
            serverButton.SetGridRow(i++);
            serverGrid.Children.Add(serverButton);
            serverButton.Click += (s, e) => {
                ((Button)s).IsEnabled = false;
                Console.UnityEngine.Debug.Log($"Sending play request for {server.Longname}");
                login.Play(server);
            };*/
        }
    }

    void OnPlaySuccess(object sender, ServerListElement? server)
    {
        Debug.Log("Play Response: " + server);
        if (isInitialized) return;
        if (server == null)
        {
            Debug.LogError("No server responded, likely a malformed request");
            return;
        }
        world = new WorldStream(server.Value.WorldIP, 9000, login.accountID, login.sessionKey);
        //SceneSystem.SceneInstance.Scene = Content.Load<Scene>("WorldScene");
        //var wui = SceneSystem.SceneInstance.Scene.Entities.Where(x => x.Name == "WorldDialog").First();
        /*foreach (var comp in wui.Components)
            if (comp is WorldScript)
                ((WorldScript)comp).world = new WorldStream(server.Value.WorldIP, 9000, login.accountID, login.sessionKey);*/

        InitializeWorld();
    }

    private void OnDestroy()
    {
        Debug.Log("Cleaning up subscriptions on exit");
        if (login != null)
        {
            login.PlaySuccess -= OnPlaySuccess;
            login.LoginSuccess -= OnLoginSuccess;
            login.ServerList -= OnServerList;

            login.Disconnect();
        }
        login = null;

        if (world != null)
        {
            world.CharacterList -= OnCharacterList;
            world.ChatServerList -= OnChatServerList;
            world.ZoneServer -= OnZoneServer;

            world.Disconnect();
        }
        world = null;

        if (zone != null)
        {
            //try to camp out
            zone.SendCamp();

            zone.ZoneEntry -= OnZoneEntry;
            zone.DeleteSpawn -= OnDeleteSpawn;
            zone.ChannelMessage -= OnChannelMessage;
            zone.SpawnHPUpdate -= OnSpawnHPUpdate;
            zone.SpawnPositionUpdate -= OnSpawnPositionUpdate;
            zone.PlayerPositionUpdateServer -= OnPlayerPositionUpdateServer;
            zone.Disconnect();
        }
        zone = null;        
    }

    void OnZoneEntry(object sender, Spawn spawn)
    {
        SpawnQueue.Add(spawn);
    }

    void OnSpawnPositionUpdate(object sender, SpawnPositionUpdate spawnPositionUpdate)
    {
        SpawnPositionUpdateQueue.Add(spawnPositionUpdate);
    }

    void OnDeleteSpawn(object sender, DeleteSpawn spawn)
    {
        SpawnDeleteQueue.Add(spawn);        
    }
    
    void OnChannelMessage(object sender, ChannelMessage message)
    {
        Debug.Log("ChannelMessage from "+message.Sender + " to "+message.TargetName+" in chan " + message.ChannelNumber + ": " + message.Message);
    }


    void OnSpawnHPUpdate(object sender, SpawnHPUpdate hpUpdate)
    {
        SpawnHPUpdateQueue.Add(hpUpdate);
    }

    void OnPlayerPositionUpdateServer(object sender, PlayerPositionUpdateServer posUpdate)
    {
        PlayerPositionUpdateServerQueue.Add(posUpdate);
    }

    private void Update()
    {
        if (zone == null) return;

        for (int i = SpawnQueue.Count-1; i >= 0; i--)
        {
            var spawn = SpawnQueue[i];
            if (SpawnPool.ContainsKey(spawn.SpawnID))
            {
                //update previous entry
                continue;
            }
            var obj = Instantiate(spawnPrefab, new Vector3(spawn.Position.X/1000, spawn.Position.Z / 1000, spawn.Position.Y / 1000), Quaternion.Euler(0, spawn.Position.Heading, 0), spawnPool.transform);
            var npc = obj.GetComponent<NPC>();
            npc.spawnData = spawn;
            
            obj.name = "[" + spawn.Level + "] "+ spawn.CharType +" "+ spawn.Name + " "+((spawn.CharType == CharType.NPC) ? spawn.CurHP+"%" : "100%");
            SpawnPool.Add(spawn.SpawnID, obj);
            SpawnQueue.RemoveAt(i);
        }

        for (int i = PlayerPositionUpdateServerQueue.Count - 1; i >= 0; i--)
        { 
            var posUpdate = PlayerPositionUpdateServerQueue[i];
        
            if (!SpawnPool.ContainsKey((uint)posUpdate.SpawnID)) continue;
            var spawn = SpawnPool[(uint)posUpdate.SpawnID];            
            Debug.Log("Updating position on " + spawn.name + "via ClientUpdate");
            spawn.transform.position = new Vector3(posUpdate.X, posUpdate.Z, posUpdate.Y);
            //var npc = spawn.GetComponent<NPC>();
            //npc.spawnData.Position = posUpdate.Position;
            PlayerPositionUpdateServerQueue.RemoveAt(i);
        }

        for (int i = SpawnDeleteQueue.Count - 1; i >= 0; i--)
        {
            var spawn = SpawnDeleteQueue[i];
            if (!SpawnPool.ContainsKey(spawn.SpawnID)) continue;
            Debug.Log("Deleting spawn " + SpawnPool[spawn.SpawnID].name + " ( do puff effect? " + spawn.Decay + ")");
            GameObject.Destroy(SpawnPool[spawn.SpawnID]);
            SpawnPool.Remove(spawn.SpawnID);
            SpawnDeleteQueue.RemoveAt(i);
        }

        for (int i = SpawnHPUpdateQueue.Count - 1; i >= 0; i--)
        {
            var hpUpdate = SpawnHPUpdateQueue[i];
            if (!SpawnPool.ContainsKey((uint)hpUpdate.SpawnID)) continue;
            var spawn = SpawnPool[(uint)hpUpdate.SpawnID];
            var npc = spawn.GetComponent<NPC>();
            Debug.Log("Updating hp on "+spawn.name+" from "+npc.spawnData.CurHP+" to "+hpUpdate.CurrentHP+" curhp, "+hpUpdate.MaxHP+" maxhp");
            SpawnHPUpdateQueue.RemoveAt(i);
        }

        for (int i = SpawnPositionUpdateQueue.Count - 1; i >= 0; i--)
        {
            var posUpdate = SpawnPositionUpdateQueue[i];
            if (!SpawnPool.ContainsKey((uint)posUpdate.SpawnID)) continue;
            var spawn = SpawnPool[(uint)posUpdate.SpawnID];
            var npc = spawn.GetComponent<NPC>();
            Debug.Log("Updating position on " + spawn.name);
            spawn.transform.position = new Vector3(posUpdate.Position.X, posUpdate.Position.Z, posUpdate.Position.Y);
            npc.spawnData.Position = posUpdate.Position;
            SpawnPositionUpdateQueue.RemoveAt(i);
        }
        
    }

    void OnCharacterList(object sender, List<CharacterSelectEntry> chars)
    {
        //var i = 0;
        Debug.Log("Character list");
        foreach (var character in chars)
        {
            if (isInitialized) return;
            // Debug.Log(character.Name);
            if (character.Name == CharacterName)
            {
                isInitialized = true;
                Debug.Log("Found character " + CharacterName + ", class " + character.Class + ", ready to enter world.");
                characterSelected = character;              
            }
            /* charGrid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 25));
             var namefield = new TextBlock
             {
                 Text = character.Name,
                 Font = charNameHeader.Font,
                 TextSize = charNameHeader.TextSize,
                 TextColor = charNameHeader.TextColor
             };
             namefield.SetGridColumn(0);
             namefield.SetGridRow(i);
             charGrid.Children.Add(namefield);

             var classfield = new TextBlock
             {
                 Text = ((ClassTypes)character.Class).GetClassName(),
                 Font = charNameHeader.Font,
                 TextSize = charNameHeader.TextSize,
                 TextColor = charNameHeader.TextColor
             };
             classfield.SetGridColumn(1);
             classfield.SetGridRow(i);
             charGrid.Children.Add(classfield);

             var levelfield = new TextBlock
             {
                 Text = character.Level.ToString(),
                 Font = charNameHeader.Font,
                 TextSize = charNameHeader.TextSize,
                 TextColor = charNameHeader.TextColor
             };
             levelfield.SetGridColumn(2);
             levelfield.SetGridRow(i);
             charGrid.Children.Add(levelfield);

             var serverButton = new Button { MouseOverImage = buttonCreateCharacter.MouseOverImage, NotPressedImage = buttonCreateCharacter.NotPressedImage, PressedImage = buttonCreateCharacter.PressedImage };
             var buttonLabel = new TextBlock { Text = "Play", Font = charNameHeader.Font, TextSize = 8, TextColor = charNameHeader.TextColor, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
             serverButton.Content = buttonLabel;
             serverButton.SetGridColumn(3);
             serverButton.SetGridRow(i++);
             charGrid.Children.Add(serverButton);
             serverButton.Click += (s, e) => {
                 ((Button)s).IsEnabled = false;
                 world.ResetAckForZone();
                 world.SendEnterWorld(new EnterWorld
                 {
                     Name = character.Name,
                     GoHome = false,
                     Tutorial = false
                 });
             };
             */
        }
    }

    void OnChatServerList(object sender, byte[] chatBytes)
    {
            Debug.Log("Added chatserver");
            world.ChatServers.Add(new OpenEQ.Chat.ChatServer(chatBytes));
    }


    void OnZoneServer(object sender, ZoneServerInfo server)
    {
            //UnityEngine.Debug.Log($"Zone server info: {server.Host}:{server.Port}");

            Debug.Log("Connecting to zone " + server.Host + ", " + server.Port + ", as character " + CharacterName);
            zone = new ZoneStream(server.Host, server.Port, CharacterName);
            InitializeZone();
    }

    public void DoWorldJoin()
    {
        if (world == null)
        {
            Debug.LogError("World isn't ready to join yet");
            return;
        }

        //world.ResetAckForZone();
        world.SendEnterWorld(new EnterWorld
        {
            Name = characterSelected.Name,
            GoHome = false,
            Tutorial = false
        });
        Debug.Log("Sending EnterWorld event...");
    }

    private void InitializeWorld()
    {
        world.CharacterList += OnCharacterList;
        world.ChatServerList += OnChatServerList;
        world.ZoneServer += OnZoneServer;
    }

    void InitializeZone()
    {
        zone.ZoneEntry += OnZoneEntry;
        zone.DeleteSpawn += OnDeleteSpawn;
        zone.ChannelMessage += OnChannelMessage;
        zone.SpawnHPUpdate += OnSpawnHPUpdate;
        zone.SpawnPositionUpdate += OnSpawnPositionUpdate;
        zone.PlayerPositionUpdateServer += OnPlayerPositionUpdateServer;
    }
    
	
}
 