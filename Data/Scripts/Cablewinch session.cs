using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace Phantombite.CableWinch
{
    /// <summary>
    /// CableWinch_Session — Core v2.0.0 Anbindung
    ///
    /// Protokoll (Mod → Core, Kanal 1995000):
    ///   REGISTER|cablewinch|Phantombite CableWinch|1.0.0|1995002
    ///   PERFACK|cablewinch|confirmedLevel
    ///
    /// Protokoll (Core → Mod, Kanal 1995002):
    ///   READY
    ///   LOGLEVEL|0/1/2
    ///   PERFLEVEL|0-3
    ///
    /// Performance:
    ///   Level 0 — Kabel-Visualisierung aktiv (Normal)
    ///   Level 3 — Kabel-Visualisierung deaktiviert (Script aus)
    ///
    /// Netzwerk:
    ///   PERFLEVEL_PACKET (19502) — Server → alle Clients (PERFLEVEL weiterleiten)
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CableWinchSession : MySessionComponentBase
    {
        private const long   CORE_CHANNEL       = 1995000L;
        private const long   MY_CHANNEL         = 1995002L;
        private const long   LOG_CHANNEL        = 1995999L;
        private const string MOD_NAME           = "Phantombite_CableWinch";
        private const string VERSION            = "1.0.0";
        private const ushort PERFLEVEL_PACKET   = 19502;

        private bool _initialized = false;
        private int  _logLevel    = 0;

        // ── Statischer PerfLevel — von CableWinchLogic (GameLogic) gelesen ──
        public static int PerfLevel { get; private set; } = 0;

        // ── LoadData ─────────────────────────────────────────────────────────

        public override void LoadData()
        {
            try
            {
                // Auf allen Maschinen: PERFLEVEL-Netzwerkpaket empfangen (Client-Relay)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PERFLEVEL_PACKET, OnPerfLevelPacket);

                // Core-Kanal nur auf Server registrieren
                if (MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.RegisterMessageHandler(MY_CHANNEL, OnCoreMessage);

                _initialized = true;
                Log("LoadData — warte auf Core READY");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.cablewinch] [ERROR] LoadData: " + ex);
            }
        }

        // ── Core Kommunikation ────────────────────────────────────────────────

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
                    int level;
                    if (int.TryParse(msg.Substring(10), out level))
                    {
                        level = Math.Max(0, Math.Min(3, level));
                        ApplyPerfLevel(level);

                        // PERFACK zurück an Core
                        MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                            "PERFACK|cablewinch|" + level);

                        Log("PERFLEVEL gesetzt: " + level + " — " +
                            (level >= 3 ? "Visualisierung AUS" : "Visualisierung AN"));
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.cablewinch] [ERROR] OnCoreMessage: " + ex);
            }
        }

        private void SendRegister()
        {
            // Kein Command — nur PERFLEVEL-Steuerung
            string msg = "REGISTER|cablewinch|Phantombite CableWinch|" + VERSION + "|" + MY_CHANNEL;
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
        }

        // ── PerfLevel anwenden + an Clients weiterleiten ─────────────────────

        private void ApplyPerfLevel(int level)
        {
            PerfLevel = level;

            // Server: PERFLEVEL per Netzwerkpaket an alle Clients weiterleiten
            if (MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                return; // Singleplayer — kein Relay nötig, PerfLevel schon gesetzt

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                byte[] data = new byte[] { (byte)level };
                MyAPIGateway.Multiplayer.SendMessageToOthers(PERFLEVEL_PACKET, data);
                Log("PERFLEVEL " + level + " → an alle Clients weitergeleitet");
            }
        }

        // ── Client: PERFLEVEL-Paket vom Server empfangen ──────────────────────

        private void OnPerfLevelPacket(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0) return;
                int level = Math.Max(0, Math.Min(3, (int)data[0]));
                PerfLevel = level;
                Log("PERFLEVEL vom Server empfangen: " + level);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.cablewinch] [ERROR] OnPerfLevelPacket: " + ex);
            }
        }

        // ── Unload ───────────────────────────────────────────────────────────

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MY_CHANNEL, OnCoreMessage);

                if (MyAPIGateway.Multiplayer != null)
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(PERFLEVEL_PACKET, OnPerfLevelPacket);

                PerfLevel    = 0;
                _initialized = false;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.cablewinch] [ERROR] UnloadData: " + ex);
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private void Log(string msg, int level = 0)
        {
            if (level > _logLevel) return;
            try
            {
                MyLog.Default.WriteLineAndConsole("[PB.cablewinch] " + msg);
                MyAPIGateway.Utilities.SendModMessage(LOG_CHANNEL,
                    "LOG|" + MOD_NAME + "|" + level + "|CableWinch_Session|" + msg);
            }
            catch { }
        }
    }
}