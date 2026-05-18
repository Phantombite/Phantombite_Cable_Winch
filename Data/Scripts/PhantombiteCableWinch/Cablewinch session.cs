using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace Phantombite.CableWinch
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CableWinchSession : MySessionComponentBase
    {
        private const long   CORE_CHANNEL     = 1995000L;
        private const long   MY_CHANNEL       = 1995002L;
        private const long   LOG_CHANNEL      = 1995999L;
        private const string MOD_NAME         = "Phantombite_CableWinch";
        private const string VERSION          = "1.0.0";
        private const ushort PERFLEVEL_PACKET = 19502;

        private int _logLevel = 0;

        public override void LoadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PERFLEVEL_PACKET, OnPerfLevelPacket);
                if (MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.RegisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                Log("LoadData — warte auf Core READY");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.CableWinch] [ERROR] LoadData: " + ex);
            }
        }

        private void OnCoreMessage(object data)
        {
            try
            {
                string msg = data as string;
                if (string.IsNullOrEmpty(msg)) return;

                if (msg == "READY")
                {
                    SendRegister();
                    Log("READY empfangen — REGISTER gesendet");
                    return;
                }
                if (msg.StartsWith("LOGLEVEL|"))
                {
                    int lvl;
                    if (int.TryParse(msg.Substring(9), out lvl))
                        _logLevel = Math.Max(0, Math.Min(3, lvl));
                    Log("LOGLEVEL gesetzt: " + _logLevel, 1);
                    return;
                }
                if (msg.StartsWith("PERFLEVEL|"))
                {
                    int lvl;
                    if (int.TryParse(msg.Substring(10), out lvl))
                    {
                        lvl = Math.Max(0, Math.Min(3, lvl));
                        CableWinchLogic.PerfLevel = lvl;
                        Log("PERFLEVEL gesetzt: " + lvl + (lvl >= 3 ? " — Visualisierung AUS" : " — Visualisierung AN"));
                        MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, "PERFACK|cablewinch|" + lvl);
                        if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
                            MyAPIGateway.Multiplayer.SendMessageToOthers(PERFLEVEL_PACKET, new byte[] { (byte)lvl });
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.CableWinch] [ERROR] OnCoreMessage: " + ex);
            }
        }

        private void OnPerfLevelPacket(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0) return;
                CableWinchLogic.PerfLevel = Math.Max(0, Math.Min(3, (int)data[0]));
                Log("PERFLEVEL vom Server empfangen: " + CableWinchLogic.PerfLevel, 1);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.CableWinch] [ERROR] OnPerfLevelPacket: " + ex);
            }
        }

        private void SendRegister()
        {
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                "REGISTER|cablewinch|Phantombite CableWinch|" + VERSION + "|" + MY_CHANNEL);
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, "PERFACK|cablewinch|0");
        }

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                if (MyAPIGateway.Multiplayer != null)
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(PERFLEVEL_PACKET, OnPerfLevelPacket);
                CableWinchLogic.PerfLevel = 0;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.CableWinch] [ERROR] UnloadData: " + ex);
            }
        }

        private void Log(string msg, int level = 0)
        {
            if (level > _logLevel) return;
            try
            {
                MyLog.Default.WriteLineAndConsole("[PB.CableWinch] " + msg);
                MyAPIGateway.Utilities.SendModMessage(LOG_CHANNEL,
                    "LOG|" + MOD_NAME + "|" + level + "|CableWinch_Session|" + msg);
            }
            catch { }
        }
    }
}