using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchTradeGoods
    {
        public Dictionary<Guid, long> FullItems { get; } = new Dictionary<Guid, long>();

        public BatchTradeGoods()
        {
            FullItems = new Dictionary<Guid, long>();
        }

        public void AddTradeGood(TradeGoodSD theGood, long amount)
        {
            FullItems[theGood.ID] += Math.Abs(amount);
        }
    }
}
