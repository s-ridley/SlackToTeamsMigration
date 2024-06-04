// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using STMigration.Models;

namespace STMigration.Utils {
    public class MessageHandling {
        #region Method - GetFilesForChannel

        public static IEnumerable<string> GetFilesForChannel(string channelPath) {
            foreach (var file in Directory.GetFiles(channelPath)) {
                yield return file;
            }
        }

        #endregion
        #region Method - GetMessagesForDay

        public static IEnumerable<STMessage> GetMessagesForDay(string path, List<STChannel> channels, List<STUser> users) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"File {path}");
            Console.ResetColor();

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

                    STUser? messageSender = FindMessageSender(obj, users);
                    string messageText = GetFormattedText(obj, channels, users);

                    string? threadTS = obj.SelectToken("thread_ts")?.ToString();

                    List<STAttachment> attachments = GetFormattedAttachments(obj);

                    STMessage message = new(messageSender, messageTS, threadTS, messageText, attachments);

                    yield return message;
                }
            }
        }

        #endregion
        #region Method - GetFormattedText

        static string GetFormattedText(JObject obj, List<STChannel> channelList, List<STUser> userList) {
            var richTextArray = obj.SelectTokens("blocks[*].elements[*].elements[*]").ToList();

            // Simple text, get it directly from text field
            if (richTextArray == null || richTextArray.Count == 0) {
                string? text = obj.SelectToken("text")?.ToString();
                return text ?? string.Empty;
            }

            StringBuilder formattedText = new();
            FormatText(formattedText, richTextArray, channelList, userList);

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormatText

        static void FormatText(StringBuilder formattedText, List<JToken> tokens, List<STChannel> channelList, List<STUser> userList) {
            string? text;

            foreach (JToken token in tokens) {
                string? type = token.SelectToken("type")?.ToString();
                switch (type) {
                    case "text":
                        text = token.SelectToken("text")?.ToString();

                        if (string.IsNullOrEmpty(text)) {
                            break;
                        }

                        _ = formattedText.Append(text);
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

                        _ = formattedText.Append($"<a href='{link}'>{linkText}</a>");
                        break;
                    case "user":
                        string? userID = token.SelectToken("user_id")?.ToString();

                        if (string.IsNullOrEmpty(userID)) {
                            break;
                        }

                        string userName = DisplayNameFromUserID(userList, userID);

                        _ = formattedText.Append($"@{userName}");
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

                        if (string.IsNullOrEmpty(unicodeHex)) {
                            break;
                        }

                        try {
                            int decValue = Convert.ToInt32(unicodeHex, 16);

                            _ = formattedText.Append($"<emoji alt=\"&#{decValue};\"></emoji>");
                        } catch (Exception ex) {
                            Console.WriteLine(ex.Message);
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

        static STUser? FindMessageSender(JObject obj, List<STUser> userList) {
            var userID = obj.SelectToken("user")?.ToString();

            if (!string.IsNullOrEmpty(userID)) {
                if (userID == "USLACKBOT") {
                    return STUser.SLACK_BOT;
                }

                return userList.FirstOrDefault(user => user.SlackUserID == userID);
            }

            return null;
        }

        #endregion
        #region Method - DisplayNameFromChannelID

        static string DisplayNameFromChannelID(List<STChannel> channelList, string channelId) {
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

        static string DisplayNameFromUserID(List<STUser> userList, string userID) {
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

        static List<STAttachment> GetFormattedAttachments(JObject obj) {
            var attachmentsArray = obj.SelectTokens("files[*]").ToList();

            List<STAttachment> formattedAttachments = [];
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

                formattedAttachments.Add(new STAttachment(url, fileType, name, date));
                index++;
            }

            return formattedAttachments;
        }

        #endregion
    }
}
