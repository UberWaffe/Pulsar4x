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

        public long NumberOfIndustry { get; private set; }

        public IndustrySector(IndustrySD instance, long size = 0)
        {
            _data = instance;
            NumberOfIndustry = size;

            if (_data.BatchRecipe == null) _data.BatchRecipe = new BatchRecipe();
        }

        public BatchTradeGoods ConsumptionResult()
        {
            return _data.BatchRecipe.InputGoods;
        }

        public BatchTradeGoods ProductionResult()
        {
            return _data.BatchRecipe.ResultGoods;
        }

        public void ChangeSectorSize(long newSize)
        {
            NumberOfIndustry = newSize;
        }
    }


}