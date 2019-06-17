using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Industry
{
    public static class IndustryImporter
    {
        public static TradeGoodSD ConvertObjectToTradeGood(string rawJson)
        {
            TradeGoodSD result = JsonConvert.DeserializeObject<TradeGoodSD>(rawJson);

            return result;
        }
    }
}
