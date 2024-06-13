using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace SlackToTeams.Models {
    public class SlackReaction {
        #region Properties

        public DateTimeOffset? CreatedDateTime { get; private set; }
        public string? ReactionType { get; private set; }
        public SlackUser User { get; private set; }

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackReaction(DateTimeOffset? createdDateTime, string reactionType, SlackUser user) {
            CreatedDateTime = createdDateTime;
            ReactionType = reactionType;
            User = user;
        }

        #endregion
        #region Method - ToChatMessageReaction

        public ChatMessageReaction ToChatMessageReaction() {
            return new ChatMessageReaction {
                CreatedDateTime = DateTimeOffset.Now,
                ReactionType = ReactionType,
                User = User.ToChatMessageReactionIdentitySet()
            };
        }

        #endregion
    }
}
