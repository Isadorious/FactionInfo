using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Filters;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace FactionInfo
{
    public class FactionInfoCommands : CommandModule
    {
        public static readonly Logger Log = LogManager.GetLogger("FactionInfo");
        public FactionInfoPlugin Plugin => (FactionInfoPlugin) Context.Plugin;

        [Command("factioninfo", "Returns some faction information based upon filtering options")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void FactionInfo()
        {
            #region Command Arguments
            List<string> args = Context.Args;
            Dictionary<string, dynamic> filters = filterOptions(args);
            #endregion
            var sb = new StringBuilder();

            /*#region Feedback Applied Filter Options
            var appliedFiltersOutput = "Filters applied: ";
            if (playerCount > 0) appliedFiltersOutput += "Factions with more than " + playerCount + " members, ";
            if (getPublicInfo) appliedFiltersOutput += "Public Info, ";
            if (getPrivateInfo) appliedFiltersOutput += "Private Info, ";
            if (acceptAll) appliedFiltersOutput += "Accept All, ";
            if (getFounder) appliedFiltersOutput += "Get Founder, ";
            if (getLeaders) appliedFiltersOutput += "Get Leaders, ";
            if (getMembers) appliedFiltersOutput += "Get Members, ";
            if (includeNPC) appliedFiltersOutput += "Include NPC";
            if (getSpecificFaction) appliedFiltersOutput += "Searching for faction with tag: " + factionTag + " ignoring count arg";
            sb.AppendLine(appliedFiltersOutput);
            #endregion*/

            // Checks if a filter has been added, this adds a seperator in
            if (filters.Count > 0) sb.AppendLine();

            if (filters.ContainsKey("getOne")) {
                filters.TryGetValue("getOne", out dynamic factionTag);
                try {
                    var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
                    sb.AppendLine(GetFactionInfo(filters, faction));
                } catch(Exception e) {
                    sb.AppendLine("That faction doesn't exist!");
                }
            } else if (filters.ContainsKey("getPlayer")) {
                filters.TryGetValue("getPlayer", out dynamic playerName);
                IMyPlayer targetPlayer = Utils.GetPlayerByNameOrId(playerName);
                try
                {
                    var faction = MySession.Static.Factions.TryGetPlayerFaction(targetPlayer.IdentityId);
                    sb.AppendLine(GetFactionInfo(filters, faction));
                } catch(Exception ex)
                {
                    sb.AppendLine("That player is not in a faction!");
                }
            }
            else
            {
                foreach (var factionId in MySession.Static.Factions) {
                    var faction = factionId.Value;
                    filters.TryGetValue("npc", out dynamic getNPC);
                    if (getNPC != true && faction.IsEveryoneNpc()) continue;
                    sb.AppendLine(GetFactionInfo(filters, faction));
                }
            }

            if (Context.Player == null)
            {
                Context.Respond((sb.ToString()));
            }
            else if (Context?.Player?.SteamUserId > 0)
            {
                ModCommunication.SendMessageTo(new DialogMessage("Faction Info", null, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("faction private", "Replaces a factions private info with a message to not break the rules.")]
        public void ResetFactionPrivateInfo(string tag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(tag);

            faction.PrivateInfo = "Please do not put anything in your private info that is against the rules";
        }

        /***
         * Method to work out the filters, returns a Dictionary with keys of type string with values being dynamic 
         ***/
        private Dictionary<string, dynamic> filterOptions(List<string> args)
        {
            Dictionary<string, dynamic> filterOptions = new Dictionary<string, dynamic>();
            foreach (string arg in args)
            {
                if (arg.StartsWith("-count>"))
                {
                    string[] splitArray = arg.Split('>');
                    var success = int.TryParse(splitArray[1], out int playerCount);
                    if (success) filterOptions.Add("players", playerCount);
                    if (!success) filterOptions.Add("players", -1); // Set to -1 to indicate a failure with parsing. All factions have at least 1 member
                }
                else if (arg.StartsWith("-public")) filterOptions.Add("public", true);
                else if (arg.Equals("-private")) filterOptions.Add("private", true);
                else if (arg.Equals("-founder")) filterOptions.Add("founder", true);
                else if (arg.Equals("-leaders")) filterOptions.Add("leaders", true);                
                else if (arg.Equals("-members")) filterOptions.Add("members", true);
                else if (arg.Equals("-npc")) filterOptions.Add("npc", true);
                else if (arg.Equals("-acceptall")) filterOptions.Add("autoAccept", true);
                else if (arg.StartsWith("-tag=")) filterOptions.Add("getOne", arg.Split('=')[1]);
                else if (arg.StartsWith("-player=")) filterOptions.Add("getPlayer", arg.Split('=')[1]);
            }
            return filterOptions;
        }

        /***
         * Returns all the information for a specific faction based upon the active filters.
         **/
        private string GetFactionInfo(Dictionary<string, dynamic> filters, IMyFaction faction)
        {
            dynamic playerCount = 0;
            if(filters.ContainsKey("players")) {
                filters.TryGetValue("players", out playerCount);
            }

            StringBuilder sb = new StringBuilder();
            if (faction.Members.Count > playerCount) {
                sb.AppendLine(faction.Tag + " - " + faction.Name + " - " + faction.Members.Count);

                filters.TryGetValue("public", out dynamic getPublicInfo);
                filters.TryGetValue("private", out dynamic getPrivateInfo);
                filters.TryGetValue("autoAccept", out dynamic autoAccept);
                filters.TryGetValue("founder", out dynamic getFounder);
                filters.TryGetValue("leaders", out dynamic getLeaders);
                filters.TryGetValue("members", out dynamic getMembers);

                if (getPublicInfo == true) sb.AppendLine("Public Info: " + faction.Description);
                if (getPrivateInfo == true) sb.AppendLine("Private Info: " + faction.PrivateInfo);
                if (autoAccept == true) sb.AppendLine("Accept All: " + faction.AutoAcceptMember);
                if (getFounder == true || getLeaders == true || getMembers == true)
                {
                    var now = DateTime.Now;
                    foreach (var player in faction?.Members)
                    {
                        if (!MySession.Static.Players.HasIdentity(player.Key) && !MySession.Static.Players.IdentityIsNpc(player.Key) || string.IsNullOrEmpty(MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).DisplayName)) continue; //This is needed to filter out players with no id.
                        if (player.Value.IsFounder == true)
                        { // Always true Founder is a leader & a member
                            sb.AppendLine("Founder: " + MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).DisplayName +" ("+MySession.Static?.Players.TryGetSteamId(player.Value.PlayerId)+")");
                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;
                            if (difference.HasValue) sb.AppendLine($"   Last logout: {(difference.Value.Days > 0 ? difference.Value.Days + " days " : "")} {(difference.Value.Hours > 0 ? difference.Value.Hours + " hours" : "")} {(difference.Value.Minutes > 0 ? difference.Value.Minutes + " minutes" : "")}");
                        }
                        else if (player.Value.IsLeader == true && (getLeaders == true|| getMembers == true))
                        {
                            sb.AppendLine("Leader: " + MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).DisplayName + " (" + MySession.Static?.Players.TryGetSteamId(player.Value.PlayerId) + ")");
                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;
                            if (difference.HasValue) sb.AppendLine($"   Last logout: {(difference.Value.Days > 0 ? difference.Value.Days + " days " : "")} {(difference.Value.Hours > 0 ? difference.Value.Hours + " hours" : "")} {(difference.Value.Minutes > 0 ? difference.Value.Minutes + " minutes" : "")}");
                        }
                        else if (getMembers == true)
                        {
                            sb.AppendLine("Members: " + MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).DisplayName + " (" + MySession.Static?.Players.TryGetSteamId(player.Value.PlayerId) + ")");
                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;
                            if (difference.HasValue) sb.AppendLine($"   Last logout: {(difference.Value.Days > 0 ? difference.Value.Days + " days " : "")} {(difference.Value.Hours > 0 ? difference.Value.Hours + " hours" : "")} {(difference.Value.Minutes > 0 ? difference.Value.Minutes + " minutes" : "")}");
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}
