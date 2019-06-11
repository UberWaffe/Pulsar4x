using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class TradeGoodLibrary
    {
        private List<TradeGoodSD> _goods;
        private Dictionary<Guid, TradeGoodSD> _definitions;

        public TradeGoodLibrary(List<TradeGoodSD> theGoods)
        {
            _goods = theGoods;
            _definitions = new Dictionary<Guid, TradeGoodSD>();

            foreach (var entry in theGoods)
            {
                _definitions.Add(entry.ID, entry);
            }
        }

        public List<TradeGoodSD> GetAll()
        {
            return _goods;
        }

        public TradeGoodSD Get(string nameOfTradeGood)
        {
            if (_goods.Any(tg => tg.Name == nameOfTradeGood))
            {
                return _goods.Single(tg => tg.Name == nameOfTradeGood);
            }

            throw new Exception("Trade good with the name " + nameOfTradeGood + " not found in TradeGoodLibrary. Was the trade good properly loaded?");
        }

        public TradeGoodSD Get(Guid guidOfTradeGood)
        {
            return _definitions[guidOfTradeGood];
        }
    }
}
