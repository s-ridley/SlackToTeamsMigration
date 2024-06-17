// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace SlackToTeams.Models {
    public class SlackUser {
        #region Fields

        public static readonly SlackUser SLACK_BOT = BotUser("USLACKBOT", "Slack Bot");

        #endregion
        #region Properties

        public string DisplayName { get; private set; }
        public string? Email { get; private set; }
        public string SlackUserID { get; private set; }
        public string TeamsUserID { get; private set; }
        public bool IsBot { get; set; } = false;

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackUser(string slackUserID, string? teamsUserID, string displayName, string? email, bool isBot) {
            SlackUserID = slackUserID;
            TeamsUserID = teamsUserID ?? string.Empty;

            DisplayName = displayName;
            Email = email;
            IsBot = isBot;
        }

        public SlackUser(string slackUserID, string displayName, string? email, bool isBot) : this(slackUserID, string.Empty, displayName, email, isBot) {
        }

        #endregion
        #region Method - BotUser

        public static SlackUser BotUser(string slackUserID, string displayName) {
            return new SlackUser(slackUserID, displayName, string.Empty, true);
        }

        #endregion
        #region Method - SetTeamUserID

        public void SetTeamUserID(string? id) {
            TeamsUserID = id ?? string.Empty;
        }

        #endregion
        #region Method - ToChatMessageMention

        public ChatMessageMention ToChatMessageMention(int mentionId) {
            return new ChatMessageMention {
                Id = mentionId,
                MentionText = DisplayName,
                Mentioned = ToChatMessageMentionedIdentitySet()
            };
        }

        #endregion
        #region Method - ToChatMessageFromIdentitySet

        public ChatMessageFromIdentitySet? ToChatMessageFromIdentitySet() {
            if (!IsBot) {
                return new ChatMessageFromIdentitySet {
                    User = new Identity {
                        Id = TeamsUserID ?? null,
                        DisplayName = DisplayName ?? "Unknown"
                    }
                };
            } else {
                return null;
            }
        }

        #endregion
        #region Method - ToChatMessageMentionedIdentitySet

        public ChatMessageMentionedIdentitySet ToChatMessageMentionedIdentitySet() {
            return new ChatMessageMentionedIdentitySet {
                User = new Identity {
                    Id = TeamsUserID ?? null,
                    DisplayName = DisplayName ?? "Unknown"
                }
            };
        }

        #endregion
        #region Method - ToChatMessageReactionIdentitySet

        public ChatMessageReactionIdentitySet ToChatMessageReactionIdentitySet() {
            return new ChatMessageReactionIdentitySet {
                User = new Identity {
                    Id = TeamsUserID ?? null,
                    DisplayName = DisplayName ?? "Unknown"
                }
            };
        }

        #endregion
    }
}
