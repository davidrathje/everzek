﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OpenEQ.Network;

public class TestNetwork : MonoBehaviour {

    LoginStream login;
    WorldStream world;
    ZoneStream zone;
    bool isInitialized = false;


    public string LoginUrl = "login.eqemulator.net";
    public int LoginPort = 5998;
    public string Username;
    public string Password;
    public string ServerToJoin = "[R] RebuildEQ.com - Check forums to be a tester!";    
    public string CharacterName = "";


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
            Debug.Log(server.Longname);
            if (server.Longname.Equals(ServerToJoin))
            {
                Debug.Log("Joining " + ServerToJoin);
                login.Play(server);
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
                Console.WriteLine($"Sending play request for {server.Longname}");
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
        }
        login = null;

        if (world != null)
        {
            world.CharacterList -= OnCharacterList;
            world.ChatServerList -= OnChatServerList;
            world.ZoneServer -= OnZoneServer;
        }
        world = null;
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
                Debug.Log("Found character " + CharacterName + ", class " + character.Class + ", entering world...");
                world.ResetAckForZone();
                world.SendEnterWorld(new EnterWorld
                {
                    Name = CharacterName,
                    GoHome = false,
                    Tutorial = false
                });
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
            //WriteLine($"Zone server info: {server.Host}:{server.Port}");

            Debug.Log("Connecting to zone " + server.Host + ", " + server.Port + ", as character " + CharacterName);
            zone = new ZoneStream(server.Host, server.Port, CharacterName);
            InitializeZone();
    }

    private void InitializeWorld()
    {
        world.CharacterList += OnCharacterList;
        world.ChatServerList += OnChatServerList;
        world.ZoneServer += OnZoneServer;
    }

    void InitializeZone()
    {
    }
    
	
}
 