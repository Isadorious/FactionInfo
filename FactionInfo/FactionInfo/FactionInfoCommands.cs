using System;
using System.Collections.Generic;
using System.Text;
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
        public FactionInfoPlugin Plugin => (FactionInfoPlugin) Context.Plugin;

        // From Essentials
        public static IMyPlayer GetPlayerByNameOrId(string nameOrPlayerId)
        {
            if (!long.TryParse(nameOrPlayerId, out long id))
            {
                foreach (var identity in MySession.Static.Players.GetAllIdentities())
                {
                    if (identity.DisplayName == nameOrPlayerId)
                    {
                        id = identity.IdentityId;
                    }
                }
            }

            if (MySession.Static.Players.TryGetPlayerId(id, out MyPlayer.PlayerId playerId))
            {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player))
                {
                    return player;
                }
            }

            return null;
        }

        [Command("factioninfo", "Returns some faction information based upon filtering options")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void FactionPlayerCount()
        {
            #region Command Arguments

            List<string> args = Context.Args;
            uint playerCount = 0;
            string factionTag = "";
            bool getPublicInfo = false;
            bool getPrivateInfo = false;
            bool getFounder = false;
            bool getLeaders = false;
            bool getMembers = false;
            bool incNPCs = false;
            bool getSpecificFaction = false;
            bool acceptAll = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("-count>"))
                {
                    string[] splitArray = arg.Split('>');

                    var success = uint.TryParse(splitArray[1], out playerCount);

                    if (!success)
                    {
                        Context.Respond("Could not parse count, ignoring");
                    }
                }
                else if (arg.Equals("-public"))
                    getPublicInfo = true;
                else if (arg.Equals("-private"))
                    getPrivateInfo = true;
                else if (arg.Equals("-founders"))
                    getFounder = true;
                else if (arg.Equals("-leaders"))
                    getLeaders = true;
                else if (arg.Equals("-members"))
                    getMembers = true;
                else if (arg.Equals("-NPC"))
                    incNPCs = true;
                else if (arg.Equals("-AcceptAll"))
                    acceptAll = true;
                else if (arg.StartsWith("-tag="))
                {
                    getSpecificFaction = true;

                    string[] splitArray = arg.Split('=');

                    factionTag = splitArray[1];

                }
            }

            #endregion

            var sb = new StringBuilder();

            #region Feedback Applied Filter Options

            var appliedFiltersOutput = "Filters applied: ";

            if (playerCount > 0)
                appliedFiltersOutput += "Factions with more than " + playerCount + " members, ";

            if (getPublicInfo)
                appliedFiltersOutput += "Public Info, ";

            if (getPrivateInfo)
                appliedFiltersOutput += "Private Info, ";

            if (acceptAll)
                appliedFiltersOutput += "Accept All, ";

            if (getFounder)
                appliedFiltersOutput += "Get Founder, ";

            if (getLeaders)
                appliedFiltersOutput += "Get Leaders, ";

            if (getMembers)
                appliedFiltersOutput += "Get Members, ";

            if (incNPCs)
                appliedFiltersOutput += "Include NPCs";

            if (getSpecificFaction)
                appliedFiltersOutput += "Searching for faction with tag: " + factionTag + " ignoring count arg";


            sb.AppendLine(appliedFiltersOutput);

            #endregion

            // Checks that a switch has been activated so that an empty line gets put in
            if (getFounder || getLeaders || getMembers || getPrivateInfo || getPublicInfo || playerCount > 0)
            {
                sb.AppendLine();
            }

            if (getSpecificFaction == true)
            {
                var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);

                sb.AppendLine(faction.Tag + " - " + faction.Name + " - " + faction.Members.Count);

                if (getPublicInfo)
                {
                    sb.AppendLine("Public Info:: " + faction.Description);
                }

                if (getPrivateInfo)
                {
                    sb.AppendLine("Private Info:: " + faction.PrivateInfo);
                }

                if (acceptAll)
                {
                    sb.AppendLine("Accept All:: " + faction.AutoAcceptMember);
                }

                if (getFounder || getLeaders || getMembers)
                {
                    var now = new DateTime();

                    foreach (var player in faction?.Members)
                    {
                        if (!MySession.Static.Players.HasIdentity(player.Key) &&
                            !MySession.Static.Players.IdentityIsNpc(player.Key) ||
                            string.IsNullOrEmpty(MySession.Static?.Players
                                ?.TryGetIdentity(player.Value.PlayerId)
                                .DisplayName)) continue; //This is needed to filter out players with no id.

                        if (player.Value.IsFounder) // Always true Founder is a leader & a member
                        {
                            sb.AppendLine("Founder:: " + MySession.Static?.Players
                                              ?.TryGetIdentity(player.Value.PlayerId).DisplayName);

                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                            if(difference.HasValue)
                                sb.AppendLine("   Last logout: " + difference.Value.Days);

                        }

                        else if (player.Value.IsLeader && (getLeaders || getMembers))
                        {
                            sb.AppendLine("Leader:: " + MySession.Static?.Players
                                              ?.TryGetIdentity(player.Value.PlayerId).DisplayName);
                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                            if (difference.HasValue)
                                sb.AppendLine("   Last logout: " + difference.Value.Days);
                        }

                        else if (getMembers)
                        {
                            sb.AppendLine("Members:: " + MySession.Static?.Players
                                              ?.TryGetIdentity(player.Value.PlayerId).DisplayName);
                            TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                            if (difference.HasValue)
                                sb.AppendLine("   Last logout: " + difference.Value.Days);
                        }

                    }
                }
            }
            else
            {

                foreach (var factionId in MySession.Static.Factions)
                {

                    var faction = factionId.Value;
                    double memberCount = faction.Members.Count;

                    if (!incNPCs)
                    {
                        if (faction.IsEveryoneNpc())
                        {
                            continue;
                        }
                    }

                    if (memberCount > playerCount)
                    {
                        sb.AppendLine(faction.Tag + " - " + faction.Name + " - " + memberCount);


                        if (getPublicInfo)
                        {
                            sb.AppendLine("Public Info:: " + faction.Description);
                        }

                        if (getPrivateInfo)
                        {
                            sb.AppendLine("Private Info:: " + faction.PrivateInfo);
                        }

                        if (acceptAll)
                        {
                            sb.AppendLine("Accept All:: " + faction.AutoAcceptMember);
                        }

                        if (getFounder || getLeaders || getMembers)
                        {
                            var now = new DateTime();
                            foreach (var player in faction?.Members)
                            {
                                if (!MySession.Static.Players.HasIdentity(player.Key) &&
                                    !MySession.Static.Players.IdentityIsNpc(player.Key) ||
                                    string.IsNullOrEmpty(MySession.Static?.Players
                                        ?.TryGetIdentity(player.Value.PlayerId)
                                        .DisplayName)) continue; //This is needed to filter out players with no id.

                                if (player.Value.IsFounder) // Always true Founder is a leader & a member
                                {
                                    sb.AppendLine("Founder:: " + MySession.Static?.Players
                                                      ?.TryGetIdentity(player.Value.PlayerId).DisplayName);
                                    TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                                    if (difference.HasValue)
                                        sb.AppendLine("   Last logout: " + difference.Value.Days);
                                }

                                else if (player.Value.IsLeader && (getLeaders || getMembers))
                                {
                                    sb.AppendLine("Leader:: " + MySession.Static?.Players
                                                      ?.TryGetIdentity(player.Value.PlayerId).DisplayName);
                                    TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                                    if (difference.HasValue)
                                        sb.AppendLine("   Last logout: " + difference.Value.Days);
                                }

                                else if (getMembers)
                                {
                                    sb.AppendLine("Members:: " + MySession.Static?.Players
                                                      ?.TryGetIdentity(player.Value.PlayerId).DisplayName);
                                    TimeSpan? difference = now - MySession.Static?.Players?.TryGetIdentity(player.Value.PlayerId).LastLogoutTime;

                                    if (difference.HasValue)
                                        sb.AppendLine("   Last logout: " + difference.Value.Days);
                                }

                            }
                        }

                        // Checks that a switch has been activated so that an empty line gets put in
                        if (getFounder || getLeaders || getMembers || getPrivateInfo || getPublicInfo ||
                            memberCount > playerCount)
                        {
                            sb.AppendLine();
                        }
                    }
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
    }
}
