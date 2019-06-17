using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class BatchRecipe
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public BatchTradeGoods InputGoods { get; private set; }
        public BatchTradeGoods ResultGoods { get; private set; }
        public int Priority { get; private set; }
        public long WorkRequired { get; private set; }

        public BatchRecipe() : this(Guid.NewGuid(), "", new BatchTradeGoods(), new BatchTradeGoods(), 1, 1)
        { }

        public BatchRecipe(Guid id, string name, BatchTradeGoods inputGoods, BatchTradeGoods resultGoods, int priority = 1, long effort = 1)
        {
            Id = id;
            Name = name;
            InputGoods = inputGoods;
            ResultGoods = resultGoods;
            Priority = priority;
            WorkRequired = effort;
        }
    }
}
