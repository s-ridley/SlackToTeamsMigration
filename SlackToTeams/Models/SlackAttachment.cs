// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Graph.Models;
using Serilog;
using SlackToTeams.Utils;

namespace SlackToTeams.Models {
    public partial class SlackAttachment(
        string? slackUrl,
        string? name,
        string? title,
        string? fileType,
        string? mimeType,
        long? size,
        DateTimeOffset? date
    ) {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(SlackAttachment));

        #endregion
        #region Properties

        public string? Id { get; set; } = string.Empty;
        public string? SlackURL { get; set; } = slackUrl;
        public string? Name { get; set; } = FormatDisplayName(ConvertHelper.FileSystemSafe(name), date);
        public string? DisplayName { get; set; }
        public string? Title { get; set; } = title;
        public string? FileType { get; set; } = fileType;
        public long? Size { get; set; } = size;
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTimeOffset? Date { get; set; } = date;
        public string? Content { get; private set; }
        public byte[]? ContentBytes { get; private set; }
        public string? MimeType { get; set; } = mimeType;
        public string? ContentURL { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public bool FileMissing { get; private set; } = false;

        #endregion
        #region Method - FormatDisplayName

        private static string FormatDisplayName(string? name, DateTimeOffset? date) {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(name)) {
                string timeString = string.Empty;

                if (date != null) {
                    DateTime dateTime = date.Value.LocalDateTime;
                    timeString = $"{dateTime:s}";

                }

                if (!string.IsNullOrEmpty(timeString)) {
                    timeString = timeString.Replace(":", "_");
                    timeString = timeString.Replace("-", "_");
                    result = $"{timeString}_{name}";
                } else {
                    result = name;
                }
            }

            return result;
        }

        #endregion
        #region Method - DownloadBytes

        public async Task DownloadBytes() {
            if (!string.IsNullOrWhiteSpace(SlackURL)) {
                HttpClient? client = null;
                try {
                    client = new();
                    s_logger.Debug("ToBase64 - Converting SlackURL [{SlackURL}] to Base64", SlackURL);
                    Console.WriteLine("Converting \"{0}\" from Slack to Base64", Name);
                    var response = await client.GetAsync($"{SlackURL}");
                    // Check if the file still exsits
                    FileMissing = (response.StatusCode == HttpStatusCode.NotFound);
                    // Only proceed if the response was not 404
                    if (!FileMissing) {
                        // Make sure the response is a success 
                        _ = response.EnsureSuccessStatusCode();
                        // Read the response content into a byte array
                        response.Content.ReadAsByteArrayAsync().Wait();
                        // Read out the response byte array
                        ContentBytes = response.Content.ReadAsByteArrayAsync().Result;
                        // Convert the byte array to a base64 string
                        //Base64 = Convert.ToBase64String(slackBytes);
                        s_logger.Debug("ToBase64 - Successfully SlackURL [{SlackURL}] to to Base64", SlackURL);
                        Console.WriteLine("Successfully converted to Base64");
                    } else {
                        s_logger.Debug("ToBase64 - Downloaded failed SlackURL [{SlackURL}] response code [{responseCode}]", SlackURL, response.StatusCode);
                        Console.WriteLine("Failed to convert to Base64 response code \"{0}\"", response.StatusCode);
                    }
                } catch (Exception ex) {
                    s_logger.Error(ex, "ToBase64 - Error Converting SlackURL [{SlackURL}] to Base64 error:{errorMessage}", SlackURL, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error unable to download :{SlackURL}");
                    Console.WriteLine(ex);
                    Console.ResetColor();
                } finally {
                    client?.Dispose();
                }
            }
        }

        #endregion
        #region Method - DownloadFile

        public async Task DownloadFile(string downloadFolder, bool overwriteFile) {
            if (!string.IsNullOrWhiteSpace(downloadFolder)) {
                // Create the download folder
                Directory.CreateDirectory(downloadFolder);

                // Confirm the path exists
                if (
                    Path.Exists(downloadFolder) &&
                    !string.IsNullOrWhiteSpace(Name) &&
                    !string.IsNullOrWhiteSpace(SlackURL)
                ) {
                    string fullFilePath = $"{downloadFolder}/{Name}";
                    if (
                        (
                            (
                                File.Exists(fullFilePath) &&
                                overwriteFile
                            ) ||
                            !File.Exists(fullFilePath)
                        )
                    ) {
                        HttpClient? client = null;
                        try {
                            client = new();
                            s_logger.Debug("DownloadFile - Downloading SlackURL [{SlackURL}] to [{fullFilePath}]", SlackURL, fullFilePath);
                            Console.WriteLine("Downloading \"{0}\" to \"{1}\"", Name, downloadFolder);
                            var response = await client.GetAsync($"{SlackURL}");
                            // Check if the file still exsits
                            FileMissing = (response.StatusCode == HttpStatusCode.NotFound);
                            // Only proceed if the response was not 404
                            if (!FileMissing) {
                                // Make sure the response is a success 
                                _ = response.EnsureSuccessStatusCode();
                                // Read the response content into a Stream
                                await using var slackStream = await response.Content.ReadAsStreamAsync();
                                // Return the stream to the start
                                _ = slackStream.Seek(0, SeekOrigin.Begin);
                                // Create the FileStream
                                using var fileStream = new FileStream(fullFilePath, FileMode.Create);
                                // Copy slackFileStream to fileStream
                                await slackStream.CopyToAsync(fileStream);
                                s_logger.Debug("DownloadFile - Successfully Downloaded SlackURL [{SlackURL}] to [{fullFilePath}]", SlackURL, downloadFolder);
                                Console.WriteLine("Successfully Downloaded \"{0}\" to \"{1}\"", Name, downloadFolder);
                            } else {
                                s_logger.Debug("DownloadFile - Downloaded failed SlackURL [{SlackURL}] response code [{responseCode}]", SlackURL, response.StatusCode);
                                Console.WriteLine("Downloaded failed \"{0}\" response code \"{1}\"", Name, response.StatusCode);
                            }
                        } catch (Exception ex) {
                            s_logger.Error(ex, "DownloadFile - Error downloading SlackURL [{SlackURL}] to [{fullFilePath}] error:{errorMessage}", SlackURL, fullFilePath, ex.Message);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error unable to download :{SlackURL}");
                            Console.WriteLine(ex);
                            Console.ResetColor();
                        } finally {
                            client?.Dispose();
                        }
                    }
                } else {
                    s_logger.Error("Download folder does not exist - folder:{downloadFolder}", downloadFolder);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Download folder does not exist :{downloadFolder}");
                    Console.ResetColor();
                }
            }
        }

        #endregion
        #region Method - ToChatMessageAttachment

        public ChatMessageAttachment ToChatMessageAttachment() {
            ChatMessageAttachment result = new() {
                Id = Id,
                Content = Content,
                ContentType = "reference",
                ContentUrl = ContentURL,
                Name = Name,
                ThumbnailUrl = ThumbnailUrl
            };
            return result;
        }

        #endregion
    }
}
