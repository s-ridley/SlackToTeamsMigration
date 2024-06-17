using Serilog;

namespace SlackToTeams.Utils {
    public class ConvertHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(ConvertHelper));

        #endregion
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
                    "bed" => Emoji.Bed,
                    "burrito" => Emoji.Burrito,
                    "clap" => Emoji.ClappingHands,
                    "coffee" => Emoji.HotBeverage,
                    "cop" => Emoji.PoliceOfficer,
                    "confused" => Emoji.ConfusedFace,
                    "dark_sunglasses" or "sunglasses" => Emoji.Sunglasses,
                    "grimacing" => Emoji.GrimacingFace,
                    "grinning" => Emoji.GrinningFace,
                    "japanese_goblin" => Emoji.Goblin,
                    "joy" => Emoji.FaceWithTearsOfJoy,
                    "man-bowing" => Emoji.Bowling,
                    "mantelpiece_clock" => Emoji.MantelpieceClock,
                    "money_mouth_face" => Emoji.MoneyMouthFace,
                    "ok_hand" => Emoji.OKHand,
                    "parrot" or "party_parrot" => Emoji.Parrot,
                    "relieved" => Emoji.RelievedFace,
                    "rocket" => Emoji.Rocket,
                    "rolling_on_the_floor_laughing" => Emoji.RollingOnTheFloorLaughing,
                    "smile" or "simple_smile" => Emoji.SmilingFace,
                    "slightly_smiling_face" => Emoji.SlightlySmilingFace,
                    "smirk" or "vee" => Emoji.SmirkingFace,
                    "smith" => Emoji.Hammer,
                    "stuck_out_tongue_winking_eye" => Emoji.WinkingFaceWithTongue,
                    "sun" => Emoji.Sun,
                    "star" => Emoji.Star,
                    "tada" => Emoji.MagicWand,
                    "tophat" => Emoji.TopHat,
                    "toot" => Emoji.PartyingFace,
                    "turtle" => Emoji.Turtle,
                    "unamused" => Emoji.UnamusedFace,
                    "upside_down_face" => Emoji.UpsideDownFace,
                    "wave" => Emoji.WavingHand,
                    "zzz" => Emoji.Zzz,
                    _ => slackReaction,
                };
                if (result.Equals(slackReaction)) {
                    s_logger.Warning("No Emoji found for slackReaction[{slackReaction}]", slackReaction);
                }
            }

            return result;
        }

        #endregion
    }
}
