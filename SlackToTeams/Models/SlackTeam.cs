using System.Text.Json.Serialization;

namespace SlackToTeams.Models {
    [method: JsonConstructor]
    public class SlackTeam(
        string displayName,
        string description,
        DateTime createdDateTime
    ) {
        #region Properties

        public string DisplayName { get; private set; } = displayName;
        public string Description { get; private set; } = description;
        public DateTime CreatedDateTime { get; private set; } = createdDateTime;
        public string TeamCreationMode { get; private set; } = "migration";
        public string Template { get; private set; } = "https://graph.microsoft.com/v1.0/teamsTemplates('standard')";
        public string? TeamId { get; set; }

        #endregion
    }
}
