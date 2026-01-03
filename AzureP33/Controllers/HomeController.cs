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
                var sec = _configuration.GetSection("Azure").GetSection("Translator");

                if (sec == null) throw new Exception("Configuration error");

                string key = sec.GetValue<string>("Key");
                string endpoint = sec.GetValue<string>("Endpoint");
                string location = sec.GetValue<string>("Location");
                string translatorPath = sec.GetValue<string>("TranslatorPath");
                string apiVersion = sec.GetValue<string>("ApiVersion");

                if (!string.IsNullOrWhiteSpace(formModel.OriginalText) && formModel.OriginalText.Trim().Length >= 2)
                {
                    string route = $"{translatorPath}?api-version={apiVersion}&from={formModel.LangFrom}&to={formModel.LangTo}";
                    object[] body = new object[] { new { Text = formModel.OriginalText } };
                    var requestBody = JsonSerializer.Serialize(body);

                    using (var client2 = new HttpClient())
                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(endpoint + route);
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                        request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                        HttpResponseMessage translationResponse = await client2.SendAsync(request).ConfigureAwait(false);
                        string jsonResult = await translationResponse.Content.ReadAsStringAsync();

                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonResult))
                            {
                                string translatedText = doc.RootElement[0]
                                                        .GetProperty("translations")[0]
                                                        .GetProperty("text")
                                                        .GetString();

                                ViewData["result"] = translatedText;
                            }
                        }
                        catch
                        {
                            viewModel.ErrorResponse = JsonSerializer.Deserialize<TranslatorErrorResponse>(jsonResult);
                            ViewData["result"] = jsonResult;
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
                {
                    viewModel.ErrorMessage = "Text must be at least 2 characters long.";
                }
            }

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
}