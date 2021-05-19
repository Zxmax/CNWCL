using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CNWCL.Models
{
    public class Friend
    {

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        //职业

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }
        //专精
        public string Spec { get; set; }
        //盟约
        public string Covenant { get; set; }
        public int CovenantId { get; set; }
        public double ItemLevel { get; set; }
        public List<Gear> Gears { get; set; } = new();
        public List<Talent> Talents { get; set; } = new();
        public string TalentNumber { get; set; }

        [JsonProperty(PropertyName = "fights")]
        public List<FightDetail> Fights { get; set; } = new();
        public Friend(string json)
        {
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
            if (jsonObject == null) return;
            Id = jsonObject["id"];
            Name = (string) jsonObject["name"];
            Server= (string)jsonObject["server"];
            Type = (string) jsonObject["type"] switch
            {
                "DeathKnight" => "Death Knight",
                "DemonHunter" => "Demon Hunter",
                _ => (string) jsonObject["type"]
            };
            foreach (var fight in jsonObject["fights"])
            {
                int id = fight["id"];
                Fights.Add(new FightDetail(id));
            }
        }
    }

    public class Gear
    {
        public int Id { get; set; }
        public int ItemLevel { get; set; }
        public bool HasGems { get; set; }
    }

    public class Talent
    {
        public string Id { get; set; }
    }
    public class FightDetail
    {

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
        public FightDetail(int id)
        {
            Id = id;
        }

    }
}

