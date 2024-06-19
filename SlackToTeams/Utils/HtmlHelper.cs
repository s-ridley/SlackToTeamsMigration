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
                        File.AppendAllText(htmlFilePath, $"<head>{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"<style>{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"html {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    font-family: Arial, Helvetica, sans-serif;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"blockquote {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    margin: 0;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    padding: 5px;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    background: #eee;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    border-radius: 5px;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    border-style: solid ;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    border-width: 1px;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"hr {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    border-top: 1px solid #c0c0c0;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"#user_id {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    font-weight: bold;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"#epoch_time {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    font-weight: lighter;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"#message_text {{{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    font-weight: normal;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"    white-space: pre-wrap;{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"}}{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"</style>{Environment.NewLine}");
                        File.AppendAllText(htmlFilePath, $"</head>{Environment.NewLine}");
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
                    File.AppendAllText(htmlFilePath, $"<span id=\"user_id\">{message.User?.DisplayName}</span>");
                    File.AppendAllText(htmlFilePath, $"&nbsp;");
                    File.AppendAllText(htmlFilePath, $"<span id=\"epoch_time\">{ConvertHelper.SlackTimestampToDateTimeOffset(message.Date).DateTime:G}</span>{Environment.NewLine}");
                    File.AppendAllText(htmlFilePath, $"<div id=\"message_text\">{htmlBody}</div>{Environment.NewLine}");
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
