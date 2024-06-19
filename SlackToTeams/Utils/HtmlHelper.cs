using Serilog;
using SlackToTeams.Models;

namespace SlackToTeams.Utils {
    public class HtmlHelper {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(HtmlHelper));

        #endregion
        #region Constants

        public const string EXPORT_FILE = "export.html";

        #endregion
        #region Method - StartHtml

        public static void StartHtml(string htmlFolder) {
            if (!string.IsNullOrWhiteSpace(htmlFolder)) {
                // Create the html export folder
                Directory.CreateDirectory(htmlFolder);

                // Confirm the path exists
                if (Path.Exists(htmlFolder)) {
                    string htmlFilePath = $"{htmlFolder}/{EXPORT_FILE}";
                    if (
                        !string.IsNullOrEmpty(htmlFilePath) &&
                        !File.Exists(htmlFilePath)
                    ) {
                        File.WriteAllText(htmlFilePath, $"<!DOCTYPE html>{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"<html>{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"<body>{Environment.NewLine}");

                        s_logger.Debug($"Started HTML export file:{htmlFilePath}");
                    }
                }
            }
        }

        #endregion
        #region Method - MessageToHtml

        public static void MessageToHtml(string htmlFolder, SlackMessage? message) {
            if (Path.Exists(htmlFolder)) {
                string htmlFilePath = $"{htmlFolder}/{EXPORT_FILE}";
                if (
                    !string.IsNullOrEmpty(htmlFilePath) &&
                    File.Exists(htmlFilePath) &&
                    message != null &&
                    (
                        (
                            message.Text != null &&
                            !string.IsNullOrWhiteSpace(message.Text)
                        ) ||
                        (
                            message.Attachments != null &&
                            message.Attachments.Count > 0
                        ) ||
                        (
                            message.Reactions != null &&
                            message.Reactions.Count > 0
                        ) ||
                        (
                            message.HostedContents != null &&
                            message.HostedContents.Count > 0
                        )
                    )
                ) {
                    string htmlBody = "";
                    if (!string.IsNullOrWhiteSpace(message.Text)) {
                        htmlBody += message.Text.Trim();
                    }
                    if (
                        message.Attachments != null &&
                        message.Attachments.Count > 0
                    ) {
                        htmlBody += message.HtmlAttachments();
                    }
                    if (
                        message.HostedContents != null &&
                        message.HostedContents.Count > 0
                    ) {
                        htmlBody += message.HtmlHostedContents();
                    }
                    if (
                        message.Reactions != null &&
                        message.Reactions.Count > 0
                    ) {
                        htmlBody += message.HtmlReactions();
                    }

                    htmlBody = htmlBody.Replace(Environment.NewLine, "<br>");

                    File.AppendAllText(htmlFilePath, $"<div>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"<span id=\"user_id\" style=\"font-weight:bold;\">{message.User?.DisplayName}</span>");
                    File.AppendAllText(htmlFilePath, $"&nbsp;");
                    File.AppendAllText(htmlFilePath, $"<span id=\"epoch_time\" style=\"font-weight:lighter;\">{ConvertHelper.SlackTimestampToDateTimeOffset(message.Date).DateTime:G}</span>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"<div id=\"message_text\" style=\"font-weight:normal;white-space:pre-wrap;\">{htmlBody}</div>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"</div>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"<hr>{Environment.NewLine}");
                }
            }
        }

        #endregion
        #region Method - EndHtml

        public static void EndHtml(string htmlFolder) {
            if (Path.Exists(htmlFolder)) {
                string htmlFilePath = $"{htmlFolder}/{EXPORT_FILE}";
                if (
                    !string.IsNullOrEmpty(htmlFilePath) &&
                    File.Exists(htmlFilePath)
                ) {
                    File.AppendAllText(htmlFilePath, $"</html>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"</body>{Environment.NewLine}");

                    s_logger.Debug($"Started HTML export file:{htmlFilePath}");
                }
            }
        }

        #endregion
    }
}
