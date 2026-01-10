using AzureP33.Models;
using AzureP33.Models.Home;
using AzureP33.Models.ORM;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AzureP33.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> IndexAsync(HomeIndexFormModel? formModel)
        {
            using HttpClient client = new();
            var response = JsonSerializer.Deserialize<LanguagesResponse>(
                await client.GetStringAsync(@"https://api.translator.azure.cn/languages?api-version=3.0")
            ) ?? throw new Exception("Error in resp");

            string defaultLang = "uk";

            if (formModel == null)
            {
                formModel = new HomeIndexFormModel();
            }

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
                LanguagesResponse = response
            };

            if (response.Transltations.TryGetValue(formModel.LangFrom, out var selectedLangData))
            {
                viewModel.Lang = selectedLangData;
            }

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

                    string result = await RequestApi(query, requestBody, ApiMode.Transliterate);
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

            if (formModel?.Action == "transliterate")
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
                        ViewData["result"] = await RequestApi(query, requestBody, ApiMode.Transliterate);  
                    }
                    else if (string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
                    {
                        viewModel.ErrorMessage = "Text must be at least 2 characters long.";
                    }
                }
                catch {}
            }

            return View(viewModel);
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