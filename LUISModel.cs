// <auto-generated>
// Code generated by LUISGen .\luismodel-0.1.json -cs Luis.LUISModel -o .
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace GameATron4000
{
    public class LUISModel: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            look_at, 
            None, 
            pick_up, 
            talk_to
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] Al;
            public string[] Guy_Scotthrie;
            public string[] Ian;
            public string[] newspaper;

            // Instance
            public class _Instance
            {
                public InstanceData[] Al;
                public InstanceData[] Guy_Scotthrie;
                public InstanceData[] Ian;
                public InstanceData[] newspaper;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<LUISModel>(JsonConvert.SerializeObject(result));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}