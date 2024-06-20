// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public class MessageHandling {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(MessageHandling));

        #endregion
        #region Method - GetMessagesForDay

        public static IEnumerable<SlackMessage> GetMessagesForDay(string channel, string path, List<SlackChannel> channels, List<SlackUser> users) {
            s_logger.Debug("Getting message for channel:{channel} from file:{path}", channel, path);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"FolderName {channel} File {path}");
            Console.ResetColor();

            if (File.Exists(path)) {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fs);
                using JsonTextReader reader = new(sr);

                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        JObject obj = JObject.Load(reader);

                        // Make sure timestamp valid
                        string? messageTs = obj.SelectToken("ts")?.ToString();
                        if (string.IsNullOrEmpty(messageTs)) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine($"{messageTs} is not valid in");
                            Console.Error.WriteLine($"{obj}");
                            Console.ResetColor();
                            Console.WriteLine();
                            continue;
                        }

                        // Check if message is deleted and stop processing if so
                        string? subtype = obj.SelectToken("subtype")?.ToString();
                        if (
                            !string.IsNullOrWhiteSpace(subtype) &&
                            subtype.Equals("message_deleted", StringComparison.CurrentCultureIgnoreCase)
                        ) {
                            s_logger.Debug("message deleted channel:{channel} from file:{path} timestamp:{messageTs}", channel, path, messageTs);
                            continue;
                        }

                        (string messageText, List<SlackUser>? mentions) = ProcessJson(obj, channels, users);

                        SlackUser? messageSender = GetMessageSender(obj, users);

                        string? threadTS = GetThreadTimestmap(obj);

                        List<SlackAttachment>? attachments = GetAttachments(obj);

                        List<SlackReaction>? reactions = GetReactions(obj, users);

                        (List<SlackHostedContent>? hostedContents, attachments) = GetHostedContent(attachments);

                        SlackMessage message = new(
                            messageSender,      // user
                            messageTs,          // date
                            threadTS,           // threadDate
                            messageText,        // text
                            attachments,
                            mentions,
                            reactions,
                            hostedContents
                        );

                        yield return message;
                    }
                }
            } else {
                s_logger.Warning("{path} is not valid", path);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{path} is not valid");
                Console.ResetColor();
            }
        }

        #endregion
        #region Method - GetAttachments

        static List<SlackAttachment>? GetAttachments(JObject obj) {
            var attachmentsArray = obj.SelectTokens("files[*]").ToList();

            List<SlackAttachment>? attachments = null;

            if (
                attachmentsArray != null &&
                attachmentsArray.Count > 0
            ) {
                attachments = [];
                foreach (var attachment in attachmentsArray) {
                    string? url = attachment.SelectToken("url_private_download")?.ToString();
                    string? fileType = attachment.SelectToken("filetype")?.ToString();
                    string? mimeType = attachment.SelectToken("mimetype")?.ToString();
                    string? name = attachment.SelectToken("name")?.ToString();
                    _ = int.TryParse(attachment.SelectToken("original_w")?.ToString(), out int width);
                    _ = int.TryParse(attachment.SelectToken("original_h")?.ToString(), out int height);

                    if (
                        !string.IsNullOrWhiteSpace(url) &&
                        !string.IsNullOrWhiteSpace(fileType) &&
                        !string.IsNullOrWhiteSpace(mimeType) &&
                        !string.IsNullOrWhiteSpace(name)
                    ) {
                        string? title = attachment.SelectToken("title")?.ToString();
                        string? size = attachment.SelectToken("size")?.ToString();
                        string? created = attachment.SelectToken("created")?.ToString();

                        SlackAttachment slackAttachment = new(
                            url,                                                    // slackUrl
                            name,
                            title,
                            fileType,
                            mimeType,
                            Convert.ToInt64(size),                                  // size
                            ConvertHelper.SlackTimestampToDateTimeOffset(created)   // date
                        ) {
                            Width = width,
                            Height = height
                        };

                        attachments.Add(slackAttachment);
                    }
                }
            }

            return attachments;
        }

        #endregion
        #region Method - GetHostedContent

        static (List<SlackHostedContent>?, List<SlackAttachment>?) GetHostedContent(List<SlackAttachment>? attachments) {
            List<SlackHostedContent>? hostedContents = null;
            if (
                attachments != null &&
                attachments.Count > 0
            ) {
                var imageAttachments = attachments.Where(a => GraphHelper.ValidImageMimeType(a.MimeType));

                if (
                    imageAttachments != null &&
                    imageAttachments.Any()
                ) {
                    if (GraphHelper.ValidHostedContent(imageAttachments)) {
                        // Remove image attachments from attachments result
                        attachments = attachments.Where(a => !GraphHelper.ValidImageMimeType(a.MimeType)).ToList();

                        // Process the image attachments as HostedContent
                        foreach (var attachment in imageAttachments) {
                            try {
                                // Check if the attachment is an image
                                // Download the file and convert to base64
                                attachment.DownloadBytes().Wait();
                                if (
                                    attachment.ContentBytes != null &&
                                    attachment.ContentBytes.Length > 0
                                ) {
                                    // Add a SlackHostedContent object to the message
                                    hostedContents ??= [];
                                    hostedContents.Add(
                                        new SlackHostedContent(
                                            attachment.ContentBytes,
                                            attachment.MimeType,
                                            attachment.Height,
                                            attachment.Width
                                        )
                                    );
                                }
                            } catch (Exception ex) {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }

            return (hostedContents, attachments);
        }

        #endregion
        #region Method - GetMessageSender

        static SlackUser? GetMessageSender(JObject obj, List<SlackUser> userList) {
            string? userId = obj.SelectToken("user")?.ToString();
            string? username = obj.SelectToken("username")?.ToString();
            string? botId = obj.SelectToken("bot_id")?.ToString();

            if (!string.IsNullOrEmpty(userId)) {
                if (userId == SlackUser.SLACK_BOT_ID) {
                    return SlackUser.SLACK_BOT;
                } else {
                    SlackUser? userFound = userList.FirstOrDefault(user => user.SlackUserId == userId);
                    if (
                        userFound != null &&
                        !string.IsNullOrEmpty(userFound.DisplayName)
                    ) {
                        return userFound;
                    } else {
                        return SlackUser.UNKNOWN;
                    }
                }
            } else {
                return SlackUser.BotUser(botId, username);
            }
        }

        #endregion
        #region Method - GetNameFromChannelId

        static string GetNameFromChannelId(List<SlackChannel> channelList, string channelId) {
            if (!string.IsNullOrWhiteSpace(channelId)) {
                var channel = channelList.FirstOrDefault(channel => channel.SlackId == channelId);
                if (channel != null) {
                    return channel.DisplayName;
                }
            }

            return "Unknown FolderName";
        }

        #endregion
        #region Method - GetReactions

        static List<SlackReaction>? GetReactions(JObject obj, List<SlackUser> userList) {
            var reactionsArray = obj.SelectTokens("reactions[*]").ToList();

            List<SlackReaction>? reactions = null;

            if (
                reactionsArray != null &&
                reactionsArray.Count > 0
            ) {
                foreach (JToken reaction in reactionsArray) {
                    string? name = reaction.SelectToken("name")?.ToString();
                    if (!string.IsNullOrWhiteSpace(name)) {
                        var usersArray = reaction.SelectTokens("users[*]").ToList();
                        if (
                            usersArray != null &&
                            usersArray.Count > 0
                        ) {
                            foreach (JToken user in usersArray) {
                                string? userId = user.ToString();
                                if (!string.IsNullOrWhiteSpace(userId)) {
                                    SlackUser userFound = UsersHelper.FindUser(userList, userId);

                                    if (userFound != null) {
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

            return reactions;
        }

        #endregion
        #region Method - GetThreadTimestmap

        static string? GetThreadTimestmap(JObject obj) {
            string? threadTs = obj.SelectToken("thread_ts")?.ToString();
            // Make sure threadTs is valid
            if (!string.IsNullOrWhiteSpace(threadTs)) {
                // Remove the dot if required
                threadTs = threadTs.Replace(".", "");
                // Try and convert to long
                if (!long.TryParse(threadTs, out long threadMs) || threadMs <= 0) {
                    // If that fails then threadTs is not valid so set to null
                    threadTs = null;
                }
            }
            return threadTs;
        }

        #endregion
        #region Method - ProcessJson

        static (string, List<SlackUser>?) ProcessJson(JObject obj, List<SlackChannel> channelList, List<SlackUser> userList) {
            string? subtype = obj.SelectToken("subtype")?.ToString();
            string messageText = string.Empty;
            List<SlackUser>? mentions = null;

            bool stopProcessing = false;

            if (!string.IsNullOrWhiteSpace(subtype)) {
                switch (subtype) {
                    case "bot_message":
                        string? title = obj.SelectToken("attachments[0].title")?.ToString();
                        string? titleLink = obj.SelectToken("attachments[0].title_link")?.ToString();
                        string? preText = obj.SelectToken("attachments[0].pretext")?.ToString();
                        var fields = obj.SelectTokens("attachments[0].fields[*]").ToList();
                        string? footer = obj.SelectToken("attachments[0].footer")?.ToString();

                        StringBuilder formattedText = new();

                        if (!string.IsNullOrEmpty(title)) {
                            if (string.IsNullOrEmpty(titleLink)) {
                                formattedText.AppendLine($"<strong>{title}</strong>");
                            } else {
                                formattedText.AppendLine($"<strong><a href='{titleLink}'>{HttpUtility.HtmlEncode(title)}</a></strong>");
                            }
                        }

                        if (!string.IsNullOrEmpty(preText)) {
                            formattedText.AppendLine($"{preText}");
                        }

                        if (
                            fields != null &&
                            fields.Count > 0
                        ) {
                            foreach (JToken field in fields) {
                                string? fieldTitle = field.SelectToken("title")?.ToString();
                                string? fieldValue = field.SelectToken("value")?.ToString();

                                if (
                                    !string.IsNullOrEmpty(fieldTitle) &&
                                    !string.IsNullOrEmpty(fieldValue)
                                ) {
                                    fieldValue = ConvertHelper.ReplaceUserIdWithName(
                                        fieldValue, // textToCheck
                                        userList
                                    );
                                    formattedText.AppendLine($"<strong>{HttpUtility.HtmlEncode(fieldTitle)}:</strong> {HttpUtility.HtmlEncode(fieldValue)}");
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(footer)) {
                            formattedText.AppendLine($"{footer}");
                        }

                        messageText = formattedText.ToString();
                        stopProcessing = true;
                        break;
                    case "channel_join":
                        string? userID = obj.SelectToken("user")?.ToString();

                        if (string.IsNullOrEmpty(userID)) {
                            break;
                        }

                        SlackUser userFound = UsersHelper.FindUser(userList, userID);

                        if (userFound != null) {
                            if (!string.IsNullOrWhiteSpace(userFound.TeamsUserId)) {
                                mentions ??= [];
                                mentions.Add(userFound);
                                messageText = $"<at id=\"{mentions.Count}\">{HttpUtility.HtmlEncode(userFound.DisplayName)}</at> has joined the channel";
                            } else {
                                messageText = HttpUtility.HtmlEncode($"<{userFound.DisplayName}> has joined the channel");
                            }
                        } else {
                            messageText = HttpUtility.HtmlEncode("<Unknown User> has joined the channel");
                        }
                        stopProcessing = true;
                        break;
                }
            }

            if (!stopProcessing) {
                // Check for rich text block
                var richTextArray = obj.SelectTokens("blocks[*].elements[*].elements[*]").ToList();

                if (
                    richTextArray != null &&
                    richTextArray.Count > 0
                ) {
                    // Process the rich text block
                    StringBuilder formattedText = new();
                    mentions = ProcessRichText(formattedText, richTextArray, channelList, userList, mentions);
                    messageText = formattedText.ToString();
                } else {
                    // Simple text, get it directly from text field
                    string? text = obj.SelectToken("text")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) {
                        text = ConvertHelper.ReplaceUserIdWithName(
                            text, // textToCheck
                            userList
                        );

                        messageText = HttpUtility.HtmlEncode(text);
                    }
                }
            }

            return (messageText, mentions);
        }

        #endregion
        #region Method - ProcessRichText

        static List<SlackUser>? ProcessRichText(StringBuilder formattedText, List<JToken> tokens, List<SlackChannel> channelList, List<SlackUser> userList, List<SlackUser>? mentions) {
            string? text;

            foreach (JToken token in tokens) {
                string? type = token.SelectToken("type")?.ToString();
                switch (type) {
                    case "text":
                        text = token.SelectToken("text")?.ToString();

                        if (!string.IsNullOrEmpty(text)) {
                            text = ConvertHelper.ReplaceUserIdWithName(
                                text,       // textToCheck
                                userList
                            );
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

                        mentions = ProcessRichText(formattedText, subTokens, channelList, userList, mentions);
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

                        SlackUser userFound = UsersHelper.FindUser(userList, userID);

                        if (userFound != null) {
                            if (!string.IsNullOrWhiteSpace(userFound.TeamsUserId)) {
                                mentions ??= [];
                                mentions.Add(userFound);
                                _ = formattedText.Append($"<at id=\"{mentions.Count}\">{HttpUtility.HtmlEncode(userFound.DisplayName)}</at>");
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
                                s_logger.Error(ex, "Error coverting emoji error:{errorMessage}", ex.Message);
                            }
                        }
                        break;
                    case "channel":
                        string? channelId = token.SelectToken("channel_id")?.ToString();

                        if (string.IsNullOrEmpty(channelId)) {
                            break;
                        }

                        string channelName = GetNameFromChannelId(channelList, channelId);

                        _ = formattedText.Append($"@{channelName}");
                        break;
                    case "broadcast":
                        // This is used to send a message to one or more channels. Does not have equvialent in Teams so will ignore
                        break;
                    default:
                        s_logger.Warning("{type} not taken into account!", type);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{type} not taken into account!");
                        Console.ResetColor();
                        break;
                }
            }
            return mentions;
        }

        #endregion
    }
}
