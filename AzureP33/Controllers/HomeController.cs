using AzureP33.Models;
using AzureP33.Models.Cosmos;
using AzureP33.Models.Home;
using AzureP33.Models.ORM;
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

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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

            string defaultLang = "en";


            if (string.IsNullOrEmpty(formModel.LangFrom))
            {
                formModel.LangFrom = defaultLang;
            }

            if (formModel.Action == "replace")
            {
                var temp = formModel.LangFrom;
                formModel.LangFrom = formModel.LangTo;
                formModel.LangTo = temp;

                if (!string.IsNullOrEmpty(formModel.TranslatedText))
                {
                    formModel.OriginalText = formModel.TranslatedText;

                    formModel.TranslatedText = null;
                    ViewData["result"] = "";

                    formModel.Action = "translate";
                }
            }

            HomeIndexViewModel viewModel = new()
            {
                PageTitle = "Translation",
                FormModel = formModel,
                //LanguagesResponse = response
            };

            if (formModel?.Action != null)
            {
                viewModel.FormModel = formModel;

                if (string.IsNullOrWhiteSpace(formModel.OriginalText) && formModel.Action == "translate")
                {
                    viewModel.ErrorMessage = "Please enter any text to translate";
                }
            }

            if (formModel?.Action == "translate")
            {
                if (!string.IsNullOrWhiteSpace(formModel.OriginalText) && formModel.OriginalText.Trim().Length >= 2)
                {
                    string query = $"from={formModel.LangFrom}&to={formModel.LangTo}";
                    object[] body = new object[] { new { Text = formModel.OriginalText } };
                    var requestBody = JsonSerializer.Serialize(body);

                    string result = await RequestApi(query, requestBody, ApiMode.Translate);
                    if (result[0] == '[')
                    {
                        viewModel.Items = JsonSerializer.Deserialize<List<TranslatorResponseItem>>(result);
                    }
                    else 
                    {
                        viewModel.ErrorResponse = JsonSerializer.Deserialize<TranslatorErrorResponse>(result);
                    }

                }
                else if (string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
                {
                    viewModel.ErrorMessage = "Text must be at least 2 characters long.";
                }
            }

            var response = await responseTask;

            if (response.Transltations.TryGetValue(formModel.LangFrom, out var selectedLangData))
            {
                viewModel.Lang = selectedLangData;
            }

            if (viewModel.Items != null) 
            {
                LangData langData;

                try
                {
                    if (!string.IsNullOrWhiteSpace(formModel.OriginalText) && formModel.OriginalText.Trim().Length >= 2)
                    {
                        langData = response.Translatirations[formModel.LangFrom];
                        string fromScript = langData.Scripts![0].Code!;
                        string toScript = langData.Scripts![0].ToScripts![0].Code!;

                        string query = $"language={formModel.LangFrom}&fromScript={fromScript}&toScript={toScript}";
                        var requestBody = JsonSerializer.Serialize(new object[]
                        {
                            new { Text = formModel.OriginalText }
                        });

                        string jsonResult = await RequestApi(query, requestBody, ApiMode.Transliterate);

                        var resultItems = JsonSerializer.Deserialize<List<TransliterationResponseItem>>(jsonResult);

                        if (resultItems != null && resultItems.Count > 0)
                        {
                            viewModel.FromTransliteration = resultItems[0];
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
                    {
                        viewModel.ErrorMessage = "Text must be at least 2 characters long.";
                    }
                }
                catch (Exception ex)
                {
                    viewModel.ErrorMessage = "Transliteration failed.";
                }

                try
                {
                    if (viewModel.Items?.Count > 0 && viewModel.Items[0].Translations?.Count > 0)
                    {
                        string translatedText = viewModel.Items[0].Translations[0].Text;

                        if (response.Translatirations.TryGetValue(formModel.LangTo, out var targetLangData))
                        {
                            string fromScript = targetLangData.Scripts![0].Code!;
                            string toScript = targetLangData.Scripts![0].ToScripts![0].Code!;

                            string query = $"language={formModel.LangTo}&fromScript={fromScript}&toScript={toScript}";

                            var requestBody = JsonSerializer.Serialize(new object[]
                            {
                                new { Text = translatedText }
                            });

                            string jsonResult = await RequestApi(query, requestBody, ApiMode.Transliterate);

                            var resultItems = JsonSerializer.Deserialize<List<TransliterationResponseItem>>(jsonResult);

                            if (resultItems != null && resultItems.Count > 0)
                            {
                                viewModel.ToTransliteration = resultItems[0];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    viewModel.ErrorMessage = "Target transliteration failed: " + ex.Message;
                }
            }

            viewModel.LanguagesResponse = await responseTask;
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

        public async Task<IActionResult> CosmosAsync()
        {
/*            CosmosClient client = new(
                connectionString: ""
            );*/

            Database database = client.GetDatabase("cosmicworks");
            database = await database.ReadAsync();

            Container container = database.GetContainer("products");
            container = await container.ReadContainerAsync();

            var query = new QueryDefinition(
                query: "SELECT * FROM c WHERE p.categoryId = @category"
            )
                .WithParameter("@category", "26C74104-40BC-4541-8EF5-9892F7F03D72");

            using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(
                queryDefinition: query
            );

            List<Product> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<Product> response = await feed.ReadNextAsync();
                foreach (Product item in response)
                {
                    items.Add(item);
                }
                requestCharge += response.RequestCharge;
            }

            return View(new HomeCosmosViewModel
            {
                Products = items,
                RequestCharge = requestCharge
            });
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