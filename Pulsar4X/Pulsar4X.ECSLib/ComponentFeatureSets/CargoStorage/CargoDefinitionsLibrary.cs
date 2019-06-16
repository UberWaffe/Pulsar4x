using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage
{
    public interface ICargoDefinitionsLibrary
    {
        void LoadDefinitions(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<TradeGoodSD> goodsCargo,
            List<ICargoable> otherCargo);

        void LoadMineralDefinitions(List<MineralSD> minerals);
        void LoadMaterialsDefinitions(List<ProcessedMaterialSD> materials);
        void LoadTradeGoodsDefinitions(List<TradeGoodSD> goodsCargo);
        void LoadOtherDefinitions(List<ICargoable> otherCargo);

        Dictionary<Guid, ICargoable> GetAll();
        List<ICargoable> GetAllList();

        object GetAny(Guid id);
        ICargoable GetCargo(Guid id);
        Guid GetCargoType(Guid id);

        bool IsOther(Guid id);
        ICargoable GetOther(string nameOfCargo);
        ICargoable GetOther(Guid guidOfCargo);

        bool IsMineral(Guid id);
        MineralSD GetMineral(string name);
        MineralSD GetMineral(Guid guid);
        Dictionary<Guid, MineralSD> GetMinerals();
        List<MineralSD> GetMineralsList();

        bool IsMaterial(Guid id);
        ProcessedMaterialSD GetMaterial(string name);
        ProcessedMaterialSD GetMaterial(Guid guid);
        Dictionary<Guid, ProcessedMaterialSD> GetMaterials();
        List<ProcessedMaterialSD> GetMaterialsList();

        bool IsTradeGood(Guid id);
        TradeGoodSD GetTradeGood(string name);
        TradeGoodSD GetTradeGood(Guid guid);
        Dictionary<Guid, TradeGoodSD> GetTradeGoods();
        List<TradeGoodSD> GetTradeGoodsList();
    }

    public class CargoDefinitionsLibrary : ICargoDefinitionsLibrary
    {
        private Dictionary<Guid, ICargoable> _definitions;
        private Dictionary<Guid, MineralSD> _minerals;
        private Dictionary<Guid, TradeGoodSD> _goods;
        private Dictionary<Guid, ProcessedMaterialSD> _processedMaterials;

        public CargoDefinitionsLibrary() : this(new List<MineralSD>(),
            new List<ProcessedMaterialSD>(),
            new List<TradeGoodSD>(),
            new List<ICargoable>())
        {
        }

        public CargoDefinitionsLibrary(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<TradeGoodSD> goodsCargo,
            List<ICargoable> otherCargo)
        {
            _definitions = new Dictionary<Guid, ICargoable>();
            _minerals = new Dictionary<Guid, MineralSD>();
            _goods = new Dictionary<Guid, TradeGoodSD>();
            _processedMaterials = new Dictionary<Guid, ProcessedMaterialSD>();

            LoadDefinitions(minerals, processedMaterials, goodsCargo, otherCargo);
        }

        
        public void LoadDefinitions(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<TradeGoodSD> goodsCargo,
            List<ICargoable> otherCargo)
        {
            LoadMineralDefinitions(minerals);
            LoadMaterialsDefinitions(processedMaterials);
            LoadOtherDefinitions(otherCargo);
        }

        #region Load definitions
        public void LoadMineralDefinitions(List<MineralSD> minerals)
        {
            if (minerals != null)
            {
                foreach (var entry in minerals)
                {
                    _definitions[entry.ID] = entry;
                    _minerals[entry.ID] = entry;
                }
            }
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterialSD> materials)
        {
            if (materials != null)
            {
                foreach (var entry in materials)
                {
                    _definitions[entry.ID] = entry;
                    _processedMaterials[entry.ID] = entry;
                }
            }
        }

        public void LoadTradeGoodsDefinitions(List<TradeGoodSD> goodsCargo)
        {
            if (goodsCargo != null)
            {
                foreach (var entry in goodsCargo)
                {
                    _definitions[entry.ID] = entry;
                    _goods[entry.ID] = entry;
                }
            }
        }

        public void LoadOtherDefinitions(List<ICargoable> otherCargo)
        {
            if (otherCargo != null)
            {
                foreach (var entry in otherCargo)
                {
                    _definitions[entry.ID] = entry;
                }
            }
        }
        #endregion

        #region All
        public object GetAny(Guid id)
        {
            if (_minerals.ContainsKey(id))
                return _minerals[id];

            if (_processedMaterials.ContainsKey(id))
                return _processedMaterials[id];

            if (_definitions.ContainsKey(id))
                return _definitions[id];

            return null;
        }

        public ICargoable GetCargo(Guid id)
        {
            return _definitions[id];
        }

        public Guid GetCargoType(Guid id)
        {
            return _definitions[id].CargoTypeID;
        }

        public Dictionary<Guid, ICargoable> GetAll()
        {
            return _definitions;
        }

        public List<ICargoable> GetAllList()
        {
            return _definitions.Values.ToList();
        }
        #endregion

        #region Other Goods
        public bool IsOther(Guid id)
        {
            return _definitions.ContainsKey(id) && (IsMineral(id) == false) && (IsMaterial(id) == false);
        }

        public ICargoable GetOther(string nameOfCargo)
        {
            if (_definitions.Values.Any(tg => tg.Name == nameOfCargo))
            {
                return _definitions.Values.Single(tg => tg.Name == nameOfCargo);
            }

            throw new Exception("Cargo item with the name " + nameOfCargo + " not found in TradeGoodLibrary. Was the trade good properly loaded?");
        }

        public ICargoable GetOther(Guid guidOfCargo)
        {
            return _definitions[guidOfCargo];
        }
        #endregion

        #region Minerals
        public bool IsMineral(Guid id)
        {
            return _minerals.ContainsKey(id);
        }

        public MineralSD GetMineral(string name)
        {
            var result = GetOther(name);
            return _minerals[result.ID];
        }

        public MineralSD GetMineral(Guid guid)
        {
            var result = GetOther(guid);
            return _minerals[result.ID];
        }

        public Dictionary<Guid, MineralSD> GetMinerals()
        {
            return _minerals;
        }

        public List<MineralSD> GetMineralsList()
        {
            return _minerals.Values.ToList();
        }
        #endregion

        #region Materials
        public bool IsMaterial(Guid id)
        {
            return _processedMaterials.ContainsKey(id);
        }

        public ProcessedMaterialSD GetMaterial(string name)
        {
            var result = GetOther(name);
            return _processedMaterials[result.ID];
        }

        public ProcessedMaterialSD GetMaterial(Guid guid)
        {
            var result = GetOther(guid);
            return _processedMaterials[result.ID];
        }

        public Dictionary<Guid, ProcessedMaterialSD> GetMaterials()
        {
            return _processedMaterials;
        }

        public List<ProcessedMaterialSD> GetMaterialsList()
        {
            return _processedMaterials.Values.ToList();
        }
        #endregion

        #region Trade Goods
        public bool IsTradeGood(Guid id)
        {
            return _goods.ContainsKey(id);
        }

        public TradeGoodSD GetTradeGood(string name)
        {
            var result = GetOther(name);
            return _goods[result.ID];
        }

        public TradeGoodSD GetTradeGood(Guid guid)
        {
            var result = GetOther(guid);
            return _goods[result.ID];
        }

        public Dictionary<Guid, TradeGoodSD> GetTradeGoods()
        {
            return _goods;
        }

        public List<TradeGoodSD> GetTradeGoodsList()
        {
            return _goods.Values.ToList();
        }
        #endregion
    }
}
