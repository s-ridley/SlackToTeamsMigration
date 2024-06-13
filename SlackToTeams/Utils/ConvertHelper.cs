using System;

namespace SlackToTeams.Utils {
    public class ConvertHelper {
        #region Method - SlackTimestampToDateTime

        public static DateTime SlackTimestampToDateTime(string timestamp) {
            return SlackTimestampToDateTimeOffset(timestamp).LocalDateTime;
        }

        #endregion
        #region Method - SlackTimestampToDateTimeOffset

        public static DateTimeOffset SlackTimestampToDateTimeOffset(string timestamp) {
            DateTimeOffset result = DateTimeOffset.MinValue;

            if (!string.IsNullOrWhiteSpace(timestamp)) {
                if (timestamp.IndexOf(".000") > 0) {
                    string tempTs = timestamp.Replace(".000", "");
                    if (long.TryParse(tempTs, out long ms)) {
                        result = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                    }
                } else {
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
                }
            }
            return result;
        }

        #endregion
        #region Method - SlackToTeamsReaction

        public static string? SlackToTeamsReaction(string? slackReaction) {
            string? result = null;

            if (!string.IsNullOrWhiteSpace(slackReaction)) {
                // result can be one of : like, angry, sad, laugh, heart, surprised

                // man-bowing
                // money_mouth_face
                // rocket
                // rolling_on_the_floor_laughing
                // grimacing
                // sunglasses
                // wave

                result = slackReaction switch {
                    "rolling_on_the_floor_laughing" => "laugh",
                    "ok_hand" or "smirk" or "vee" or "+1" => "like",
                    _ => slackReaction,
                };
            }

            return result;
        }

        #endregion
    }
}
