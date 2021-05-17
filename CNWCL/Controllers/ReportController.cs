using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CNWCL.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace CNWCL.Controllers
{
    public class ReportController : Controller
    {
        private new const string Url = "https://cn.warcraftlogs.com:443/v1/";
        private const string UrlParameters = "?api_key=" + Key;
        private const string Key = "2b1f831583857b7f53019586915226cf";
        private static readonly MongoClient Client = new("mongodb://localhost:27017");
        private readonly IMongoDatabase _database = Client.GetDatabase("WOW");

        private readonly ILogger<ReportController> _logger;

        public ReportController(ILogger<ReportController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string reportId)
        {
            var report = GetReportByReportId(reportId).Result;
            ViewData["curReport"] = report;
            return View(report);
        }




        #region 获取资源信息方法

        public async Task<Report> GetReportByReportId(string reportId)
        {
            var client = new HttpClient();
            var getReportsUrl = Url + "report/fights/" + reportId;

            client.BaseAddress = new Uri(getReportsUrl);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            // List data response.
            var response = await client.GetAsync(UrlParameters);  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            // Parse the response body.
            if (!response.IsSuccessStatusCode) return null;
            var dataObjects =
                await response.Content
                    .ReadAsStringAsync(); //Make sure to add a reference to System.Net.Http.Formatting.dll
            var report = new Report(dataObjects);
            return report;

        }



        #endregion
    }
}
