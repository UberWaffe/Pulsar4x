using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib
{
    public class IndustryAllSectors
    {
        private List<IndustrySector> _sectors;

        public IndustryAllSectors()
        {
            _sectors = new List<IndustrySector>();
        }

        public IndustryAllSectors(List<IndustrySD> industries)
        {
            _sectors = new List<IndustrySector>();

            foreach (var entry in industries)
            {
                _sectors.Add(new IndustrySector(entry));
            }
        }

        public void SetCount(string name, int count)
        {
            var targetIndustry = _sectors.SingleOrDefault(sect => sect.IsIndustry(name));
            targetIndustry.SetCount(count);
        }

        public List<IndustrySector> GetSectorsInOrderOfDescendingPriority()
        {
            var orderedList = _sectors.OrderByDescending(ind => ind.GetRecipe().Priority);
            return orderedList.ToList();
        }
    }


}