// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

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
    }
}
