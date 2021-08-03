using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CNWCL.Models
{
    public class LifeSavingData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //职业
        public string Type { get; set; }
        //专精
        public string Spec { get; set; }
        //盟约
        public string Covenant { get; set; }
        public List<KeyValuePair<string, double>> HealingData { get; set; } = new ();

    }
}
