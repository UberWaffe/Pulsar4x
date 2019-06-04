using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchTradeGoods
    {
        public Dictionary<Guid, int> FullItems { get; } = new Dictionary<Guid, int>();

        public BatchTradeGoods()
        {
            FullItems = new Dictionary<Guid, int>();
        }

        public void AddTradeGood(TradeGoodSD theGood, int amount)
        {
            if (FullItems.ContainsKey(theGood.ID) == false)
            {
                FullItems[theGood.ID] = 0;
            }

            FullItems[theGood.ID] += Math.Abs(amount);
        }
    }
}
