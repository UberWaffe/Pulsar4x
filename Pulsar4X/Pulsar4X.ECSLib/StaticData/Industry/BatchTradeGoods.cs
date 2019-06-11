using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchTradeGoods
    {
        public Dictionary<Guid, long> FullItems { get; private set;  } = new Dictionary<Guid, long>();

        public BatchTradeGoods()
        {
            FullItems = new Dictionary<Guid, long>();
        }

        public BatchTradeGoods(BatchTradeGoods batchToCopy)
        {
            FullItems = new Dictionary<Guid, long>();

            foreach (var entry in batchToCopy.FullItems)
            {
                FullItems.Add(entry.Key, entry.Value);
            }
        }

        public void AddTradeGood(TradeGoodSD theGood, int amount)
        {
            if (FullItems.ContainsKey(theGood.ID) == false)
            {
                FullItems[theGood.ID] = 0;
            }

            FullItems[theGood.ID] += Math.Abs(amount);
        }

        public void MultiplyDivideBy(long multiplier = 1, long divider = 1)
        {
            var updatedDictionary = new Dictionary<Guid, long>();
            foreach (var entry in FullItems)
            {
                var result = entry.Value;
                result *= multiplier;
                result /= divider;
                updatedDictionary[entry.Key] = result;
            }
            FullItems = updatedDictionary;
        }
    }
}
