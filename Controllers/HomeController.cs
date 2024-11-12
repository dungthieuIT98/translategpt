using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Mammoth;
using TranslateGPT.DTOs;
using TranslateGPT.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;

namespace TranslateGPT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly List<string> mostUsedLanguages = new List<string>()
        {
            "English",
            "Mandarin Chinese",
            "Spanish",
            "Hindi",
            "Arabic",
            "Bengali",
            "Portuguese",
            "Russian",
            "Japanese",
            "French",
            "German",
            "Urdu",
            "Italian",
            "Indonesian",
            "Vietnamese",
            "Turkish",
            "Korean",
            "Tamil",
            "Albanian"
        };

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            ViewBag.Languages = new SelectList(mostUsedLanguages);
            return View();
        }

        [HttpPost]
        public async Task<string> OpenAIGPT(string query, string selectedLanguage)
        {
           
            //Define the request payload
            var payload = new
            {
                model = "gpt-4",
                messages = new object[]
                {
                    //new { role = "system", content = $"Translate to {selectedLanguage}"},
                    new { role = "system", content = $"DỊCH ĐẦY ĐỦ, KHÔNG ĐƯỢC TÓM TẮT, DỊCH SANG TIẾNG BỒ ĐÀO NHA (BRASIL)"},
                    new {role = "user", content = @query}
                },
                temperature = 0,
                max_tokens = 256
            };
            string jsonPayload = JsonConvert.SerializeObject(payload);
            HttpContent httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            //Send the request
            var responseMessage = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
            var responseMessageJson = await responseMessage.Content.ReadAsStringAsync();

            //Return a response
            var response = JsonConvert.DeserializeObject<OpenAIResponse>(responseMessageJson);

            //ViewBag.Result = response.Choices[0].Message.Content;
            //ViewBag.Languages = new SelectList(mostUsedLanguages);

            return response.Choices[0].Message.Content;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        [HttpPost("savefile")]
        public async Task<IActionResult> SaveFile(SingleFileModel model)
        {
            var openAPIKey = _configuration["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAPIKey}");

            var str = _configuration["OpenAI:savefile_vi"] ?? "";
            var date = DateTime.Now.ToString("dd_mm_yy");
            if (model.File.Length > 0)
            {
                string filePath = Path.Combine(str, date + "_" + model.File.FileName);
                using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }
                var converter = new DocumentConverter();
                var result = converter.ExtractRawText(filePath);
                var html = result.Value; // The raw text
                var warnings = result.Warnings; // Any warnings during conversion

                if (warnings.Count > 0)
                {
                    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }
                var list = (html.Split("\n").ToList()).Where(x => x.Length > 1);

                string primeNumbers = "";
                foreach (var data in list)
                {
                    var rs = await OpenAIGPT(data, "English");
                    primeNumbers += rs + "\n\n";
                }

                using (StreamWriter outputFile = new StreamWriter(Path.Combine(str, Guid.NewGuid() + ".txt"), true))
                {
                    outputFile.WriteLine(primeNumbers);
                }
            }

            //ViewBag.Result = response.Choices[0].Message.Content;
            ViewBag.Languages = new SelectList(mostUsedLanguages);
            return View("Index");

        }

    }

    public class SingleFileModel
    {
        [Required(ErrorMessage = "Please enter file name")]
        public string FileName { get; set; }
        [Required(ErrorMessage = "Please select file")]
        public IFormFile File { get; set; }
    }
}