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