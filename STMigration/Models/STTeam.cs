using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace STMigration.Models {
    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class STTeam {
        #region Properties

        [DataMember(IsRequired = true, Name = "displayName"), JsonProperty]
        public string DisplayName { get; private set; }

        [DataMember(IsRequired = false, Name = "description"), JsonProperty]
        public string? Description { get; private set; }

        [DataMember(IsRequired = false, Name = "createdDateTime"),
            JsonProperty,
            JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime? CreatedDateTime { get; private set; }

        #endregion
        #region Constructors

        public STTeam(string displayName, string? description, DateTime? createdDateTime) {
            DisplayName = displayName;
            Description = description;
            CreatedDateTime = createdDateTime;
        }

        #endregion
    }
}
