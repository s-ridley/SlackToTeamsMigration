using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Newtonsoft.Json;
using SlackToTeams.Models;
using SlackToTeams.Utils;

namespace SlackToTeams.Services {
    internal class MigrationService : IMigrationService {
        #region Fields

        private readonly IConfiguration _config;
        private readonly ILogger<MigrationService> _logger;

        #endregion
        #region Constants

        public const string ACTION_FIND = "finding";
        public const string ACTION_CREATE = "creating";

        #endregion
        #region Constructors

        public MigrationService(IConfiguration config, ILogger<MigrationService> logger) {
            _config = config;
            _logger = logger;
        }

        #endregion
        #region Method - MigrateAsync

        public async Task MigrateAsync() {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine();
            Console.WriteLine("================================");
            Console.WriteLine("|| [MIGRATION] Slack -> Teams ||");
            Console.WriteLine("================================");
            Console.WriteLine();
            Console.ResetColor();

            _logger.LogDebug("Migration - START");

            /*
            ** INITIALIZATION
            */
            AuthenticationConfig? config = _config.Get<AuthenticationConfig>();

            if (config != null) {
                string? input;
                string? teamId = null;

                GraphHelper graphHelper = new(config);

                /*
                ** FILE HANDLING
                */
                string slackArchiveBasePath;
                if (string.IsNullOrWhiteSpace(config.SlackExportDir)) {
                    string directory = Directory.GetCurrentDirectory();
                    string[] arguments = Environment.GetCommandLineArgs();
                    slackArchiveBasePath = GetSlackArchiveBasePath(directory, arguments.Length > 0 ? arguments[0] : string.Empty);
                } else {
                    slackArchiveBasePath = config.SlackExportDir;
                }

                /*
                ** LOADING SLACK CHANNEL LIST
                */

                // Get slack channels JSON from the slack export archive
                string slackChannelsPath = GetSlackChannelsPath(slackArchiveBasePath);

                // Scan channels from the JSON
                List<SlackChannel> channelList = ChannelsHelper.ScanChannelsFromSlack(slackChannelsPath);

                if (
                    channelList != null &&
                    channelList.Count > 0
                ) {
                    _logger.LogInformation("Loaded Slack channels - count:{channelListCount}:", channelList.Count);

                    /*
                    ** LOADING USER LIST OR CREATING NEW
                    */
                    bool loadCurrentUserList = false;
                    if (UsersHelper.UserListExists()) {
                        _logger.LogInformation("Found User list ");

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"We found existing User List: {UsersHelper.USER_LIST_FILE}");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Do you want to load it? [Y/n] ");
                        Console.ResetColor();
                        input = Console.ReadLine();
                        if (
                            string.IsNullOrEmpty(input) ||
                            input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                        ) {
                            loadCurrentUserList = true;
                            _logger.LogInformation("Loading User list");
                        } else {
                            _logger.LogInformation("NOT loading User list");
                        }
                    }

                    List<SlackUser> userList = await ScanAndHandleUsers(graphHelper, slackArchiveBasePath, loadCurrentUserList);
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Do you want to create a new migration team and migrate messages? [y/N] ");
                    Console.ResetColor();
                    input = Console.ReadLine();

                    if (
                        !string.IsNullOrEmpty(input) &&
                        (
                            input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                        )
                    ) {
                        _logger.LogInformation("Starting to migrate messages");

                        /*
                        ** MIGRATE MESSAGES FROM SLACK TO TEAMS
                        */
                        teamId = await CreateTeam(graphHelper);

                        if (!string.IsNullOrWhiteSpace(teamId)) {
                            // Scan and send messages in Teams
                            await ScanAndHandleMessages(graphHelper, slackArchiveBasePath, channelList, userList, teamId);

                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write("Do you want to finish migrating the team? [y/N] ");
                            Console.ResetColor();
                            input = Console.ReadLine();

                            if (
                                !string.IsNullOrEmpty(input) &&
                                (
                                    input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                                    input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                                    input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                                )
                            ) {
                                if (!string.IsNullOrEmpty(teamId)) {
                                    await FinishMigrating(graphHelper, teamId);
                                }
                            }
                        }
                    } else {
                        /*
                        ** SHOW OPTION TO CLOSE MIGRATION BY TEAM NAME
                        */
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Do you want to finish migrating an existing team stuck in migration? [y/N] ");
                        Console.ResetColor();
                        input = Console.ReadLine();

                        if (
                            !string.IsNullOrEmpty(input) &&
                            (
                                input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                                input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                                input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                            )
                        ) {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write("Do you want to enter a name? [y/N] ");
                            Console.ResetColor();
                            input = Console.ReadLine();

                            if (
                                !string.IsNullOrEmpty(input) &&
                                (
                                    input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                                    input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                                    input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                                )
                            ) {
                                while (string.IsNullOrEmpty(teamId)) {
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine("Which team do you want to finish migrating a team?");
                                    Console.Write("Input Team Name: ");

                                    Console.ResetColor();
                                    string? teamName = Console.ReadLine();

                                    // Get the team ID
                                    if (!string.IsNullOrEmpty(teamName)) {
                                        teamId = await GetTeamByName(graphHelper, teamName);
                                        if (!string.IsNullOrEmpty(teamId)) {
                                            await FinishMigrating(graphHelper, teamId);
                                        }
                                    }
                                }
                            } else {
                                // Get the team name
                                SlackTeam? team = _config.Get<SlackTeam>();
                                if (
                                    team != null &&
                                    !string.IsNullOrWhiteSpace(team.DisplayName)
                                ) {
                                    await FinishMigratingByName(graphHelper, team.DisplayName);
                                }
                            }
                        }
                    }

                    /*
                    ** MIGRATE ATTACHMENTS TO EXISTING TEAM
                    */
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Do you want to migrate attachments to a team? [y/N] ");
                    Console.ResetColor();
                    input = Console.ReadLine();

                    if (
                        !string.IsNullOrEmpty(input) &&
                        (
                            input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                            input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                        )
                    ) {
                        string? uploadTeamId = null;
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

                            uploadTeamId = teams.Value[choice].Id;
                        }
                        if (!string.IsNullOrEmpty(uploadTeamId)) {
                            await UploadAttachmentsToTeam(graphHelper, slackArchiveBasePath, channelList, userList, uploadTeamId);
                        }
                    }
                } else {
                    _logger.LogError("No Slack channels found");
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

            _logger.LogInformation("Migration - END");
        }

        #endregion
        #region Method - UserInputIndexOfList

        private static int UserInputIndexOfList() {
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

        #endregion
        #region Method - FinishMigratingByName

        private async Task FinishMigratingByName(GraphHelper graphHelper, string teamName) {
            _logger.LogDebug("FinishMigrating - Start - name:{teamName}", teamName);
            Console.WriteLine();

            // Get the team ID
            if (!string.IsNullOrEmpty(teamName)) {
                string? teamId = await GetTeamByName(graphHelper, teamName);

                if (!string.IsNullOrEmpty(teamId)) {
                    await FinishMigrating(graphHelper, teamId);
                }
            }

            _logger.LogDebug("FinishMigratingByName - End");
        }

        #endregion
        #region Method - FinishMigrating

        // If migration failed and you're left with a team stuck in migration mode, use this function!
        private async Task FinishMigrating(GraphHelper graphHelper, string teamId) {
            _logger.LogDebug("FinishMigrating - Start - ID[{teamId}]", teamId);
            Console.WriteLine();

            var channels = await ListTeamChannelsAsync(graphHelper, teamId);
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
                        await CompleteChannelMigrationAsync(graphHelper, teamId, channel.Id, channel.DisplayName);
                    }
                }
            }

            await CompleteTeamMigrationAsync(graphHelper, teamId);

            await AssignTeamOwnerAsync(graphHelper, teamId);

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("===========================================");
            Console.WriteLine("|| !! MIGRATION OF TEAM WAS A SUCCESS !! ||");
            Console.WriteLine("===========================================");
            Console.ResetColor();

            _logger.LogDebug("FinishMigrating - End");
        }

        #endregion
        #region Upload Handling

        #region Method - UploadAttachmentsToTeam

        private async Task UploadAttachmentsToTeam(GraphHelper graphHelper, string slackArchiveBasePath, List<SlackChannel> channelList, List<SlackUser> userList, string teamID) {
            _logger.LogDebug("UploadAttachmentsToTeam - Start");
            if (channelList != null) {
                foreach (var channel in channelList) {
                    if (string.Equals(channel.DisplayName, "general", StringComparison.CurrentCultureIgnoreCase)) {
                        channel.DisplayName = "General";
                    }

                    string? channelID = await GetChannelByName(graphHelper, teamID, channel.DisplayName);

                    if (!string.IsNullOrWhiteSpace(channelID)) {
                        string slackChannelFilesPath = Path.Combine(slackArchiveBasePath, channel.DisplayName);

                        foreach (var file in MessageHandling.GetFilesForChannel(slackChannelFilesPath, "*.old")) {
                            foreach (var message in MessageHandling.GetMessagesForDay(channel.DisplayName, file, channelList, userList)) {
                                if (
                                    message != null &&
                                    message.Attachments != null &&
                                    message.Attachments.Count > 0
                                ) {
                                    foreach (var attachment in message.Attachments) {
                                        // Check if the attachment is not an image
                                        if (
                                            attachment != null &&
                                            !string.IsNullOrWhiteSpace(attachment.SlackURL) &&
                                            !GraphHelper.ValidHostedContent(attachment)
                                        ) {
                                            await UploadFileToPath(graphHelper, teamID, channel.DisplayName, attachment);
                                        }
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
            _logger.LogDebug("UploadAttachmentsToTeam - End");
        }

        #endregion

        #endregion
        #region User Handling

        #region Method - ScanAndHandleUsers

        private async Task<List<SlackUser>> ScanAndHandleUsers(GraphHelper graphHelper, string slackArchiveBasePath, bool loadUserListInstead) {
            _logger.LogDebug("ScanAndHandleUsers - Start");
            async Task PopulateTeamUsers(List<SlackUser> users) {
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
                    _logger.LogError(ex, "ScanAndHandleUsers - Error team users export path:{slackArchiveBasePath} loadUserList:{loadUserListInstead} error:{errorMessage}", slackArchiveBasePath, loadUserListInstead, ex.Message);
                    Environment.Exit(1);
                }
            }

            async Task AskToPopulateTeamIDs(List<SlackUser> users) {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Do you want to populate TeamIDs? [y/N] ");
                Console.ResetColor();
                string? input = Console.ReadLine();

                if (
                    !string.IsNullOrEmpty(input) &&
                    (
                        input.Equals("y", StringComparison.CurrentCultureIgnoreCase) ||
                        input.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                        input.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                    )
                ) {
                    // Fill in team IDs in the userList for the users based on their email
                    await PopulateTeamUsers(users);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("The Users Team IDs have been updated!");
                    Console.ResetColor();
                    _logger.LogDebug("ScanAndHandleUsers - Populate user team IDs");
                    UsersHelper.StoreUserList(users);
                } else {
                    // Keep the team IDs as they are and don't make any changes
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("The Users Team IDS have been kept as is!");
                    Console.ResetColor();
                    _logger.LogDebug("ScanAndHandleUsers - DO NOT populate user team IDs");
                }
            }

            // Load userList instead of generating one from the slack archive
            if (loadUserListInstead) {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Loading user from existing User List!");
                Console.ResetColor();

                List<SlackUser> users = UsersHelper.LoadUserList();
                await AskToPopulateTeamIDs(users);

                _logger.LogDebug("ScanAndHandleUsers - End - userCount:{userCount}", (users != null ? users.Count : 0));
                return (List<SlackUser>?)users ?? [];
            }

            // Get slack user JSON from the slack export archive
            string slackUsersPath = GetSlackUsersPath(slackArchiveBasePath);

            // Scan users from the JSON and get team IDs for the respective users based on their email
            // Then store it locally in the userList
            List<SlackUser> userList = UsersHelper.ScanUsersFromSlack(slackUsersPath);
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
                List<SlackUser> users = UsersHelper.LoadUserList();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("The User List has been reloaded!");
                Console.ResetColor();

                // Now ask to repopulate team IDs based on emails in the userList
                await AskToPopulateTeamIDs(users);

                _logger.LogDebug("ScanAndHandleUsers - End - userCount:{userCount}", (users != null ? users.Count : 0));
                return (List<SlackUser>?)users ?? [];
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("The User List has been kept as is!");
            Console.ResetColor();

            _logger.LogDebug("ScanAndHandleUsers - End - userCount:{userCount}", (userList != null ? userList.Count : 0));
            return (List<SlackUser>?)userList ?? [];
        }

        #endregion

        #endregion
        #region Message Handling

        #region Method - ScanAndHandleMessages

        private async Task ScanAndHandleMessages(GraphHelper graphHelper, string slackArchiveBasePath, List<SlackChannel> channelList, List<SlackUser> userList, string teamId) {
            _logger.LogDebug("ScanAndHandleMessages - Start");

            foreach (var channel in channelList) {
                if (channel != null) {
                    // Create migration channel
                    string? channelId = await CreateChannel(graphHelper, teamId, channel);

                    if (
                        !string.IsNullOrEmpty(channelId) &&
                        !string.IsNullOrEmpty(channel.SlackFolder)
                    ) {
                        string slackChannelFilesPath = Path.Combine(slackArchiveBasePath, channel.SlackFolder);

                        if (Path.Exists(slackChannelFilesPath)) {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Processing channel:{channel.DisplayName} folder:{slackChannelFilesPath}");
                            Console.ResetColor();

                            _logger.LogDebug("Processing channel:{channelName} folder:{slackExportPath}", channel.DisplayName, slackChannelFilesPath);

                            string htmlFile = Path.Combine(slackChannelFilesPath, "export.html");
                            HtmlHelper.StartHtml(htmlFile);

                            foreach (var file in MessageHandling.GetFilesForChannel(slackChannelFilesPath, "*.json")) {
                                _logger.LogDebug("Processing file:{file}", file);
                                foreach (var message in MessageHandling.GetMessagesForDay(channel.DisplayName, file, channelList, userList)) {
                                    if (message != null) {
                                        if (
                                            message.Attachments != null &&
                                            message.Attachments.Count > 0
                                        ) {
                                            foreach (var attachment in message.Attachments) {
                                                // Check if the attachment is not an image
                                                if (
                                                    attachment != null &&
                                                    !string.IsNullOrWhiteSpace(attachment.SlackURL) &&
                                                    !GraphHelper.ValidHostedContent(attachment)
                                                ) {
                                                    // If so upload to teams drive
                                                    await attachment.DownloadFile(
                                                        slackArchiveBasePath,   // baseDownloadPath
                                                        true                    //overwriteFile
                                                    );
                                                }
                                            }
                                        }

                                        if (message.IsInThread && !message.IsParentThread) {
                                            _logger.LogDebug("Processing message as in thread sent:{dateTime} from:{from}", message.Date, message.User?.DisplayName);
                                            _ = await SendMessageToChannelThread(graphHelper, teamId, channelId, message);
                                        } else {
                                            _logger.LogDebug("Processing message sent:{dateTime} from:{from}", message.Date, message.User?.DisplayName);
                                            _ = await SendMessageToTeamChannel(graphHelper, teamId, channelId, message);
                                        }
                                        HtmlHelper.MessageToHtml(htmlFile, message);
                                    }
                                }
                                try {
                                    // Rename the file so it will not be processed again
                                    File.Move(file, Path.ChangeExtension(file, ".old"));
                                    _logger.LogDebug("Mark file:{file} as done", file);
                                } catch (Exception ex) {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Error renaming {file} to old: {ex.Message}");
                                    Console.ResetColor();
                                    _logger.LogError(ex, "Error renaming {file} to old error:{errorMessage}", file, ex.Message);
                                    Environment.Exit(1);
                                }
                            }

                            HtmlHelper.EndHtml(htmlFile);
                        } else {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Channel folder does not exist :{slackChannelFilesPath}");
                            Console.ResetColor();
                            _logger.LogWarning("Channel folder does not exist :{slackChannelFilesPath}", slackChannelFilesPath);
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Cannot find ID for - teamID:{teamId}: channel:{channel.DisplayName}:");
                        Console.ResetColor();
                        _logger.LogWarning("Channel details invalid ID[{channelId}] folder:{slackFolder}", channelId, channel.SlackFolder);
                    }
                }
            }
        }

        #endregion

        #endregion
        #region Migration Handling

        #region Method - CompleteChannelMigrationAsync

        private async Task CompleteChannelMigrationAsync(GraphHelper graphHelper, string teamId, string channelId, string channelName) {
            _logger.LogDebug("CompleteChannelMigrationAsync - Start team[{teamId}] channel {channelName}", teamId, channelName);
            try {
                await graphHelper.CompleteChannelMigrationAsync(teamId, channelId);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of channel TeamID[{teamId}] channelID[{channelId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}[");
                Console.ResetColor();
                _logger.LogError(odataError, "CompleteChannelMigrationAsync - Error finishing migration of channel - TeamID[{teamId}] channelID[{channelId}] code:{errorCode} message:{errorMessage}", teamId, channelId, odataError?.Error?.Code, odataError?.Error?.Message);
                Environment.Exit(1);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of channel: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "CompleteChannelMigrationAsync - Error finishing migration of channel - TeamID[{teamId}] channelID[{channelId}] error:{errorMessage}", teamId, channelId, ex.Message);
                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Channel {channelName} [{channelId}] has been migrated!");
            Console.ResetColor();
            _logger.LogDebug("CompleteChannelMigrationAsync - End");
        }

        #endregion
        #region Method - CompleteTeamMigrationAsync

        private async Task CompleteTeamMigrationAsync(GraphHelper graphHelper, string teamId) {
            _logger.LogDebug("CompleteTeamMigrationAsync - Start team[{teamId}]", teamId);
            try {
                await graphHelper.CompleteTeamMigrationAsync(teamId);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of team[{teamId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "CompleteTeamMigrationAsync - Error finishing migration of team - TeamID[{teamId}] code:{errorCode} message:{errorMessage}", teamId, odataError?.Error?.Code, odataError?.Error?.Message);
                Environment.Exit(1);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of team: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "CompleteTeamMigrationAsync - Error finishing migration of team - TeamID[{teamId}] error:{errorMessage}", teamId, ex.Message);
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Team [{teamId}] has been migrated!");
            Console.WriteLine();
            Console.ResetColor();
            _logger.LogDebug("CompleteTeamMigrationAsync - End");
        }

        #endregion
        #region Method - AssignTeamOwnerAsync

        private async Task AssignTeamOwnerAsync(GraphHelper graphHelper, string teamId) {
            _logger.LogDebug("AssignTeamOwnerAsync - Start Team[{teamId}]", teamId);
            try {
                await graphHelper.AssignTeamOwnerAsync(teamId);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error assigning owner to team[{teamId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "AssignTeamOwnerAsync - Error assigning owner to team - TeamID[{teamId}] code:{errorCode} message:{errorMessage}", teamId, odataError?.Error?.Code, odataError?.Error?.Message);
                Environment.Exit(1);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error finishing migration of team: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "AssignTeamOwnerAsync - Error assigning owner to team - TeamID[{teamId}] error:{errorMessage}", teamId, ex.Message);
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Owner assigned to team [{teamId}]");
            Console.WriteLine();
            Console.ResetColor();
            _logger.LogDebug("AssignTeamOwnerAsync - End");
        }

        #endregion

        #endregion
        #region File Handling

        #region Method - GetSlackArchiveBasePath

        private string GetSlackArchiveBasePath(string directory, string arg) {
            _logger.LogDebug("GetSlackArchiveBasePath - Start");
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

                    _logger.LogError("GetSlackArchiveBasePath - Slack export path not valid :{slackArchiveBasePath}", slackArchiveBasePath);
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

                    _logger.LogError("GetSlackArchiveBasePath - Slack export path not valid :{slackArchiveBasePath}", slackArchiveBasePath);
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Export folder");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackArchiveBasePath);
            Console.WriteLine();
            Console.ResetColor();

            _logger.LogInformation("GetSlackArchiveBasePath - Slack export base path :{slackArchiveBasePath}", slackArchiveBasePath);

            return slackArchiveBasePath;
        }

        #endregion
        #region Method - GetSlackChannelsPath

        private string GetSlackChannelsPath(string slackArchiveBasePath) {
            _logger.LogDebug("GetSlackChannelsPath - Start");
            string slackChannelsPath = Path.Combine(slackArchiveBasePath, "channels.json");

            if (!File.Exists(slackChannelsPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find channels.json: {slackChannelsPath}");
                Console.WriteLine("Exiting...");
                Console.ResetColor();

                _logger.LogError("GetSlackChannelsPath - Could not find channels file :{slackChannelsPath}", slackChannelsPath);

                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Channels file");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackChannelsPath);
            Console.ResetColor();

            _logger.LogInformation("GetSlackChannelsPath - Found channels file :{slackChannelsPath}", slackChannelsPath);

            return slackChannelsPath;
        }

        #endregion
        #region Method - GetSlackUsersPath

        private string GetSlackUsersPath(string slackArchiveBasePath) {
            _logger.LogDebug("GetSlackUsersPath - Start");
            string slackUsersPath = Path.Combine(slackArchiveBasePath, "users.json");

            if (!File.Exists(slackUsersPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find users.json in : {slackUsersPath}");
                Console.WriteLine("Exiting...");
                Console.ResetColor();

                _logger.LogError("GetSlackUsersPath - Could not find users file :{slackChannelsPath}", slackUsersPath);

                Environment.Exit(1);
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Successfully retrieved Slack Users file");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(slackUsersPath);
            Console.ResetColor();

            _logger.LogInformation("GetSlackUsersPath - Found users file :{slackChannelsPath}", slackUsersPath);

            return slackUsersPath;
        }

        #endregion

        #endregion
        #region Graph Callers

        #region Method - GetTeamByName

        private async Task<string?> GetTeamByName(GraphHelper graphHelper, string teamName) {
            _logger.LogDebug("GetTeamByName - Start");

            string? teamId = string.Empty;

            try {
                teamId = await graphHelper.GetTeamByNameAsync(teamName);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error {ACTION_FIND} team:{teamName} [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "GetTeamByName - Error {ACTION_FIND} Team {teamName} code:{errorCode} message:{errorMessage}", ACTION_FIND, teamName, odataError?.Error?.Code, odataError?.Error?.Message);
                Environment.Exit(1);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error {ACTION_FIND} Team {teamName}: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "GetTeamByName - Could not find team by name :{teamName}", teamName);
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Got Team [{teamName}] ID [{teamId}]");
            Console.ResetColor();

            _logger.LogInformation("GetTeamByName - Got Team ID [{teamId}] by name :{teamName}", teamId, teamName);

            return teamId;
        }

        #endregion
        #region Method - CreateTeam

        private async Task<string?> CreateTeam(GraphHelper graphHelper) {
            _logger.LogDebug("CreateTeam - Start");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Processing team config");
            Console.ResetColor();

            string? teamId = string.Empty;
            string teamName = "";
            string actionName = ACTION_FIND;

            try {
                // Get the team name
                SlackTeam? team = _config.Get<SlackTeam>();

                if (
                    team != null &&
                    !string.IsNullOrWhiteSpace(team.DisplayName)
                ) {
                    teamName = team.DisplayName;

                    // First check if the channel exists
                    teamId = await graphHelper.GetTeamByNameAsync(team.DisplayName);

                    _logger.LogDebug("CreateTeam - Team found result {teamId}", teamId);

                    // If not found then create
                    if (string.IsNullOrWhiteSpace(teamId)) {
                        string json = JsonConvert.SerializeObject(team);
                        teamId = await graphHelper.CreateTeamAsync(json);
                        actionName = ACTION_CREATE;

                        _logger.LogDebug("Team creation result {teamId}", teamId);
                    }
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error {actionName} Team '{teamName}', {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "CreateTeam - Could not create team :{teamName} error:{error}", teamName, ex.Message);
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(teamId)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error {actionName} Team '{teamName}', ID came back null!");
                Console.ResetColor();

                _logger.LogError("CreateTeam - Could not create team :{teamName} ID came back null", teamName);

                Environment.Exit(1);
            } else {
                await Task.Delay(2000); // ? Wait for team to be accessible (otherwise first channel migration will fail!)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Sucess {actionName} Team '{teamName}' ID[{teamId}]");
                Console.ResetColor();

                _logger.LogInformation("CreateTeam - Sucessfully created team :{teamName} ID[{teamId}]", teamName, teamId);
            }

            return teamId;
        }

        #endregion
        #region Methods - ListJoinedTeamsAsync

        private async Task<TeamCollectionResponse?> ListJoinedTeamsAsync(GraphHelper graphHelper) {
            _logger.LogDebug("ListJoinedTeamsAsync - Start");
            try {
                return await graphHelper.GetUserTeamsAsync();
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting users teams: [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "ListJoinedTeamsAsync - Error getting users teams code:{errorCode} message:{errorMessage}", odataError?.Error?.Code, odataError?.Error?.Message);
                throw;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting users teams: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "ListJoinedTeamsAsync - Error getting users teams error:{errorMessage}", ex.Message);
                throw;
            }
        }

        #endregion
        #region Methods - ListTeamChannelsAsync

        private async Task<ChannelCollectionResponse?> ListTeamChannelsAsync(GraphHelper graphHelper, string teamId) {
            _logger.LogDebug("ListTeamChannelsAsync - Start");
            try {
                return await graphHelper.GetTeamsChannelsAsync(teamId);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting teams:{teamId} channels [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "ListTeamChannelsAsync - Error getting teams:{teamId} channels code:{errorCode} message:{errorMessage}", teamId, odataError?.Error?.Code, odataError?.Error?.Message);
                throw;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting teams channels: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "ListTeamChannelsAsync - Error getting teams:{teamId} channels error:{errorMessage}", teamId, ex.Message);
                throw;
            }
        }

        #endregion
        #region Method - GetChannelByName

        private async Task<string?> GetChannelByName(GraphHelper graphHelper, string teamId, string channelName) {
            _logger.LogDebug("GetChannelByName - Start");
            string? channelId = string.Empty;

            try {
                channelId = await graphHelper.GetChannelByNameAsync(teamId, channelName);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting chanelId name:{channelName} [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "GetChannelByName - Error getting chanelId name:{channelName} code:{errorCode} message:{errorMessage}", channelName, odataError?.Error?.Code, odataError?.Error?.Message);
                throw;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error getting Channel {channelName}: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "GetChannelByName - Error getting chanelId name:{channelName} error:{errorMessage}", channelName, ex.Message);
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Got Channel [{channelName}] ID [{channelId}]");
            Console.ResetColor();

            _logger.LogDebug("GetChannelByName - End ID[{channelId}]", channelId);
            return channelId;
        }

        #endregion
        #region Method - CreateChannel

        private async Task<string?> CreateChannel(GraphHelper graphHelper, string teamID, SlackChannel channel) {
            _logger.LogDebug("CreateChannel - Start");
            string? channelId = string.Empty;
            string actionName = ACTION_FIND;

            if (channel != null) {
                try {
                    // First check if the channel exists
                    channelId = await graphHelper.GetChannelByNameAsync(teamID, channel.DisplayName);
                    _logger.LogDebug("CreateChannel - Found channel ID[{channelId}]", channelId);
                    // If not found then create
                    if (string.IsNullOrWhiteSpace(channelId)) {
                        channelId = await graphHelper.CreateChannelAsync(teamID, channel);
                        actionName = ACTION_CREATE;
                        _logger.LogDebug("CreateChannel - Created channel ID[{channelId}]", channelId);
                    }
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error {actionName} Channel '{channel.DisplayName}', {ex.Message}");
                    Console.ResetColor();
                    _logger.LogError(ex, "CreateChannel - Error {actionName} channel name:{channelName} error:{errorMessage}", actionName, channel.DisplayName, ex.Message);
                    return channelId;
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"CreateChannel - Sucess {actionName} Channel '{channel.DisplayName}' [{channelId}]");
                Console.ResetColor();
            }

            _logger.LogDebug("CreateChannel - End ID[{channelId}]", channelId);
            return channelId;
        }

        #endregion
        #region Method - SendMessageToChannelThread

        private async Task<ChatMessage?> SendMessageToChannelThread(GraphHelper graphHelper, string teamId, string channelId, SlackMessage message) {
            _logger.LogDebug("SendMessageToChannelThread - Start");
            try {
                if (string.IsNullOrEmpty(message.TeamID)) {
                    return null;
                }
                return await graphHelper.SendMessageToChannelThreadAsync(teamId, channelId, message.TeamID, message);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message TeamID[{teamId}] ChannelID[{channelId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "SendMessageToChannelThread - Error sending message TeamID[{teamId}] ChannelID[{channelId}] Date[{date}] From[{from}] code:{errorCode} message:{errorMessage}", teamId, channelId, message.Date, message.User?.DisplayName, odataError?.Error?.Code, odataError?.Error?.Message);
                return null;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "SendMessageToChannelThread - Error sending message TeamID[{teamId}] ChannelID[{channelId}] Date[{date}] From[{from}] error:{errorMessage}", teamId, channelId, message.Date, message.User?.DisplayName, ex.Message);
                return null;
            }
        }

        #endregion
        #region Method - SendMessageToTeamChannel

        private async Task<ChatMessage?> SendMessageToTeamChannel(GraphHelper graphHelper, string teamId, string channelId, SlackMessage message) {
            _logger.LogDebug("SendMessageToTeamChannel - Start");
            try {
                return await graphHelper.SendMessageToChannelAsync(teamId, channelId, message);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message TeamID[{teamId}] ChannelID[{channelId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "SendMessageToTeamChannel - Error sending message TeamID[{teamId}] ChannelID[{channelId}] Date[{date}] From[{from}] code:{errorCode} message:{errorMessage}", teamId, channelId, message.Date, message.User?.DisplayName, odataError?.Error?.Code, odataError?.Error?.Message);
                return null;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending message: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "SendMessageToTeamChannel - Error sending message TeamID[{teamId}] ChannelID[{channelId}] Date[{date}] From[{from}] error:{errorMessage}", teamId, channelId, message.Date, message.User?.DisplayName, ex.Message);
                return null;
            }
        }

        #endregion
        #region Method - UploadFileToPath

        private async Task UploadFileToPath(GraphHelper graphHelper, string teamId, string channelName, SlackAttachment attachment) {
            _logger.LogDebug("UploadFileToPath - Start");
            try {
                await graphHelper.UploadFileToTeamChannelAsync(teamId, channelName, attachment);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error adding attachment to message TeamID[{teamId}] Channel Name:{channelName} [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "Error adding attachment to message TeamID[{teamId}] Channel Name:{channelName} code:{errorCode} message:{errorMessage}", teamId, channelName, odataError?.Error?.Code, odataError?.Error?.Message);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error uploading file: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "UploadFileToPath - Error adding attachment to message TeamID[{teamId}] Channel Name:{channelName} error:{errorMessage}", teamId, channelName, ex.Message);
            }
        }

        #endregion
        #region Method - AddAttachmentsToMessage

        private async Task AddAttachmentsToMessage(GraphHelper graphHelper, string teamId, string channelId, SlackMessage message) {
            _logger.LogDebug("AddAttachmentsToMessage - Start");
            try {
                await graphHelper.AddAttachmentsToMessageAsync(teamId, channelId, message);
            } catch (ODataError odataError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error adding attachment to message TeamID[{teamId}] ChannelID[{channelId}] [{odataError?.Error?.Code} / {odataError?.Error?.Message}]");
                Console.ResetColor();
                _logger.LogError(odataError, "Error adding attachment to message TeamID[{teamId}] ChannelID[{channelId}] code:{errorCode} message:{errorMessage}", teamId, channelId, odataError?.Error?.Code, odataError?.Error?.Message);
                throw;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error adding attachment to message: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "AddAttachmentsToMessage - Error adding attachment to message TeamID[{teamId}] ChannelID[{channelId}] error:{errorMessage}", teamId, channelId, ex.Message);
            }
        }

        #endregion

        #endregion
    }
}
