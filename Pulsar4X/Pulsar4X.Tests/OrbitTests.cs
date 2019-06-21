﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests kepler form velocity")]
    public class OrbitTests
    {

        public void TestOrbitEpoch()
        {
            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            Vector3 position = new Vector3() { X = 0.57 }; //Halley's Comet at periapse aprox
            Vector3 velocity = new Vector3() { Y = Distance.KmToAU(54) };

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);
            double sgp = OrbitMath.CalculateStandardGravityParameter(parentMass, objMass);

            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector3 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());
        }


        [Test]
        public void TestPreciseOrbitalSpeed()
        {
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgpKm = OrbitMath.CalculateStandardGravityParameterInKm3S2(parentMass, objMass);
            var speedKm = OrbitMath.InstantaneousOrbitalSpeed(sgpKm, 405400, 384399);
            Assert.AreEqual(0.97, speedKm, 0.01);
        }


        [Test]
        public void TestAngles() 
        {
            var e = 0.5;
            var a = 100;
            var p = EllipseMath.SemiLatusRectum(a, e);
            double angleDelta = 0.00001;
            var i = 0;
            for (double angle = 0; angle < Math.PI; angle += 0.0174533)
            {
                var r = OrbitMath.RadiusAtAngle(angle, p, e);
                var theta = OrbitMath.AngleAtRadus(r, p, e);
                Assert.AreEqual(angle, theta, angleDelta,  "inc: " + i + " r: " + r);
                i++;
            }
        }

        [Test]
        public void TestKeplerElementsFromVectors()
        {
            Vector3 position = new Vector3() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector3 velocity = new Vector3() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = OrbitMath.CalculateStandardGravityParameter(parentMass, objMass);
            KeplerElements elements = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, new DateTime());

            Vector3 postionKm = new Vector3() { X = 405400 };
            Vector3 velocityKm = new Vector3() { Y = 0.97 };
            double sgpKm = OrbitMath.CalculateStandardGravityParameterInKm3S2(parentMass, objMass);

            KeplerElements elementsKm = OrbitMath.KeplerFromPositionAndVelocity(sgpKm, postionKm, velocityKm, new DateTime());

            //check that the function is unit agnostic.
            Assert.AreEqual(Distance.AuToKm(elements.SemiMajorAxis), elementsKm.SemiMajorAxis, 0.001);
            Assert.AreEqual(elements.Eccentricity, elementsKm.Eccentricity, 1.0e-9); //this is where inacuarcy from units stars creaping in, not much can do about that.
            Assert.AreEqual(Distance.AuToKm(elements.Apoapsis), elementsKm.Apoapsis, 0.001);
            Assert.AreEqual(Distance.AuToKm(elements.Periapsis), elementsKm.Periapsis, 0.001);

            //var ta = OrbitMath.TrueAnomalyFromEccentricAnomaly(elements.Eccentricity, elements.)
            var speedAU = OrbitMath.InstantaneousOrbitalSpeed(sgp, position.Length(), elements.SemiMajorAxis);
            //var speedVectorAU = OrbitMath.PreciseOrbitalVelocityVector(sgp, position, elements.SemiMajorAxis, elements.Eccentricity, elements.LoAN + elements.AoP);
            //Assert.AreEqual(speedAU, speedVectorAU.Length());

            Assert.AreEqual(elementsKm.Apoapsis + elementsKm.Periapsis, elementsKm.SemiMajorAxis * 2, 0.001);


            var speedKm = velocityKm.Length();
            var speedKm2 = OrbitMath.InstantaneousOrbitalSpeed(sgpKm, postionKm.Length(), elementsKm.SemiMajorAxis);
            Assert.AreEqual(speedKm, speedKm2, 0.001);


            Assert.GreaterOrEqual(elements.Apoapsis, elements.Periapsis);
            Assert.GreaterOrEqual(elementsKm.Apoapsis, elementsKm.Periapsis);

            //below was some experimentation with different ways of calculating things, and an attempt to use decimal for Eccentricity.
            //not sure it's worth the minor slowdown or complication, didn't seem to fix the problem I was seeing in anycase. 
            #region experimentation 

            var H = Vector3.Cross(postionKm, velocityKm).Length();
            var p = H * H / sgpKm;
            var sma = 1 / (2 / postionKm.Length() - velocityKm.Length() * velocityKm.Length() / sgpKm); //  semi-major axis


            decimal E;
            double Periapsis;
            double Apoapsis;

            if (sma < (double)decimal.MaxValue)
            {
                decimal smad = (decimal)sma;
                E = GMath.Sqrt(1 - (decimal)p / smad);

                decimal PlusMinus = smad * E;
                Periapsis = (double)(smad - PlusMinus);
                Apoapsis = (double)(smad + PlusMinus);
            }
            else
            {
                E = (decimal)Math.Sqrt(1 - p / sma);  // eccentricity

                double PlusMinus = sma * (double)E;
                Periapsis = sma - PlusMinus;
                Apoapsis = sma + PlusMinus;

            }
            Assert.AreEqual(Periapsis + Apoapsis, sma * 2, 0.0001);
            var peStr = Periapsis.ToString("R");
            var apStr = Apoapsis.ToString("R");
            //Assert.AreEqual(elementsKm.SemiMajorAxis, sma);
            var difference1 = (Periapsis + Apoapsis) - sma * 2;
            var difference2 = (elementsKm.Apoapsis + elementsKm.Periapsis) - elementsKm.SemiMajorAxis * 2;

            #endregion

            if (velocity.Z == 0)
                Assert.IsTrue(elements.Inclination == 0);


        }


        [Test]
        public void OrbitsFromVectorTests()
        {
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            Vector3 position = new Vector3() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector3 velocity = new Vector3() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            //test high eccentricity orbit
            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector3() { X = 0.57 }; //Halley's Comet at periapse aprox
            velocity = new Vector3() { Y = Distance.KmToAU(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);



            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector3() { X = 0.25, Y = 0.25 }; 
            velocity = new Vector3() { Y = Distance.KmToAU(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            parentMass = 1.989e30;
            objMass = 10000;
            position = new Vector3() { X = 0.25, Y = 0.25 };
            velocity = new Vector3() { X = Distance.KmToAU(0), Y = Distance.KmToAU(1) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

        }

        [Test]
        public void Distance_AuToMt_When_Given1_Should_Give149597870700()
        {
            Assert.AreEqual(149597870700d, Distance.AuToMt(1.0d));
        }

        [Test]
        public void OrbitMath_CalculateAngularMomentum_When_ZeroXPositiveYVelocity_Should_GiveCorrectResults()
        {
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = Distance.KmToM(2) };
            var expectedResult = new Vector3(
                0,
                0,
                74798935350000.0
            );
            var calculatedResult = OrbitMath.CalculateAngularMomentum(position, velocity);
            Assert.IsTrue(TestVectorsAreEqual(expectedResult, calculatedResult, 1.0d));
        }

        [Test]
        public void OrbitMath_CalculateAngularMomentum_When_ZeroXNegativeYVelocity_Should_GiveCorrectResults()
        {
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = Distance.KmToM(-1) };
            var expectedResult = new Vector3(
                0,
                0,
                -37399467675000.0d
            );
            var calculatedResult = OrbitMath.CalculateAngularMomentum(position, velocity);
            Assert.IsTrue(TestVectorsAreEqual(expectedResult, calculatedResult, 1.0d));
        }

        [Test]
        public void OrbitMath_KeplerFromPositionAndVelocity_When_ZeroXPositiveYVelocity_Should_GiveCorrectResults()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;

            // To help visualize vectors, a useful tool at : https://academo.org/demos/3d-vector-plotter/
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToKm(0.25), Y = Distance.AuToKm(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = 1 };
            var expectedKeplerResult = new KeplerElements()
            {
                SemiMajorAxis = Distance.MToKm(26450687774.528255),
                Eccentricity = 0.9998007596175803,
                Inclination = Angle.ToRadians(0.0),
                LongdOfAN = Angle.ToRadians(0.0),
                ArgumentOfPeriapsis = Angle.ToRadians(225.01141904591285),
                MeanAnomalyAtEpoch = Angle.ToRadians(177.71233527026365),
                TrueAnomalyAtEpoch = Angle.ToRadians(179.98858095408718),
                Periapsis = Distance.MToKm(5270045.1474609375),
                Apoapsis = Distance.MToKm(52896105503.90905)
            };
            var calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            Assert.IsTrue(TestKeplerOrbitSpecificResult(calculatedKepler, expectedKeplerResult));
        }

        [Test]
        public void OrbitMath_KeplerFromPositionAndVelocity_When_ZeroXNegativeYVelocity_Should_GiveCorrectResults()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;

            // To help visualize vectors, a useful tool at : https://academo.org/demos/3d-vector-plotter/
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToKm(0.25), Y = Distance.AuToKm(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = -1 };
            var expectedKeplerResult = new KeplerElements()
            {
                SemiMajorAxis = Distance.MToKm(26466512098.241333),
                Eccentricity = 0.999203276935673,
                Inclination = Angle.ToRadians(180.00000000000017),
                LongdOfAN = Angle.ToRadians(45.00000153199437),
                ArgumentOfPeriapsis = Angle.ToRadians(179.95429650816112),
                MeanAnomalyAtEpoch = Angle.ToRadians(184.57578603454385),
                TrueAnomalyAtEpoch = Angle.ToRadians(180.04570350068457),
                Periapsis = Distance.MToKm(21086480.62095642),
                Apoapsis = Distance.MToKm(52911937715.861725)
            };
            var calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            Assert.IsTrue(TestKeplerOrbitSpecificResult(calculatedKepler, expectedKeplerResult));
        }

        [Test]
        public void OrbitMath_KeplerFromPositionAndVelocity_When_PositiveXZeroYVelocity_Should_GiveCorrectResults()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;

            // To help visualize vectors, a useful tool at : https://academo.org/demos/3d-vector-plotter/
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToKm(0.25), Y = Distance.AuToKm(0.25) };
            Vector3 velocity = new Vector3() { X = 1, Y = 0 };
            var expectedKeplerResult = new KeplerElements()
            {
                SemiMajorAxis = Distance.MToKm(26450687774.528263),
                Eccentricity = 0.9998007596281745,
                Inclination = Angle.ToRadians(0),
                LongdOfAN = Angle.ToRadians(0),
                ArgumentOfPeriapsis = Angle.ToRadians(225.01142056906158),
                MeanAnomalyAtEpoch = Angle.ToRadians(177.7123352094565),
                TrueAnomalyAtEpoch = Angle.ToRadians(179.98858095408733),
                Periapsis = Distance.MToKm(179.98858095408733),
                Apoapsis = Distance.MToKm(52896105504.189285)
            };
            var calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            Assert.IsTrue(TestKeplerOrbitSpecificResult(calculatedKepler, expectedKeplerResult));
        }

        [Test]
        public void OrbitMath_KeplerFromPositionAndVelocity_When_NegativeXZeroYVelocity_Should_GiveCorrectResults()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;

            // To help visualize vectors, a useful tool at : https://academo.org/demos/3d-vector-plotter/
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToKm(0.25), Y = Distance.AuToKm(0.25) };
            Vector3 velocity = new Vector3() { X = -1, Y = 0 };
            var expectedKeplerResult = new KeplerElements()
            {
                SemiMajorAxis = Distance.MToKm(26450687774.528255),
                Eccentricity = 0.9998007596175803,
                Inclination = Angle.ToRadians(180.00000000000017),
                LongdOfAN = Angle.ToRadians(45.00000153199437),
                ArgumentOfPeriapsis = Angle.ToRadians(179.9885809629329),
                MeanAnomalyAtEpoch = Angle.ToRadians(182.28766472973598),
                TrueAnomalyAtEpoch = Angle.ToRadians(180.0114190459128),
                Periapsis = Distance.MToKm(5270045.1474609375),
                Apoapsis = Distance.MToKm(52896105503.90905)
            };
            var calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            Assert.IsTrue(TestKeplerOrbitSpecificResult(calculatedKepler, expectedKeplerResult));
        }

        [Test]
        public void FailingOrbitsFromVectorTests()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;
			
            // To help visualize vectors, a useful tool at : https://academo.org/demos/3d-vector-plotter/
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToKm(0.25), Y = Distance.AuToKm(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = 1 }; //passes
            var calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = Distance.KmToAU(0), Y = -Distance.KmToAU(2) }; //fails
            calculatedKepler = CalculateKeplerOrbitElements(parentMass, objMass, position, velocity);
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = Distance.KmToAU(1), Y = Distance.KmToAU(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = -Distance.KmToAU(1), Y = Distance.KmToAU(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

        }

        public KeplerElements CalculateKeplerOrbitElements(double parentMass, double objMass, Vector3 position, Vector3 velocity)
        {
            KeplerElements ke = OrbitMath.KeplerFromPositionAndVelocity(parentMass, objMass, position, velocity, new DateTime());
            
            return ke;
        }

        public bool TestKeplerOrbitSpecificResult(KeplerElements keplerResults, KeplerElements expectedKeplerResults)
        {
            var requiredAccuracyForRadians = 0.01; //0.0000000001
            var requiredAccuracyForKm = 2.0; //0.001
            Assert.AreEqual(expectedKeplerResults.SemiMajorAxis, keplerResults.SemiMajorAxis, requiredAccuracyForKm); //a
            Assert.AreEqual(expectedKeplerResults.Eccentricity, keplerResults.Eccentricity, requiredAccuracyForRadians); //e
            Assert.AreEqual(expectedKeplerResults.Inclination, keplerResults.Inclination, requiredAccuracyForRadians); //i
            Assert.AreEqual(expectedKeplerResults.LongdOfAN, keplerResults.LongdOfAN, requiredAccuracyForRadians); //Ω
            Assert.AreEqual(expectedKeplerResults.ArgumentOfPeriapsis, keplerResults.ArgumentOfPeriapsis, requiredAccuracyForRadians); //ω
            // Assert.AreEqual(expectedKeplerResults.MeanMotion, keplerResults.MeanMotion, requiredAccuracy); //n
            Assert.AreEqual(expectedKeplerResults.MeanAnomalyAtEpoch, keplerResults.MeanAnomalyAtEpoch, requiredAccuracyForRadians); //M0
            Assert.AreEqual(expectedKeplerResults.TrueAnomalyAtEpoch, keplerResults.TrueAnomalyAtEpoch, requiredAccuracyForRadians); //v
            Assert.AreEqual(expectedKeplerResults.Periapsis, keplerResults.Periapsis, requiredAccuracyForKm); //q
            Assert.AreEqual(expectedKeplerResults.Apoapsis, keplerResults.Apoapsis, requiredAccuracyForKm); //Q

            return true;
        }

        public bool TestVectorsAreEqual(Vector3 expected, Vector3 actual, double requiredAccuracy = 0.01)
        {
            Assert.AreEqual(expected.X, actual.X, requiredAccuracy);
            Assert.AreEqual(expected.Y, actual.Y, requiredAccuracy);
            Assert.AreEqual(expected.Z, actual.Z, requiredAccuracy);

            return true;
        }

        public void TestOrbitDBFromVectors(double parentMass, double objMass, Vector3 position, Vector3 velocity)
        {
            double angleΔ = 0.01; //0.0000000001
            double sgp = OrbitMath.CalculateStandardGravityParameter(parentMass, objMass);
            KeplerElements ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, new DateTime());

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);


            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector3 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());

            //check LoAN
            var objLoAN = Angle.ToRadians(objOrbit.LongitudeOfAscendingNode);
            var keLoAN = ke.LongdOfAN;
            var loANDifference = objLoAN - keLoAN;
            Assert.AreEqual(keLoAN, objLoAN, angleΔ);

            //check AoP
            var objAoP = Angle.ToRadians(objOrbit.ArgumentOfPeriapsis);
            var keAoP = ke.ArgumentOfPeriapsis;
            var difference = objAoP - keAoP;
            Assert.AreEqual(keAoP, objAoP, angleΔ);


            //check MeanAnomalyAtEpoch
            var objM0 = Angle.ToRadians(objOrbit.MeanAnomalyAtEpoch);
            var keM0 = ke.MeanAnomalyAtEpoch;
            Assert.AreEqual(keM0, objM0, angleΔ);
            Assert.AreEqual(objM0, OrbitMath.GetMeanAnomalyFromTime(objM0, objOrbit.MeanMotion, 0), "meanAnomalyError");

            //checkEpoch
            var objEpoch = objOrbit.Epoch;
            var keEpoch = ke.Epoch;
            Assert.AreEqual(keEpoch, objEpoch);

            //check EccentricAnomaly:
            var objE = (OrbitProcessor.GetEccentricAnomaly(objOrbit, objOrbit.MeanAnomalyAtEpoch));
            var keE =   (OrbitMath.GetEccentricAnomalyFromStateVectors(position, ke.SemiMajorAxis, ke.LinierEccentricity, ke.ArgumentOfPeriapsis));
            if (objE != keE)
            {
                var dif = objE - keE;
                //Assert.AreEqual(keE, objE, angleΔ);
            }

            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            var orbtaDeg = Angle.ToDegrees(orbTrueAnom);
            var differenceInRadians = orbTrueAnom - ke.TrueAnomalyAtEpoch;
            var differenceInDegrees = Angle.ToDegrees(differenceInRadians);
            if (ke.TrueAnomalyAtEpoch != orbTrueAnom) 
            { 

                Vector3 eccentVector = OrbitMath.EccentricityVector(sgp, position, velocity);
                var tacalc1 = OrbitMath.TrueAnomaly(eccentVector, position, velocity);
                var tacalc2 = OrbitMath.TrueAnomaly(sgp, position, velocity);

                var diffa = differenceInDegrees;
                var diffb = Angle.ToDegrees(orbTrueAnom - tacalc1);
                var diffc = Angle.ToDegrees(orbTrueAnom - tacalc2);

                var ketaDeg = Angle.ToDegrees(tacalc1);
            }

            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(ke.TrueAnomalyAtEpoch, orbTrueAnom), angleΔ,
                "more than " + angleΔ + " radians difference, at " + differenceInRadians + " \n " +
                "(more than " + Angle.ToDegrees(angleΔ) + " degrees difference at " + differenceInDegrees + ")" + " \n " +
                "ke Angle: " + ke.TrueAnomalyAtEpoch + " obj Angle: " + orbTrueAnom + " \n " +
                "ke Angle: " + Angle.ToDegrees(ke.TrueAnomalyAtEpoch) + " obj Angle: " + Angle.ToDegrees(orbTrueAnom));
                
            Assert.AreEqual(ke.Eccentricity, objOrbit.Eccentricity);
            Assert.AreEqual(ke.SemiMajorAxis, objOrbit.SemiMajorAxis);


            var lenke1 = ke.SemiMajorAxis * 2;
            var lenke2 = ke.Apoapsis + ke.Periapsis;

            var diff = Math.Abs(lenke1 - lenke2);
            Assert.LessOrEqual(diff, 0.1);

            var lendb1 = objOrbit.SemiMajorAxis * 2;
            var lendb2 = objOrbit.Apoapsis + objOrbit.Periapsis;

            diff = Math.Abs(lendb1 - lendb2);
            Assert.LessOrEqual(diff, 0.1);

            diff = Math.Abs(lenke1 - lendb1);
            Assert.LessOrEqual(diff, 0.1);

            diff = Math.Abs(lenke2 - lendb2);
            Assert.LessOrEqual(diff, 0.1);



            var ke_apkm = Distance.AuToKm(ke.Apoapsis);
            var db_apkm = Distance.AuToKm(objOrbit.Apoapsis);
            var differnce = ke_apkm - db_apkm;
            Assert.AreEqual(ke.Apoapsis, objOrbit.Apoapsis); 
            Assert.AreEqual(ke.Periapsis, objOrbit.Periapsis);

            Vector3 posKM = Distance.AuToKm(position);
            Vector3 resultKM = Distance.AuToKm(resultPos);

            double keslr = EllipseMath.SemiLatusRectum(ke.SemiMajorAxis, ke.Eccentricity);
            double keradius = OrbitMath.RadiusAtAngle(ke.TrueAnomalyAtEpoch, keslr, ke.Eccentricity);
            Vector3 kemathPos = OrbitMath.GetRalitivePosition(ke.LongdOfAN, ke.ArgumentOfPeriapsis, ke.Inclination, ke.TrueAnomalyAtEpoch, keradius);
            Vector3 kemathPosKM = Distance.AuToKm(kemathPos);
            Assert.AreEqual(kemathPosKM.Length(), posKM.Length(), 0.01);

            Assert.AreEqual(posKM.Length(), resultKM.Length(), 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.X, resultKM.X, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.Y, resultKM.Y, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.Z, resultKM.Z, 0.01, "TA: " + orbtaDeg);

            if (velocity.Z == 0)
            {
                Assert.IsTrue(ke.Inclination == 0);
                Assert.IsTrue(objOrbit.Inclination == 0);
            }

            //var speedVectorAU = OrbitProcessor.PreciseOrbitalVector(sgp, position, ke.SemiMajorAxis);
            //var speedVectorAU2 = OrbitProcessor.PreciseOrbitalVector(objOrbit, new DateTime());
            //Assert.AreEqual(speedVectorAU, speedVectorAU2);

        }


        [Test]
        public void TestIntercept()
        {
            double myMass = 10000;
            double parentMass = 1.989e30; //solar mass.
            Game game = new Game();
            EntityManager mgr = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(mgr, parentblobs);

            Vector3 currentPos = new Vector3 { X=-0.77473184638034, Y = 0.967145228951685 };
            Vector3 currentVelocity = new Vector3 { Y = Distance.KmToAU(40) };
            double nonNewtSpeed = Distance.KmToAU( 283.018);

            Vector3 targetObjPosition = new Vector3 { X = 0.149246434443459, Y=-0.712107888348067 };
            Vector3 targetObjVelocity = new Vector3 { Y = Distance.KmToAU(35) };


            double sgp = OrbitMath.CalculateStandardGravityParameter(parentMass, myMass);
            //KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, targetObjPosition, targetObjVelocity);

            var currentDateTime = new DateTime(2000, 1, 1);

            OrbitDB targetOrbit = OrbitDB.FromVector(parentEntity, myMass, parentMass, sgp, targetObjPosition, targetObjVelocity, currentDateTime);



            var intercept = InterceptCalcs.GetInterceptPosition2(currentPos, nonNewtSpeed, targetOrbit ,currentDateTime);

            var futurePos1 = Distance.AuToKm( OrbitProcessor.GetAbsolutePosition_AU(targetOrbit, intercept.Item2));

            var futurePos2 = Distance.AuToKm( intercept.Item1);




            Assert.AreEqual(futurePos1.Length(), futurePos2.Length(), 0.01);
            Assert.AreEqual(futurePos1.X, futurePos2.X, 0.01);
            Assert.AreEqual(futurePos1.Y, futurePos2.Y, 0.01);
            Assert.AreEqual(futurePos1.Z, futurePos2.Z, 0.01);
            var time = intercept.Item2 - currentDateTime;

            var distance = (currentPos - intercept.Item1).Length();
            var distancekm = Distance.AuToKm(distance);

            var speed = distance / time.TotalSeconds;
            var speed2 = distancekm / time.TotalSeconds;

            var distb = nonNewtSpeed * time.TotalSeconds;
            var distbKM = Distance.AuToKm(distb);
            var timeb = distance / nonNewtSpeed;

            Assert.AreEqual(nonNewtSpeed, speed, 1.0e-10 );

            var dif = distancekm - distbKM;
            Assert.AreEqual(distancekm, distbKM, 0.25);
        }

        [Test]
        public void TestNewtonTrajectory()
        {
            Game game = new Game();
            EntityManager mgr = new EntityManager(game, false);
            Entity parentEntity = TestingUtilities.BasicEarth(mgr);

            PositionDB pos1 = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 8.52699302490434E-05, Z = 0 };
            BaseDataBlob[] objBlobs1 = new BaseDataBlob[3];
            objBlobs1[0] = pos1;
            objBlobs1[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs1[2] = new NewtonMoveDB(parentEntity)
            {
                CurrentVector_kms = new Vector3(-10.0, 0, 0)
            };
            Entity objEntity1 = new Entity(mgr, objBlobs1);
            PositionDB pos2 = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 8.52699302490434E-05, Z = 0 };
            BaseDataBlob[] objBlobs2 = new BaseDataBlob[3];
            objBlobs2[0] = pos2;
            objBlobs2[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs2[2] = new NewtonMoveDB(parentEntity)
            {
                CurrentVector_kms = new Vector3(-10.0, 0, 0)
            };
            Entity objEntity2 = new Entity(mgr, objBlobs2);

            var seconds = 100;
            for (int i = 0; i < seconds; i++)
            {
                NewtonionMovementProcessor.NewtonMove(objEntity1, 1);
            }
            NewtonionMovementProcessor.NewtonMove(objEntity2, seconds);
            var distance1 = Distance.AuToKm(pos1.AbsolutePosition_AU.Length());
            var distance2 = Distance.AuToKm(pos2.AbsolutePosition_AU.Length());

            //this test is currently failing and I'm unsure why. right now the code is using a 1s timestep so it should come out exact...
            //it looks ok graphicaly though so I'm not *too* conserned about this one right now. 
            Assert.AreEqual(distance1, distance2); //if we put the variable timstep which is related to the speed of the object in we'll have to give this a delta


        }
    }
}
