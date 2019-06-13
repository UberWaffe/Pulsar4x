using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchTradeGoods
    {
        public Dictionary<Guid, long> Items { get; private set;  } = new Dictionary<Guid, long>();
        public Dictionary<Guid, long> Services { get; private set; } = new Dictionary<Guid, long>();

        public BatchTradeGoods()
        {
            Items = new Dictionary<Guid, long>();
            Services = new Dictionary<Guid, long>();
        }

        public BatchTradeGoods(BatchTradeGoods batchToCopy)
        {
            Items = new Dictionary<Guid, long>();
            Services = new Dictionary<Guid, long>();

            foreach (var entry in batchToCopy.Items)
            {
                Items.Add(entry.Key, entry.Value);
            }
            foreach (var entry in batchToCopy.Services)
            {
                Services.Add(entry.Key, entry.Value);
            }
        }

        public void AddTradeGood(TradeGoodSD theGood, int amount)
        {
            if (Items.ContainsKey(theGood.ID) == false)
            {
                Items[theGood.ID] = 0;
            }

            Items[theGood.ID] += Math.Abs(amount);
        }

        public void ChangeService(EconomyServiceSD theService, int amount)
        {
            if (Services.ContainsKey(theService.ID) == false)
            {
                Services[theService.ID] = 0;
            }

            Services[theService.ID] += amount;
            Services[theService.ID] = Math.Max(Services[theService.ID], 0);
        }

        public void MultiplyDivideBy(long multiplier = 1, long divider = 1)
        {
            var updatedDictionary = new Dictionary<Guid, long>();
            foreach (var entry in Items)
            {
                var result = entry.Value;
                result *= multiplier;
                result /= divider;
                updatedDictionary[entry.Key] = result;
            }
            Items = updatedDictionary;

            updatedDictionary = new Dictionary<Guid, long>();
            foreach (var entry in Services)
            {
                var result = entry.Value;
                result *= multiplier;
                result /= divider;
                updatedDictionary[entry.Key] = result;
            }
            Services = updatedDictionary;
        }
    }
}
