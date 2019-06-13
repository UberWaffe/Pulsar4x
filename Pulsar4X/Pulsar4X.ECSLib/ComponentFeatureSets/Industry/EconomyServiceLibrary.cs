using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class EconomyServiceLibrary
    {
        private List<EconomyServiceSD> _services;
        private Dictionary<Guid, EconomyServiceSD> _definitions;

        public EconomyServiceLibrary(List<EconomyServiceSD> theServices)
        {
            _services = theServices;
            _definitions = new Dictionary<Guid, EconomyServiceSD>();

            foreach (var entry in theServices)
            {
                _definitions.Add(entry.ID, entry);
            }
        }

        public List<EconomyServiceSD> GetAll()
        {
            return _services;
        }

        public EconomyServiceSD Get(string nameOfService)
        {
            if (_services.Any(tg => tg.Name == nameOfService))
            {
                return _services.Single(tg => tg.Name == nameOfService);
            }

            throw new Exception("SService with the name " + nameOfService + " not found in EconomyServiceLibrary. Was the service properly loaded?");
        }

        public EconomyServiceSD Get(Guid guidOfService)
        {
            return _definitions[guidOfService];
        }
    }
}
