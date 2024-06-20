using System.Text;
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

        public static void StartHtml(string htmlFolder, string exportPrefix) {
            if (!string.IsNullOrWhiteSpace(htmlFolder)) {
                // Create the html export folder
                Directory.CreateDirectory(htmlFolder);

                // Confirm the path exists
                if (Path.Exists(htmlFolder)) {
                    string htmlFilePath = $"{htmlFolder}/{exportPrefix}_{EXPORT_FILE}";
                    if (
                        !string.IsNullOrEmpty(htmlFilePath) &&
                        !File.Exists(htmlFilePath)
                    ) {
                        File.WriteAllText(htmlFilePath, @"<!DOCTYPE html>
<html>
<head>
<style>
html {
    font-family: Arial, Helvetica, sans-serif;
}
blockquote {
    margin: 0;
    padding: 5px;
    background: #eee;
    border-radius: 5px;
    border-style: solid ;
    border-width: 1px;
}
hr {
    border-top: 1px solid #c0c0c0;
}
#user_id {
    font-weight: bold;
}
#epoch_time {
    font-weight: lighter;
}
#message_text {
    font-weight: normal;
    white-space: pre-wrap;
}
</style>
</head>
<body>");

                        s_logger.Debug($"Started HTML export file:{htmlFilePath}");
                    }
                }
            }
        }

        #endregion
        #region Method - MessageToHtml

        public static void MessageToHtml(string htmlFolder, string exportPrefix, SlackMessage? message) {
            if (Path.Exists(htmlFolder)) {
                string htmlFilePath = $"{htmlFolder}/{exportPrefix}_{EXPORT_FILE}";
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
                    StringBuilder htmlBody = new();
                    if (!string.IsNullOrWhiteSpace(message.Text)) {
                        htmlBody.Append(message.Text.Trim());
                    }
                    if (
                        message.Attachments != null &&
                        message.Attachments.Count > 0
                    ) {
                        htmlBody.Append(message.HtmlAttachments());
                    }
                    if (
                        message.HostedContents != null &&
                        message.HostedContents.Count > 0
                    ) {
                        htmlBody.Append(message.HtmlHostedContents());
                    }
                    if (
                        message.Reactions != null &&
                        message.Reactions.Count > 0
                    ) {
                        htmlBody.Append(message.HtmlReactions());
                    }

                    htmlBody = htmlBody.Replace(Environment.NewLine, "<br>");

                    File.AppendAllText(htmlFilePath, $@"
<div>
<span id='user_id'>{message.User?.DisplayName}</span>&nbsp;<span id='epoch_time'>{ConvertHelper.SlackTimestampToDateTime(message.Date):G}</span>
<div id='message_text'>{htmlBody}</div>
</div>
<hr>");
                }
            }
        }

        #endregion
        #region Method - EndHtml

        public static void EndHtml(string htmlFolder, string exportPrefix) {
            if (Path.Exists(htmlFolder)) {
                string htmlFilePath = $"{htmlFolder}/{exportPrefix}_{EXPORT_FILE}";
                if (
                    !string.IsNullOrEmpty(htmlFilePath) &&
                    File.Exists(htmlFilePath)
                ) {
                    File.AppendAllText(htmlFilePath, @"
</body>
</html>");

                    s_logger.Debug($"Started HTML export file:{htmlFilePath}");
                }
            }
        }

        #endregion
    }
}
