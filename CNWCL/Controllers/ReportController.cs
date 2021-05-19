using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CNWCL.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            TempData["curReport"] = JsonConvert.SerializeObject(report);
            TempData.Keep();
            if (report != null)
            {
                ViewData["curReportId"] = reportId;
                return View("index",report);
            }
            ViewData["curReportId"] = "NULL";
            return Redirect("/");

        }

        public async Task<IActionResult> AnalysisFightDetailsAsync(int fightId)
        {
            var reportJson = TempData["curReport"].ToString();
            TempData.Keep();
            var report = new Report(reportJson,true);
            var friends = await GetFriendFullInfo(report,fightId);
            return View("FightDetail",friends);
        }

        public IActionResult AnalysisFriendDetails()
        {
            TempData.Keep();
            throw new NotImplementedException();
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
            var report = new Report(dataObjects) {ReportId = reportId};
            return report;

        }

        public async Task<List<Friend>> GetFriendFullInfo(Report report,int fightId)
        {
            var friends = new List<Friend>();
            foreach (var friend in report.Friends)
            {
                friends.AddRange(from fight in friend.Fights where fight.Id == fightId && friend.Type!="NPC" select friend);
            }

            var startTime = report.Fights.Find(p => p.Id == fightId).StartTimeUnix;
            var endTime = report.Fights.Find(p => p.Id == fightId).EndTimeUnix;

            var client = new HttpClient();
            var getReportsUrl = Url + "report/events/summary/" + report.ReportId;

            client.BaseAddress = new Uri(getReportsUrl);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var urlParameters = UrlParameters +
                                "&start=" + startTime + "&end=" + endTime;

            // List data response.
            var response = await client.GetAsync(urlParameters);  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            // Parse the response body.
            if (!response.IsSuccessStatusCode) return null;

            var fightJson =
                await response.Content
                    .ReadAsStringAsync(); //Make sure to add a reference to System.Net.Http.Formatting.dll
            var parsedObjectItemLevel = JObject.Parse(fightJson);
            // ReSharper disable once PossibleNullReferenceException
            foreach (var p in parsedObjectItemLevel["events"])
            {
                if (p["type"]?.ToString() != "combatantinfo") continue;
                var gearJson = p["gear"]?.ToString();
                var talentJson = p["talents"]?.ToString();
                var covenantIdJson = p["covenantID"]?.ToString();
                var sourceId = p["sourceID"]?.ToObject<int>();
                var gears = JsonConvert.DeserializeObject<List<Gear>>(gearJson ?? string.Empty);
                var talents = JsonConvert.DeserializeObject<List<Talent>>(talentJson ?? string.Empty);
                var covenantId = JsonConvert.DeserializeObject<int>(covenantIdJson ?? string.Empty);
                friends.Find(friendly => friendly.Id == sourceId).Gears = gears;
                friends.Find(friendly => friendly.Id == sourceId).Talents = talents;
                friends.Find(friendly => friendly.Id == sourceId).CovenantId = covenantId;
                switch (covenantId)
                {
                    case 1:
                        friends.Find(friendly => friendly.Id == sourceId).Covenant = "格里恩";
                        break;
                    case 2:
                        friends.Find(friendly => friendly.Id == sourceId).Covenant = "温西尔";
                        break;
                    case 3:
                        friends.Find(friendly => friendly.Id == sourceId).Covenant = "法夜";
                        break;
                    case 4:
                        friends.Find(friendly => friendly.Id == sourceId).Covenant = "通灵";
                        break;
                }
            }

            foreach (var friend in friends)
            {
                //计算装等
                var itemLevelSum = 0d;
                var itemNumber = 0;
                foreach (var gear in friend.Gears.Where(gear => gear.ItemLevel >= 168))
                {
                    itemLevelSum += gear.ItemLevel;
                    itemNumber++;
                }
                friend.ItemLevel = Math.Round(itemLevelSum / itemNumber, 2); //保留小数点后2位
            }

            return friends;
        }

        #endregion

        
    }
}
