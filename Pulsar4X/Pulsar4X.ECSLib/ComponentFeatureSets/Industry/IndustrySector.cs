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

        public void SetCount(int count)
        {
            NumberOfIndustry = count;
        }

        public BatchRecipe GetRecipe()
        {
            return _data.BatchRecipe;
        }

        public BatchTradeGoods GetInputForCount(long count)
        {
            var result = new BatchTradeGoods(GetRecipe().InputGoods);
            result.MultiplyDivideBy(count, 1);

            return result;
        }

        public BatchTradeGoods GetInput()
        {
            return GetInputForCount(NumberOfIndustry);
        }

        public BatchTradeGoods GetOutputForCount(long count)
        {
            var result = new BatchTradeGoods(GetRecipe().ResultGoods);
            result.MultiplyDivideBy(count, 1);

            return result;
        }

        public BatchTradeGoods GetOutput()
        {
            return GetOutputForCount(NumberOfIndustry);
        }

        public bool IsIndustry(string name)
        {
            return (_data.Name == name);
        }
    }


}