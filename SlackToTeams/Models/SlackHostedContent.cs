using Microsoft.Graph.Models;
using Newtonsoft.Json;
using SlackToTeams.Utils;

namespace SlackToTeams.Models {
    public class SlackHostedContent {
        #region Properties

        public string? Id { get; private set; }
        public byte[]? ContentBytes { get; private set; }
        public string? ContentType { get; private set; }

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackHostedContent(byte[] contentBytes, string contentType) {
            // Set Id Base36 string of a new GUID
            Id = ConvertHelper.GuidToBase36(Guid.NewGuid());
            ContentBytes = contentBytes;
            ContentType = contentType;
        }

        public SlackHostedContent(string id, byte[] contentBytes, string contentType) {
            Id = id;
            ContentBytes = contentBytes;
            ContentType = contentType;
        }

        #endregion
        #region Method - ToChatMessageReaction

        public ChatMessageHostedContent ToChatMessageHostedContent(int tempId) {
            ChatMessageHostedContent result = new ChatMessageHostedContent {
                ContentBytes = ContentBytes,
                ContentType = ContentType
            };
            result.AdditionalData.Add(
                "@microsoft.graph.temporaryId",
                tempId
            );
            return result;
        }

        #endregion
    }
}
