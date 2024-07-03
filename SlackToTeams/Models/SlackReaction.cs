using SlackToTeams.Utils;

namespace SlackToTeams.Models {
    public class SlackReaction(
        DateTimeOffset? createdDateTime,
        string? slackName,
        SlackUser? user
    ) {
        #region Properties

        public DateTimeOffset? CreatedDateTime { get; private set; } = createdDateTime;
        public string? Emoji { get; private set; } = ConvertHelper.SlackToEmoji(slackName);
        public string? SlackName { get; private set; } = slackName;
        public SlackUser? User { get; private set; } = user;

        #endregion
    }
}
