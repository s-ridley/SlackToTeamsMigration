﻿using Newtonsoft.Json;
using SlackToTeams.Utils;

namespace SlackToTeams.Models {
    public class SlackReaction {
        #region Properties

        public DateTimeOffset? CreatedDateTime { get; private set; }
        public string? Emoji { get; private set; }
        public string? SlackName { get; private set; }
        public SlackUser User { get; private set; }

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackReaction(DateTimeOffset? createdDateTime, string slackName, SlackUser user) {
            CreatedDateTime = createdDateTime;
            SlackName = slackName;
            User = user;

            Emoji = ConvertHelper.SlackToEmoji(SlackName);
        }

        #endregion
    }
}
