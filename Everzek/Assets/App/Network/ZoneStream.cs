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
                  // UnityEngine.Debug.Log("ClientUpdate");
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
                    break;

                case ZoneOp.PlayerStateAdd:
                    break;
                case ZoneOp.PlayerStateRemove:
                    break;
                case ZoneOp.ChannelMessage:
                    break;

                case ZoneOp.HPUpdate:
                 //   UnityEngine.Debug.Log("HpUpdate");
                    break;
                case ZoneOp.ManaUpdate:
                    break;
                case ZoneOp.EnduranceUpdate:
                    break;
                default:
                    UnityEngine.Debug.Log($"Unhandled packet in ZoneStream: {(ZoneOp) packet.Opcode} (0x{packet.Opcode:X04})");
                    Hexdump(packet.Data);
                    break;
            }
        }
    }
}
