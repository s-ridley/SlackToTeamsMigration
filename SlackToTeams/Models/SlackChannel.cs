// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json.Serialization;

namespace SlackToTeams.Models {
    [method: JsonConstructor]
    public class SlackChannel(
        string? displayName,
        string? description,
        DateTime? createdDateTime,
        bool? isArchived,
        string? slackId,
        string? slackCreatorId
    ) {
        #region Properties

        public string? DisplayName { get; set; } = FormatDisplayName(displayName);
        public string? Description { get; set; } = description;
        public DateTime? CreatedDateTime { get; set; } = createdDateTime;
        public string? MembershipType { get; set; } = "standard";
        public bool? IsArchived { get; set; } = isArchived;
        public string? SlackId { get; set; } = slackId;
        public string? SlackCreatorId { get; set; } = slackCreatorId;
        public string? SlackFolder { get; set; } = displayName;

        #endregion
        #region Method - FormatDisplayName

        private static string FormatDisplayName(string? name) {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(name)) {
                result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            }
            return result;
        }

        #endregion
    }
}
