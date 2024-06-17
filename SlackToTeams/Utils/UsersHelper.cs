﻿// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public class UsersHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(UsersHelper));

        #endregion
        #region Constants

        public const string USER_LIST_FILE = "settings/userList.json";

        #endregion
        #region Method - ScanUsersFromSlack

        public static List<SlackUser> ScanUsersFromSlack(string combinedPath) {
            List<SlackUser> simpleUserList = [];

            using (FileStream fs = new(combinedPath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new(fs))
            using (JsonTextReader reader = new(sr)) {
                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        JObject obj = JObject.Load(reader);

                        // SelectToken returns null not an empty string if nothing is found
                        string? userId = obj.SelectToken("id")?.ToString();
                        string? name = obj.SelectToken("profile.real_name_normalized")?.ToString();
                        string? email = obj.SelectToken("profile.email")?.ToString();

                        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(name)) {
                            continue;
                        }

                        var is_bot = obj.SelectToken("is_bot");
                        bool isBot = false;
                        if (is_bot != null) {
                            isBot = (bool)is_bot;
                        }

                        SlackUser user;
                        if (isBot) {
                            user = SlackUser.BotUser(userId, name);
                        } else {
                            user = new(userId, name, email, isBot);
                        }

                        simpleUserList.Add(user);
                    }
                }
            }
            return simpleUserList;
        }

        #endregion
        #region Method - PopulateTeamsUsers

        public static async Task PopulateTeamsUsers(GraphHelper graphHelper, List<SlackUser> userList) {
            foreach (SlackUser user in userList) {
                if (string.IsNullOrEmpty(user.Email)) {
                    continue;
                }

                try {
                    // Check for userId matching UPN
                    var userId = await graphHelper.GetUserByUpnAsync(user.Email);

                    if (string.IsNullOrEmpty(userId)) {
                        // Check for userId matching Email
                        userId = await graphHelper.GetUserByEmailAsync(user.Email);
                    }

                    if (string.IsNullOrEmpty(userId)) {
                        // Check for userId matching DisplayName
                        userId = await graphHelper.GetUserByDisplayNameAsync(user.DisplayName);
                    }

                    if (!string.IsNullOrEmpty(userId)) {
                        user.SetTeamUserID(userId);
                    }
                } catch (Exception ex) {
                    s_logger.Error(ex, "Error getting team user by email:{email}", user.Email);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error getting team user by email: {user.Email}");
                    Console.ResetColor();
                    Console.WriteLine();
                    throw;
                }
            }
        }

        #endregion
        #region Method - StoreUserList

        public static void StoreUserList(List<SlackUser> userList) {
            using StreamWriter file = File.CreateText(USER_LIST_FILE);

            JsonSerializer serializer = new() {
                Formatting = Formatting.Indented
            };
            serializer.Serialize(file, userList);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Stored computed users to file");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Path.GetFullPath(USER_LIST_FILE));
            Console.ResetColor();
        }

        #endregion
        #region Method - UserListExists

        public static bool UserListExists() {
            return File.Exists(USER_LIST_FILE);
        }

        #endregion
        #region Method - LoadUserList

        public static List<SlackUser> LoadUserList() {
            try {
                using StreamReader file = File.OpenText(USER_LIST_FILE);

                JsonSerializer serializer = new();
                var userList = serializer.Deserialize(file, typeof(List<SlackUser>));

                return (List<SlackUser>?)userList ?? [];
            } catch (FileNotFoundException ex) {
                s_logger.Error(ex, "No existing userList:{userList}", USER_LIST_FILE);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No existing userList!");
                Console.WriteLine(ex);
                Console.ResetColor();
            } catch (Exception ex) {
                s_logger.Error(ex, "Error loading userList:{userList}", USER_LIST_FILE);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            return [];
        }

        #endregion
        #region Method - FindUser

        public static SlackUser FindUser(List<SlackUser> userList, string userID) {
            if (userID != SlackUser.SLACK_BOT_ID) {
                var simpleUser = userList.FirstOrDefault(user => user.SlackUserID == userID);
                if (simpleUser != null) {
                    return simpleUser;
                }
            }
            return SlackUser.SLACK_BOT;
        }

        #endregion
    }
}
