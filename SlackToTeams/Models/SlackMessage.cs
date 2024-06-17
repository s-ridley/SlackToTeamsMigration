// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Graph.Models;
using SlackToTeams.Utils;

namespace SlackToTeams.Models {
    public class SlackMessage {
        #region Properties

        public SlackUser? User { get; private set; }
        public string Date { get; private set; }
        public string? ThreadDate { get; private set; }
        public bool IsInThread { get; private set; }
        public bool IsParentThread { get; private set; }
        public string Text { get; private set; }
        public List<SlackAttachment>? Attachments { get; set; }
        public List<SlackUser>? Mentions { get; set; }
        public List<SlackReaction>? Reactions { get; set; }
        public List<SlackHostedContent>? HostedContents { get; set; }
        // Team Message IDs are the Timestamps first 13 digits
        public string? TeamID => ThreadDate?.Replace(".", "")[..13] ?? Date.Replace(".", "")[..13];

        #endregion
        #region Constructors

        public SlackMessage(SlackUser? user, string date, string? threadDate, string text, List<SlackAttachment>? attachments, List<SlackUser>? mentions, List<SlackReaction>? reactions, List<SlackHostedContent>? hostedContents) {
            User = user;
            Date = date;
            ThreadDate = threadDate;
            Text = text;
            Attachments = attachments;
            Mentions = mentions;
            Reactions = reactions;
            HostedContents = hostedContents;

            IsInThread = !string.IsNullOrEmpty(threadDate);
            IsParentThread = IsInThread && ThreadDate == Date;
        }

        #endregion
        #region Method - AttachmentsMessage

        public string AttachmentsMessage() {
            return $"<strong>[{ConvertHelper.SlackTimestampToDateTime(Date)}] {User?.DisplayName ?? "UNKNOWN"}</strong><br>{FormattedAttachedAttachments()}";
        }

        #endregion
        #region Method - FormattedMessage

        private string FormattedMessage() {
            string formattedText = FormattedText();
            string hostedContents = FormattedHostedContents();
            string attachments = FormattedAttachments();
            string reactions = FormattedReactions();

            if (
                string.IsNullOrEmpty(formattedText) &&
                string.IsNullOrEmpty(hostedContents) &&
                string.IsNullOrEmpty(attachments) &&
                string.IsNullOrEmpty(reactions)
            ) {
                return $"EMPTY TEXT<br>Possibly a reference to a message/thread";
            } else {
                return $"{formattedText}{(!string.IsNullOrEmpty(hostedContents) ? $"{hostedContents}" : "")}{(!string.IsNullOrEmpty(attachments) ? $"<blockquote>{attachments}</blockquote>" : "")}{(!string.IsNullOrEmpty(reactions) ? $"<blockquote>{reactions}</blockquote>" : "")}";
            }
        }

        #endregion
        #region Method - FormattedText

        private string FormattedText() {
            return Text.TrimEnd().Replace("\n", "<br>");
        }

        #endregion
        #region Method - FormattedAttachments

        private string FormattedAttachments() {
            StringBuilder formattedText = new();
            if (Attachments != null) {
                foreach (var attachment in Attachments) {
                    if (
                        attachment != null &&
                        !string.IsNullOrWhiteSpace(attachment.Name) &&
                        !string.IsNullOrWhiteSpace(attachment.MimeType) &&
                        !string.IsNullOrWhiteSpace(attachment.SlackURL) &&
                        !attachment.MimeType.StartsWith("image/")
                    ) {
                        _ = formattedText.Append($"[{attachment.Name}]<br>");
                    }
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedAttachedAttachments

        private string FormattedAttachedAttachments() {
            StringBuilder formattedText = new();
            if (Attachments != null) {
                foreach (var attachment in Attachments) {
                    if (
                        attachment != null &&
                        !string.IsNullOrWhiteSpace(attachment.Id)
                    ) {
                        _ = formattedText.Append($"<attachment id='{attachment.Id}'></attachment>");
                    }
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedReactions

        private string FormattedReactions() {
            StringBuilder formattedText = new();
            if (
                Reactions != null &&
                Reactions.Count > 0
            ) {
                foreach (var reaction in Reactions) {
                    Mentions ??= [];
                    Mentions.Add(reaction.User);
                    _ = formattedText.Append($"{reaction.Emoji} <at id=\"{Mentions.Count}\">{reaction.User.DisplayName}</at><br>");
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedHostedContents

        private string FormattedHostedContents() {
            StringBuilder formattedText = new();
            if (
                HostedContents != null &&
                HostedContents.Count > 0
            ) {
                int tempId = 1;
                foreach (var hostedContent in HostedContents) {
                    _ = formattedText.Append($"<span><img{(hostedContent.Height > 0 && hostedContent.Width > 0 ? $" height=\"{hostedContent.Height}\" width=\"{hostedContent.Width}\" style=\"vertical-align:bottom; width:{hostedContent.Width}px; height:{hostedContent.Height}px\" " : " ")}src=\"../hostedContents/{tempId}/$value\"></span>");
                    tempId++;
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - HtmlAttachments

        public string HtmlAttachments() {
            StringBuilder formattedText = new();
            if (Attachments != null) {
                foreach (var attachment in Attachments) {
                    if (
                        attachment != null &&
                        !string.IsNullOrWhiteSpace(attachment.Name) &&
                        !string.IsNullOrWhiteSpace(attachment.MimeType) &&
                        !string.IsNullOrWhiteSpace(attachment.SlackURL) &&
                        !attachment.MimeType.StartsWith("image/")
                    ) {
                        _ = formattedText.Append($"{attachment.Name}{Environment.NewLine}");
                    }
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - HtmlReactions

        public string HtmlReactions() {
            StringBuilder formattedText = new();
            if (
                Reactions != null &&
                Reactions.Count > 0
            ) {
                foreach (var reaction in Reactions) {
                    Mentions ??= [];
                    Mentions.Add(reaction.User);
                    _ = formattedText.Append($"{reaction.Emoji} {reaction.User.DisplayName}{Environment.NewLine}");
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - HtmlHostedContents

        public string HtmlHostedContents() {
            StringBuilder formattedText = new();
            if (
                HostedContents != null &&
                HostedContents.Count > 0
            ) {
                foreach (var hostedContent in HostedContents) {
                    if (hostedContent.ContentBytes != null) {
                        _ = formattedText.Append($"<img{(hostedContent.Height > 0 && hostedContent.Width > 0 ? $" height=\"{hostedContent.Height}\" width=\"{hostedContent.Width}\" " : " ")}src=\"data:image/png;base64, {Convert.ToBase64String(hostedContent.ContentBytes)}\">");
                    }
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - ToChatMessageFromIdentitySet

        private ChatMessageFromIdentitySet? ToChatMessageFromIdentitySet() {
            if (User != null) {
                return User.ToChatMessageFromIdentitySet();
            } else {
                return null;
            }
        }

        #endregion
        #region Method - ToMentions

        private List<ChatMessageMention> ToMentions() {
            List<ChatMessageMention> formattedMentions = [];
            if (
                Mentions != null &&
                Mentions.Count > 0
            ) {
                int mentionId = 1;
                foreach (var mention in Mentions) {
                    formattedMentions.Add(mention.ToChatMessageMention(mentionId));
                    mentionId++;
                }
            }
            return formattedMentions;
        }

        #endregion
        #region Method - ToHostedContents

        private List<ChatMessageHostedContent> ToHostedContents() {
            List<ChatMessageHostedContent> formattedHostedContents = [];
            if (
                HostedContents != null &&
                HostedContents.Count > 0
            ) {
                int tempId = 1;
                foreach (var hostedContent in HostedContents) {
                    formattedHostedContents.Add(hostedContent.ToChatMessageHostedContent(tempId));
                    tempId++;
                }
            }
            return formattedHostedContents;
        }

        #endregion
        #region Method - ToAttachments

        private List<ChatMessageAttachment> ToAttachments() {
            List<ChatMessageAttachment> attachments = [];
            if (
                Attachments != null &&
                Attachments.Count > 0
            ) {
                foreach (var attachment in Attachments) {
                    if (
                        attachment != null &&
                        !string.IsNullOrWhiteSpace(attachment.Id) &&
                        !string.IsNullOrWhiteSpace(attachment.ContentURL)
                    ) {
                        attachments.Add(attachment.ToChatMessageAttachment());
                    }
                }
            }
            return attachments;
        }

        #endregion
        #region Method - ToChatMessage

        public ChatMessage ToChatMessage() {
            // Message that doesn't have team user equivalent
            ChatMessage chatMessage = new();

            string content = FormattedMessage();
            if (!string.IsNullOrWhiteSpace(content)) {
                chatMessage.Body = new ItemBody {
                    Content = content,
                    ContentType = BodyType.Html,
                };
            }

            ChatMessageFromIdentitySet? from = ToChatMessageFromIdentitySet();
            if (from != null) {
                chatMessage.From = from;
            }

            DateTime createdDateTime = ConvertHelper.SlackTimestampToDateTime(Date);
            if (createdDateTime != DateTime.MinValue) {
                chatMessage.CreatedDateTime = createdDateTime;
            }

            List<ChatMessageMention> mentions = ToMentions();
            if (
                mentions != null &&
                mentions.Count > 0
            ) {
                chatMessage.Mentions = mentions;
            }

            List<ChatMessageHostedContent> hostedContents = ToHostedContents();
            if (
                hostedContents != null &&
                hostedContents.Count > 0
            ) {
                chatMessage.HostedContents = hostedContents;
            }

            List<ChatMessageAttachment> attachments = ToAttachments();
            if (
                attachments != null &&
                attachments.Count > 0
            ) {
                chatMessage.Attachments = attachments;
            }

            return chatMessage;
        }

        #endregion
    }
}
