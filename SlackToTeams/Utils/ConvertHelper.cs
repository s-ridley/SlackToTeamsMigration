using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
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
        #region Method - EmojiToHtml

        public static string? EmojiToHtml(string? toConvert) {
            string? result = toConvert;
            // Dont convert emojis that are still in Slack format
            if (
                !string.IsNullOrEmpty(toConvert) &&
                !toConvert.Contains(':')
            ) {
                char[] chars = HttpUtility.HtmlEncode(toConvert).ToCharArray();
                StringBuilder encodedValue = new();
                foreach (char c in chars) {
                    // above normal ASCII
                    if ((int)c > 127) {
                        encodedValue.Append("&#" + (int)c + ";");
                    } else {
                        encodedValue.Append(c);
                    }
                }
                result = encodedValue.ToString();
            }
            return result;
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
                string processedReaction = slackReaction.Replace("_", string.Empty);
                processedReaction = processedReaction.Replace("-", string.Empty);
                processedReaction = processedReaction.Replace("::skintone1", "_LightSkinTone");
                processedReaction = processedReaction.Replace("::skintone2", "_MediumLightSkinTone");
                processedReaction = processedReaction.Replace("::skintone3", "_MediumSkinTone");
                processedReaction = processedReaction.Replace("::skintone4", "_MediumDarkSkinTone");
                processedReaction = processedReaction.Replace("::skintone5", "_DarkSkinTone");

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

                if (string.IsNullOrWhiteSpace(result)) {
                    result = slackReaction switch {
                        "+1" => Emoji.ThumbsUp,
                        "-1" => Emoji.ThumbsDown,
                        "+1::skin-tone-2" => Emoji.ThumbsUp_MediumLightSkinTone,
                        "+1::skin-tone-3" => Emoji.ThumbsUp_MediumSkinTone,
                        "100" => Emoji.HundredPoints,
                        "10bux" or "20bux" => Emoji.DollarBanknote,
                        "allears" or "nallears" => Emoji.Ear,
                        "argh" => Emoji.WearyFace,
                        "beer" or "beers" => Emoji.BeerMug,
                        "bike" => Emoji.Bicycle,
                        "biohazard_sign" => Emoji.Biohazard,
                        "boom" => Emoji.Collision,
                        "bulb" => Emoji.LightBulb,
                        "cake" => Emoji.BirthdayCake,
                        "champagne" => Emoji.BottleWithPoppingCork,
                        "chart_with_upwards_trend" => Emoji.ChartIncreasing,
                        "clap" => Emoji.ClappingHands,
                        "cocktail" => Emoji.CocktailGlass,
                        "coffee" or "coffee2" => Emoji.HotBeverage,
                        "colbert" => Emoji.FaceWithRaisedEyebrow,
                        "cold_sweat" => Emoji.AnxiousFaceWithSweat,
                        "confuoot" => Emoji.ConfusedFace,
                        "congratulations" => Emoji.JapaneseCongratulationsButton,
                        "cop" => Emoji.PoliceOfficer,
                        "cripes" => Emoji.AstonishedFace,
                        "cry" => Emoji.CryingFace,
                        "dagger_knife" => Emoji.Dagger,
                        "dark_sunglasses" => Emoji.Sunglasses,
                        "earth_asia" => Emoji.GlobeShowingAsiaAustralia,
                        "eyeglasses" => Emoji.Glasses,
                        "face_holding_back_tears" => Emoji.BeamingFaceWithSmilingEyes,
                        "face_palm" or "facepalm" => Emoji.PersonFacepalming,
                        "facepunch" => Emoji.OncomingFist,
                        "flag-in" => Emoji.FlagInHole,
                        "grin" => Emoji.GrinningFace,
                        "hand" => Emoji.HandWithFingersSplayed,
                        "hammer-down" => Emoji.Hammer,
                        "headphones" => Emoji.Headphone,
                        "heart" => Emoji.RedHeart,
                        "heart_eyes" => Emoji.SmilingFaceWithHeartEyes,
                        "heart_eyes_cat" => Emoji.SmilingCatWithHeartEyes,
                        "hearts" => Emoji.TwoHearts,
                        "heavy_check_mark" or "white_check_mark" => Emoji.CheckBoxWithCheck,
                        "heavy_plus_sign" => Emoji.Plus,
                        "hmmyes" => Emoji.ThinkingFace,
                        "hugging_face" => Emoji.SmilingFaceWithOpenHands,
                        "iphone" => Emoji.MobilePhone,
                        "innocent" => Emoji.SmilingFaceWithHalo,
                        "japanese_ogre" => Emoji.Ogre,
                        "japanese_goblin" => Emoji.Goblin,
                        "joy" or "laughing" => Emoji.FaceWithTearsOfJoy,
                        "joy_cat" => Emoji.CatWithTearsOfJoy,
                        "kissing_heart" => Emoji.KissingFace,
                        "knife_fork_plate" => Emoji.ForkAndKnifeWithPlate,
                        "laugh" => Emoji.RollingOnTheFloorLaughing,
                        "lower_left_ballpoint_pen" => Emoji.Pen,
                        "lower_left_paintbrush" => Emoji.Paintbrush,
                        "mad" => Emoji.EnragedFace,
                        "mag" => Emoji.MagnifyingGlassTiltedLeft,
                        "male-police-officer" => Emoji.ManPoliceOfficer,
                        "man-bowing" => Emoji.Bowling,
                        "man_in_business_suit_levitating" => Emoji.PersonInSuitLevitating,
                        "man-shrugging" => Emoji.ManShrugging,
                        "mask" => Emoji.FaceWithMedicalMask,
                        "metal" or "the_horns" => Emoji.SignOfTheHorns,
                        "munch" => Emoji.FaceScreamingInFear,
                        "muscle" => Emoji.FlexedBiceps,
                        "no_entry_sign" => Emoji.NoEntry,
                        "open_mouth" => Emoji.FaceWithOpenMouth,
                        "party_parrot" => Emoji.Parrot,
                        "persevere" => Emoji.PerseveringFace,
                        "point_up_2" => Emoji.BackhandIndexPointingUp,
                        "pray" => Emoji.FoldedHands,
                        "pray::skin-tone-1" => Emoji.FoldedHands_LightSkinTone,
                        "pray::skin-tone-2" => Emoji.FoldedHands_MediumLightSkinTone,
                        "pray::skin-tone-3" => Emoji.FoldedHands_MediumSkinTone,
                        "pray::skin-tone-4" => Emoji.FoldedHands_MediumDarkSkinTone,
                        "pray::skin-tone-5" => Emoji.FoldedHands_DarkSkinTone,
                        "question" => Emoji.WhiteQuestionMark,
                        "radioactive_sign" => Emoji.Radioactive,
                        "rage" => Emoji.EnragedFace,
                        "rain_cloud" => Emoji.CloudWithRain,
                        "raised_hands" => Emoji.RaisedHand,
                        "raised_hands::skin-tone-2" => Emoji.RaisedHand_MediumLightSkinTone,
                        "relaxed" => Emoji.RelievedFace,
                        "robot_face" => Emoji.Robot,
                        "rip" => Emoji.Headstone,
                        "runner" => Emoji.PersonRunning,
                        "scream" => Emoji.FaceScreamingInFear,
                        "scream_cat" => Emoji.WearyCat,
                        "science" => Emoji.Scientist,
                        "sigh" => Emoji.FrowningFace,
                        "siren" => Emoji.PoliceCarLight,
                        "six_pointed_star" => Emoji.StarOfDavid,
                        "smile_cat" => Emoji.GrinningCat,
                        "smiley_cat" => Emoji.GrinningCatWithSmilingEyes,
                        "sob" => Emoji.LoudlyCryingFace,
                        "slick" => Emoji.SmilingFaceWithSunglasses,
                        "smile" or "simple_smile" or "smiley" => Emoji.SmilingFace,
                        "smirk" or "vee" => Emoji.SmirkingFace,
                        "smith" => Emoji.Hammer,
                        "spock-hand" => Emoji.VulcanSalute,
                        "ssh" => Emoji.ShushingFace,
                        "stare" => Emoji.FaceWithoutMouth,
                        "stonk" => Emoji.AstonishedFace,
                        "stuck_out_tongue" => Emoji.Tongue,
                        "stuck_out_tongue_winking_eye" => Emoji.WinkingFaceWithTongue,
                        "stwoon" => Emoji.SmilingFaceWithHearts,
                        "suspect" => Emoji.FaceWithRaisedEyebrow,
                        "suspense" => Emoji.GrimacingFace,
                        "sweat_smile" => Emoji.GrinningFaceWithSweat,
                        "swimmer" => Emoji.PersonSwimming,
                        "table_tennis_paddle_and_ball" => Emoji.PingPong,
                        "tada" => Emoji.MagicWand,
                        "thatsright" or "this" or "thumbsup_all" => Emoji.ThumbsUp,
                        "toot" => Emoji.PartyingFace,
                        "trollin" => Emoji.GrinningSquintingFace,
                        "unicorn_face" => Emoji.Unicorn,
                        "wave" => Emoji.WavingHand,
                        "wave::skin-tone-2" => Emoji.WavingHand_MediumLightSkinTone,
                        "wink" => Emoji.WinkingFace,
                        "yum" => Emoji.FaceSavoringFood,
                        _ => $":{slackReaction}:",
                    };
                }

                if (
                    !string.IsNullOrWhiteSpace(result) &&
                    result.Equals($":{slackReaction}:")
                ) {
                    s_logger.Warning("No Emoji found for slackReaction[{slackReaction}]", slackReaction);
                }
            }

            return result;
        }

        #endregion
    }
}
