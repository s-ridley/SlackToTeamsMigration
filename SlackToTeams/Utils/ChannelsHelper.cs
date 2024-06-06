using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public class ChannelsHelper {
        #region Method - ScanChannelsFromSlack

        public static List<SlackChannel> ScanChannelsFromSlack(string combinedPath) {
            List<SlackChannel> channelList = [];

            using (FileStream fs = new(combinedPath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new(fs))
            using (JsonTextReader reader = new(sr)) {
                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        JObject obj = JObject.Load(reader);

                        // SelectToken returns null not an empty string if nothing is found
                        string? slackId = obj.SelectToken("id")?.ToString();
                        string? slackCreatorId = obj.SelectToken("creator")?.ToString();
                        string? displayName = obj.SelectToken("name")?.ToString();
                        string? description = obj.SelectToken("purpose.value")?.ToString();
                        string? createdDateTimeUnixTick = obj.SelectToken("created")?.ToString();
                        _ = bool.TryParse(obj.SelectToken("is_archived")?.ToString(), out bool isArchived);

                        DateTime createdDateTime = DateTime.UtcNow;

                        if (long.TryParse(createdDateTimeUnixTick, out long ticks)) {
                            createdDateTime = DateTimeOffset.FromUnixTimeSeconds(ticks).DateTime;
                        }

                        if (string.IsNullOrEmpty(displayName)) {
                            continue;
                        }

                        if (string.IsNullOrEmpty(description)) {
                            description = "";
                        }

                        SlackChannel channel = new(displayName, description, createdDateTime, isArchived, slackId, slackCreatorId);

                        channelList.Add(channel);
                    }
                }
            }
            return channelList;
        }

        #endregion
    }
}
