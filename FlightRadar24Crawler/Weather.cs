using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DataCrawlers
{
    public class Weather
    {
       
        [JsonProperty("name")]
        public string City { get; set; }

        [JsonProperty("dt")]
        public string TimeStamp { get; set; }

        public DateTime Date { get; set; }

        [JsonProperty("coord")]
        public dynamic coord { get; set; }

        public string Lat { get; set; }

        public string Long { get; set; }

        [JsonProperty("wind")]
        public dynamic Wind { get; set; }

        public object WindSpeed { get; set; }

        public object WindDirection { get; set; }

        public string Season { get; set; }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });
        }
    }
}