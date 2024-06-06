using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SlackToTeams.Models {
    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SlackTeam {
        #region Properties

        [DataMember(IsRequired = true, Name = "displayName"), JsonProperty]
        public string DisplayName { get; private set; }

        [DataMember(IsRequired = false, Name = "description"), JsonProperty]
        public string Description { get; private set; } = "";

        [DataMember(IsRequired = false, Name = "createdDateTime"),
            JsonProperty,
            JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreatedDateTime { get; private set; } = DateTime.UtcNow;

        [DataMember(IsRequired = false, Name = "@microsoft.graph.teamCreationMode"), JsonProperty]
        public string TeamCreationMode { get; private set; } = "migration";

        [DataMember(IsRequired = false, Name = "template@odata.bind"), JsonProperty]
        public string Template { get; private set; } = "https://graph.microsoft.com/v1.0/teamsTemplates('standard')";

        #endregion
        #region Constructors

        public SlackTeam(string displayName, string description, DateTime createdDateTime) {
            DisplayName = displayName;
            Description = description;
            CreatedDateTime = createdDateTime;
        }

        #endregion
    }
}
