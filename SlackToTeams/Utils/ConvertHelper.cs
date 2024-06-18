using System.Globalization;
using System.Reflection;
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
                TextInfo currentTextInfo = CultureInfo.CurrentCulture.TextInfo;

                string processedReaction = slackReaction.Replace("_", string.Empty);
                /*
                string processedReaction = string.Empty;

                string[] reactionArray = slackReaction.Split('_');
                if (
                    reactionArray != null &&
                    reactionArray.Length > 0
                ) {
                    foreach (string reaction in reactionArray) {
                        processedReaction += currentTextInfo.ToTitleCase(reaction);
                    }
                }
                */
                // Check for exact match
                foreach (FieldInfo field in typeof(Emoji).GetFields().Where(f => f.Name.Equals(processedReaction, StringComparison.CurrentCultureIgnoreCase))) {
                    object? rawObject = field.GetRawConstantValue();
                    if (rawObject != null) {
                        result = rawObject.ToString();
                    }
                }

                // Check for faces
                if (string.IsNullOrWhiteSpace(result)) {
                    foreach (FieldInfo field in typeof(Emoji).GetFields().Where(f => f.Name.Equals($"{processedReaction}Face", StringComparison.CurrentCultureIgnoreCase))) {
                        object? rawObject = field.GetRawConstantValue();
                        if (rawObject != null) {
                            result = rawObject.ToString();
                        }
                    }
                }

                // Check for starts with
                if (string.IsNullOrWhiteSpace(result)) {
                    foreach (FieldInfo field in typeof(Emoji).GetFields().Where(f => f.Name.StartsWith(processedReaction, StringComparison.CurrentCultureIgnoreCase))) {
                        object? rawObject = field.GetRawConstantValue();
                        if (rawObject != null) {
                            result = rawObject.ToString();
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(result)) {
                    result = slackReaction switch {
                        "+1" => Emoji.ThumbsUp,
                        "cake" => Emoji.BirthdayCake,
                        "clap" => Emoji.ClappingHands,
                        "coffee" or "coffee2" => Emoji.HotBeverage,
                        "cold_sweat" => Emoji.SweatDroplets,
                        "cop" => Emoji.PoliceOfficer,
                        "cripes" => Emoji.AstonishedFace,
                        "dark_sunglasses" => Emoji.Sunglasses,
                        "japanese_goblin" => Emoji.Goblin,
                        "joy" => Emoji.FaceWithTearsOfJoy,
                        "mag" => Emoji.MagnifyingGlassTiltedLeft,
                        "man-bowing" => Emoji.Bowling,
                        "metal" or "the_horns" => Emoji.SignOfTheHorns,
                        "muscle" => Emoji.FlexedBiceps,
                        "nallears" => Emoji.Ear,
                        "party_parrot" => Emoji.Parrot,
                        "rip" => Emoji.Headstone,
                        "sob" => Emoji.LoudlyCryingFace,
                        "slick" => Emoji.SmilingFaceWithSunglasses,
                        "smile" or "simple_smile" => Emoji.SmilingFace,
                        "smirk" or "vee" => Emoji.SmirkingFace,
                        "smith" => Emoji.Hammer,
                        "ssh" => Emoji.ShushingFace,
                        "stuck_out_tongue_winking_eye" => Emoji.WinkingFaceWithTongue,
                        "suspect" => Emoji.FaceWithRaisedEyebrow,
                        "tada" => Emoji.MagicWand,
                        "toot" => Emoji.PartyingFace,
                        "trollin" => Emoji.GrinningSquintingFace,
                        "wave" => Emoji.WavingHand,
                        "white_check_mark" => Emoji.CheckBoxWithCheck,
                        _ => slackReaction,
                    };
                }

                if (
                    !string.IsNullOrWhiteSpace(result) &&
                    result.Equals(slackReaction)
                ) {
                    s_logger.Warning("No Emoji found for slackReaction[{slackReaction}]", slackReaction);
                }
            }

            return result;
        }

        #endregion
    }
}
