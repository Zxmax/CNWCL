using Microsoft.AspNetCore.Mvc;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CNWCL.Controllers
{
    public class ReportController : Controller
    {
        public IConfiguration Configuration { get; }

        public ReportController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IActionResult Index(string reportId)
        {
            if(ReportService.CurReportId==reportId)
            {
                var report = ReportService.CurReport;
                ReportService.CurReportId = reportId;
                return View("index", report);
            }
            else
            {
                var report = ReportService.GetReportByReportId(reportId).Result;
                if (report == null) return Redirect("/");
                ReportService.CurReportId = reportId;
                ReportService.CurReport= report;
                return View("index", report);

            }

        }

        public async Task<IActionResult> AnalysisFightDetailsAsync(int fightId)
        {
            var report = ReportService.CurReport;
            report.Friends = await ReportService.GetFriendFullInfo(report,fightId);
            
            ReportService.CurFightId = fightId;
            return View("FightDetail", report.Friends);
        }

        public IActionResult InputEruptMinCd(int fightId)
        {
            ReportService.CurFightId  = fightId;
            return View("InputTime");
        }
        
        public async Task<IActionResult> AnalysisErupt(int eruptCd)
        {
            var curFightId = ReportService.CurFightId;
            var report =ReportService.CurReport; 
            var enemyList = report.Enemies;
            var enemies = (from enemy in enemyList let isInThisFight = enemy.Fights.Any(fight => fight.Id == curFightId) where isInThisFight select enemy).ToList();
            var friendList = report.Friends;
            var friends = (from friend in friendList let isInThisFight = friend.Fights.Any(fight => fight.Id == curFightId) where isInThisFight select friend).ToList();
            friends = friends.Where(p => p.Type != "NPC" && p.Type != "Boss").ToList();
            var eruptList = new List<EruptTimeLine>();

            var list = eruptList;
            var tasks = friends.Select(async friendly =>
            {
                // some pre stuff
                var castListTemp = await ReportService.GetCastList(report, curFightId, friendly);
                var dicErupt = await ReportService.GetErupt(castListTemp,
                    report.Fights.Find(p => p.Id == curFightId).StartTimeUnix, eruptCd);
                list.AddRange(dicErupt);
                // some post stuff
            });
            await Task.WhenAll(tasks);
            var tasks2 = enemies.Select(async enemy =>
            {
                // some pre stuff
                var castListTemp = await ReportService.GetEnemyCastList(report, curFightId, enemy);
                var dicErupt = await ReportService.GetEnemyErupt(castListTemp,
                    report.Fights.Find(p => p.Id == curFightId).StartTimeUnix);
                list.AddRange(dicErupt);
                // some post stuff
            });
            await Task.WhenAll(tasks2);
            
            eruptList = list.OrderBy(p => p.Time).ToList();
            return View("EruptAnalysis", eruptList);
        }

        public async Task<IActionResult> AnalysisFriendDetailsAsync(int friendId)
        {
            var curFightId = ReportService.CurFightId;
            var report = ReportService.CurReport;
            
            var curFight = report.Fights.FirstOrDefault(p => p.Id == curFightId);
            var curFriendly = report.Friends.FirstOrDefault(p => p.Id == friendId);

            var talentList= ReportService.GetTalentByType(curFriendly.Talents);
            curFriendly.TalentNumber = talentList[0] + talentList[1] + talentList[2] + talentList[3] + talentList[4] + talentList[5] + talentList[6];


            var casts = await ReportService.GetCastAsync(report, curFightId, friendId, true);
            var durationRole = (curFight.EndTimeUnix - curFight.StartTimeUnix) / 1000.0;
            int sameTalentCovenant;
            Dictionary<string, int> castsModel;
            double durationModel;
            (sameTalentCovenant, castsModel, durationModel) = await ReportService.GetSameTalentCovenant(curFight.Boss, curFriendly);
            var indexCast = casts.Keys.ToList().Union(castsModel.Keys.ToList()).ToList();
            
            return View("CastCompare", new Tuple<Dictionary<string,int>,double,int, Dictionary<string, int>, double,List<string>>(casts,durationRole,sameTalentCovenant,castsModel,durationModel,indexCast));
        }

        

    }
}
