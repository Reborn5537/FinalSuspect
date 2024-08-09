using HarmonyLib;
using System.Text;
using UnityEngine;
using static FinalSuspect_Xtreme.Translator;
using AmongUs.GameOptions;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace FinalSuspect_Xtreme;


[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.BootFromVent))]
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{

    public static bool Prefix()
    {
        if (GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        target.SetDeathReason(DataDeathReason.Kill);
        target.GetPlayerData().RealKiller = __instance.GetPlayerData();
        target.GetPlayerData().Murdered = true;
        target.SetDead();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
class DiePatch
{

    public static bool Prefix()
    {
        if (GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(1)] bool assginGhostRole)
    {
        if (!assginGhostRole) return;
        __instance.SetDeathReason(DataDeathReason.Kill);
        __instance.SetDead();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoSetRole))]
class CoSetRolePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleTypes)
    {
        __instance.SetRole(roleTypes);
        __instance.SetIsImp(Utils.IsImpostor(roleTypes));
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;

        
        var self = __instance == PlayerControl.LocalPlayer;
        var color ="#ffffff";
        var nametext = __instance.GetTrueName();
        var dr = false;

        if (GameStates.IsLobby)
        {
            if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver) && ver != null)
            {
                if (Main.ForkId != ver.forkId)
                {
                    nametext = $"<size=1.5>{ver.forkId}</size>\n{nametext}";
                    color = "#BFFFB9";
                }
                else if (Main.version.CompareTo(ver.version) == 0)
                {
                    var currectbranch = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
                    nametext = currectbranch
                        ? nametext : $"<size=1.5>{ver.tag}</size>\n{nametext}";
                    color = currectbranch ? "#B1FFE7" : "#ffff00";
                }
                else
                {
                    nametext = $"<size=1.5>v{ver.version}</size>\n{nametext}";
                    color = "#ff0000";
                }
            }
            else 
            {
                color = self ? "#B1FFE7" : "#E1E0B3";
            }
        }
        else if (GameStates.IsInGame)
        {
            if (Main.playerVersion.TryGetValue(0, out var ver) && Main.ForkId != ver.forkId) return;

            var rt = "";
            var roleType = __instance.GetRoleType();
            var dead = !__instance.IsAlive();

           


            color = Utils.GetRoleColorCode(roleType);
            if (
                self || 
                !PlayerControl.LocalPlayer.IsAlive() && 
                ((dead && PlayerControl.LocalPlayer.GetRoleType() is RoleTypes.GuardianAngel)||
                (PlayerControl.LocalPlayer.GetRoleType() is not RoleTypes.GuardianAngel)))
            {
                rt = $"<size=80%>{Translator.GetRoleString(roleType.ToString())}</size>";
                dr = true;
            }
            else if (PlayerControl.LocalPlayer.IsImpostor() && __instance.IsImpostor())
            {
                color = "#ff1919";
                if (!PlayerControl.LocalPlayer.IsAlive())
                {
                    rt = $"<size=80%>{Translator.GetRoleString(roleType.ToString())}</size>";
                    dr = true;

                }
            }
            else
            {
                color = "#ffffff";
            }

            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();

            RoleText.enabled = true;
            RoleText.text = $"<color={color}>" + rt + $"</color> {Utils.GetProgressText(__instance)}";
            RoleText.transform.SetLocalY(0.2f);

        }

        __instance.cosmetics.nameText.text = $"<color={color}>" + nametext + "</color>";
        if (dr && !PlayerControl.LocalPlayer.IsAlive())
            __instance.cosmetics.nameText.text += Utils.GetVitalText(__instance.PlayerId);


    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.transform.localScale = new(1f, 1f, 1f);
        roleText.fontSize = Main.RoleTextSize;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
class PlayerControlSetTasksPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo.TaskInfo> tasks)
    {
        var pc = __instance;
        pc.SetTaskTotalCount(tasks.Count);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{

    public static void Postfix(PlayerControl __instance)
    {
        var pc = __instance;
        Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
        pc.OnCompleteTask();

        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
    }
}
#region 名称检查
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckName))]
class PlayerControlCheckNamePatch
{
    public static void Postfix(PlayerControl __instance, string playerName)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby ) return;

        var name = playerName;
        Main.AllPlayerNames.Remove(__instance.PlayerId);
        Main.AllPlayerNames.TryAdd(__instance.PlayerId, name);

    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckName))]
class CmdCheckNameVersionCheckPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (AmongUsClient.Instance.AmHost)
           RPC.RpcVersionCheck();
    }
}
#endregion