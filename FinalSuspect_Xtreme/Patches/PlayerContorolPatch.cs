using HarmonyLib;
using System.Text;
using UnityEngine;
using static FinalSuspect_Xtreme.Translator;
using AmongUs.GameOptions;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace FinalSuspect_Xtreme;


[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.BootFromVent))]
class BootFromVentPatch
{

    public static bool Prefix()
    {
        if (XtremeGameData.GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{

    public static bool Prefix()
    {
        if (XtremeGameData.GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (XtremeGameData.GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return;
        }
        target.SetRealKiller(__instance);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
class DiePatch
{

    public static bool Prefix()
    {
        if (XtremeGameData.GameStates.IsLobby)
        {
            RPC.NotificationPop(GetString("Warning.RoomBroken"));
            return false;
        }
        return true;
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
        
        var nametext = __instance.GetRealName();
        __instance.GetLobbyText(ref nametext, out string color);

        var ingame = __instance.GetGameText(out string colorgame, out bool appendText, out string roleText);

        if (ingame)
        {
            color = colorgame;
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();

            RoleText.enabled = true;
            RoleText.text = roleText;
            RoleText.transform.SetLocalY(0.2f);

            if (appendText && !PlayerControl.LocalPlayer.IsAlive())
                __instance.cosmetics.nameText.text += Utils.GetVitalText(__instance.PlayerId);
        }
        __instance.cosmetics.nameText.text = $"<color={color}>" + nametext + "</color>";

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
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckName))]
class CmdCheckNameVersionCheckPatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.AmHost)
           RPC.RpcVersionCheck();
    }
}
