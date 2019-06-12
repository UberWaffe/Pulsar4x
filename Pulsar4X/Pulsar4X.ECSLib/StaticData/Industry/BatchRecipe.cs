using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchRecipe
    {
        public string Name { get; private set; }
        public BatchTradeGoods InputGoods { get; private set; }
        public BatchTradeGoods ResultGoods { get; private set; }
        public int Priority { get; private set; }
        public long WorkRequired { get; private set; }

        public BatchRecipe()
        {
            Name = "";
            InputGoods = new BatchTradeGoods();
            ResultGoods = new BatchTradeGoods();
            Priority = 1;
            WorkRequired = 1;
        }

        public BatchRecipe(string name, BatchTradeGoods inputGoods, BatchTradeGoods resultGoods, int priority = 1, long effort = 1)
        {
            Name = name;
            InputGoods = inputGoods;
            ResultGoods = resultGoods;
            Priority = priority;
            WorkRequired = effort;
        }
    }
}
