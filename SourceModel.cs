using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataLocalizer
{
    [JsonSerializable(typeof(SourceModel))]
    public partial class MyJsonContext : JsonSerializerContext
    {
    }
    public class SourceModel
    {
        public string? HomeTip { get; set; }
        public string? homeLogo { get; set; }

        public string? Spider { get; set; }
        public List<SiteModel> Sites { get; set; }

    }

    public class SiteModel
    {

        public string? key { get; set; }
        public string? name { get; set; }
        public int type { get; set; }
        public string? api { get; set; }
        public int searchable { get; set; }
        public int quickSearch { get; set; }
        public int filterable { get; set; }
        public string? ext { get; set; }

    }
}
