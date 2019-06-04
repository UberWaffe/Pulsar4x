using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct IndustrySD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public BatchRecipe BatchRecipe;
    }
}
