﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STMigration.Models;

namespace STMigration.Utils {
    public class ChannelsHelper {
        #region Method - ScanChannelsFromSlack

        public static List<STChannel> ScanChannelsFromSlack(string combinedPath) {
            List<STChannel> channelList = [];

            using (FileStream fs = new(combinedPath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new(fs))
            using (JsonTextReader reader = new(sr)) {
                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        JObject obj = JObject.Load(reader);

                        // SelectToken returns null not an empty string if nothing is found
                        string? displayName = obj.SelectToken("name")?.ToString();
                        string? description = obj.SelectToken("purpose.value")?.ToString();
                        string? createdDateTimeUnixTick = obj.SelectToken("created")?.ToString();

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

                        STChannel channel = new(displayName, description, createdDateTime);

                        channelList.Add(channel);
                    }
                }
            }
            return channelList;
        }

        #endregion
    }
}
