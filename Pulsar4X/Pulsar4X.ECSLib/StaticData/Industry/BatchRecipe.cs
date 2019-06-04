using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchRecipe
    {
        public BatchTradeGoods InputGoods { get; private set; }
        public BatchTradeGoods ResultGoods { get; private set; }

        public BatchRecipe()
        {
            InputGoods = new BatchTradeGoods();
            ResultGoods = new BatchTradeGoods();
        }

        public BatchRecipe(BatchTradeGoods inputGoods, BatchTradeGoods resultGoods)
        {
            InputGoods = inputGoods;
            ResultGoods = resultGoods;
        }
    }
}
