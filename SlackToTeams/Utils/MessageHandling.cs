// Copyright (c) Isak Viste. All rights reserved.
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
                        string messageText = GetFormattedText(obj, channels, users);

                        string? threadTS = obj.SelectToken("thread_ts")?.ToString();

                        List<SlackAttachment> attachments = GetFormattedAttachments(obj);

                        SlackMessage message = new(messageSender, messageTS, threadTS, messageText, attachments);

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

        static string GetFormattedText(JObject obj, List<SlackChannel> channelList, List<SlackUser> userList) {
            string? subtype = obj.SelectToken("subtype")?.ToString();

            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(subtype)) {

                switch (subtype) {
                    case "channel_join":
                        string? userID = obj.SelectToken("user")?.ToString();

                        if (string.IsNullOrEmpty(userID)) {
                            break;
                        }

                        string userName = DisplayNameFromUserID(userList, userID);

                        result = HttpUtility.HtmlEncode($"<@{userName}> has joined the channel");
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(result)) {
                // Check for rich text block
                var richTextArray = obj.SelectTokens("blocks[*].elements[*].elements[*]").ToList();

                if (
                    richTextArray != null &&
                    richTextArray.Count > 0
                ) {
                    // Process the rich text block
                    StringBuilder formattedText = new();
                    FormatText(formattedText, richTextArray, channelList, userList);
                    result = formattedText.ToString();
                } else {
                    // Simple text, get it directly from text field
                    string? text = obj.SelectToken("text")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) {
                        result = HttpUtility.HtmlEncode(text);
                    }
                }
            }

            return result;
        }

        #endregion
        #region Method - FormatText

        static void FormatText(StringBuilder formattedText, List<JToken> tokens, List<SlackChannel> channelList, List<SlackUser> userList) {
            string? text;

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

                        FormatText(formattedText, subTokens, channelList, userList);

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

                        string userName = DisplayNameFromUserID(userList, userID);

                        _ = formattedText.Append(HttpUtility.HtmlEncode($"<@{userName}>"));
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
            var userID = obj.SelectToken("user")?.ToString();

            if (!string.IsNullOrEmpty(userID)) {
                if (userID == "USLACKBOT") {
                    return SlackUser.SLACK_BOT;
                }

                return userList.FirstOrDefault(user => user.SlackUserID == userID);
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

        static string DisplayNameFromUserID(List<SlackUser> userList, string userID) {
            if (userID != "USLACKBOT") {
                var simpleUser = userList.FirstOrDefault(user => user.SlackUserID == userID);
                if (simpleUser != null) {
                    return simpleUser.DisplayName;
                }

                return "Unknown User";
            }

            return "SlackBot";
        }

        #endregion
        #region Method - GetFormattedAttachments

        static List<SlackAttachment> GetFormattedAttachments(JObject obj) {
            var attachmentsArray = obj.SelectTokens("files[*]").ToList();

            List<SlackAttachment> formattedAttachments = [];
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

            return formattedAttachments;
        }

        #endregion
    }
}
