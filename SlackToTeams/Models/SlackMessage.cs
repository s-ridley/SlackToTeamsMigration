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
        // Team Message IDs are the Timestamps first 13 digits
        public string? TeamID => ThreadDate?.Replace(".", "")[..13] ?? Date.Replace(".", "")[..13];

        #endregion
        #region Constructors

        public SlackMessage(SlackUser? user, string date, string? threadDate, string text, List<SlackAttachment>? attachments, List<SlackUser>? mentions, List<SlackReaction>? reactions) {
            User = user;
            Date = date;
            ThreadDate = threadDate;
            Text = text;
            Attachments = attachments;
            Mentions = mentions;
            Reactions = reactions;

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
            string attachments = FormattedAttachments();
            string formattedText = FormattedText();

            if (string.IsNullOrEmpty(formattedText)) {
                if (string.IsNullOrEmpty(attachments)) {
                    return $"EMPTY TEXT<br>Possibly a reference to a message/thread";
                }
                return attachments;
            }

            if (string.IsNullOrEmpty(attachments)) {
                return formattedText;
            }

            return $"{formattedText}<blockquote>{attachments}</blockquote>";
        }

        #endregion
        #region Method - FormattedText

        private string FormattedText() {
            StringBuilder stringBuilder = new(Text.TrimEnd());

            stringBuilder.Replace("\n", "<br>");

            return stringBuilder.ToString();
        }

        #endregion
        #region Method - FormattedFrom

        private ChatMessageFromIdentitySet? FormattedFrom() {
            if (User != null) {
                return User.ToChatMessageFromIdentitySet();
            } else {
                return null;
            }
        }

        #endregion
        #region Method - FormattedAttachments

        private string FormattedAttachments() {
            StringBuilder formattedText = new();
            if (Attachments != null) {
                foreach (var att in Attachments) {
                    _ = formattedText.Append($"[{att.Name}]<br>");
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedAttachedAttachments

        private string FormattedAttachedAttachments() {
            StringBuilder formattedText = new();
            if (Attachments != null) {
                foreach (var att in Attachments) {
                    _ = formattedText.Append($"<attachment id='{att.TeamsGUID}'></attachment>");
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedMentions

        private List<Microsoft.Graph.Models.ChatMessageMention> FormattedMentions() {
            List<Microsoft.Graph.Models.ChatMessageMention>? formattedMentions = [];
            if (
                Mentions != null &&
                Mentions.Count > 0
            ) {
                int mentionId = 0;
                foreach (var mention in Mentions) {
                    formattedMentions.Add(mention.ToChatMessageMention(mentionId));
                    mentionId++;
                }
            }
            return formattedMentions;
        }

        #endregion
        #region Method - FormattedReactions

        private List<Microsoft.Graph.Models.ChatMessageReaction> FormattedReactions() {
            List<Microsoft.Graph.Models.ChatMessageReaction>? formattedReactions = [];
            if (
                Reactions != null &&
                Reactions.Count > 0
            ) {
                foreach (var reaction in Reactions) {
                    formattedReactions.Add(reaction.ToChatMessageReaction());
                }
            }
            return formattedReactions;
        }

        #endregion
        #region Method - ToChatMessage

        public ChatMessage ToChatMessage() {
            // Message that doesn't have team user equivalent
            return new ChatMessage {
                Body = new ItemBody {
                    Content = FormattedMessage(),
                    ContentType = BodyType.Html,
                },
                From = FormattedFrom(),
                CreatedDateTime = ConvertHelper.SlackTimestampToDateTime(Date),
                Mentions = FormattedMentions(),
                Reactions = FormattedReactions()
                //HostedContents = FormattedContent()
            };
        }

        #endregion
    }
}
