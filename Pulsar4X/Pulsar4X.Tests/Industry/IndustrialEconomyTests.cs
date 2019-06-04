using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Industrial Economy Tests")]
    class IndustrialEconomyTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _workerSpecies;

        [SetUp]
        public void Init()
        {
            var gameSettings = new NewGameSettings();
            gameSettings.MaxSystems = 10;
            _game = new Game(gameSettings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _entityManager = new EntityManager(_game);

            _faction = FactionFactory.CreateFaction(_game, "Plebians");
            _workerSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _workerSpecies = null;
        }

        [Test]
        public void ForACargoHasRequiredItemCheckTheResultShouldBeFalseIfItDoesnotContainTheTargetCargoType()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void ForACargoHasRequiredItemCheckTheResultShouldBeFalseIfItDoesnotContainTheTargetCargo()
        {
            var cookies = SetupCookieTradeGood();
            var biscuits = SetupCookieTradeGood();
            biscuits.CargoTypeID = cookies.CargoTypeID;

            var cookiePile = new CargoStorageDB();
            StorageSpaceProcessor.AddCargo(cookiePile, biscuits, 1);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }
        [Test]
        public void CivilianIndustryShouldBeAbleToProduceTradeGoods()
        {
            var tradeGoodsDefinitions = new Dictionary<Guid, TradeGoodSD>();

            var cookies = new TradeGoodSD();
            cookies.Name = "Cookies";
            cookies.Description = "Tastes like carpal tunnel and time.";
            cookies.ID = Guid.NewGuid();
            cookies.CargoTypeID = Guid.NewGuid();
            cookies.Mass = 1;

            tradeGoodsDefinitions.Add(cookies.ID, cookies);

            var cookieClickery = new IndustrySD();
            cookieClickery.Name = "Cookie Clickery";
            cookieClickery.Description = "It is like a bakery, but with less flour and more eldritch horrors.";
            cookieClickery.ID = Guid.NewGuid();
            cookieClickery.Output = new BatchTradeGoods();
            cookieClickery.Output.AddTradeGood(cookies, 1);

            var cookieSector = new IndustrySector(cookieClickery);

            var cookiePile = new CargoStorageDB();

            var cookieCheck = new Dictionary<ICargoable, int>();
            cookieCheck.Add(cookies, 1);

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);

            var industry = new IndustryProcessor(tradeGoodsDefinitions);
            industry.ProcessIndustrySector(cookieSector, cookiePile);

            hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsTrue(hasCookies);
        }

    }
}
