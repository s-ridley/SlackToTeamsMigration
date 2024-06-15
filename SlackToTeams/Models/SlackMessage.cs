// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Web;
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
                    _ = formattedText.Append($"[{HttpUtility.HtmlEncode(reaction.ReactionType)}] <at id=\"{Mentions.Count}\">{reaction.User.DisplayName}</at><br>");
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
                foreach (var hostedContent in HostedContents) {
                    _ = formattedText.Append($"<span><img src=\"../hostedContents/{hostedContent.Id}/$value\"></span>");
                }
            }

            return formattedText.ToString();
        }

        #endregion
        #region Method - ToMentions

        private List<Microsoft.Graph.Models.ChatMessageMention> ToMentions() {
            List<Microsoft.Graph.Models.ChatMessageMention>? formattedMentions = [];
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
        #region Method - ToReactions

        private List<Microsoft.Graph.Models.ChatMessageReaction> ToReactions() {
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
        #region Method - ToHostedContents

        private List<Microsoft.Graph.Models.ChatMessageHostedContent>? ToHostedContents() {
            List<Microsoft.Graph.Models.ChatMessageHostedContent>? formattedHostedContents = null;
            if (
                HostedContents != null &&
                HostedContents.Count > 0
            ) {
                foreach (var hostedContent in HostedContents) {
                    formattedHostedContents ??= [];
                    formattedHostedContents.Add(hostedContent.ToChatMessageHostedContent());
                }
            }
            return formattedHostedContents;
        }

        #endregion
        #region Method - ToAttachments

        private List<ChatMessageAttachment>? ToAttachments() {
            List<ChatMessageAttachment>? attachments = null;
            if (
                Attachments != null &&
                Attachments.Count > 0
            ) {
                foreach (var attachment in Attachments) {
                    if (
                        attachment != null &&
                        !string.IsNullOrWhiteSpace(attachment.TeamsGUID) &&
                        !string.IsNullOrWhiteSpace(attachment.TeamsURL)
                    ) {
                        attachments ??= [];
                        attachments.Add(new ChatMessageAttachment {
                            Id = attachment.TeamsGUID,
                            ContentType = "reference",
                            ContentUrl = attachment.TeamsURL,
                            Name = attachment.Name
                        });
                    }
                }
            }
            return attachments;
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
                Mentions = ToMentions(),
                Reactions = ToReactions(),
                HostedContents = ToHostedContents(),
                Attachments = ToAttachments()
            };
        }

        #endregion
    }
}
