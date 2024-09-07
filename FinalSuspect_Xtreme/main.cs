using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FinalSuspect_Xtreme.Attributes;
using FinalSuspect_Xtreme.Modules;
using UnityEngine;
using FinalSuspect_Xtreme.Modules.Managers;

[assembly: AssemblyFileVersion(FinalSuspect_Xtreme.Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(FinalSuspect_Xtreme.Main.PluginVersion)]
[assembly: AssemblyVersion(FinalSuspect_Xtreme.Main.PluginVersion)]
namespace FinalSuspect_Xtreme;

[BepInPlugin(PluginGuid, "FinalSuspect_Xtreme", PluginVersion)]
[BepInIncompatibility("jp.ykundesu.supernewroles")]
[BepInProcess("Among Us.exe")]
public class Main : BasePlugin
{
    // == 程序基本设定 / Program Config ==
    public static readonly string ModName = "Final Suspect_Xtreme";
    public static readonly string TeamColor = "#cdfffd";
    public static readonly Color32 TeamColor32 = new(205, 255, 253, 255);
    public static readonly string ModColor = "#CECDFD";
    public static readonly Color32 ModColor32 = new(206, 205, 253, 255);
    public static readonly Color32 OutColor = new(180,179,231, 255);
    public static readonly Color32 HalfYellow = new(255, 255, 25, 160);
    public static readonly Color32 ModColor32_semi_transparent = new (206, 205, 253, 160);
    public static readonly string ForkId = "FinalSuspect_Xtreme";
    public const string PluginGuid = "cn.finalsuspect_xtreme.xtremewave";
    // == 认证设定 / Authentication Config ==
    public static HashAuth DebugKeyAuth { get; private set; }
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    // == 版本相关设定 / Version Config ==
    public const string LowestSupportedVersion = "2024.8.13";
    public static readonly bool IsPublicAvailableOnThisVersion = true;
    public const string PluginVersion = "1.0.0";
    public const string ShowVersion_Head = "1.0_20240814";
    public const string ShowVersion_TestText = "_Preview";
    public const string ShowVersion = ShowVersion_Head + ShowVersion_TestText;
    public const int PluginCreation = 1;
    // == 链接相关设定 / Link Config ==
    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteUrl = Translator.IsChineseLanguageUser ? "https://fsusx.top/" : "https://fsusx.top/en/";
    public static readonly bool ShowQQButton = true;
    public static readonly string QQInviteUrl = "http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=4ojpzbUU42giZZeQ-DTaal-tC5RIpL46&authKey=49OYQwsCza2x5eHGdXDHXD1M%2FvYvQcEhJBNL5h8Gq7AxOu5eMfTc6g2edtlsMuCm&noverify=0&group_code=733425569";
    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/kz787Zg7h8";
    public static readonly bool ShowGithubUrl = true;
    public static readonly string GithubRepoUrl = "https://github.com/XtremeWave/FinalSuspect_Xtreme";

    // ==========

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static string CredentialsText;
    public static NormalGameOptionsV08 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV08 HideNSeekOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;

    //Client Options
    public static ConfigEntry<bool> KickPlayerFriendCodeNotExist { get; private set; }
    public static ConfigEntry<bool> ApplyDenyNameList { get; private set; }
    public static ConfigEntry<bool> ApplyBanList { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<string> AprilFoolsMode { get; private set; }
    public static ConfigEntry<bool> AutoStartGame { get; private set; }
    public static ConfigEntry<bool> AutoEndGame { get; private set; }
    public static ConfigEntry<bool> EnableMapBackGround { get; private set; }
    public static ConfigEntry<bool> EnableRoleBackGround { get; private set; }
    public static ConfigEntry<bool> DisableVanillaSound { get; private set; }

    public static ConfigEntry<bool> VersionCheat { get; private set; }
    public static ConfigEntry<bool> GodMode { get; private set; }


    public static Dictionary<byte, XtremeGameData.PlayerVersion> playerVersion = new();

    public static readonly string[] allAprilFoolsModes =
    {
        "NoAprilFoolsMode", "HorseMode", "LongMode"
    };
    //Other Configs
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<bool> ShowResults { get; private set; }
    public static ConfigEntry<string> WebhookURL { get; private set; }


    public static Dictionary<RoleTypes, string> roleColors;
    public static List<int> clientIdList = new();

    public static string HostNickName = "";
    public static bool IsInitialRelease = DateTime.Now.Month == 2 && DateTime.Now.Day is 9;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public const float RoleTextSize = 2f;

    public static IEnumerable<PlayerControl> AllPlayerControls => 
        //(PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) && LoadEnd
        //? AllPlayerControls : 
        PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static IEnumerable<PlayerControl> AllAlivePlayerControls => 
        //(PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) && LoadEnd
        //? AllAlivePlayerControls :
        PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected);

    public static Main Instance;

    //TONX

    public static bool NewLobby = false;

    public static List<string> TName_Snacks_CN = 
        new() { "冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶", 
            "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼",
            "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖" };
    public static List<string> TName_Snacks_EN = new() 
    { "Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy", "Milk", 
        "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast", 
        "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle", "Macaron",
        "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant", "toffee" };
    public static string Get_TName_Snacks => TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese ?
        TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)] :
        TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    public override void Load()
    {
        Instance = this;
        
        // From YuEzTools
        ResourceUtils.WriteToFileFromResource(
            "BepInEx/core/YamlDotNet.dll",
            "FinalSuspect_Xtreme.Resources.InDLL.Depends.YamlDotNet.dll");
        ResourceUtils.WriteToFileFromResource(
            "BepInEx/core/YamlDotNet.xml",
            "FinalSuspect_Xtreme.Resources.InDLL.Depends.YamlDotNet.xml");
        
        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "FSX");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");
        ShowResults = Config.Bind("Result", "Show Results", true);
        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        AprilFoolsMode = Config.Bind("Client Options", "AprilFoolsMode", allAprilFoolsModes[0]);
        KickPlayerFriendCodeNotExist = Config.Bind("Client Options", "AutoStartGame", true);
        ApplyBanList = Config.Bind("Client Options", "ApplyBanList", true);
        ApplyDenyNameList = Config.Bind("Client Options", "ApplyDenyNameList", true);
        AutoStartGame = Config.Bind("Client Options", "AutoStartGame", false);
        AutoEndGame = Config.Bind("Client Options", "AutoEndGame", false);
        EnableMapBackGround = Config.Bind("Client Options", "EnableMapBackGround", true);
        EnableRoleBackGround = Config.Bind("Client Options", "EnableRoleBackGround", true);
        DisableVanillaSound = Config.Bind("Client Options", "DisableVanillaSound", false);
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("FinalSuspect_Xtreme");
        FinalSuspect_Xtreme.Logger.Enable();
        FinalSuspect_Xtreme.Logger.Disable("NotifyRoles");
        FinalSuspect_Xtreme.Logger.Disable("SwitchSystem");
        FinalSuspect_Xtreme.Logger.Disable("ModNews");
        FinalSuspect_Xtreme.Logger.Disable("CustomRpcSender");
        FinalSuspect_Xtreme.Logger.Disable("CoBegin");
        if (!DebugModeManager.AmDebugger)
        {
            FinalSuspect_Xtreme.Logger.Disable("CheckRelease");
            FinalSuspect_Xtreme.Logger.Disable("CustomRpcSender");
            //FinalSuspect_Xtreme.Logger.Disable("ReceiveRPC");
            FinalSuspect_Xtreme.Logger.Disable("SendRPC");
            FinalSuspect_Xtreme.Logger.Disable("SetRole");
            FinalSuspect_Xtreme.Logger.Disable("Info.Role");
            FinalSuspect_Xtreme.Logger.Disable("TaskState.Init");
            //FinalSuspect_Xtreme.Logger.Disable("Vote");
            FinalSuspect_Xtreme.Logger.Disable("RpcSetNamePrivate");
            //FinalSuspect_Xtreme.Logger.Disable("SendChat");
            FinalSuspect_Xtreme.Logger.Disable("SetName");
            //FinalSuspect_Xtreme.Logger.Disable("AssignRoles");
            //FinalSuspect_Xtreme.Logger.Disable("RepairSystem");
            //FinalSuspect_Xtreme.Logger.Disable("MurderPlayer");
            //FinalSuspect_Xtreme.Logger.Disable("CheckMurder");
            FinalSuspect_Xtreme.Logger.Disable("PlayerControl.RpcSetRole");
            FinalSuspect_Xtreme.Logger.Disable("SyncCustomSettings");
            FinalSuspect_Xtreme.Logger.Disable("CancelPet");
            FinalSuspect_Xtreme.Logger.Disable("Pet");
            //FinalSuspect_Xtreme.Logger.Disable("SetScanner");
            FinalSuspect_Xtreme.Logger.Disable("test");
            FinalSuspect_Xtreme.Logger.Disable("ver");
            FinalSuspect_Xtreme.Logger.Disable("RpcTeleport");
        }
        //FinalSuspect_Xtreme.Logger.isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        WebhookURL = Config.Bind("Other", "WebhookURL", "none");

        hasArgumentException = false;
        ExceptionMessage = "";
        try
        {
            roleColors = new Dictionary<RoleTypes, string>()
            {
                {RoleTypes.CrewmateGhost, "#8cffff"},
                {RoleTypes.GuardianAngel, "#8CFFDB"},
                {RoleTypes.Crewmate, "#8cffff"},
                {RoleTypes.Scientist, "#F8FF8C"},
                {RoleTypes.Engineer, "#8C90FF"},
                {RoleTypes.Noisemaker, "#FFC08C"},
                {RoleTypes.Tracker, "#93FF8C"},
                {RoleTypes.ImpostorGhost, "#ff1919"},
                {RoleTypes.Impostor, "#ff1919"},
                {RoleTypes.Shapeshifter, "#ff1919"},
                {RoleTypes.Phantom, "#ff1919"},
            };
        }
        catch (ArgumentException ex)
        {
            FinalSuspect_Xtreme.Logger.Error("错误：字典出现重复项", "LoadDictionary");
            FinalSuspect_Xtreme.Logger.Exception(ex, "LoadDictionary");
            hasArgumentException = true;
            ExceptionMessage = ex.Message;
            ExceptionMessageIsShown = false;
        }

        RegistryManager.Init(); // 这是优先级最高的模块初始化方法，不能使用模块初始化属性

        PluginModuleInitializerAttribute.InitializeAll();

        IRandom.SetInstance(new NetRandomWrapper());

        FinalSuspect_Xtreme.Logger.Info($"{Application.version}", "AmongUs Version");

        var handler = FinalSuspect_Xtreme.Logger.Handler("GitVersion");
        handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
        handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
        handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
        handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
        handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
        handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();


        SystemEnvironment.SetEnvironmentVariables();

        Harmony.PatchAll();

        if (!DebugModeManager.AmDebugger) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();

        FinalSuspect_Xtreme.Logger.Msg("========= FinalSuspect_Xtreme loaded! =========", "Plugin Load");
    }
}