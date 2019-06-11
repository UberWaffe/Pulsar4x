using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class IndustryLibrary
    {
        private Dictionary<Guid, IndustrySD> _industries;

        public IndustryLibrary()
        {
            _industries = new Dictionary<Guid, IndustrySD>();
        }

        public void Add(IndustrySD theIndustry)
        {
            if (_industries.ContainsKey(theIndustry.ID))
            {
                throw new Exception(string.Format("There is already an industry with the ID {0} in IndustryLibrary.", theIndustry.ID.ToString()));
            }
            if (_industries.Any(ind => ind.Value.Name == theIndustry.Name))
            {
                throw new Exception(string.Format("There is already an industry with the name {0} in IndustryLibrary. It has the ID {1}.", theIndustry.Name, _industries.First(ind => ind.Value.Name == theIndustry.Name).Value.ID.ToString()));
            }

            _industries.Add(theIndustry.ID, theIndustry);
        }

        public List<IndustrySD> GetAll()
        {
            var theList = new List<IndustrySD>();

            foreach (var entry in _industries)
            {
                theList.Add(entry.Value);
            }

            return theList;
        }

        public IndustrySD Get(Guid id)
        {
            if (_industries.ContainsKey(id))
            {
                return _industries[id];
            }

            throw new Exception(string.Format("Industry with the id {0} not found in IndustryLibrary. Was the recipe properly loaded?", id));
        }

        public IndustrySD Get(string industryName)
        {
            if (_industries.Any(ind => ind.Value.Name == industryName))
            {
                return _industries.Single(ind => ind.Value.Name == industryName).Value;
            }

            throw new Exception(string.Format("Industry with the name {0} not found in IndustryLibrary. Was the recipe properly loaded?", industryName));
        }
    }
}
