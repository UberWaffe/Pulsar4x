using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Industry;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Industrial Economy Json Importer Tests")]
    class IndustrialImportTests
    {
        private Game _game;

        [SetUp]
        public void Init()
        {
            var gameSettings = new NewGameSettings();
            gameSettings.MaxSystems = 10;
            _game = new Game(gameSettings);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
        }

        [Test]
        public void IndustrialJsonImporter_When_ImportingTradeGood_Should_ImportAllBasicParameters()
        {
            var rawInputString = @"{
          ""ID"": ""5a6d46bc-38ea-471c-8094-6a8c1eab45d9"",
          ""Name"": ""Mine"",
          ""Description"": ""Mines Resources"",
          ""CargoTypeID"": ""a1e1894e-5442-4323-88a5-987858edd15d"",
          ""Mass"": ""50000"",
        }";

            var result = IndustryImporter.ConvertObjectToTradeGood(rawInputString);

            Assert.AreEqual(Guid.Parse("5a6d46bc-38ea-471c-8094-6a8c1eab45d9"), result.ID);
            Assert.AreEqual("Mine", result.Name);
            Assert.AreEqual("Mines Resources", result.Description);
            Assert.AreEqual(Guid.Parse("a1e1894e-5442-4323-88a5-987858edd15d"), result.CargoTypeID);
            Assert.AreEqual(50000, result.Mass);
        }
    }
}
