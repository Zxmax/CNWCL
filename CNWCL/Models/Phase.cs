using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CNWCL.Models
{
    public class Phase
    {
        [JsonProperty(PropertyName = "boss")]
        public int BossId { get; set; }
        public List<string> Phases { get; set; }
        public List<int> Intermissions { get; set; }
        public Phase(string json)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                if (jsonObject == null) return;
                Phases = new List<string>();
                foreach (var paraJson in jsonObject["phases"])
                {
                    Phases.Add(paraJson.ToString() ?? string.Empty);
                }
                Intermissions = new List<int>();
                foreach (var paraJson in jsonObject["intermissions"])
                {
                    Intermissions.Add(JsonConvert.DeserializeObject<int>(paraJson.ToString() ?? string.Empty));
                }
                BossId = jsonObject["boss"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
