// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Newtonsoft.Json;
using Serilog;
using SlackToTeams.Models;
using DriveUpload = Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;

namespace SlackToTeams.Utils {
    public partial class GraphHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(GraphHelper));

        private static readonly string[] s_scopes = [
            "User.Read", "Group.ReadWrite.All"
        ];

        private GraphServiceClient UserGraphClient => new(DeviceCodeCredential, s_scopes);

        [GeneratedRegex(@"\'([^'']+)\'*")]
        private static partial Regex GoupNameRegex();

        [GeneratedRegex(@"\{([^{}]+)\}*")]
        private static partial Regex GuidRegex();

        #endregion
        #region Constants

        public const long CONTENT_MAX_SIZE = 4100000;

        #endregion
        #region Poperties

        /*
        ** APP AUTHENTICATION
        */
        AuthenticationConfig Config { get; set; }

        IConfidentialClientApplication App { get; set; }

        // With client credentials flows the scopes is ALWAYS of the shape "resource/.default",
        // as the application permissions need to be set statically (in the portal or by PowerShell),
        // and then granted by a tenant administrator. 
        string[] Scopes { get; set; }

        /*
        ** CLIENT DELEGATION
        */
        private DeviceCodeCredential? DeviceCodeCredential { get; set; }

        private GraphServiceClient GraphClient { get; set; }

        #endregion
        #region Constructors

        public GraphHelper(AuthenticationConfig config) {
            Config = config;
            App = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                        .WithClientSecret(config.ClientSecret)
                        .WithAuthority(new Uri(config.Authority))
                        .Build();

            Scopes = [$"{config.ApiUrl}.default"]; // Generates a scope -> "https://graph.microsoft.com/.default"

            DeviceCodeCredential = new DeviceCodeCredential((info, cancel) => {
                // Display the device code message to
                // the user. This tells them
                // where to go to sign in and provides the
                // code to use.
                Console.WriteLine(info.Message);
                return Task.FromResult(0);
            }, Config.Tenant, Config.ClientId);

            var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(App));
            GraphClient = new GraphServiceClient(authenticationProvider);
        }

        #endregion
        #region API Handling

        #region Method - GetFromMSGraph

        public async Task<JsonNode?> GetFromMSGraph(string apiCall) {
            AuthenticationResult? result = null;
            try {
                result = await App.AcquireTokenForClient(Scopes)
                    .ExecuteAsync();
            } catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011")) {
                s_logger.Error(ex, "Scope provided is not supported - apiCall:{apiCall} error:{errorMessage}", apiCall, ex.Message);
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            // The following example uses a Raw Http call 
            if (result != null) {
                try {
                    var httpClient = new HttpClient();
                    var apiCaller = new ProtectedApiCallHelper(httpClient);
                    return await apiCaller.GetWebApiCall($"{Config.ApiUrl}v1.0/{apiCall}", result.AccessToken);
                } catch (Exception ex) {
                    s_logger.Error(ex, "Error whilte calling - apiCall:{apiCall} error:{errorMessage}", apiCall, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error whilte calling - apiCall:{apiCall}");
                    Console.ResetColor();
                }
            }

            return null;
        }

        #endregion
        #region Method - PostToMSGraph

        public async Task<HttpResponseMessage?> PostToMSGraph(string apiCall, HttpContent content) {
            AuthenticationResult? result = null;
            try {
                result = await App.AcquireTokenForClient(Scopes)
                    .ExecuteAsync();
            } catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011")) {
                s_logger.Error(ex, "Scope provided is not supported - apiCall:{apiCall} error:{errorMessage}", apiCall, ex.Message);
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            // The following example uses a Raw Http call 
            if (result != null) {
                try {
                    var httpClient = new HttpClient();
                    var apiCaller = new ProtectedApiCallHelper(httpClient);
                    return await apiCaller.PostWebApiCall($"{Config.ApiUrl}v1.0/{apiCall}", result.AccessToken, content);
                } catch (Exception ex) {
                    s_logger.Error(ex, "Error whilte calling - apiCall:{apiCall} error:{errorMessage}", apiCall, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error whilte calling - apiCall:{apiCall}");
                    Console.ResetColor();
                }
            }

            return null;
        }

        #endregion

        #endregion
        #region Team Handling

        #region Method - GetUserTeamsAsync

        public async Task<TeamCollectionResponse?> GetUserTeamsAsync() {
            return await GraphClient.Users[Config.OwnerUserId].JoinedTeams.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id", "displayName"];
            });
        }

        #endregion
        #region Method - CreateTeamAsync

        public async Task<string> CreateTeamAsync(string json) {
            string? teamId = string.Empty;

            if (!string.IsNullOrWhiteSpace(json)) {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (content != null) {
                    var response = await PostToMSGraph("teams", content);
                    if (response != null) {
                        if (response.Headers.TryGetValues("Location", out IEnumerable<string>? values)) {
                            Regex regex = GoupNameRegex();
                            if (values != null) {
                                teamId = regex.Match(values.First()).Groups[1].ToString();
                            }
                        }
                    } else {
                        throw new Exception("CreateTeamAsync - response is null");
                    }
                } else {
                    throw new Exception("CreateTeamAsync - content is null");
                }
            } else {
                throw new Exception("CreateTeamAsync - team json is corrupt");
            }

            if (string.IsNullOrEmpty(teamId)) {
                throw new Exception($"CreateTeamAsync - teamId is null");
            }

            return teamId;
        }

        #endregion
        #region Method - CompleteTeamMigrationAsync

        public async Task CompleteTeamMigrationAsync(string teamId) {
            var apiCall = $"teams/{teamId}/completeMigration";

            _ = await PostToMSGraph(apiCall, new StringContent(""));
        }

        #endregion
        #region Method - AssignTeamOwnerAsync

        public async Task AssignTeamOwnerAsync(string teamId) {
            // Add owner to the team
            var ownerUser = new AadUserConversationMember {
                Roles = [
                    "owner"
                ],
                AdditionalData = new Dictionary<string, object>() {
                {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{Config.OwnerUserId}')"}
            }
            };

            await GraphClient.Teams[teamId].Members.PostAsync(ownerUser);
        }

        #endregion

        #endregion
        #region Channel Handling

        #region Method - GetTeamsChannelsAsync

        public async Task<ChannelCollectionResponse?> GetTeamsChannelsAsync(string teamID) {
            return await GraphClient.Teams[teamID].Channels.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id", "displayName"];
            });
        }

        #endregion
        #region Method - CreateChannelAsync

        public async Task<string> CreateChannelAsync(string teamID, SlackChannel channel) {
            string? channelId = string.Empty;

            if (channel != null) {
                string json = JsonConvert.SerializeObject(channel);

                if (!string.IsNullOrEmpty(json)) {
                    // Set creation mode to migration!
                    json = json.Replace("}", ", \"@microsoft.graph.channelCreationMode\": \"migration\"}");

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await PostToMSGraph($"teams/{teamID}/channels", content);

                    if (response != null) {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        JsonNode? jsonNode = JsonNode.Parse(responseContent);

                        if (jsonNode != null) {
                            channelId = jsonNode["id"]?.GetValue<string>();
                        } else {
                            throw new Exception("CreateChannelAsync - jsonNode is null");
                        }
                    } else {
                        throw new Exception("CreateChannelAsync - response is null");
                    }
                } else {
                    throw new Exception($"CreateChannelAsync - Could not serialise - channel:{channel}:");
                }

                if (string.IsNullOrEmpty(channelId)) {
                    throw new Exception($"CreateChannelAsync - channelID is null - channel:{channel}:");
                }
            }

            return channelId;
        }

        #endregion
        #region Method - CompleteChannelMigrationAsync

        public async Task CompleteChannelMigrationAsync(string teamID, string channelID) {
            var apiCall = $"teams/{teamID}/channels/{channelID}/completeMigration";

            _ = await PostToMSGraph(apiCall, new StringContent(""));
        }

        #endregion

        #endregion
        #region Getters

        #region Method - GetUserByUpnAsync

        public async Task<string?> GetUserByUpnAsync(string userEmail) {
            var users = await GraphClient.Users.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id", "mail"];
                requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userEmail}'";
            });

            string? userId = string.Empty;
            if (
                users != null &&
                users.Value != null &&
                users.Value.Count > 0
            ) {
                // There should be only one user so get the FirstOrDefault and if not null get the Id
                userId = users.Value.FirstOrDefault()?.Id;
            }

            return userId;
        }

        #endregion
        #region Method - GetUserByEmailAsync

        public async Task<string?> GetUserByEmailAsync(string userEmail) {
            var users = await GraphClient.Users.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id", "mail"];
                requestConfiguration.QueryParameters.Filter = $"mail eq '{userEmail}'";
            });

            string? userId = string.Empty;
            if (
                users != null &&
                users.Value != null &&
                users.Value.Count > 0
            ) {
                // There should be only one user so get the FirstOrDefault and if not null get the Id
                userId = users.Value.FirstOrDefault()?.Id;
            }

            return userId;
        }

        #endregion
        #region Method - GetUserByDisplayNameAsync

        public async Task<string?> GetUserByDisplayNameAsync(string displayName) {
            var users = await GraphClient.Users.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id", "mail"];
                requestConfiguration.QueryParameters.Filter = $"displayName eq '{displayName}'";
            });

            string? userId = string.Empty;
            if (
                users != null &&
                users.Value != null &&
                users.Value.Count > 0
            ) {
                // There should be only one user so get the FirstOrDefault and if not null get the Id
                userId = users.Value.FirstOrDefault()?.Id;
            }

            return userId;
        }

        #endregion
        #region Method - GetTeamByNameAsync

        public async Task<string?> GetTeamByNameAsync(string teamName) {
            var teams = await GraphClient.Teams.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["displayName", "id"];
                requestConfiguration.QueryParameters.Filter = $"displayName eq '{teamName}'";
            });

            string? teamId = string.Empty;
            if (
                teams != null &&
                teams.Value != null &&
                teams.Value.Count > 0
            ) {
                // There should be only one team so get the FirstOrDefault and if not null get the Id
                teamId = teams.Value.FirstOrDefault()?.Id;
            }

            return teamId;
        }

        #endregion
        #region Method - GetChannelByNameAsync

        public async Task<string?> GetChannelByNameAsync(string teamID, string channelName) {
            var channels = await GraphClient.Teams[teamID].Channels.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["displayName", "id"];
                requestConfiguration.QueryParameters.Filter = $"displayName eq '{channelName}'";
            });

            string? channelId = string.Empty;
            if (
                channels != null &&
                channels.Value != null &&
                channels.Value.Count > 0
            ) {
                // There should be only one channel so get the FirstOrDefault and if not null get the Id
                channelId = channels.Value.FirstOrDefault()?.Id;
            }

            return channelId;
        }

        #endregion

        #endregion
        #region Sending Messages

        #region Method - SendMessageToChannelThreadAsync

        public async Task<ChatMessage?> SendMessageToChannelThreadAsync(string teamID, string channelID, string threadID, SlackMessage message) {
            var msg = message.ToChatMessage();

            // Send the message
            return await GraphClient.Teams[teamID].Channels[channelID].Messages[threadID].Replies.PostAsync(msg);
        }

        #endregion
        #region Method - SendMessageToChannelAsync

        public async Task<ChatMessage?> SendMessageToChannelAsync(string teamID, string channelID, SlackMessage message) {
            var msg = message.ToChatMessage();

            // Send the message
            return await GraphClient.Teams[teamID].Channels[channelID].Messages.PostAsync(msg);
        }

        #endregion

        #endregion
        #region Upload Files

        #region Method - UploadFileToTeamChannelAsync

        public async Task UploadFileToTeamChannelAsync(string teamID, string channelName, SlackAttachment attachment) {
            // Get the drives for the team
            var drives = await GraphClient.Groups[teamID].Drive.GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = ["id"];
            });

            string driveID = string.Empty;
            if (
                drives != null &&
                drives.Items != null
            ) {
                foreach (var drive in drives.Items) {
                    if (
                        drive.Root != null &&
                        drive.Id != null
                    ) {
                        driveID = drive.Id;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(driveID)) {
                // Use properties to specify the conflict behavior
                var uploadSessionRequestBody = new DriveUpload.CreateUploadSessionPostRequestBody {
                    Item = new DriveItemUploadableProperties {
                        AdditionalData = new Dictionary<string, object> {
                        { "@microsoft.graph.conflictBehavior", "rename" },
                    },
                    },
                };

                // Create the upload session
                // itemPath does not need to be a path to an existing item
                string pathToItem = $"/{channelName}/{attachment.Date}/{attachment.Name}";

                var uploadSession = await GraphClient.Drives[driveID]
                    .Items["root"]
                    .ItemWithPath(pathToItem)
                    .CreateUploadSession
                    .PostAsync(uploadSessionRequestBody);

                // Create a HttpClient to stream the slack attachement to the drive upload
                HttpClient client = new();

                var response = await client.GetAsync($"{attachment.SlackURL}");
                _ = response.EnsureSuccessStatusCode();
                await using var fileStream = await response.Content.ReadAsStreamAsync();
                _ = fileStream.Seek(0, SeekOrigin.Begin);

                // Max slice size must be a multiple of 320 KiB
                int maxSliceSize = 320 * 1024;
                var fileUploadTask = new LargeFileUploadTask<DriveItem>(
                    uploadSession, fileStream, maxSliceSize, GraphClient.RequestAdapter);

                // Create a callback that is invoked after each slice is uploaded
                //var totalLength = fileStream.Length;
                IProgress<long> progress = new Progress<long>();

                try {
                    // Upload the file
                    var uploadResult = await fileUploadTask.UploadAsync(progress);
                    if (uploadResult != null) {
                        if (!uploadResult.UploadSucceeded) {
                            s_logger.Error("Upload failed:{SlackURL}:", attachment.SlackURL);
                            Console.WriteLine($"Upload failed: {attachment.SlackURL}");
                        }
                        if (uploadResult.ItemResponse != null) {
                            if (uploadResult.ItemResponse.WebUrl != null) {
                                attachment.ContentURL = uploadResult.ItemResponse.WebUrl;
                            }
                            if (uploadResult.ItemResponse.ETag != null) {
                                Regex regex = GuidRegex();
                                attachment.Id = regex.Match(uploadResult.ItemResponse.ETag).Groups[1].ToString();
                            }
                            if (uploadResult.ItemResponse.Name != null) {
                                attachment.Name = uploadResult.ItemResponse.Name;
                            }
                        }
                    }
                } catch (ServiceException ex) {
                    s_logger.Error(ex, "Error uploading error:{errorMessage}", ex.Message);
                    Console.WriteLine($"Error uploading: {ex}");
                }
            } else {
                s_logger.Warning("Failed to find root drive for team:{teamID}:", teamID);
                Console.WriteLine($"Failed to find root drive for team:{teamID}:");
            }
        }

        #endregion
        #region Method - AddAttachmentsToMessageAsync

        public async Task AddAttachmentsToMessageAsync(string teamID, string channelID, SlackMessage message) {
            var attachments = new List<ChatMessageAttachment>();

            if (message.Attachments != null) {
                foreach (var attachment in message.Attachments) {
                    attachments.Add(new ChatMessageAttachment {
                        Id = attachment.Id,
                        ContentType = "reference",
                        ContentUrl = attachment.ContentURL,
                        Name = attachment.Name
                    });
                }

                var msg = new ChatMessage {
                    Body = new ItemBody {
                        Content = message.AttachmentsMessage(),
                        ContentType = BodyType.Html,
                    },
                    Attachments = attachments,
                };

                _ = await UserGraphClient.Teams[teamID].Channels[channelID].Messages[message.TeamID].Replies.PostAsync(msg);
            }
        }

        #endregion

        #endregion
        #region Validataion

        #region Method - ValidHostedContent

        public static bool ValidHostedContent(SlackAttachment attachment) {
            if (
                attachment != null &&
                attachment.Size < CONTENT_MAX_SIZE &&
                attachment.Size > 0 &&
                !string.IsNullOrWhiteSpace(attachment.MimeType)
            ) {
                return attachment.MimeType switch {
                    "image/gif" => true,
                    "image/jpeg" => true,
                    "image/png" => true,
                    _ => false,
                };
            } else {
                return false;
            }
        }

        #endregion

        #endregion
    }
}
