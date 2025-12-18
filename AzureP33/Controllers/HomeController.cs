using AzureP33.Models;
using AzureP33.Models.Home;
using AzureP33.Models.ORM;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace AzureP33.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> IndexAsync(HomeIndexFormModel? formModel)
        {
            using HttpClient client = new();
            var response = JsonSerializer.Deserialize<LanguagesResponse>
            (
                await client.GetStringAsync
                (
                    @"https://api.translator.azure.cn/languages?api-version=3.0"
                )
            ) ?? throw new Exception("Error in resp");

            HomeIndexViewModel viewModel = new()
            {
                PageTitle = "Translation",
                FormModel = formModel?.Action == null ? null : formModel,
                LanguagesResponse = response,
                Lang = formModel.Lang
            };

            if (formModel?.Action != null) 
            {
                viewModel.FormModel = formModel;

                if (string.IsNullOrWhiteSpace(formModel.OriginalText))
                {
                    viewModel.ErrorMessage = "Please enter any text to translate";
                }
                else 
                {
                    /////
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