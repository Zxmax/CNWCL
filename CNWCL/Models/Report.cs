using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CNWCL.Models
{
    public class Report
    {
        public List<Fight> Fights { get; set; }
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

    }
    
}
