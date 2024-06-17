using System.Text.RegularExpressions;
using Serilog;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public partial class ConvertHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(ConvertHelper));

        [GeneratedRegex(@"<\@\w+>")]
        private static partial Regex UserIdRegex();

        #endregion
        #region Method - ReplaceUserIdWithName

        public static string ReplaceUserIdWithName(string textToCheck, List<SlackUser> userList) {
            if (
                !string.IsNullOrEmpty(textToCheck) &&
                userList != null &&
                userList.Count > 0
            ) {
                Regex regex = UserIdRegex();
                foreach (Match match in regex.Matches(textToCheck)) {
                    string userId = match.Value.Replace("<@", string.Empty);
                    userId = userId.Replace(">", string.Empty);
                    SlackUser foundUser = UsersHelper.FindUser(userList, userId);
                    if (
                        foundUser != null &&
                        !foundUser.IsBot &&
                        !string.IsNullOrEmpty(foundUser.DisplayName)
                    ) {
                        textToCheck = textToCheck.Replace($"{match.Value}", $"[{foundUser.DisplayName}]");
                    }
                }
                return textToCheck;
            } else {
                return textToCheck;
            }
        }

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
                    "bookmark" => Emoji.Bookmark,
                    "burrito" => Emoji.Burrito,
                    "clap" => Emoji.ClappingHands,
                    "coffee" => Emoji.HotBeverage,
                    "cop" => Emoji.PoliceOfficer,
                    "confused" => Emoji.ConfusedFace,
                    "cripes" => Emoji.AstonishedFace,
                    "dark_sunglasses" or "sunglasses" => Emoji.Sunglasses,
                    "grimacing" => Emoji.GrimacingFace,
                    "grinning" => Emoji.GrinningFace,
                    "japanese_goblin" => Emoji.Goblin,
                    "joy" => Emoji.FaceWithTearsOfJoy,
                    "mag" => Emoji.MagnifyingGlassTiltedLeft,
                    "man-bowing" => Emoji.Bowling,
                    "mantelpiece_clock" => Emoji.MantelpieceClock,
                    "metal" => Emoji.SignOfTheHorns,
                    "money_mouth_face" => Emoji.MoneyMouthFace,
                    "ok_hand" => Emoji.OKHand,
                    "oncoming_police_car" => Emoji.OncomingPoliceCar,
                    "parrot" or "party_parrot" => Emoji.Parrot,
                    "relieved" => Emoji.RelievedFace,
                    "rocket" => Emoji.Rocket,
                    "rolling_on_the_floor_laughing" => Emoji.RollingOnTheFloorLaughing,
                    "sob" => Emoji.LoudlyCryingFace,
                    "slick" => Emoji.SmilingFaceWithSunglasses,
                    "slightly_smiling_face" => Emoji.SlightlySmilingFace,
                    "smile" or "simple_smile" => Emoji.SmilingFace,
                    "smirk" or "vee" => Emoji.SmirkingFace,
                    "smith" => Emoji.Hammer,
                    "ssh" => Emoji.ShushingFace,
                    "stuck_out_tongue_winking_eye" => Emoji.WinkingFaceWithTongue,
                    "sun" => Emoji.Sun,
                    "suspect" => Emoji.FaceWithRaisedEyebrow,
                    "star" => Emoji.Star,
                    "tada" => Emoji.MagicWand,
                    "tophat" => Emoji.TopHat,
                    "toot" => Emoji.PartyingFace,
                    "trollin" => Emoji.GrinningSquintingFace,
                    "turtle" => Emoji.Turtle,
                    "unamused" => Emoji.UnamusedFace,
                    "upside_down_face" => Emoji.UpsideDownFace,
                    "wave" => Emoji.WavingHand,
                    "white_check_mark" => Emoji.CheckBoxWithCheck,
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
