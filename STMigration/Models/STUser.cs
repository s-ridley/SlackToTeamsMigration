// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace STMigration.Models;

[method: JsonConstructor]
public class STUser(string slackUserID, string? teamsUserID, string displayName, string? email, bool isBot) {
    public string DisplayName { get; private set; } = displayName;
    public string? Email { get; private set; } = email;

    public string SlackUserID { get; private set; } = slackUserID;
    public string TeamsUserID { get; private set; } = teamsUserID ?? string.Empty;

    public bool IsBot { get; set; } = isBot;

    public static STUser BotUser(string slackUserID, string displayName) {
        return new STUser(slackUserID, string.Empty, displayName, string.Empty, true);
    }

    public static readonly STUser SLACK_BOT = BotUser("USLACKBOT", "Slack Bot");

    public void SetTeamUserID(string? id) {
        TeamsUserID = id ?? string.Empty;
    }
}
