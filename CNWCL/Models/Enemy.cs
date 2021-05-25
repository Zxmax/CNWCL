using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CNWCL.Models
{
    public class Enemy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FightDetail> Fights { get; set; } = new();
        public Enemy(string json, bool isUpCase)
        {
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
            if (isUpCase)
            {
                Id = jsonObject["Id"];
                Name = (string)jsonObject["Name"];
                foreach (var fight in jsonObject["Fights"])
                {
                    int id = fight["id"];
                    Fights.Add(new FightDetail(id));
                }
            }
            else
            {
                Id = jsonObject["id"];
                Name = (string)jsonObject["name"];
                foreach (var fight in jsonObject["fights"])
                {
                    int id = fight["id"];
                    Fights.Add(new FightDetail(id));
                }
            }
            
        }


    }
}
