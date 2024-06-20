﻿using System.Reflection;
using System.Text.RegularExpressions;
using Serilog;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public partial class ConvertHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(ConvertHelper));

        [GeneratedRegex(@"<\@\w+>")]
        private static partial Regex UserIdRegex();

        [GeneratedRegex(@"\W", RegexOptions.IgnoreCase)]
        private static partial Regex SafeFileRegex();

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
        #region Method - FileSystemSafe

        public static string FileSystemSafe(string toConvert) {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(toConvert)) {
                result = SafeFileRegex().Replace(toConvert, "");
            }

            return result;
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
                string processedReaction = slackReaction.Replace("_", string.Empty);

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
                        "+1::skin-tone-2" => Emoji.ThumbsUp_LightSkinTone,
                        "100" => Emoji.HundredPoints,
                        "20bux" => Emoji.DollarBanknote,
                        "allears" or "nallears" => Emoji.Ear,
                        "beers" => Emoji.BeerMug,
                        "cake" => Emoji.BirthdayCake,
                        "chart_with_upwards_trend" => Emoji.ChartIncreasing,
                        "clap" => Emoji.ClappingHands,
                        "coffee" or "coffee2" => Emoji.HotBeverage,
                        "cold_sweat" => Emoji.AnxiousFaceWithSweat,
                        "confuoot" => Emoji.ConfusedFace,
                        "cop" => Emoji.PoliceOfficer,
                        "cripes" => Emoji.AstonishedFace,
                        "dagger_knife" => Emoji.Dagger,
                        "dark_sunglasses" => Emoji.Sunglasses,
                        "face_holding_back_tears" => Emoji.BeamingFaceWithSmilingEyes,
                        "facepunch" => Emoji.OncomingFist,
                        "flag-in" => Emoji.FlagInHole,
                        "hammer-down" => Emoji.Hammer,
                        "headphones" => Emoji.Headphone,
                        "heart_eyes" => Emoji.SmilingFaceWithHeartEyes,
                        "heavy_check_mark" or "white_check_mark" => Emoji.CheckBoxWithCheck,
                        "heavy_plus_sign" => Emoji.Plus,
                        "hmmyes" => Emoji.ThinkingFace,
                        "hugging_face" => Emoji.SmilingFaceWithOpenHands,
                        "iphone" => Emoji.MobilePhone,
                        "japanese_ogre" => Emoji.Ogre,
                        "japanese_goblin" => Emoji.Goblin,
                        "joy" or "laughing" => Emoji.FaceWithTearsOfJoy,
                        "joy_cat" => Emoji.CatWithTearsOfJoy,
                        "laugh" => Emoji.RollingOnTheFloorLaughing,
                        "mag" => Emoji.MagnifyingGlassTiltedLeft,
                        "man-bowing" => Emoji.Bowling,
                        "man_in_business_suit_levitating" => Emoji.PersonInSuitLevitating,
                        "mask" => Emoji.FaceWithMedicalMask,
                        "metal" or "the_horns" => Emoji.SignOfTheHorns,
                        "muscle" => Emoji.FlexedBiceps,
                        "open_mouth" => Emoji.FaceWithOpenMouth,
                        "party_parrot" => Emoji.Parrot,
                        "question" => Emoji.WhiteQuestionMark,
                        "raised_hands" => Emoji.RaisedHand,
                        "raised_hands::skin-tone-2" => Emoji.RaisedHand_LightSkinTone,
                        "robot_face" => Emoji.Robot,
                        "rip" => Emoji.Headstone,
                        "scream" => Emoji.FaceScreamingInFear,
                        "sob" => Emoji.LoudlyCryingFace,
                        "slick" => Emoji.SmilingFaceWithSunglasses,
                        "smile" or "simple_smile" or "smiley" => Emoji.SmilingFace,
                        "smirk" or "vee" => Emoji.SmirkingFace,
                        "smith" => Emoji.Hammer,
                        "ssh" => Emoji.ShushingFace,
                        "stare" => Emoji.FaceWithoutMouth,
                        "stuck_out_tongue" => Emoji.Tongue,
                        "stuck_out_tongue_winking_eye" => Emoji.WinkingFaceWithTongue,
                        "suspect" => Emoji.FaceWithRaisedEyebrow,
                        "sweat_smile" => Emoji.GrinningFaceWithSweat,
                        "tada" => Emoji.MagicWand,
                        "thatsright" or "this" => Emoji.ThumbsUp,
                        "toot" => Emoji.PartyingFace,
                        "trollin" => Emoji.GrinningSquintingFace,
                        "unicorn_face" => Emoji.Unicorn,
                        "wave" => Emoji.WavingHand,
                        "yum" => Emoji.FaceSavoringFood,
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
