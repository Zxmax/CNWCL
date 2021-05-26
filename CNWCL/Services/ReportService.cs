using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CNWCL.Controllers;
using CNWCL.Models;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace CNWCL.Services
{
    public class ReportService
    {
        private const string Url = "https://cn.warcraftlogs.com:443/v1/";
        private const string UrlParameters = "?api_key=" + Key;
        private const string Key = "2b1f831583857b7f53019586915226cf";

        private static readonly MongoClient Client = new("mongodb://localhost:27017");
        private static readonly IMongoDatabase _database = Client.GetDatabase("WOW");

        public static string CurReportId = null;
        public static int CurFightId;
        public static Report CurReport = null;

        /// <summary>
        /// 获取单人施法列表
        /// </summary>
        /// <param name="report"></param>
        /// <param name="fightId"></param>
        /// <param name="friendId"></param>
        /// <param name="isTrans"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, int>> GetCastAsync(Report report, int fightId, int friendId, bool isTrans)
        {
            var startTime = report.Fights.FirstOrDefault(p => p.Id == fightId).StartTimeUnix;
            var endTime = report.Fights.FirstOrDefault(p => p.Id == fightId).EndTimeUnix;

            HttpClient client = new HttpClient();
            var getReportsUrl = Url + "report/events/casts/" + report.ReportId;

            client.BaseAddress = new Uri(getReportsUrl);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            string urlParameters;
            if (isTrans)
            {
                urlParameters = UrlParameters +
                                "&start=" + startTime + "&end=" + endTime + "&sourceid=" + friendId +
                                "&translate=true";
            }
            else
            {
                urlParameters = UrlParameters +
                                "&start=" + startTime + "&end=" + endTime + "&sourceid=" + friendId;
            }

            // List data response.
            var response = await client.GetAsync(urlParameters);  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            var dpsHpsCast = new Dictionary<string, int>();
            var sortedCast = new Dictionary<string, int>();
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                var dataObjects = await response.Content.ReadAsStringAsync();//Make sure to add a reference to System.Net.Http.Formatting.dll

                //deserialize to your class
                var parsedObject = JObject.Parse(dataObjects);
                foreach (var cast in parsedObject["events"])
                {
                    var castType = new Cast(cast.ToString());
                    if (castType.Name == null)
                        continue;
                    if (dpsHpsCast.ContainsKey(castType.Name))
                    {
                        dpsHpsCast[castType.Name] += 1;
                    }
                    else
                    {
                        dpsHpsCast.Add(castType.Name, 1);
                    }
                }
                sortedCast = dpsHpsCast.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            return sortedCast;
        }

        /// <summary>
        /// 获取团队爆发释放列表
        /// </summary>
        /// <param name="report"></param>
        /// <param name="fightId"></param>
        /// <param name="friendly"></param>
        /// <returns></returns>
        public static async Task<List<Cast>> GetCastList(Report report, int fightId, Friend friendly)
        {
            var startTime = report.Fights.Find(p => p.Id == fightId)?.StartTimeUnix;
            var endTime = report.Fights.Find(p => p.Id == fightId)?.EndTimeUnix;

            var client = new HttpClient();
            var getReportsUrl = Url + "report/events/casts/" + report.ReportId;
            client.BaseAddress = new Uri(getReportsUrl);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var urlParameters = UrlParameters +
                                "&start=" + startTime + "&end=" + endTime + "&sourceid=" + friendly.Id;

            // List data response.
            var response = await client.GetAsync(urlParameters);
            if (!response.IsSuccessStatusCode) return null;
            {
                var dataObjects =
                    await response.Content
                        .ReadAsStringAsync(); //Make sure to add a reference to System.Net.Http.Formatting.dll

                var parsedObjectHeal = JObject.Parse(dataObjects);
                return (parsedObjectHeal["events"] ?? throw new InvalidOperationException()).Select(cast => new Cast(cast.ToString())).ToList();
            }
        }

        /// <summary>
        /// 获取敌对npc施法
        /// </summary>
        /// <param name="report"></param>
        /// <param name="fightId"></param>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static async Task<List<Cast>> GetEnemyCastList(Report report, int fightId, Enemy enemy)
        {
            var startTime = report.Fights.Find(p => p.Id == fightId)?.StartTimeUnix;
            var endTime = report.Fights.Find(p => p.Id == fightId)?.EndTimeUnix;

            var client = new HttpClient();
            var getReportsUrl = Url + "report/events/casts/" + report.ReportId;
            client.BaseAddress = new Uri(getReportsUrl);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var urlParameters = UrlParameters +
                                "&start=" + startTime + "&end=" + endTime + "&hostility=1&sourceid=" + enemy.Id;

            // List data response.
            var response = await client.GetAsync(urlParameters);
            if (!response.IsSuccessStatusCode) return null;
            {
                var dataObjects =
                    await response.Content
                        .ReadAsStringAsync(); //Make sure to add a reference to System.Net.Http.Formatting.dll

                var parsedObjectHeal = JObject.Parse(dataObjects);
                return (parsedObjectHeal["events"] ?? throw new InvalidOperationException()).Select(cast => new Cast(cast.ToString())).ToList();
            }
        }

        /// <summary>
        /// 获取爆发列表
        /// </summary>
        /// <param name="allCasts"></param>
        /// <param name="startTimeUnix"></param>
        /// <param name="eruptCd"></param>
        /// <returns></returns>
        public static async Task<List<EruptTimeLine>> GetErupt(List<Cast> allCasts, long startTimeUnix, int eruptCd)
        {
            var collection = _database.GetCollection<ReportController.Erupt>("wow_erupt");
            var dicErupt = new List<EruptTimeLine>();
            string spec = null;
            var type = 0;
            foreach (var filter1 in allCasts.TakeWhile(_ => spec == null).Select(cast => Builders<ReportController.Erupt>.Filter.Eq("CNName", cast.Name)))
            {
                var results = await collection.Find(filter1).ToListAsync();
                if (results.Count is <= 0 or > 1) continue;
                spec = results.Find(p => p.Spec != null)?.Spec;
            }
            if (spec == "discipline")
                spec = "discipline";
            foreach (var cast in allCasts)
            {
                var filter1 = Builders<ReportController.Erupt>.Filter.Eq("CNName", cast.Name);
                var filter2 = Builders<ReportController.Erupt>.Filter.Gt("CoolDown", eruptCd - 1);
                try
                {
                    var results = await collection.Find(filter1 & filter2).ToListAsync();
                    switch (results.Count)
                    {
                        case <= 0:
                            continue;
                        case 1:
                            if (results[0].Type is 3 or 4)
                            {
                                type = results[0].Type;
                                break;
                            }
                            else
                            {
                                type = spec is "restoration" or "mistweaver" or "holy" or "discipline" ? 2 : 1;
                                break;

                            }
                        case > 1:
                            var temp = results.Find(p => p.Spec == spec);
                            if (temp != null) type = temp.Type;
                            break;
                    }

                    var values = dicErupt.FindAll(p => p.Name == cast.Name);
                    if (values.Count > 0)
                        if (values.Max(p => p.Time) > (cast.TimeUnix - startTimeUnix) / 1000.0 - 40)
                            continue;
                    dicErupt.Add(new EruptTimeLine(cast.Name, (cast.TimeUnix - startTimeUnix) / 1000.0, spec, type,
                        cast.SourceId));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return dicErupt;

        }
        /// <summary>
        /// 获取敌对npc的关键技能
        /// </summary>
        /// <param name="allCasts"></param>
        /// <param name="startTimeUnix"></param>
        /// <returns></returns>
        public static async Task<List<EruptTimeLine>> GetEnemyErupt(List<Cast> allCasts, long startTimeUnix)
        {
            var collection = _database.GetCollection<ReportController.EnemyErupt>("wow_castle-nathria");
            var dicErupt = new List<EruptTimeLine>();
            foreach (var cast in allCasts)
            {
                var filter = Builders<ReportController.EnemyErupt>.Filter.Eq("CastName", cast.Name);
                try
                {

                    var results = await collection.Find(filter).ToListAsync();
                    if (results.Count > 0)
                    {
                        var values = dicErupt.FindAll(p => p.Name == cast.Name);
                        if (values.Count > 0)
                            if (values.Max(p => p.Time) > (cast.TimeUnix - startTimeUnix) / 1000.0 - 5)
                                continue;
                        dicErupt.Add(new EruptTimeLine(cast.Name, (cast.TimeUnix - startTimeUnix) / 1000.0, cast.SourceId));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return dicErupt;

        }
    }
}
