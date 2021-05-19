﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CNWCL.Models
{
    public class Report
    {
        public string ReportId { get; set; }
        public List<Fight> Fights { get; set; }
        public List<Friend> Friends { get; set; }
        [JsonProperty(PropertyName = "lang")]
        public string Language { get; set; }
        public List<Phase> Phases { get; set; }

        [JsonProperty(PropertyName = "start")]
        public long StartUnix { get; set; }

        [JsonProperty(PropertyName = "end")]
        public long EndUnix { get; set; }

        public Report(string json)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                if (jsonObject == null) return;
                Fights = new List<Fight>();
                foreach (var paraJson in jsonObject["fights"])
                {
                    Fights.Add(JsonConvert.DeserializeObject<Fight>(paraJson.ToString() ?? string.Empty));
                }

                Friends = new List<Friend>();
                foreach (var paraJson in jsonObject["friendlies"])
                {
                    Friends.Add(new Friend(paraJson.ToString()));
                }
                Language = jsonObject["lang"];
                Phases = new List<Phase>(); 
                foreach (var paraJson in jsonObject["phases"])
                {
                    Phases.Add(new Phase(paraJson.ToString()));
                }
                StartUnix = jsonObject["start"];
                EndUnix = jsonObject["end"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public Report(string json,bool isCustom)
        {
            Console.Write(isCustom);
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                if (jsonObject == null) return;

                ReportId = jsonObject["ReportId"];
                Fights = new List<Fight>();
                var fightsJson = jsonObject["Fights"];
                foreach (var paraJson in fightsJson)
                {
                    Fights.Add(JsonConvert.DeserializeObject<Fight>(paraJson.ToString() ?? string.Empty));
                }

                Friends = new List<Friend>();
                var friendsJson = jsonObject["Friends"];
                foreach (var paraJson in friendsJson)
                {
                    Friends.Add(new Friend(paraJson.ToString()));
                }
                Language = jsonObject["lang"];
                Phases = new List<Phase>();
                foreach (var paraJson in jsonObject["Phases"])
                {
                    Phases.Add(new Phase(paraJson.ToString()));
                }
                StartUnix = jsonObject["start"];
                EndUnix = jsonObject["end"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
}
