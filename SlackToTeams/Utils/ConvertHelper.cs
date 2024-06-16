using System.Numerics;
using System.Text;

namespace SlackToTeams.Utils {
    public class ConvertHelper {
        #region Method - SlackTimestampToDateTime

        public static DateTime SlackTimestampToDateTime(string? timestamp) {
            return SlackTimestampToDateTimeOffset(timestamp).LocalDateTime;
        }

        #endregion
        #region Method - SlackTimestampToDateTimeOffset

        public static DateTimeOffset SlackTimestampToDateTimeOffset(string? timestamp) {
            DateTimeOffset result = DateTimeOffset.MinValue;

            if (!string.IsNullOrWhiteSpace(timestamp)) {
                if (timestamp.IndexOf(".000") > 0) {
                    string tempTs = timestamp.Replace(".000", "");
                    if (long.TryParse(tempTs, out long ms)) {
                        result = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                    }
                } else if (timestamp.IndexOf(".") > 0) {
                    string tempTs = timestamp.Replace(".", "");
                    tempTs = tempTs[..^3];
                    string lowerTs = timestamp[^3..];
                    if (
                        long.TryParse(tempTs, out long ms) &&
                        long.TryParse(lowerTs, out long lowerMs)
                    ) {
                        ms += lowerMs;
                        result = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                    }
                } else {
                    if (long.TryParse(timestamp, out long ms)) {
                        result = DateTimeOffset.FromUnixTimeSeconds(ms);
                    }
                }
            } else {
                result = DateTimeOffset.UtcNow;
            }
            return result;
        }

        #endregion
        #region Method - SlackToEmoji

        public static string? SlackToEmoji(string? slackReaction) {
            string? result = null;

            if (!string.IsNullOrWhiteSpace(slackReaction)) {
                result = slackReaction switch {
                    "+1" => Emoji.ThumbsUp,
                    "grimacing" => Emoji.GrimacingFace,
                    "man-bowing" => Emoji.Bowling,
                    "money_mouth_face" => Emoji.MoneyMouthFace,
                    "ok_hand" => Emoji.OKHand,
                    "rocket" => Emoji.Rocket,
                    "rolling_on_the_floor_laughing" => Emoji.RollingOnTheFloorLaughing,
                    "smirk" or "vee" => Emoji.SmirkingFace,
                    "sunglasses" => Emoji.Sunglasses,
                    "wave" => Emoji.WavingHand,
                    _ => slackReaction,
                };
            }

            return result;
        }

        #endregion
        #region Method - SlackToTeamsReaction

        public static string? SlackToTeamsReaction(string? slackReaction) {
            string? result = null;

            if (!string.IsNullOrWhiteSpace(slackReaction)) {
                // result can be one of : like, angry, sad, laugh, heart, surprised
                result = slackReaction switch {
                    "wave" => "angry",
                    "sunglasses" => "sad",
                    "rolling_on_the_floor_laughing" => "laugh",
                    "man-bowing" => "heart",
                    "money_mouth_face" => "surprised",
                    _ => "like",
                };
            }

            return result;
        }

        #endregion
    }
}
