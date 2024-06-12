// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Graph.Models;

namespace SlackToTeams.Models {
    public class SlackMessage {
        #region Properties

        public SlackUser? User { get; private set; }
        public string Date { get; private set; }
        public string? ThreadDate { get; private set; }
        public bool IsInThread { get; private set; }
        public bool IsParentThread { get; private set; }
        public string Text { get; private set; }
        public List<SlackAttachment> Attachments { get; set; }
        public List<SlackUser> Mentions { get; set; }
        // Team Message IDs are the Timestamps first 13 digits
        public string? TeamID => ThreadDate?.Replace(".", "")[..13] ?? Date.Replace(".", "")[..13];

        #endregion
        #region Constructors

        public SlackMessage(SlackUser? user, string date, string? threadDate, string text, List<SlackAttachment> attachments, List<SlackUser> mentions) {
            User = user;
            Date = date;
            ThreadDate = threadDate;
            Text = text;
            Attachments = attachments;
            Mentions = mentions;

            IsInThread = !string.IsNullOrEmpty(threadDate);
            IsParentThread = IsInThread && ThreadDate == Date;
        }

        #endregion
        #region Method - AttachmentsMessage

        public string AttachmentsMessage() {
            return $"<strong>[{FormattedLocalTime()}] {User?.DisplayName ?? "UNKNOWN"}</strong><br>{FormattedAttachedAttachments()}";
        }

        #endregion
        #region Method - FormattedMessage

        public string FormattedMessage() {
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

        public string FormattedText() {
            StringBuilder stringBuilder = new(Text.TrimEnd());

            stringBuilder.Replace("\n", "<br>");

            return stringBuilder.ToString();
        }

        #endregion
        #region Method - FormattedLocalTime

        public DateTime FormattedLocalTime() {
            if (!string.IsNullOrWhiteSpace(Date)) {
                if (Date.IndexOf(".000") > 0) {
                    string tempTs = Date.Replace(".000", "");
                    if (long.TryParse(tempTs, out long ms)) {
                        DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
                        return dateTime;
                    }
                } else {
                    string tempTs = Date.Replace(".", "");
                    tempTs = tempTs[..^3];
                    string lowerTs = Date[^3..];
                    if (
                        long.TryParse(tempTs, out long ms) &&
                        long.TryParse(lowerTs, out long lowerMs)
                    ) {
                        ms += lowerMs;
                        DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
                        return dateTime;
                    }
                }
            }
            return DateTime.MinValue;
        }

        #endregion
        #region Method - FormattedAttachments

        public string FormattedAttachments() {
            StringBuilder formattedText = new();
            foreach (var att in Attachments) {
                _ = formattedText.Append($"[{att.Name}]<br>");
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedAttachedAttachments

        public string FormattedAttachedAttachments() {
            StringBuilder formattedText = new();
            foreach (var att in Attachments) {
                _ = formattedText.Append($"<attachment id='{att.TeamsGUID}'></attachment>");
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - FormattedMentions

        public List<Microsoft.Graph.Models.ChatMessageMention>? FormattedMentions() {
            List<Microsoft.Graph.Models.ChatMessageMention>? formattedMentions = [];
            if (Mentions != null) {
                int mentionId = 0;
                foreach (var mention in Mentions) {
                    ChatMessageMention chatMessageMention = new() {
                        Id = mentionId,
                        MentionText = mention.DisplayName,
                        Mentioned = new ChatMessageMentionedIdentitySet() {
                            User = new Identity() {
                                DisplayName = mention.DisplayName,
                                Id = mention.TeamsUserID
                            }
                        }
                    };
                    formattedMentions.Add(chatMessageMention);
                    mentionId++;
                }
            }
            return formattedMentions;
        }

        #endregion
    }
}
