// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Serilog;

namespace SlackToTeams.Models {
    public class SlackAttachment {
        #region Fields

        private static readonly ILogger Logger = Log.ForContext(typeof(SlackAttachment));

        #endregion
        #region Properties

        public string? Id { get; set; }
        public string? Channel { get; set; }
        public string? SlackURL { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Title { get; set; }
        public string? FileType { get; set; }
        public long? Size { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTimeOffset? Date { get; set; }
        public string? Content { get; private set; }
        public byte[]? ContentBytes { get; private set; }
        public string? MimeType { get; set; }
        public string? ContentURL { get; set; }
        public string? ThumbnailUrl { get; set; }

        #endregion
        #region Constructors

        public SlackAttachment(string channel, string? slackUrl, string? name, string? title, string? fileType, string? mimeType, long? size, DateTimeOffset? date) {
            Channel = channel;
            SlackURL = slackUrl;
            Name = name;
            Title = title;
            FileType = fileType;
            MimeType = mimeType;
            Size = size;
            Date = date;
            ContentURL = string.Empty;
            Id = string.Empty;

            FormatDisplayName();
        }

        #endregion
        #region Method - FormatDisplayName

        private string FormatDisplayName() {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(Name)) {
                string timeString = string.Empty;

                if (Date != null) {
                    DateTime dateTime = Date.Value.LocalDateTime;
                    timeString = $"{dateTime.Hour:D2}.{dateTime.Minute:D2}.{dateTime.Second:D2}";
                }

                if (!string.IsNullOrEmpty(timeString)) {
                    result = $"{timeString} {Name}";
                } else {
                    result = Name;
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
                    Logger.Debug("ToBase64 - Converting SlackURL [{SlackURL}] to Base64", SlackURL);
                    Console.WriteLine("Converting SlackURL \"{0}\" to Base64", SlackURL);
                    var response = await client.GetAsync($"{SlackURL}");
                    // Make sure the response is a success 
                    _ = response.EnsureSuccessStatusCode();
                    // Read the response content into a byte array
                    response.Content.ReadAsByteArrayAsync().Wait();
                    // Read out the response byte array
                    ContentBytes = response.Content.ReadAsByteArrayAsync().Result;
                    // Convert the byte array to a base64 string
                    //Base64 = Convert.ToBase64String(slackBytes);
                    Logger.Debug("ToBase64 - Successfully SlackURL [{SlackURL}] to to Base64", SlackURL);
                    Console.WriteLine("Successfully converted to Base64");
                } catch (System.Net.WebException ex) {
                    Logger.Error(ex, "ToBase64 - Error Converting SlackURL [{SlackURL}] to Base64 error:{errorMessage}", SlackURL, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error unable to download :{SlackURL}");
                    Console.WriteLine(ex);
                    Console.ResetColor();
                    throw;
                } finally {
                    client?.Dispose();
                }
            }
        }

        #endregion
        #region Method - DownloadFile

        public async Task DownloadFile(string baseDownloadPath, bool overwriteFile) {
            string fullFilePath = $"{baseDownloadPath}/files/{Channel}/{Name}";

            if (
                !string.IsNullOrWhiteSpace(SlackURL) &&
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
                    Logger.Debug("DownloadFile - Downloading SlackURL [{SlackURL}] to [{fullFilePath}]", SlackURL, fullFilePath);
                    Console.WriteLine("Downloading SlackURL \"{0}\" to \"{1}\" .......\n\n", SlackURL, fullFilePath);
                    var response = await client.GetAsync($"{SlackURL}");
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
                    Logger.Debug("DownloadFile - Successfully Downloaded SlackURL [{SlackURL}] to [{fullFilePath}]", SlackURL, fullFilePath);
                    Console.WriteLine("Successfully Downloaded SlackURL \"{0}\" to \"{1}\"", SlackURL, fullFilePath);
                } catch (System.Net.WebException ex) {
                    Logger.Error(ex, "DownloadFile - Error downloading SlackURL [{SlackURL}] to [{fullFilePath}] error:{errorMessage}", SlackURL, fullFilePath, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error unable to download :{SlackURL}");
                    Console.WriteLine(ex);
                    Console.ResetColor();
                    throw;
                } finally {
                    client?.Dispose();
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
