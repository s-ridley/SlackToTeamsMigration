using System.Numerics;
using System.Text;

namespace SlackToTeams.Utils {
    public class ConvertHelper {
        #region Constants

        private const string Base36CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

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
        #region Method - GuidToBase36

        public static string GuidToBase36(Guid guid) {
            // Convert the GUID to a byte array
            byte[] guidBytes = guid.ToByteArray();

            // Create a new byte array with an extra byte for the sign bit
            byte[] bytes = new byte[guidBytes.Length + 1];

            // Copy the GUID bytes into the new array
            Array.Copy(guidBytes, bytes, guidBytes.Length);

            // Ensure the BigInteger is a positive number by setting the extra byte to 0
            bytes[^1] = 0;

            // Convert the byte array to a BigInteger
            BigInteger bigInt = new(bytes);

            StringBuilder result = new();
            // Convert the BigInteger to a base36 string
            do {
                result.Insert(0, Base36CharList[(int)(bigInt % 36)]);
                bigInt /= 36;
            }
            while (bigInt != 0);

            return result.ToString();
        }

        #endregion
    }
}
