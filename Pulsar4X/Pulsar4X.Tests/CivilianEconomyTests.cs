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
    [TestFixture, Description("Civilian Economy Tests")]
    class CivilianEconomyTests
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
        public void CivilianIndustryShouldBeAbleToProduceTradeGoods()
        {
            Assert.Fail();
        }

    }
}
