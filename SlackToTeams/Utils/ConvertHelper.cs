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

        public static readonly List<string> ReservedFilenames = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

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

        public static string FileSystemSafe(string? toConvert) {
            if (!string.IsNullOrWhiteSpace(toConvert)) {
                // A filename cannot be one of PatientrackConst.ReservedFilenames
                if (ReservedFilenames.Contains(toConvert)) {
                    // If it is then it needs to have '~' appended
                    toConvert += "~";
                } else {
                    toConvert = toConvert.Replace(' ', '~');
                    toConvert = toConvert.Replace('<', '~');
                    toConvert = toConvert.Replace('>', '~');
                    toConvert = toConvert.Replace(':', '~');
                    toConvert = toConvert.Replace('"', '~');
                    toConvert = toConvert.Replace('/', '~');
                    toConvert = toConvert.Replace('\\', '~');
                    toConvert = toConvert.Replace('|', '~');
                    toConvert = toConvert.Replace('?', '~');
                    toConvert = toConvert.Replace('*', '~');
                }
                return toConvert;
            } else {
                return string.Empty;
            }
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
                } else if (timestamp.IndexOf('.') > 0) {
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
                string processedReaction = slackReaction.Replace("male_", "man_");
                processedReaction = processedReaction.Replace("female_", "woman_");
                processedReaction = processedReaction.Replace("_", string.Empty);
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
                        "+1::skin-tone-1" => Emoji.ThumbsUp_LightSkinTone,
                        "+1::skin-tone-2" => Emoji.ThumbsUp_MediumLightSkinTone,
                        "+1::skin-tone-3" => Emoji.ThumbsUp_MediumSkinTone,
                        "+1::skin-tone-4" => Emoji.ThumbsUp_MediumDarkSkinTone,
                        "+1::skin-tone-5" => Emoji.ThumbsUp_DarkSkinTone,
                        "100" => Emoji.HundredPoints,
                        "10bux" or "20bux" => Emoji.DollarBanknote,
                        "allears" or "nallears" => Emoji.Ear,
                        "argh" => Emoji.WearyFace,
                        "bangbang" => Emoji.DoubleExclamationMark,
                        "beer" or "beers" => Emoji.BeerMug,
                        "bike" => Emoji.Bicycle,
                        "biohazard_sign" => Emoji.Biohazard,
                        "blush" => Emoji.FlushedFace,
                        "boom" => Emoji.Collision,
                        "bulb" => Emoji.LightBulb,
                        "cake" => Emoji.BirthdayCake,
                        "champagne" => Emoji.BottleWithPoppingCork,
                        "chart_with_upwards_trend" => Emoji.ChartIncreasing,
                        "clap" => Emoji.ClappingHands,
                        "clock12" => Emoji.TwelveOclock,
                        "clock1230" => Emoji.TwelveThirty,
                        "clock1" => Emoji.OneOclock,
                        "clock130" => Emoji.OneThirty,
                        "clock2" => Emoji.TwoOclock,
                        "clock230" => Emoji.TwoThirty,
                        "clock3" => Emoji.ThreeOclock,
                        "clock330" => Emoji.ThreeThirty,
                        "clock4" => Emoji.FourOclock,
                        "clock430" => Emoji.FourThirty,
                        "clock5" => Emoji.FiveOclock,
                        "clock530" => Emoji.FiveThirty,
                        "clock6" => Emoji.SixOclock,
                        "clock630" => Emoji.SixThirty,
                        "clock7" => Emoji.SevenOclock,
                        "clock730" => Emoji.SevenThirty,
                        "clock8" => Emoji.EightOclock,
                        "clock830" => Emoji.EightThirty,
                        "clock9" => Emoji.NineOclock,
                        "clock930" => Emoji.NineThirty,
                        "clock10" => Emoji.TenOclock,
                        "clock1030" => Emoji.TenThirty,
                        "clock11" => Emoji.ElevenOclock,
                        "clock1130" => Emoji.ElevenThirty,
                        "cocktail" => Emoji.CocktailGlass,
                        "coffee" or "coffee2" => Emoji.HotBeverage,
                        "colbert" => Emoji.FaceWithRaisedEyebrow,
                        "cold_sweat" or "sweat" => Emoji.AnxiousFaceWithSweat,
                        "condi" => Emoji.Salt,
                        "confuoot" => Emoji.ConfusedFace,
                        "congratulations" => Emoji.JapaneseCongratulationsButton,
                        "cop" => Emoji.PoliceOfficer,
                        "cripes" => Emoji.AstonishedFace,
                        "cry" => Emoji.CryingFace,
                        "crying_cat_face" => Emoji.CryingCat,
                        "dagger_knife" => Emoji.Dagger,
                        "dancer" => Emoji.WomanDancing,
                        "dark_sunglasses" => Emoji.Sunglasses,
                        "dash" => Emoji.DashingAway,
                        "dizzy_face" => Emoji.FaceWithCrossedOutEyes,
                        "Drug" => Emoji.Pill,
                        "earth_asia" => Emoji.GlobeShowingAsiaAustralia,
                        "eyeglasses" => Emoji.Glasses,
                        "face_holding_back_tears" => Emoji.BeamingFaceWithSmilingEyes,
                        "face_palm" or "facepalm" or "faceplam" => Emoji.PersonFacepalming,
                        "fishing_pole_and_fish" => Emoji.FishingPole,
                        "facepunch" => Emoji.OncomingFist,
                        "flag-au" => Emoji.Flag_Australia,
                        "flag-in" => Emoji.FlagInHole,
                        "fries" => Emoji.FrenchFries,
                        "gift" => Emoji.WrappedGift,
                        "grey_question" or "question" => Emoji.WhiteQuestionMark,
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
                        "hourglass_flowing_sand" => Emoji.HourglassNotDone,
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
                        "lion_face" => Emoji.Lion,
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
                        "point_up" => Emoji.BackhandIndexPointingUp,
                        "point_up_2" => Emoji.BackhandIndexPointingUp,
                        "pray" => Emoji.FoldedHands,
                        "pray::skin-tone-1" => Emoji.FoldedHands_LightSkinTone,
                        "pray::skin-tone-2" => Emoji.FoldedHands_MediumLightSkinTone,
                        "pray::skin-tone-3" => Emoji.FoldedHands_MediumSkinTone,
                        "pray::skin-tone-4" => Emoji.FoldedHands_MediumDarkSkinTone,
                        "pray::skin-tone-5" => Emoji.FoldedHands_DarkSkinTone,
                        "radioactive_sign" => Emoji.Radioactive,
                        "racing_motorcycle" => Emoji.Motorcycle,
                        "rage" => Emoji.EnragedFace,
                        "rain_cloud" => Emoji.CloudWithRain,
                        "raised_hands" => Emoji.RaisedHand,
                        "raised_hands::skin-tone-1" => Emoji.RaisedHand_LightSkinTone,
                        "raised_hands::skin-tone-2" => Emoji.RaisedHand_MediumLightSkinTone,
                        "raised_hands::skin-tone-3" => Emoji.RaisedHand_MediumSkinTone,
                        "raised_hands::skin-tone-4" => Emoji.RaisedHand_MediumDarkSkinTone,
                        "raised_hands::skin-tone-5" => Emoji.RaisedHand_DarkSkinTone,
                        "relaxed" => Emoji.RelievedFace,
                        "reasons" => Emoji.ThinkingFace,
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
                        "smiling_face_with_3_hearts" => Emoji.SmilingFaceWithHearts,
                        "smiling_imp" => Emoji.SmilingFaceWithHorns,
                        "sob" => Emoji.LoudlyCryingFace,
                        "slick" => Emoji.SmilingFaceWithSunglasses,
                        "smile" or "simple_smile" or "smiley" => Emoji.SmilingFace,
                        "smirk" or "vee" => Emoji.SmirkingFace,
                        "smith" => Emoji.Hammer,
                        "space_invader" => Emoji.AlienMonster,
                        "spock-hand" => Emoji.VulcanSalute,
                        "ssh" => Emoji.ShushingFace,
                        "stare" => Emoji.FaceWithoutMouth,
                        "stew" => Emoji.PotOfFood,
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
                        "v" => Emoji.VictoryHand,
                        "v::skin-tone-1" => Emoji.VictoryHand_LightSkinTone,
                        "v::skin-tone-2" => Emoji.VictoryHand_MediumLightSkinTone,
                        "v::skin-tone-3" => Emoji.VictoryHand_MediumSkinTone,
                        "v::skin-tone-4" => Emoji.VictoryHand_MediumDarkSkinTone,
                        "v::skin-tone-5" => Emoji.VictoryHand_DarkSkinTone,
                        "wave" => Emoji.WavingHand,
                        "wave::skin-tone-1" => Emoji.WavingHand_LightSkinTone,
                        "wave::skin-tone-2" => Emoji.WavingHand_MediumLightSkinTone,
                        "wave::skin-tone-3" => Emoji.WavingHand_MediumSkinTone,
                        "wave::skin-tone-4" => Emoji.WavingHand_MediumDarkSkinTone,
                        "wave::skin-tone-5" => Emoji.WavingHand_DarkSkinTone,
                        "whale2" => Emoji.Whale,
                        "white_frowning_face" => Emoji.FrowningFaceWithOpenMouth,
                        "wink" => Emoji.WinkingFace,
                        "x" => Emoji.CrossMark,
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
