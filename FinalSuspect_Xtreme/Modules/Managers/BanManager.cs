using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinalSuspect_Xtreme.Attributes;
using static FinalSuspect_Xtreme.Translator;

namespace FinalSuspect_Xtreme.Modules.Managers;

public static class BanManager
{
    private static readonly string DENY_NAME_LIST_PATH = @"./FinalSuspect_Data/DenyName.txt";
    private static readonly string BAN_LIST_PATH = @"./FinalSuspect_Data/BanList.txt";
    private static List<string> EACList = new();

    [PluginModuleInitializer]
    public static void Init()
    {
        try
        {
            Directory.CreateDirectory("FinalSuspect_Data");

            if (!File.Exists(BAN_LIST_PATH))
            {
                Logger.Warn("Create New BanList.txt", "BanManager");
                File.Create(BAN_LIST_PATH).Close();
            }
            if (!File.Exists(DENY_NAME_LIST_PATH))
            {
                Logger.Warn("Create New DenyName.txt", "BanManager");
                File.Create(DENY_NAME_LIST_PATH).Close();
                File.WriteAllText(DENY_NAME_LIST_PATH, GetResourcesTxt("FinalSuspect_Xtreme.Resources.Configs.DenyName.txt"));
            }

            //读取EAC名单
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FinalSuspect_Xtreme.Resources.Configs.EACList.txt");
            stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                if (Main.AllPlayerControls.Any(p => p.IsDev() && line.Contains(p.FriendCode))) continue;
                EACList.Add(line);
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "BanManager");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string GetHashedPuid(this PlayerControl player)
        => player.GetClient().GetHashedPuid();
    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return null;
        string puid = player.ProductUserId;
        if (string.IsNullOrEmpty(puid)) return puid;

        using SHA256 sha256 = SHA256.Create();
        string sha256Hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(puid))).Replace("-", "").ToLower();
        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
    }
    public static void AddBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        if (player.IsBannedPlayer())
        {
            File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.GetHashedPuid()},{player.PlayerName}\n");
            Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
        }
    }
    public static void CheckDenyNamePlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Main.ApplyDenyNameList.Value) return;
        try
        {
            Directory.CreateDirectory("FinalSuspect_Data");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            using StreamReader sr = new(DENY_NAME_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (Main.AllPlayerControls.Any(p => p.IsDev() && line.Contains(p.FriendCode))) continue;
                if (Regex.IsMatch(player.PlayerName, line)
                    || Regex.IsMatch(player.PlayerName.ToLower(), "ez hacked"))
                {
                    Utils.KickPlayer(player.Id, false, "DenyName");
                    RPC.NotificationPop(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckDenyNamePlayer");
        }
    }

    public static void CheckBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Main.ApplyBanList.Value) return;
        if (player.IsBannedPlayer())
        {
            Utils.KickPlayer(player.Id, true, "BanList");
            RPC.NotificationPop(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
        }
        else if (player.IsEACPlayer())
        {
            Utils.KickPlayer(player.Id, true, "EACList");
            RPC.NotificationPop(string.Format(GetString("Message.BanedByEACList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}存在于EAC封禁名单", "BAN");
        }
    }

    public static bool IsBannedPlayer(this PlayerControl player)
        => player?.GetClient()?.IsBannedPlayer() ?? false;
    public static bool IsBannedPlayer(this ClientData player)
        => CheckBanStatus(player?.FriendCode, player?.GetHashedPuid());
    public static bool CheckBanStatus(string friendCode, string hashedPuid)
    {
        try
        {
            Directory.CreateDirectory("FinalSuspect_Data");
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!string.IsNullOrWhiteSpace(friendCode) && line.Contains(friendCode)) return true;
                if (!string.IsNullOrWhiteSpace(hashedPuid) && line.Contains(hashedPuid)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }

    public static bool IsEACPlayer(this PlayerControl player)
        => player?.GetClient()?.IsEACPlayer() ?? false;
    public static bool IsEACPlayer(this ClientData player)
        => CheckEACStatus(player?.FriendCode, player?.GetHashedPuid());
    public static bool CheckEACStatus(string friendCode, string hashedPuid)
        => EACList.Any(line =>
        !string.IsNullOrWhiteSpace(friendCode) && line.Contains(friendCode) ||
        !string.IsNullOrWhiteSpace(hashedPuid) && line.Contains(hashedPuid));
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (recentClient.IsBannedPlayer())
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}