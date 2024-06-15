// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

namespace SlackToTeams.Models {
    public class SlackAttachment {
        #region Properties

        public string? Channel { get; set; }
        public string? SlackURL { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Title { get; set; }
        public string? FileType { get; set; }
        public string? MimeType { get; set; }
        public long? Size { get; set; }
        public DateTimeOffset? Date { get; set; }
        public string? TeamsURL { get; set; }
        public string? TeamsGUID { get; set; }
        public string? Base64 { get; set; }

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
            TeamsURL = string.Empty;
            TeamsGUID = string.Empty;

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
        #region Method - ToBase64

        public async Task ToBase64() {
            if (!string.IsNullOrWhiteSpace(SlackURL)) {
                HttpClient? client = null;
                try {
                    client = new();
                    Console.WriteLine("Converting SlackURL \"{0}\" to Base64File", SlackURL);
                    var response = await client.GetAsync($"{SlackURL}");
                    // Make sure the response is a success 
                    _ = response.EnsureSuccessStatusCode();
                    // Read the response content into a Stream
                    await using var slackStream = await response.Content.ReadAsStreamAsync();
                    // Return the stream to the start
                    _ = slackStream.Seek(0, SeekOrigin.Begin);
                    // Read the stream into a byte array
                    byte[] slackBytes;
                    using (var reader = new StreamReader(slackStream)) {
                        slackBytes = System.Text.Encoding.UTF8.GetBytes(reader.ReadToEnd());
                    }
                    // Convert the byte array to a base64 string
                    Base64 = Convert.ToBase64String(slackBytes);
                    Console.WriteLine("Successfully converted to Base64");
                } catch (System.Net.WebException ex) {
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
                    Console.WriteLine("Downloading File \"{0}\" from \"{1}\" .......\n\n", fullFilePath, SlackURL);
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
                    Console.WriteLine("Successfully Downloaded File \"{0}\" from \"{1}\"", fullFilePath, SlackURL);
                } catch (System.Net.WebException ex) {
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
    }
}
