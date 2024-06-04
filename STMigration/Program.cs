// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Newtonsoft.Json;
using STMigration.Models;
using STMigration.Utils;

namespace STMigration {
    class Program {
        #region Fields

        public static readonly string TEAM_DATA_FILE = "Data/team.json";

        #endregion
        #region Main Program

        #region Method - Main

        static void Main(string[] args) {
            try {
                RunAsync(args).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.Write("Press any key to exit ");
            Console.ResetColor();
            Console.ReadKey();
        }

        #endregion
        #region Method - RunAsync

        private static async Task RunAsync(string[] args) {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine();
            Console.WriteLine("================================");
            Console.WriteLine("|| [MIGRATION] Slack -> Teams ||");
            Console.WriteLine("================================");
            Console.WriteLine();
            Console.ResetColor();

            /*
            ** INITIALIZATION
            */
            AuthenticationConfig? config = AuthenticationConfig.ReadFromJsonFile("Data/appsettings.json");

            if (config != null) {
                GraphHelper graphHelper = new(config);

                /*
                ** FILE HANDLING
                */
                string slackArchiveBasePath;
                if (string.IsNullOrWhiteSpace(config.SlackExportDir)) {
                    string directory = Directory.GetCurrentDirectory();
                    slackArchiveBasePath = GetSlackArchiveBasePath(directory, args.Length > 0 ? args[0] : string.Empty);
                } else {
                    slackArchiveBasePath = config.SlackExportDir;
                }

                /*
                ** LOADING SLACK CHANNEL LIST
                */

                // Get slack channels JSON from the slack export archive
                string slackChannelsPath = GetSlackChannelsPath(slackArchiveBasePath);

                // Scan channels from the JSON
                List<STChannel> channelList = ChannelsHelper.ScanChannelsFromSlack(slackChannelsPath);

                /*
                ** LOADING USER LIST OR CREATING NEW
                */
                string? input;
                bool loadCurrentUserList = false;
                if (UsersHelper.UserListExists()) {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"We found existing User List: {UsersHelper.USER_LIST_FILE}");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Do you want to load it? [Y/n] ");
                    Console.ResetColor();
                    input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase)) {
                        loadCurrentUserList = true;
                    }
                }

                List<STUser> userList = await ScanAndHandleUsers(graphHelper, slackArchiveBasePath, loadCurrentUserList);
                Console.WriteLine();

                /*
                ** MIGRATE MESSAGES FROM SLACK TO TEAMS
                */
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Do you want to create a new migration team and migrate Messages? [Y/n] ");
                Console.ResetColor();
                input = Console.ReadLine();

                string? teamID = string.Empty;
                if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase)) {
                    // Create new migration team
                    teamID = await CreateTeam(graphHelper);

                    if (!string.IsNullOrWhiteSpace(teamID)) {
                        // Scan and send messages in Teams
                        await ScanAndHandleMessages(graphHelper, slackArchiveBasePath, channelList, userList, teamID);
                    }
                }

                /*
                ** FINISH MIGRATION BY MIGRATING CHANNELS AND TEAM
                */
                bool migrateTeam = !string.IsNullOrEmpty(teamID);
                if (!migrateTeam) { // If we didn't just migrate a team, ask for which team to migrate
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Do you want to finish migrating an existing team stuck in migration? [y/N] ");
                    Console.ResetColor();
                    input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input) && (input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase))) {
                        while (string.IsNullOrEmpty(teamID)) {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine("Which team do you want to finish migrating?");
                            Console.Write("Input Team ID: ");
                            Console.ResetColor();
                            teamID = Console.ReadLine();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(teamID)) {
                    await FinishMigrating(graphHelper, teamID);
                }

                /*
                ** MIGRATE ATTACHMENTS TO EXISTING TEAM
                */
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Do you want to migrate ATTACHMENTS to a team? [Y/n] ");
                Console.ResetColor();
                input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase)) {
                    // If we did not just migrate, we can ask the user to provide the team
                    if (string.IsNullOrEmpty(teamID)) {
                        var teams = await ListJoinedTeamsAsync(graphHelper);
                        if (
                            teams != null &&
                            teams.Value != null
                        ) {
                            int index = 0;
                            Console.ForegroundColor = ConsoleColor.White;
                            foreach (var team in teams.Value) {
                                Console.WriteLine($"[{index}] {team.DisplayName} ({team.Id})");
                                index++;
                            }
                            Console.ResetColor();

                            int choice;
                            do {
                                choice = UserInputIndexOfList();
                                if (choice < 0 || choice >= teams.Value.Count) {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Not a valid selection, must be between 0 and {teams.Value.Count}");
                                    Console.ResetColor();
                                }
                            } while (choice < 0 || choice >= teams.Value.Count);

                            teamID = teams.Value[choice].Id;
                        }
                    }
                    if (!string.IsNullOrEmpty(teamID)) {
                        await UploadAttachmentsToTeam(graphHelper, slackArchiveBasePath, channelList, userList, teamID);
                    }
                }
            } else {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine();
                Console.WriteLine("================================");
                Console.WriteLine(" Warning could not load config");
                Console.WriteLine("================================");
                Console.WriteLine();
                Console.ResetColor();
            }

            static int UserInputIndexOfList() {
                var choice = -1;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Select: ");
                Console.ResetColor();
                try {
                    choice = int.Parse(Console.ReadLine() ?? string.Empty);
                } catch (FormatException ex) {
                    Console.WriteLine(ex.Message);
                }
                return choice;
            }
        }

        #endregion
        #region Method - FinishMigrating

        // If migration failed and you're left with a team stuck in migration mode, use this function!
        private static async Task FinishMigrating(GraphHelper graphHelper, string teamID) {
            Console.WriteLine();

            var channels = await ListJoinedTeamsAsync(graphHelper, teamID);
            if (
                channels != null &&
                channels.Value != null
            ) {
                foreach (var channel in channels.Value) {
                    if (
                        channel != null &&
                        channel.Id != null &&
                        channel.DisplayName != null
                    ) {
                        await CompleteChannelMigrationAsync(graphHelper, teamID, channel.Id, channel.DisplayName);
                    }
                }
            }

            await CompleteTeamMigrationAsync(graphHelper, teamID);

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("===========================================");
            Console.WriteLine("|| !! MIGRATION OF TEAM WAS A SUCCESS !! ||");
            Console.WriteLine("===========================================");
            Console.ResetColor();
        }

        #endregion

        #endregion
        #region Upload Handling

        #region Method - UploadAttachmentsToTeam

        static async Task UploadAttachmentsToTeam(GraphHelper graphHelper, string slackArchiveBasePath, List<STChannel> channelList, List<STUser> userList, string teamID) {
            if (channelList != null) {
                foreach (var channel in channelList) {
                    if (string.Equals(channel.DisplayName, "general", StringComparison.CurrentCultureIgnoreCase)) {
                        channel.DisplayName = "General";
                    }

                    string? channelID = await GetChannelByName(graphHelper, teamID, channel.DisplayName);

                    if (!string.IsNullOrWhiteSpace(channelID)) {
                        string slackChannelFilesPath = Path.Combine(slackArchiveBasePath, channel.DisplayName);

                        foreach (var file in MessageHandling.GetFilesForChannel(slackChannelFilesPath)) {
                            foreach (var message in MessageHandling.GetMessagesForDay(file, channelList, userList)) {
                                if (
                                    message != null &&
                                    message.Attachments != null &&
                                    message.Attachments.Count > 0
                                ) {
                                    foreach (var attachment in message.Attachments) {
                                        await UploadFileToPath(graphHelper, teamID, channel.DisplayName, attachment);
                                    }

                                    await AddAttachmentsToMessage(graphHelper, teamID, channelID, message);
                                }
                            }
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Cannot find ID for - teamID:{teamID}: channel:{channel.DisplayName}:");
                        Console.ResetColor();
                    }
                }
            }
        }

        #endregion

        #endregion
        #region User Handling

        #region Method - ScanAndHandleUsers

        static async Task<List<STUser>> ScanAndHandleUsers(GraphHelper graphHelper, string slackArchiveBasePath, bool loadUserListInstead) {
            async Task PopulateTeamUsers(List<STUser> users) {
                try {
                    await UsersHelper.PopulateTeamsUsers(graphHelper, users);
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error getting team users!");
                    if (ex.InnerException != null) {
                        Console.WriteLine(ex.InnerException.Message);
                    } else {
                        Console.WriteLine(ex.Message);
                    }
                    Console.ResetColor();
                    Environment.Exit(1);
                }
            }

            async Task AskToPopulateTeamIDs(List<STUser> users) {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Do you want to populate TeamIDs? [Y/n] ");
                Console.ResetColor();
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase)) {
                    // Fill in team IDs in the userList for the users based on their email
                    await PopulateTeamUsers(users);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("The Users Team IDs have been updated!");
                    Console.ResetColor();

                    UsersHelper.StoreUserList(users);
                } else {
                    // Keep the team IDs as they are and don't make any changes
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("The Users Team IDS have been kept as is!");
                    Console.ResetColor();
                }
            }

            // Load userList instead of generating one from the slack archive
            if (loadUserListInstead) {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Loading user from existing User List!");
                Console.ResetColor();

                List<STUser> users = UsersHelper.LoadUserList();
                await AskToPopulateTeamIDs(users);

                return users;
            }

            // Get slack user JSON from the slack export archive
            string slackUsersPath = GetSlackUsersPath(slackArchiveBasePath);

            // Scan users from the JSON and get team IDs for the respective users based on their email
            // Then store it locally in the userList
            List<STUser> userList = UsersHelper.ScanUsersFromSlack(slackUsersPath);
            await PopulateTeamUsers(userList);
            UsersHelper.StoreUserList(userList);

            // Ask user if he wants to reload it so he can make changes to it
            // after it has been computed and stored
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("You now have the possibility to make changes to this file if you want");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Press any key to continue ");
            Console.ResetColor();
            Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Do you want to reload the User List from disk? [Y/n] ");
            Console.ResetColor();
            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase) || input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || input.Equals("true", StringComparison.CurrentCultureIgnoreCase)) {
                // Reload the userList from disk (used when user has made manual changes to the userList)
                List<STUser> users = UsersHelper.LoadUserList();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("The User List has been reloaded!");
                Console.ResetColor();

                // Now ask to repopulate team IDs based on emails in the userList
                await AskToPopulateTeamIDs(users);

                return users;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("The User List has been kept as is!");
            Console.ResetColor();
            return userList;
        }

        #endregion

        #endregion
        #region Message Handling

        #region Method - ScanAndHandleMessages

        static async Task ScanAndHandleMessages(GraphHelper graphHelper, string slackArchiveBasePath, List<STChannel> channelList, List<STUser> userList, string teamID) {
            foreach (var channel in channelList) {
                if (channel != null) {
                    // Create migration channel
                    string? channelID = await CreateChannel(graphHelper, teamID, channel);

                    if (!string.IsNullOrEmpty(channelID)) {
                        string slackChannelFilesPath = Path.Combine(slackArchiveBasePath, channel.DisplayName);

                        if (!File.Exists(slackChannelFilesPath)) {
                            foreach (var file in MessageHandling.GetFilesForChannel(slackChannelFilesPath)) {
                                foreach (var message in MessageHandling.GetMessagesForDay(file, channelList, userList)) {
                                    if (message.IsInThread && !message.IsParentThread) {
                                        await SendMessageToChannelThread(graphHelper, teamID, channelID, message);
                                        continue;
                                    }
                                    await SendMessageToTeamChannel(graphHelper, teamID, channelID, message);
                                }
                            }
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Cannot find ID for - teamID:{teamID}: channel:{channel.DisplayName}:");
                        Console.ResetColor();
                    }
                }
            }
        }

        #endregion

        #endregion
        #region Migration Handling

        #region Method - CompleteChannelMigrationAsync

        static async Task CompleteChannelMigrationAsync(GraphHelper graphHelper, string teamID, string channelID, string channelName) {
            try {
                await graphHelper.CompleteChannelMigrationAsync(teamID, channelID);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of channel: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Channel {channelName} [{channelID}] has been migrated!");
            Console.ResetColor();
        }

        #endregion
        #region Method - CompleteTeamMigrationAsync

        static async Task CompleteTeamMigrationAsync(GraphHelper graphHelper, string teamID) {
            try {
                await graphHelper.CompleteTeamMigrationAsync(teamID);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of team: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Team [{teamID}] has been migrated!");
            Console.WriteLine();
            Console.ResetColor();
        }

        #endregion

        #endregion
        #region File Handling

        #region Method - GetSlackArchiveBasePath

        static string GetSlackArchiveBasePath(string directory, string arg) {
            string slackArchiveBasePath = string.Empty;
            bool isValidPath = false;

            if (!string.IsNullOrEmpty(arg)) {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("Retrieving Slack Export folder...");
                Console.ResetColor();

                slackArchiveBasePath = Path.GetFullPath(Path.Combine(directory, @arg));
                isValidPath = Directory.Exists(slackArchiveBasePath);
                if (!isValidPath) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{slackArchiveBasePath} is not a valid path!");
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }

            while (!isValidPath) {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Relative path to local Slack Archive folder: ");
                Console.ResetColor();
                var userReadPath = Console.ReadLine() ?? string.Empty;
                slackArchiveBasePath = Path.GetFullPath(Path.Combine(directory, @userReadPath));
                isValidPath = Directory.Exists(slackArchiveBasePath);
                if (!isValidPath) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{slackArchiveBasePath} is not a valid path! Try again...");
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Export folder");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackArchiveBasePath);
            Console.WriteLine();
            Console.ResetColor();

            return slackArchiveBasePath;
        }

        #endregion
        #region Method - GetSlackChannelsPath

        static string GetSlackChannelsPath(string slackArchiveBasePath) {
            string slackChannelsPath = Path.Combine(slackArchiveBasePath, "channels.json");

            if (!File.Exists(slackChannelsPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find channels.json: {slackChannelsPath}");
                Console.WriteLine("Exiting...");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Channels file");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackChannelsPath);
            Console.ResetColor();

            return slackChannelsPath;
        }

        #endregion
        #region Method - GetSlackUsersPath

        static string GetSlackUsersPath(string slackArchiveBasePath) {
            string slackUsersPath = Path.Combine(slackArchiveBasePath, "users.json");

            if (!File.Exists(slackUsersPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find users.json in : {slackUsersPath}");
                Console.WriteLine("Exiting...");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Users file");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackUsersPath);
            Console.ResetColor();

            return slackUsersPath;
        }

        #endregion

        #endregion
        #region Graph Callers

        #region Method - CreateTeam

        static async Task<string?> CreateTeam(GraphHelper graphHelper) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Processing team data file: {TEAM_DATA_FILE}");
            Console.ResetColor();

            string? teamId = string.Empty;
            try {
                using StreamReader reader = new(TEAM_DATA_FILE);
                string json = reader.ReadToEnd();

                // Get the team name
                STTeam? team = JsonConvert.DeserializeObject<STTeam>(json);

                if (
                    team != null &&
                    !string.IsNullOrWhiteSpace(team.DisplayName)
                ) {
                    string teamName = team.DisplayName;
                    teamId = await graphHelper.CreateTeamAsync(json);
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating Team: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(teamId)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating Team, ID came back null!");
                Console.ResetColor();
                Environment.Exit(1);
            } else {
                await Task.Delay(2000); // ? Wait for team to be accessible (otherwise first channel migration will fail!)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Created Team with ID: {teamId}");
                Console.ResetColor();
            }

            return teamId;
        }

        #endregion
        #region Methods - ListJoinedTeamsAsync

        static async Task<TeamCollectionResponse?> ListJoinedTeamsAsync(GraphHelper graphHelper) {
            try {
                return await graphHelper.GetJoinedTeamsAsync();
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting user's teams: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        static async Task<ChannelCollectionResponse?> ListJoinedTeamsAsync(GraphHelper graphHelper, string teamID) {
            try {
                return await graphHelper.GetTeamsChannelsAsync(teamID);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting teams channels: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        #endregion
        #region Method - GetChannelByName

        static async Task<string?> GetChannelByName(GraphHelper graphHelper, string teamID, string channelName) {
            string? channelId = string.Empty;

            try {
                channelId = await graphHelper.GetChannelByNameAsync(teamID, channelName);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting Channel {channelName}: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Got Channel [{channelName}] ID [{channelId}]");
            Console.ResetColor();

            return channelId;
        }

        #endregion
        #region Method - CreateChannel

        static async Task<string?> CreateChannel(GraphHelper graphHelper, string teamID, STChannel channel) {
            string? channelId = string.Empty;
            string actionName = "finding";

            if (channel != null) {
                try {
                    // First check if the channel exists
                    channelId = await graphHelper.GetChannelByNameAsync(teamID, channel.DisplayName);
                    // If not found then create
                    if (string.IsNullOrWhiteSpace(channelId)) {
                        channelId = await graphHelper.CreateChannelAsync(teamID, channel);
                        actionName = "creating";
                    }
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error {actionName} channel {channel.DisplayName}: {ex.Message}");
                    Console.ResetColor();
                    return channelId;
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Sucess {actionName} channel '{channel.DisplayName}' [{channelId}]");
                Console.ResetColor();
            }

            return channelId;
        }

        #endregion
        #region Method - SendMessageToChannelThread

        static async Task SendMessageToChannelThread(GraphHelper graphHelper, string teamID, string channelID, STMessage message) {
            try {
                if (string.IsNullOrEmpty(message.TeamID)) {
                    return;
                }

                await graphHelper.SendMessageToChannelThreadAsync(teamID, channelID, message.TeamID, message);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message: {ex.Message}");
                Console.ResetColor();
            }
        }

        #endregion
        #region Method - SendMessageToTeamChannel

        static async Task SendMessageToTeamChannel(GraphHelper graphHelper, string teamID, string channelID, STMessage message) {
            try {
                await graphHelper.SendMessageToChannelAsync(teamID, channelID, message);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message: {ex.Message}");
                Console.ResetColor();
            }
        }

        #endregion
        #region Method - UploadFileToPath

        static async Task UploadFileToPath(GraphHelper graphHelper, string teamID, string channelName, STAttachment attachment) {
            try {
                await graphHelper.UploadFileToTeamChannelAsync(teamID, channelName, attachment);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error uploading file: {ex.Message}");
                Console.ResetColor();
            }
        }

        #endregion
        #region Method - AddAttachmentsToMessage

        static async Task AddAttachmentsToMessage(GraphHelper graphHelper, string teamID, string channelID, STMessage message) {
            try {
                await graphHelper.AddAttachmentsToMessageAsync(teamID, channelID, message);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error adding attachment to message: {ex.Message}");
                Console.ResetColor();
            }
        }

        #endregion

        #endregion
    }
}
