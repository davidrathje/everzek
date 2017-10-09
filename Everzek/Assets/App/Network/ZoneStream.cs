using System;
using System.Collections.Generic;
using System.Text;
using static OpenEQ.Network.Utility;
using UnityEngine;

namespace OpenEQ.Network {
    public class ZoneStream : EQStream {
        string charName;
        bool entering = true;


        public event EventHandler<Spawn> ZoneEntry;
        public event EventHandler<DeleteSpawn> DeleteSpawn;
        public event EventHandler<ChannelMessage> ChannelMessage;
        public event EventHandler<SpawnHPUpdate> SpawnHPUpdate;
        public event EventHandler<SpawnPositionUpdate> SpawnPositionUpdate;
        public event EventHandler<PlayerPositionUpdateServer> PlayerPositionUpdateServer;

        public ZoneStream(string host, int port, string charName) : base(host, port) {
            SendKeepalives = true;
            this.charName = charName;
            
            UnityEngine.Debug.Log("Starting zone connection...");
            Connect();
            SendSessionRequest();
        }

        protected override void HandleSessionResponse(Packet packet) {
            Send(packet);

            Send(AppPacket.Create(ZoneOp.ZoneEntry, new ClientZoneEntry(charName)));
        }

        //Camp requests have a 29000ms camp timer
        public void SendCamp()
        {
           Send(AppPacket.Create(ZoneOp.Camp));
        }

        public void SendMessage()
        {
            Send(AppPacket.Create(ZoneOp.ChannelMessage, new ChannelMessage("Shin", "Xuluu", 0, 0, 0, "Hello")));
        }

        protected override void HandleAppPacket(AppPacket packet) {
            switch((ZoneOp) packet.Opcode) {
                case ZoneOp.PlayerProfile:
                    var player = packet.Get<PlayerProfile>();
                   UnityEngine.Debug.Log("Profile:" +player);
                    break;

                case ZoneOp.CharInventory:
                  //  var inventory = packet.Get<CharInventory>();
                  //  UnityEngine.Debug.Log("Inventory: "+inventory);
                    break;

                case ZoneOp.TimeOfDay:
                  //  var timeofday = packet.Get<TimeOfDay>();
                  //  UnityEngine.Debug.Log(timeofday);
                    break;

                case ZoneOp.TaskActivity:
                  //  var activity = packet.Get<TaskActivity>();
                //    UnityEngine.Debug.Log(activity);
                    break;

                case ZoneOp.TaskDescription:
                  //  var desc = packet.Get<TaskDescription>();
                //    UnityEngine.Debug.Log(desc);
                    break;

                case ZoneOp.CompletedTasks:
                  //  var comp = packet.Get<CompletedTasks>();
                 //   UnityEngine.Debug.Log(comp);
                    break;

                case ZoneOp.XTargetResponse:
                   // var xt = packet.Get<XTarget>();
                 //   UnityEngine.Debug.Log(xt);
                    break;

                case ZoneOp.Weather:
                   // var weather = packet.Get<Weather>();
                //    UnityEngine.Debug.Log(weather);

                    if(entering)
                        Send(AppPacket.Create(ZoneOp.ReqNewZone));
                    break;

                case ZoneOp.TributeTimer:
                    //var timer = packet.Get<TributeTimer>();
                 //   UnityEngine.Debug.Log(timer);
                    break;

                case ZoneOp.TributeUpdate:
                   // var update = packet.Get<TributeInfo>();
                 //   UnityEngine.Debug.Log(update);
                    break;

                case ZoneOp.ZoneEntry:
                    var mob = packet.Get<Spawn>();
                    ZoneEntry.Invoke(this, mob);
                    //UnityEngine.Debug.Log(mob);
                    break;

                case ZoneOp.NewZone:
                    Send(AppPacket.Create(ZoneOp.ReqClientSpawn));

                    break;

                case ZoneOp.SendExpZonein:
                    if(packet.Data.Length == 0) {
                        Send(AppPacket.Create(ZoneOp.ClientReady));
                        entering = false;
                    }
                    break;

                case ZoneOp.SendFindableNPCs:
                  //  var npc = packet.Get<FindableNPC>();
                 //   UnityEngine.Debug.Log(npc);
                    break;

                case ZoneOp.ClientUpdate:
                    UnityEngine.Debug.Log("Sending Client Update");
                    PlayerPositionUpdateServer.Invoke(this, packet.Get<PlayerPositionUpdateServer>());
                    break;
                case ZoneOp.SpawnAppearance:
                    break;
                case ZoneOp.Stamina:
                    break;
                case ZoneOp.SpecialMesg:
                    break;
                case ZoneOp.Death:
                    break;
                case ZoneOp.DeleteSpawn:
                    DeleteSpawn.Invoke(this, packet.Get<DeleteSpawn>());
                    break;
                case ZoneOp.PlayerStateAdd:
                    break;
                case ZoneOp.PlayerStateRemove:
                    break;
                case ZoneOp.ChannelMessage:
                    ChannelMessage.Invoke(this, packet.Get<ChannelMessage>());
                    break;

                case ZoneOp.HPUpdate:
                    SpawnHPUpdate.Invoke(this, packet.Get<SpawnHPUpdate>());
                    break;
                case ZoneOp.ManaUpdate:
                    break;
                case ZoneOp.EnduranceUpdate:
                    break;
                case ZoneOp.SpawnPositionUpdate:
                    SpawnPositionUpdate.Invoke(this, packet.Get<SpawnPositionUpdate>());
                    break;
                case ZoneOp.BuffCreate:
                    break;
                case ZoneOp.AltCurrency:
                    break;
                case ZoneOp.WearChange:
                    break;
                case ZoneOp.GuildMOTD:
                    break;
                case ZoneOp.RaidUpdate:
                    break;
                case ZoneOp.ExpUpdate:
                    break;
                case ZoneOp.WorldObjectsSent:
                    break;
                case 0x0:
                    //This is a catch for an empty OP that happens.. dunno
                    break;
                case ZoneOp.SendAAStats:
                    break;
                case ZoneOp.SendZonepoints:
                    break;
                case ZoneOp.GroundSpawn:
                    break;
                case ZoneOp.SpawnDoor:
                    break;
                default:
                    UnityEngine.Debug.Log($"Unhandled packet in ZoneStream: {(ZoneOp) packet.Opcode} (0x{packet.Opcode:X04})");
                    Hexdump(packet.Data);
                    break;
            }
        }
    }
}
