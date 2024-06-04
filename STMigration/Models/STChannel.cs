// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Runtime.Serialization;

namespace STMigration.Models {
    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class STChannel {
        #region Properties

        [DataMember(IsRequired = true, Name = "displayName"), JsonProperty]
        public string DisplayName { get; set; }

        [DataMember(IsRequired = false, Name = "description"), JsonProperty]
        public string Description { get; set; } = "";

        [DataMember(IsRequired = false, Name = "createdDateTime"),
            JsonProperty,
            JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        [DataMember(IsRequired = true, Name = "membershipType"), JsonProperty]
        public string MembershipType { get; set; } = "standard";

        public bool IsArchived { get; set; } = false;

        public string? SlackId { get; set; }

        public string? SlackCreatorId { get; set; }

        #endregion
        #region Constructors

        public STChannel(string displayName, string description, DateTime createdDateTime, bool isArchived, string? slackId, string? slackCreatorId) {
            DisplayName = displayName;

            if (string.Equals(DisplayName, "general", StringComparison.CurrentCultureIgnoreCase)) {
                DisplayName = "Old General";
            }

            TextInfo currentTextInfo = CultureInfo.CurrentCulture.TextInfo;

            DisplayName = currentTextInfo.ToTitleCase(DisplayName);

            Description = description;
            CreatedDateTime = createdDateTime;
            IsArchived = isArchived;
            SlackId = slackId;
            SlackCreatorId = slackCreatorId;
        }

        #endregion
    }
}
