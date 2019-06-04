using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib
{
    public class IndustrySector
    {
        private IndustrySD _data;

        public IndustrySector(IndustrySD instance)
        {
            _data = instance;

            if (_data.Input == null) _data.Input = new BatchTradeGoods();
            if (_data.Output == null) _data.Output = new BatchTradeGoods();
        }

        public BatchTradeGoods ConsumptionResult()
        {
            return _data.Input;
        }

        public BatchTradeGoods ProductionResult()
        {
            return _data.Output;
        }
    }


}