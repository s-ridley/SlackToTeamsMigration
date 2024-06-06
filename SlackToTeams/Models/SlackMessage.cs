// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;

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
        // Team Message IDs are the Timestamps first 13 digits
        public string? TeamID => ThreadDate?.Replace(".", "")[..13] ?? Date.Replace(".", "")[..13];

        #endregion
        #region Constructors

        public SlackMessage(SlackUser? user, string date, string? threadDate, string text, List<SlackAttachment> attachments) {
            User = user;

            Date = date;
            ThreadDate = threadDate;
            IsInThread = !string.IsNullOrEmpty(threadDate);
            IsParentThread = IsInThread && ThreadDate == Date;

            Text = text;
            Attachments = attachments;
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
            var ms = long.Parse(Date.Replace(".", "")) / 1000;
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
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
    }
}
