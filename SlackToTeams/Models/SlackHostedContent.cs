using Microsoft.Graph.Models;

namespace SlackToTeams.Models {
    public class SlackHostedContent(byte[] contentBytes, string? contentType, int? height, int? width) {
        #region Properties

        public byte[]? ContentBytes { get; private set; } = contentBytes;
        public string? ContentType { get; private set; } = contentType;
        public int? Width { get; set; } = width;
        public int? Height { get; set; } = height;

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
