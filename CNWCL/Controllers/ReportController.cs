﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CNWCL.Models;
using CNWCL.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CNWCL.Controllers
{
    public class ReportController : Controller
    {
        public IConfiguration Configuration { get; }
        private new const string Url = "https://cn.warcraftlogs.com:443/v1/";
        private const string UrlParameters = "?api_key=" + Key;
        private const string Key = "2b1f831583857b7f53019586915226cf";
        
        private static readonly MongoClient Client = new("mongodb://localhost:27017");
        private readonly IMongoDatabase _database = Client.GetDatabase("WOW");

        public ReportController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IActionResult Index(string reportId)
        {
            if(ReportService.CurReportJson !=null)
            {
                var reportJson = ReportService.CurReportJson;
                var report = new Report(reportJson, true,false);
                ReportService.CurReportId = reportId;
                //TempData["curReportId"] = reportId;
                //TempData.Keep();
                return View("index", report);
            }
            else
            {
                var report = GetReportByReportId(reportId).Result;
                if (report != null)
                {
                    //TempData["curReportId"] = reportId;
                    //TempData[reportId] = JsonConvert.SerializeObject(report);
                    //TempData.Keep();
                    ReportService.CurReportId = reportId;
                    ReportService.CurReportJson= JsonConvert.SerializeObject(report);
                    return View("index", report);
                }
                
                return Redirect("/");
            }

        }

        public async Task<IActionResult> AnalysisFightDetailsAsync(int fightId)
        {
            var reportJson = TempData[TempData["curReportId"].ToString() ?? string.Empty].ToString();
            var report = new Report(reportJson,true,false);
            var friends = await GetFriendFullInfo(report,fightId);
            
            TempData["curFightId"] = fightId;
            TempData[TempData["curReportId"].ToString() ?? string.Empty] = JsonConvert.SerializeObject(report);
            TempData.Keep();
            return View("FightDetail", friends);
        }
        public IActionResult InputEruptMinCd(int curFightId)
        {
            TempData["curFightId"] = curFightId;
            TempData.Keep();
            return View("InputTime");
        }

        public IActionResult ReturnInputEruptMinCd()
        {
            TempData.Keep();
            return View("InputTime");
        }
        public async Task<IActionResult> AnalysisErupt(int eruptCd)
        {
            var reportJson = TempData[TempData["curReportId"].ToString() ?? string.Empty].ToString();
            var curFightId = (int)TempData["curFightId"] ;
            var report = new Report(reportJson, true, false);
            var enemyList = report.Enemies;
            var enemies = (from enemy in enemyList let isInThisFight = enemy.Fights.Any(fight => fight.Id == curFightId) where isInThisFight select enemy).ToList();
            var friendList = report.Friends;
            var friends = (from friend in friendList let isInThisFight = friend.Fights.Any(fight => fight.Id == curFightId) where isInThisFight select friend).ToList();
            friends = friends.Where(p => p.Type != "NPC" && p.Type != "Boss").ToList();
            var eruptList = new List<EruptTimeLine>();
            TempData.Keep();
            foreach(var friendly in friends)
            {
                var castListTemp = await ReportService.GetCastList(report, curFightId, friendly);
                var dicErupt = await ReportService.GetErupt(castListTemp, report.Fights.Find(p => p.Id == curFightId).StartTimeUnix, eruptCd);
                eruptList.AddRange(dicErupt);
            }
            foreach(var enemy in enemies)
            {
                var castListTemp = await ReportService.GetEnemyCastList(report, curFightId, enemy);
                var dicErupt = await ReportService.GetEnemyErupt(castListTemp, report.Fights.Find(p => p.Id == curFightId).StartTimeUnix);
                eruptList.AddRange(dicErupt);
            }
            eruptList = eruptList.OrderBy(p => p.Time).ToList();
            return View("EruptAnalysis", eruptList);
        }

        public async Task<IActionResult> AnalysisFriendDetailsAsync(int friendId)
        {
            var curReportId = (string)TempData["curReportId"];
            var curFightId = (int)TempData["curFightId"];
            var reportJson = TempData[curReportId].ToString();
            var report = new Report(reportJson, true,true);
            
            var curFight = report.Fights.FirstOrDefault(p => p.Id == curFightId);
            var curFriendly = report.Friends.FirstOrDefault(p => p.Id == friendId);

            var talentList= GetTalentByType(curFriendly.Talents);
            curFriendly.TalentNumber = talentList[0] + talentList[1] + talentList[2] + talentList[3] + talentList[4] + talentList[5] + talentList[6];


            var casts = await GetCastAsync(report, curFightId, friendId, true);
            var durationRole = (curFight.EndTimeUnix - curFight.StartTimeUnix) / 1000.0;
            int sameTalentCovenant;
            Dictionary<string, int> castsModel;
            double durationModel;
            (sameTalentCovenant, castsModel, durationModel) = await GetSameTalentCovenant(curFight.Boss, curFriendly);
            var indexCast = casts.Keys.ToList().Union(castsModel.Keys.ToList()).ToList();

            TempData.Keep();
            return View("CastCompare", new Tuple<Dictionary<string,int>,double,int, Dictionary<string, int>, double,List<string>>(casts,durationRole,sameTalentCovenant,castsModel,durationModel,indexCast));
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
            var friends = (from friend in report.Friends let isInThisFight = friend.Fights.Any(fight => fight.Id == fightId) where isInThisFight select friend).ToList();
            friends = friends.Where(p => p.Type != "NPC" && p.Type != "Boss").ToList();
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
                var specIdJson = p["specID"]?.ToString();
                var sourceId = p["sourceID"]?.ToObject<int>();
                var gears = JsonConvert.DeserializeObject<List<Gear>>(gearJson ?? string.Empty);
                var talents = JsonConvert.DeserializeObject<List<Talent>>(talentJson ?? string.Empty);
                var covenantId = JsonConvert.DeserializeObject<int>(covenantIdJson ?? string.Empty);
                var specId = JsonConvert.DeserializeObject<int>(specIdJson ?? string.Empty);
                friends.Find(friendly => friendly.Id == sourceId).Gears = gears;
                friends.Find(friendly => friendly.Id == sourceId).Talents = talents;
                friends.Find(friendly => friendly.Id == sourceId).CovenantId = covenantId;
                friends.Find(friendly => friendly.Id == sourceId).SpecId = specId;

                #region spec

                switch (specId)
                {
                     case 250 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Blood";
                         break;
                     case 251 : 
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Frost";
                         break;
                     case 252 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Unholy";
                        break;  
                     case 577 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Havoc";
                        break; 
                     case 581 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Vengeance";
                        break; 
                     case 102 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Balance";
                        break; 
                     case 103 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Feral";
                        break; 
                     case 104 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Guardian";
                        break; 
                     case 105 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Restoration";
                        break; 
                     case 253 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Beast Mastery";
                        break; 
                     case 254 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Marksmanship";
                        break; 
                     case 255 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Survival";
                        break; 
                     case 62  :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Arcane";
                        break; 
                     case 63  :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Fire";
                        break; 
                     case 64  :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Frost";
                        break; 
                     case 268 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Brewmaster";
                        break; 
                     case 269 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Windwalker";
                        break; 
                     case 270 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Mistweaver";
                        break; 
                     case 65  :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Holy";
                         break; 
                     case 66  :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Protection";
                        break; 
                     case 70  :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Retribution";
                        break; 
                     case 256 :
                        friends.Find(friendly => friendly.Id == sourceId).Spec = "Discipline";
                        break; 
                     case 257 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Holy";
                         break; 
                     case 258 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Shadow";
                         break; 
                     case 259 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Assassination";
                         break; 
                     case 260 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Outlaw";
                         break; 
                     case 261 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Subtlety";
                         break; 
                     case 262 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Elemental";
                         break; 
                     case 263 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Enhancement";
                         break; 
                     case 264 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Restoration";
                         break; 
                     case 265 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Affliction";
                         break; 
                     case 266 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Demonology";
                         break; 
                     case 267 :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Destruction";
                         break; 
                     case 71  :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Arms";
                         break; 
                     case 72  :
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Fury";
                         break; 
                     case 73:
                         friends.Find(friendly => friendly.Id == sourceId).Spec = "Protection";
                         break; 
                }
                

                #endregion
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
        /// 获取相同天赋的模板人数
        /// </summary>
        /// <param name="bossId"></param>
        /// <param name="friendly"></param>
        /// <returns></returns>
        public async Task<Tuple<int, Dictionary<string, int>, double>> GetSameTalentCovenant(int bossId, Friend friendly)
        {
            var collectionBoss = _database.GetCollection<Boss>("wclBoss");
            var bossName = collectionBoss.Find(p => p.EncounterId == bossId).FirstOrDefault().EncounterName;
            if (friendly.Spec is "Restoration" or "Mistweaver" or "Holy" or "Discipline")
                bossName = bossName.Replace(' ', '_') + "_HPS_talent";
            else
                bossName = bossName.Replace(' ', '_') + "_DPS_talent";
            var sameTalentCovenantCollection = _database.GetCollection<Rank100>(bossName);
            var filter2 = Builders<Rank100>.Filter.Eq("spec_name", friendly.Spec);
            var filter3 = Builders<Rank100>.Filter.Eq("covenant_id", friendly.CovenantId);
            var filter1 = Builders<Rank100>.Filter.Eq("class_name", friendly.Type);
            var results = await sameTalentCovenantCollection.Find(filter1 & filter2 & filter3).ToListAsync();
            for (var m = 0; m < results.Count; m++)
            {
                var tempRank100 = results[m];
                var isDelete = false;
                var talentTemp = tempRank100.Talent.Aggregate("", (current, p) => (string)(current + p));
                for (var i = 0; i < 7; i++)
                    if (talentTemp[i] != friendly.TalentNumber[i] && talentTemp[i] != 'X')
                        isDelete = true;
                if (!isDelete) continue;
                results.Remove(tempRank100);
                m--;
            }

            if (results.Count == 0)
                return new Tuple<int, Dictionary<string, int>, double>(0, new Dictionary<string, int>(), 0d);
            var model = results.First();
            var report = await GetReportByReportId(model.ReportId);
            var fights = report.Fights;
            fights = (fights ?? throw new InvalidOperationException()).Where(p => p.Boss != 0).ToList();
            var friend = report.Friends.Find(p => p.Name == model.CharacterName);
            var fight = fights.Find(p => p.StartTimeUnix == model.Start);

            var castsModel = await GetCastAsync(report, fight.Id, friend.Id, true);
            return new Tuple<int, Dictionary<string, int>, double>(results.Count, castsModel, model.Duration);
        }

        /// <summary>
        /// 根据天赋得知专精
        /// </summary>
        /// <param name="talentList"></param>
        /// <returns></returns>
        public List<string> GetTalentByType(List<Talent> talentList)
        {
            var collection = _database.GetCollection<TalentClass>("wcl_talent");
            return talentList.Select(talent => talent.Id).Select(talentId => collection.Find(p => p.TalentId == talentId).FirstOrDefault()).Select(doc => doc.TalentNum.ToString()).ToList();
        }

        #endregion

        #region inner class

        public class FriendList
        {
            public FriendList(string friendsJson)
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(friendsJson);
                if (jsonObject == null) return;
                foreach (var friendJson in jsonObject)
                {
                    Friends.Add(new Friend(friendJson.ToString(),false));
                }
            }

            public List<Friend> Friends { get; set; } = new();
        }

        public class TalentClass
        {
            public ObjectId Id { get; set; }
            [BsonElement("class_name")]
            public string ClassName { get; set; }
            [BsonElement("spec_name")]
            public string SpecName { get; set; }
            [BsonElement("talent_id")]
            public string TalentId { get; set; }
            [BsonElement("talent_num")]
            public int TalentNum { get; set; }
            [BsonElement("remark")]
            public string Remark { get; set; }

        }
        [BsonIgnoreExtraElements]
        public class Boss
        {
            public ObjectId Id { get; set; }
            [BsonElement("encounterId")]
            public int EncounterId { get; set; }
            [BsonElement("encounter_name")]
            public string EncounterName { get; set; }
        }
        [BsonIgnoreExtraElements]
        public class Rank100
        {
            public ObjectId Id { get; set; }
            [BsonElement("character_name")]
            public string CharacterName { get; set; }
            [BsonElement("class_name")]
            public string ClassName { get; set; }
            [BsonElement("covenant_id")]
            public int CovenantId { get; set; }
            [BsonElement("duration")]
            public double Duration { get; set; }
            [BsonElement("start")]
            public long Start { get; set; }
            [BsonElement("end")]
            public long End { get; set; }
            [BsonElement("report_id")]
            public string ReportId { get; set; }
            [BsonElement("fight_id")]
            public int FightId { get; set; }
            [BsonElement("spec_name")]
            public string SpecName { get; set; }
            [BsonElement("talent")]
            public List<dynamic> Talent { get; set; }
            //[BsonElement("total")]
            //public string Total { get; set; }
        }
        [BsonIgnoreExtraElements]
        public class Erupt
        {
            public ObjectId Id { get; set; }
            [BsonElement("Type")]
            public int Type { get; set; }
            [BsonElement("Spec")]
            public string Spec { get; set; }

        }
        [BsonIgnoreExtraElements]
        public class EnemyErupt
        {
            public ObjectId Id { get; set; }
            [BsonElement("Boss")]
            public string Boss { get; set; }
            [BsonElement("CastName")]
            public string CastName { get; set; }

        }
        #endregion
    }
}
