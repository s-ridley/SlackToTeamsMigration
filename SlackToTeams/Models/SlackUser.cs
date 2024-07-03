// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph.Models;

namespace SlackToTeams.Models {
    public class SlackUser(
        string? slackUserID,
        string? teamsUserID,
        string? displayName,
        string? email,
        bool isBot
    ) {
        #region Fields

        public static readonly SlackUser SLACK_BOT = BotUser(SLACK_BOT_ID, SLACK_BOT_NAME);
        public static readonly SlackUser UNKNOWN = BotUser(UNKNOWN_ID, UNKNOWN_NAME);

        #endregion
        #region Constants

        public const string SLACK_BOT_ID = "USLACKBOT";
        public const string SLACK_BOT_NAME = "Slack Bot";
        public const string UNKNOWN_ID = "UNK";
        public const string UNKNOWN_NAME = "Unknown";

        #endregion
        #region Properties

        public string DisplayName { get; private set; } = displayName ?? UNKNOWN_NAME;
        public string? Email { get; private set; } = email;
        public string SlackUserId { get; private set; } = slackUserID ?? SLACK_BOT_ID;
        public string TeamsUserId { get; private set; } = teamsUserID ?? string.Empty;
        public bool IsBot { get; set; } = isBot;

        #endregion
        #region Method - BotUser

        public static SlackUser BotUser(
            string? slackUserID,
            string? displayName
        ) {
            return new SlackUser(
                slackUserID,    // slackUserID
                null,           // teamsUserID
                displayName,    // displayName
                string.Empty,   // email
                true            // isBot
            );
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
