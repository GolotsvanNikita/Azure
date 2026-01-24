using AzureP33.Models;
using AzureP33.Models.Cosmos;
using AzureP33.Models.Home;
using AzureP33.Models.ORM;
using AzureP33.Services.CosmosDB;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AzureP33.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private LanguagesResponse? languagesResponse;
        private readonly ICosmosDbService _cosmosDbService;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ICosmosDbService cosmosDbService)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosDbService = cosmosDbService;
        }

        private async Task<LanguagesResponse> GetLanguagesAsync() 
        {
            if (languagesResponse == null) 
            {
                using HttpClient client = new();

                languagesResponse = JsonSerializer.Deserialize<LanguagesResponse>(
                    await client.GetStringAsync(@"https://api.translator.azure.cn/languages?api-version=3.0")
                );


                if (languagesResponse == null)
                {
                    throw new Exception("LanguagesResponse got NULL result");
                }
            }
            return languagesResponse;
        }

        public async Task<IActionResult> IndexAsync(HomeIndexFormModel? formModel)
        {
            Task<LanguagesResponse> responseTask = GetLanguagesAsync();
            var response = await responseTask;

            HomeIndexViewModel viewModel = new()
            {
                PageTitle = "Translation",
                FormModel = formModel,
                LanguagesResponse = response
            };

            string defaultLang = "en";
            if (string.IsNullOrEmpty(formModel.LangFrom)) formModel.LangFrom = defaultLang;

            if (formModel.Action == "replace")
            {
                (formModel.LangFrom, formModel.LangTo) = (formModel.LangTo, formModel.LangFrom);
                if (!string.IsNullOrEmpty(formModel.TranslatedText))
                {
                    formModel.OriginalText = formModel.TranslatedText;
                    formModel.TranslatedText = null;
                    formModel.Action = "translate";
                }
            }

            if (formModel?.Action == "translate")
            {
                if (!string.IsNullOrWhiteSpace(formModel.OriginalText) && formModel.OriginalText.Trim().Length >= 2)
                {
                    try
                    {
                        string query = $"from={formModel.LangFrom}&to={formModel.LangTo}";
                        object[] body = new object[] { new { Text = formModel.OriginalText } };
                        var requestBody = System.Text.Json.JsonSerializer.Serialize(body);

                        string result = await RequestApi(query, requestBody, ApiMode.Translate);

                        if (result.TrimStart().StartsWith("["))
                        {
                            viewModel.Items = JsonSerializer.Deserialize<List<TranslatorResponseItem>>(result);

                            if (response.Translatirations.TryGetValue(formModel.LangFrom, out var sourceLangData))
                            {
                                string fromScript = sourceLangData.Scripts![0].Code!;
                                string toScript = sourceLangData.Scripts![0].ToScripts![0].Code!;
                                string tQuery = $"language={formModel.LangFrom}&fromScript={fromScript}&toScript={toScript}";
                                var tBody = System.Text.Json.JsonSerializer.Serialize(new object[] { new { Text = formModel.OriginalText } });

                                string tResult = await RequestApi(tQuery, tBody, ApiMode.Transliterate);
                                var tItems = System.Text.Json.JsonSerializer.Deserialize<List<TransliterationResponseItem>>(tResult);
                                if (tItems?.Count > 0) viewModel.FromTransliteration = tItems[0];
                            }

                            if (viewModel.Items?.Count > 0 && response.Translatirations.TryGetValue(formModel.LangTo, out var targetLangData))
                            {
                                string translatedText = viewModel.Items[0].Translations[0].Text;
                                string fromScript = targetLangData.Scripts![0].Code!;
                                string toScript = targetLangData.Scripts![0].ToScripts![0].Code!;
                                string tQuery = $"language={formModel.LangTo}&fromScript={fromScript}&toScript={toScript}";
                                var tBody = System.Text.Json.JsonSerializer.Serialize(new object[] { new { Text = translatedText } });

                                string tResult = await RequestApi(tQuery, tBody, ApiMode.Transliterate);
                                var tItems = System.Text.Json.JsonSerializer.Deserialize<List<TransliterationResponseItem>>(tResult);
                                if (tItems?.Count > 0) viewModel.ToTransliteration = tItems[0];
                            }

                            try
                            {
                                var historyItem = new TranslationHistory
                                {
                                    UserId = "Anonim",
                                    OriginalText = formModel.OriginalText,
                                    FromLang = formModel.LangFrom,
                                    TranslatedText = viewModel.Items[0].Translations[0].Text,
                                    ToLang = formModel.LangTo,
                                    CreatedAt = DateTime.UtcNow,

                                    FromTransliteration = viewModel.FromTransliteration,
                                    ToTransliteration = viewModel.ToTransliteration,
                                    Type = "Translation",
                                    Category = "History"
                                };

                                Container container = await _cosmosDbService.GetContainerAsync();
                                await container.CreateItemAsync(historyItem, new PartitionKey(historyItem.Category));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"DB Save Error: {ex.Message}");
                            }
                        }
                        else
                        {
                            viewModel.ErrorResponse = System.Text.Json.JsonSerializer.Deserialize<TranslatorErrorResponse>(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        viewModel.ErrorMessage = "Translation failed: " + ex.Message;
                    }
                }
                else
                {
                    viewModel.ErrorMessage = "Text must be at least 2 characters long.";
                }
            }

            return View(viewModel);
        }

        public async Task<IActionResult> HistoryAsync()
        {
            var viewModel = new HomeIndexViewModel();

            try
            {
                Container container = await _cosmosDbService.GetContainerAsync();

                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.Type = 'Translation' ORDER BY c.CreatedAt DESC"
                );

                using (FeedIterator<TranslationHistory> feed = container.GetItemQueryIterator<TranslationHistory>(query))
                {
                    while (feed.HasMoreResults)
                    {
                        viewModel.History.AddRange(await feed.ReadNextAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = "Error history load: " + ex.Message;
            }

            return View(viewModel);
        }

        private async Task<String> RequestTranslationAsync(HomeIndexFormModel formModel) 
        {
            string query = $"from={formModel.LangFrom}&to={formModel.LangTo}";
            object[] body = new object[] { new { Text = formModel.OriginalText } };
            var requestBody = JsonSerializer.Serialize(body);

            string result = await RequestApi(query, requestBody, ApiMode.Translate);
            if (result[0] == '[')
            {
                return JsonSerializer.Deserialize<List<TranslatorResponseItem>>(result)![0].Translations[0].Text;
            }
            else
            {
                throw new Exception(JsonSerializer.Deserialize<TranslatorErrorResponse>(result)!.Error.Message);
            }
        }

        private async Task<String> RequestApi(String query, String body, ApiMode apiMode) 
        {
            var sec = _configuration.GetSection("Azure").GetSection("Translator") ?? throw new Exception("Configuration error: Azure.Translator is null");

            if (sec == null) throw new Exception("Configuration error");

            string key = sec.GetValue<string>("Key") ?? throw new Exception("Configuration error: 'Key' is null");
            string endpoint = sec.GetValue<string>("Endpoint") ?? throw new Exception("Configuration error: 'Endpoint' is null");
            string location = sec.GetValue<string>("Location") ?? throw new Exception("Configuration error: 'Location' is null");
            string apiVersion = sec.GetValue<string>("ApiVersion") ?? throw new Exception("Configuration error: 'ApiVersion' is null");

            string apiPath = apiMode switch
            {
                ApiMode.Translate => sec.GetValue<string>("TranslatorPath"),
                ApiMode.Transliterate => sec.GetValue<string>("TransliteratorPath"),
                _ => null
            } ?? throw new Exception("Configuration error: 'apiPath' is null");

            using (var client2 = new HttpClient())
            using (var request = new HttpRequestMessage()) 
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri($"{endpoint}{apiPath}?api-version={apiVersion}&{query}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                HttpResponseMessage translationResponse = await client2.SendAsync(request).ConfigureAwait(false);
                string jsonResult = await translationResponse.Content.ReadAsStringAsync();
                return jsonResult;
            }
        }

        [HttpGet]
        public async Task<JsonResult> FetchTranslationAsync(HomeIndexFormModel formModel)
        {
            string serviceUnavailableMessage = "Translate service is not working, try later.";

            try
            {
                var responseLang = await GetLanguagesAsync();
                if (responseLang == null || formModel.Action != "fetch" || string.IsNullOrEmpty(formModel.OriginalText))
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return Json(serviceUnavailableMessage);
                }

                string translatedText = await RequestTranslationAsync(formModel);
                string originalText = formModel.OriginalText.Trim();

                try
                {
                    var historyItem = new TranslationHistory
                    {
                        UserId = "Anonim",
                        OriginalText = originalText,
                        FromLang = formModel.LangFrom,
                        TranslatedText = translatedText,
                        ToLang = formModel.LangTo,
                        CreatedAt = DateTime.UtcNow,

                        FromTransliteration = null,
                        ToTransliteration = null,

                        Type = "Translation",
                        Category = "History"
                    };

                    Container container = await _cosmosDbService.GetContainerAsync();

                    await container.CreateItemAsync(historyItem, new PartitionKey(historyItem.Category));
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"DB Save Error (Selection): {dbEx.Message}");
                }

                string separator = originalText.Length > 300 ? "\n" : " - ";
                string result = $"{originalText}{separator}{translatedText}";

                return Json(result);
            }
            catch (Exception)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(serviceUnavailableMessage);
            }
        }

        public async Task<IActionResult> CosmosAsync(string? categoryId)
        {
            Container container = await _cosmosDbService.GetContainerAsync();

            var categoriesQuery = new QueryDefinition(
                "SELECT DISTINCT c.categoryId, c.categoryName FROM c"
            );

            List<Product> categories = new();
            using (FeedIterator<Product> catIterator = container.GetItemQueryIterator<Product>(categoriesQuery))
            {
                while (catIterator.HasMoreResults)
                {
                    FeedResponse<Product> response = await catIterator.ReadNextAsync();
                    categories.AddRange(response);
                }
            }

            List<Product> items = new();
            double requestCharge = 0d;

            if (!string.IsNullOrEmpty(categoryId))
            {
                var productQuery = new QueryDefinition("SELECT * FROM c WHERE c.categoryId = @category")
                    .WithParameter("@category", categoryId.ToUpper());

                using (FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(productQuery))
                {
                    while (feed.HasMoreResults)
                    {
                        FeedResponse<Product> response = await feed.ReadNextAsync();
                        items.AddRange(response);
                        requestCharge += response.RequestCharge;
                    }
                }
            }

            var viewModel = new HomeCosmosViewModel
            {
                Products = items,
                RequestCharge = requestCharge,
                AvailableCategories = categories,
                SelectedCategoryId = categoryId
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    enum ApiMode 
    {
        Translate,
        Transliterate,
    }
}