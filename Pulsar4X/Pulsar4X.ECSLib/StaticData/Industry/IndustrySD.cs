﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct IndustrySD
    {
        public Guid ID;
        public string Name;
        public string Description;
        public long WorkCapacity;
        public long Priority;

        public List<BatchRecipe> BatchRecipes;
    }
}
