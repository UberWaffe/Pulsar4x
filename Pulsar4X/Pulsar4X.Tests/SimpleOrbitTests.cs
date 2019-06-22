using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Helpers.SIValues;
using Pulsar4X.Vectors;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests for simple unrealistic circular orbits")]
    public class SimpleOrbitTests
    {
        public class VectorRadiusANgleAndResult
        {
            public Vector3 TheVector;
            public SiDistance Radius;
            public SiAngle Angle;
            public Vector3 ExpectedResult;

            public VectorRadiusANgleAndResult(Vector3 vector, double radius, double angle, Vector3 result)
            {
                TheVector = vector;
                Radius = new SiDistance(radius, DistanceEngUnits.Meters);
                Angle = new SiAngle(angle, AngleEngUnits.Radians);
                ExpectedResult = result;
            }

            public VectorRadiusANgleAndResult(Vector3 vector, SiDistance radius, SiAngle angle, Vector3 result)
            {
                TheVector = vector;
                Radius = radius;
                Angle = angle;
                ExpectedResult = result;
            }
        }

        static List<VectorRadiusANgleAndResult> allCircleCenterTestData = new List<VectorRadiusANgleAndResult>()
        {
            new VectorRadiusANgleAndResult(
                vector: new Vector3(2500, 0, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(0, 2500, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(-2500, 0, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(180, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(0, -2500, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(270, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                result: new Vector3(1000, -1000, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(180, AngleEngUnits.Degrees),
                result: new Vector3(2000, 0, 0)),

            new VectorRadiusANgleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(270, AngleEngUnits.Degrees),
                result: new Vector3(1000, 1000, 0))
        };

        [Test, TestCaseSource(nameof(allCircleCenterTestData))]
        public void CircleCalculations_When_PositionRadiusAndAngleIsGiven_Should_CorrectlyCalculateCircleCenterPosition(VectorRadiusANgleAndResult testcase)
        {
            var calculated2d = CircleCalculations.GetCenterOfCircle(new Vector2(testcase.TheVector.X, testcase.TheVector.Y), testcase.Radius, testcase.Angle);
            var calculated = new Vector3(calculated2d.X, calculated2d.Y, testcase.TheVector.Z);
            Assert.IsTrue(TestVectorsAreEqual(testcase.ExpectedResult, calculated));
        }


        static List<VectorRadiusANgleAndResult> AllCirclePositionTestData = new List<VectorRadiusANgleAndResult>()
        {
            new VectorRadiusANgleAndResult(
                vector: new Vector3(0, 0, 0),
                radius: 0.0d,
                angle: Angle.ToRadians(0.0d),
                result: new Vector3(0, 0, 0))
        };
        
        [Test, TestCaseSource(nameof(AllCirclePositionTestData))]
        public void CircleCalculations_When_CircleAndAngleIsGiven_Should_CorrectlyCalculatePositionOnItsEdgeAtThatAngle()
        {
            Assert.Fail();
        }

        [Test]
        public void CircularOrbit_When_OrbitingAStaticParentForAFixedTimespan_Should_ResultInCorrectnewPosition()
        {
            Assert.Fail();
        }

        public bool TestVectorsAreEqual(Vector3 expected, Vector3 actual, double requiredAccuracy = 0.01)
        {
            Assert.AreEqual(expected.X, actual.X, requiredAccuracy);
            Assert.AreEqual(expected.Y, actual.Y, requiredAccuracy);
            Assert.AreEqual(expected.Z, actual.Z, requiredAccuracy);

            return true;
        }

    }
}
