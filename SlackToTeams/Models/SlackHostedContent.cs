using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace SlackToTeams.Models {
    public class SlackHostedContent {
        #region Properties

        public byte[]? ContentBytes { get; private set; }
        public string? ContentType { get; private set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        #endregion
        #region Constructors

        [JsonConstructor]
        public SlackHostedContent(byte[] contentBytes, string? contentType, int? height, int? width) {
            ContentBytes = contentBytes;
            ContentType = contentType;
            Height = height;
            Width = width;
        }

        #endregion
        #region Method - ToChatMessageReaction

        public ChatMessageHostedContent ToChatMessageHostedContent(int tempId) {
            ChatMessageHostedContent result = new() {
                ContentBytes = ContentBytes,
                ContentType = ContentType
            };
            result.AdditionalData.Add(
                "@microsoft.graph.temporaryId",
                tempId.ToString()
            );
            return result;
        }

        #endregion
    }
}
