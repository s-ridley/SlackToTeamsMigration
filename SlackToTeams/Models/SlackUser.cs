// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace SlackToTeams.Models {
    public class SlackUser {
        #region Fields

        public static readonly SlackUser SLACK_BOT = BotUser(SLACK_BOT_ID, SLACK_BOT_NAME);

        #endregion
        #region Constants

        public const string SLACK_BOT_ID = "USLACKBOT";
        public const string SLACK_BOT_NAME = "Slack Bot";
        public const string UNKNOWN_NAME = "Unknown";

        #endregion
        #region Properties

        public string DisplayName { get; private set; }
        public string? Email { get; private set; }
        public string SlackUserId { get; private set; }
        public string TeamsUserId { get; private set; }
        public bool IsBot { get; set; } = false;

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackUser(string? slackUserID, string? teamsUserID, string? displayName, string? email, bool isBot) {
            SlackUserId = slackUserID ?? SLACK_BOT_ID;
            TeamsUserId = teamsUserID ?? string.Empty;
            DisplayName = displayName ?? UNKNOWN_NAME;
            Email = email;
            IsBot = isBot;
        }

        public SlackUser(string? slackUserID, string? displayName, string? email, bool isBot) : this(slackUserID, string.Empty, displayName, email, isBot) {
        }

        #endregion
        #region Method - BotUser

        public static SlackUser BotUser(string? slackUserID, string? displayName) {
            return new SlackUser(slackUserID, displayName, string.Empty, true);
        }

        #endregion
        #region Method - SetTeamUserID

        public void SetTeamUserID(string? id) {
            TeamsUserId = id ?? string.Empty;
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
            return new ChatMessageFromIdentitySet {
                User = new Identity {
                    Id = TeamsUserId ?? null,
                    DisplayName = DisplayName ?? UNKNOWN_NAME
                }
            };
        }

        #endregion
        #region Method - ToChatMessageMentionedIdentitySet

        public ChatMessageMentionedIdentitySet ToChatMessageMentionedIdentitySet() {
            return new ChatMessageMentionedIdentitySet {
                User = new Identity {
                    Id = TeamsUserId ?? null,
                    DisplayName = DisplayName ?? UNKNOWN_NAME
                }
            };
        }

        #endregion
        #region Method - ToChatMessageReactionIdentitySet

        public ChatMessageReactionIdentitySet ToChatMessageReactionIdentitySet() {
            return new ChatMessageReactionIdentitySet {
                User = new Identity {
                    Id = TeamsUserId ?? null,
                    DisplayName = DisplayName ?? UNKNOWN_NAME
                }
            };
        }

        #endregion
    }
}
