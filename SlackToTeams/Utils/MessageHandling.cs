﻿// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public class MessageHandling {
        #region Method - GetFilesForChannel

        public static IEnumerable<string> GetFilesForChannel(string channelPath, string searchPattern) {
            foreach (var file in Directory.GetFiles(channelPath, searchPattern)) {
                yield return file;
            }
        }

        #endregion
        #region Method - GetMessagesForDay

        public static IEnumerable<SlackMessage> GetMessagesForDay(string path, List<SlackChannel> channels, List<SlackUser> users) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"File {path}");
            Console.ResetColor();

            if (File.Exists(path)) {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fs);
                using JsonTextReader reader = new(sr);

                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        JObject obj = JObject.Load(reader);

                        string? messageTS = obj.SelectToken("ts")?.ToString();

                        if (string.IsNullOrEmpty(messageTS)) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine($"{messageTS} is not valid in");
                            Console.Error.WriteLine($"{obj}");
                            Console.ResetColor();
                            Console.WriteLine();
                            continue;
                        }

                        SlackUser? messageSender = FindMessageSender(obj, users);

                        (string messageText, List<SlackUser>? mentions, List<SlackReaction>? reactions) = ProcessJson(obj, channels, users);

                        string? threadTS = obj.SelectToken("thread_ts")?.ToString();
                        // Make sure threadTS is valid
                        if (!string.IsNullOrWhiteSpace(threadTS)) {
                            // Remove the dot if required
                            threadTS = threadTS.Replace(".", "");
                            // Try and convert to long
                            if (!long.TryParse(threadTS, out long threadMs) || threadMs <= 0) {
                                // If that fails then threadTS is not valid so set to null
                                threadTS = null;
                            }
                        }

                        List<SlackAttachment>? attachments = GetFormattedAttachments(obj);

                        SlackMessage message = new(messageSender, messageTS, threadTS, messageText, attachments, mentions, reactions);

                        yield return message;
                    }
                }
            } else {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{path} is not valid");
                Console.ResetColor();
            }
        }

        #endregion
        #region Method - GetFormattedText

        static (string, List<SlackUser>?, List<SlackReaction>?) ProcessJson(JObject obj, List<SlackChannel> channelList, List<SlackUser> userList) {
            string? subtype = obj.SelectToken("subtype")?.ToString();

            string messageText = string.Empty;
            List<SlackUser>? mentions = null;
            List<SlackReaction>? reactions = null;

            if (!string.IsNullOrWhiteSpace(subtype)) {
                switch (subtype) {
                    case "bot_message":
                        string? title = obj.SelectToken("attachments[0].title")?.ToString();
                        string? titleLink = obj.SelectToken("attachments[0].title_link")?.ToString();
                        string? preText = obj.SelectToken("attachments[0].pretext")?.ToString();

                        StringBuilder formattedText = new();

                        if (!string.IsNullOrEmpty(titleLink)) {
                            if (string.IsNullOrEmpty(title)) {
                                _ = formattedText.AppendLine($"<a href='{titleLink}'>{titleLink}</a>");
                            } else {
                                _ = formattedText.AppendLine($"<a href='{titleLink}'>{HttpUtility.HtmlEncode(title)}</a>");
                            }
                        }

                        if (!string.IsNullOrEmpty(preText)) {
                            _ = formattedText.AppendLine(HttpUtility.HtmlEncode(preText));
                        }

                        messageText = formattedText.ToString();
                        break;
                    case "channel_join":
                        string? userID = obj.SelectToken("user")?.ToString();

                        if (string.IsNullOrEmpty(userID)) {
                            break;
                        }

                        SlackUser userFound = FindUser(userList, userID);

                        if (userFound != null) {
                            if (userFound.TeamsUserID != null) {
                                messageText = HttpUtility.HtmlEncode($"<at id=\"0\">{userFound.DisplayName}</at> has joined the channel");
                                mentions ??= [];
                                mentions.Add(userFound);
                            } else {
                                messageText = HttpUtility.HtmlEncode($"<{userFound.DisplayName}> has joined the channel");
                            }
                        } else {
                            messageText = HttpUtility.HtmlEncode("<Unknown User> has joined the channel");
                        }
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(messageText)) {
                // Check for rich text block
                var richTextArray = obj.SelectTokens("blocks[*].elements[*].elements[*]").ToList();

                if (
                    richTextArray != null &&
                    richTextArray.Count > 0
                ) {
                    // Process the rich text block
                    StringBuilder formattedText = new();
                    FormatText(formattedText, richTextArray, channelList, userList, mentions);
                    messageText = formattedText.ToString();
                } else {
                    // Simple text, get it directly from text field
                    string? text = obj.SelectToken("text")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) {
                        messageText = HttpUtility.HtmlEncode(text);
                    }
                }
            }

            var reactionsArray = obj.SelectTokens("reactions[*]").ToList();
            if (
                reactionsArray != null &&
                reactionsArray.Count > 0
            ) {
                foreach (JToken reaction in reactionsArray) {
                    string? name = reaction.SelectToken("name")?.ToString();
                    name = ConvertHelper.SlackToTeamsReaction(name);
                    if (!string.IsNullOrWhiteSpace(name)) {
                        var usersArray = reaction.SelectTokens("users[*]").ToList();
                        if (
                            usersArray != null &&
                            usersArray.Count > 0
                        ) {
                            foreach (JToken user in usersArray) {
                                string? userId = user.ToString();
                                if (!string.IsNullOrWhiteSpace(userId)) {
                                    SlackUser userFound = FindUser(userList, userId);

                                    if (userFound != null) {
                                        if (userFound.TeamsUserID != null) {
                                            reactions ??= [];
                                            reactions.Add(
                                                new SlackReaction(
                                                    null,       // createdDateTime
                                                    name,       // reactionType
                                                    userFound   // user
                                                )
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (messageText, mentions, reactions);
        }

        #endregion
        #region Method - FormatText

        static void FormatText(StringBuilder formattedText, List<JToken> tokens, List<SlackChannel> channelList, List<SlackUser> userList, List<SlackUser>? mentions) {
            string? text;
            int mentionCount = 0;

            foreach (JToken token in tokens) {
                string? type = token.SelectToken("type")?.ToString();
                switch (type) {
                    case "text":
                        text = token.SelectToken("text")?.ToString();

                        if (!string.IsNullOrEmpty(text)) {
                            text = HttpUtility.HtmlEncode(text);
                            var style = token.SelectToken("style");
                            if (style != null) {
                                var bold = style.SelectToken("bold");
                                var code = style.SelectToken("code");
                                var italic = style.SelectToken("italic");

                                if (Convert.ToBoolean(bold)) {
                                    _ = formattedText.Append($"<strong>{text}</strong>");
                                } else if (Convert.ToBoolean(code)) {
                                    _ = formattedText.Append($"<code>{text}</code>");
                                } else if (Convert.ToBoolean(italic)) {
                                    _ = formattedText.Append($"<em>{text}</em>");
                                }
                            } else {
                                _ = formattedText.Append(text);
                            }
                        }
                        break;
                    case "rich_text_section":
                        var subTokens = token.SelectTokens("elements[*]").ToList();

                        _ = formattedText.Append("<br> • ");

                        FormatText(formattedText, subTokens, channelList, userList, mentions);
                        break;
                    case "link":
                        string? link = token.SelectToken("url")?.ToString();
                        string? linkText = token.SelectToken("text")?.ToString();

                        if (string.IsNullOrEmpty(link)) {
                            break;
                        }

                        if (string.IsNullOrEmpty(linkText)) {
                            _ = formattedText.Append($"<a href='{link}'>{link}</a>");
                            break;
                        }

                        _ = formattedText.Append($"<a href='{link}'>{HttpUtility.HtmlEncode(linkText)}</a>");
                        break;
                    case "user":
                        string? userID = token.SelectToken("user_id")?.ToString();

                        if (string.IsNullOrEmpty(userID)) {
                            break;
                        }

                        SlackUser userFound = FindUser(userList, userID);

                        if (userFound != null) {
                            if (userFound.TeamsUserID != null) {
                                _ = formattedText.Append($"<at id=\"{mentionCount}\">{HttpUtility.HtmlEncode(userFound.DisplayName)}</at>");
                                mentions ??= [];
                                mentions.Add(userFound);
                                mentionCount++;
                            } else {
                                _ = formattedText.Append(HttpUtility.HtmlEncode($"<{userFound.DisplayName}>"));
                            }
                        }
                        break;
                    case "usergroup":
                        // TODO: Figure out user group display name
                        // In the meantime, just use a temporary placeholder
                        //string? userGroup = token.SelectToken("usergroup_id")?.ToString();
                        _ = formattedText.Append($"@TEAM");
                        //Console.Write($"{userGroup}\n");
                        break;
                    case "color":
                        string? value = token.SelectToken("value")?.ToString();

                        if (string.IsNullOrEmpty(value)) {
                            break;
                        }

                        _ = formattedText.Append($"[{value}]");
                        break;
                    case "emoji":
                        string? unicodeHex = token.SelectToken("unicode")?.ToString();
                        if (!string.IsNullOrEmpty(unicodeHex)) {
                            try {
                                string[] unicodeHexArrary = unicodeHex.Split('-');
                                if (unicodeHexArrary != null) {
                                    _ = formattedText.Append($"<span>");
                                    foreach (string str in unicodeHexArrary) {
                                        _ = formattedText.Append($"&#x{str};");
                                    }
                                    _ = formattedText.Append($"</span>");
                                }
                            } catch (Exception ex) {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        break;
                    case "channel":
                        string? channelId = token.SelectToken("channel_id")?.ToString();

                        if (string.IsNullOrEmpty(channelId)) {
                            break;
                        }

                        string channelName = DisplayNameFromChannelID(channelList, channelId);

                        _ = formattedText.Append($"@{channelName}");
                        break;
                    case "broadcast":
                        // This is used to send a message to one or more channels. Does not have equvialent in Teams so will ignore
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{type} not taken into account!");
                        Console.ResetColor();
                        break;
                }
            }
        }

        #endregion
        #region Method - FindMessageSender

        static SlackUser? FindMessageSender(JObject obj, List<SlackUser> userList) {
            string? subtype = obj.SelectToken("subtype")?.ToString();
            if (
                !string.IsNullOrEmpty(subtype) &&
                string.Equals(subtype, "bot_message", StringComparison.CurrentCultureIgnoreCase)
            ) {
                string? username = obj.SelectToken("username")?.ToString();
                string? botId = obj.SelectToken("bot_id")?.ToString();
                if (
                    !string.IsNullOrEmpty(username) &&
                    !string.IsNullOrEmpty(botId)
                ) {
                    return SlackUser.BotUser(botId, username);
                }
            } else {
                var userID = obj.SelectToken("user")?.ToString();

                if (!string.IsNullOrEmpty(userID)) {
                    if (userID == "USLACKBOT") {
                        return SlackUser.SLACK_BOT;
                    }
                    return userList.FirstOrDefault(user => user.SlackUserID == userID);
                }
            }
            return null;
        }

        #endregion
        #region Method - DisplayNameFromChannelID

        static string DisplayNameFromChannelID(List<SlackChannel> channelList, string channelId) {
            if (!string.IsNullOrWhiteSpace(channelId)) {
                var channel = channelList.FirstOrDefault(channel => channel.SlackId == channelId);
                if (channel != null) {
                    return channel.DisplayName;
                }
            }

            return "Unknown Channel";
        }

        #endregion
        #region Method - DisplayNameFromUserID

        static SlackUser FindUser(List<SlackUser> userList, string userID) {
            if (userID != "USLACKBOT") {
                var simpleUser = userList.FirstOrDefault(user => user.SlackUserID == userID);
                if (simpleUser != null) {
                    return simpleUser;
                }
            }
            return SlackUser.SLACK_BOT;
        }

        #endregion
        #region Method - GetFormattedAttachments

        static List<SlackAttachment>? GetFormattedAttachments(JObject obj) {
            var attachmentsArray = obj.SelectTokens("files[*]").ToList();

            List<SlackAttachment>? formattedAttachments = null;

            if (
                attachmentsArray != null &&
                attachmentsArray.Count > 0
            ) {
                formattedAttachments = [];
                int index = 0;
                foreach (var attachment in attachmentsArray) {
                    string? url = attachment.SelectToken("url_private_download")?.ToString();
                    string? fileType = attachment.SelectToken("filetype")?.ToString();
                    string? name = attachment.SelectToken("name")?.ToString();
                    string? date = attachment.SelectToken("timestamp")?.ToString();

                    if (string.IsNullOrEmpty(url)) {
                        continue;
                    }
                    if (string.IsNullOrEmpty(fileType) && string.IsNullOrEmpty(name)) {
                        continue;
                    }

                    formattedAttachments.Add(new SlackAttachment(url, fileType, name, date));
                    index++;
                }
            }

            return formattedAttachments;
        }

        #endregion
    }
}
