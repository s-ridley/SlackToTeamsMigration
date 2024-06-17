using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Serilog;

namespace SlackToTeams.Utils {
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="httpClient">HttpClient used to call the protected API</param>
    public class ProtectedApiCallHelper(HttpClient httpClient) {
        #region Fields

        private static readonly ILogger s_logger = Log.ForContext(typeof(ProtectedApiCallHelper));

        #endregion
        #region Method - Properties

        protected HttpClient HTTPClient { get; private set; } = httpClient;

        #endregion
        #region Method - GetWebApiCall

        /// <summary>
        /// Calls the protected web API with a get async and returns the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        public async Task<JsonNode?> GetWebApiCall(string webApiUrl, string accessToken) {
            if (!string.IsNullOrEmpty(accessToken)) {
                var defaultRequestHeaders = HTTPClient.DefaultRequestHeaders;
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json")) {
                    HTTPClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await HTTPClient.GetAsync(webApiUrl);
                if (response.IsSuccessStatusCode) {
                    string json = await response.Content.ReadAsStringAsync();
                    JsonNode? result = JsonNode.Parse(json);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return result;
                } else {
                    s_logger.Error("Failed to call the web API - URL:{webApiUrl} StatusCode:{statusCode}", webApiUrl, response.StatusCode);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to call the web API: {response.StatusCode}");
                    string content = await response.Content.ReadAsStringAsync();

                    s_logger.Error("Called - URL:{webApiUrl} content:{content}", content);
                    // Note that if you got response.Code == 403 and response.content.code == "Authorization_RequestDenied"
                    // this is because the tenant admin as not granted consent for the application to call the Web API
                    Console.WriteLine($"Content: {content}");
                }
                Console.ResetColor();
            }

            return null;
        }

        #endregion
        #region Method - PostWebApiCall

        /// <summary>
        /// Calls the protected web API with a post async and returns the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="content">Content of the post call</param>
        public async Task<HttpResponseMessage?> PostWebApiCall(string webApiUrl, string accessToken, HttpContent content) {
            if (string.IsNullOrEmpty(accessToken)) {
                return null;
            }

            var defaultRequestHeaders = HTTPClient.DefaultRequestHeaders;
            if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json")) {
                HTTPClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await HTTPClient.PostAsync(webApiUrl, content);
            if (response.IsSuccessStatusCode) {
                return response;
            }

            s_logger.Error("Failed to call the web API - URL:{webApiUrl} StatusCode:{statusCode}", webApiUrl, response.StatusCode);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to call the web API: {response.StatusCode}");
            string responseContent = await response.Content.ReadAsStringAsync();

            s_logger.Error("Called - URL:{webApiUrl} content:{content}", responseContent);
            // Note that if you got response.Code == 403 and response.content.code == "Authorization_RequestDenied"
            // this is because the tenant admin as not granted consent for the application to call the Web API
            Console.WriteLine($"Content: {responseContent}");

            Console.ResetColor();
            return null;
        }

        #endregion
    }
}
