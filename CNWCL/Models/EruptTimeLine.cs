namespace CNWCL.Models
{
    public class EruptTimeLine
    {
        public int SourceId { get; set; }
        public int Type { get; set; }
        public string Spec { get; set; }
        public string Name { get; set; }
        public double Time { get; set; }

        public EruptTimeLine(string name, double time, string spec, int type, int sourceId)
        {
            Name = name;
            Time = time;
            Spec = spec;
            Type = type;
            SourceId = sourceId;
        }

        public EruptTimeLine(string name, double time,  int sourceId)
        {
            Name = name;
            Time = time;
            Type = 5;
            SourceId = sourceId;
        }
        
    }
}
