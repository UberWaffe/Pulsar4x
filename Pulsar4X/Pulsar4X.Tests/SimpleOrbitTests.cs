using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Orbit;
using Pulsar4X.ECSLib.Helpers.SIValues;
using Pulsar4X.Vectors;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests for simple unrealistic circular orbits")]
    public class SimpleOrbitTests
    {
        public class VectorRadiusAngleAndResult
        {
            public Vector3 TheVector;
            public SiDistance Radius;
            public SiAngle Angle;
            public Vector3 ExpectedResult;

            public VectorRadiusAngleAndResult(Vector3 vector, double radius, double angle, Vector3 result)
            {
                TheVector = vector;
                Radius = new SiDistance(radius, DistanceEngUnits.Meters);
                Angle = new SiAngle(angle, AngleEngUnits.Radians);
                ExpectedResult = result;
            }

            public VectorRadiusAngleAndResult(Vector3 vector, SiDistance radius, SiAngle angle, Vector3 result)
            {
                TheVector = vector;
                Radius = radius;
                Angle = angle;
                ExpectedResult = result;
            }
        }

        static List<VectorRadiusAngleAndResult> allCircleCenterTestData = new List<VectorRadiusAngleAndResult>()
        {
            new VectorRadiusAngleAndResult(
                vector: new Vector3(2500, 0, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, 2500, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(-2500, 0, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(180, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, -2500, 0),
                radius: new SiDistance(2.5, DistanceEngUnits.Kilometers),
                angle: new SiAngle(270, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                result: new Vector3(0, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                result: new Vector3(1000, -1000, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(180, AngleEngUnits.Degrees),
                result: new Vector3(2000, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(1000, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(270, AngleEngUnits.Degrees),
                result: new Vector3(1000, 1000, 0))
        };

        [Test, TestCaseSource(nameof(allCircleCenterTestData))]
        public void CircleCalculations_When_PositionRadiusAndAngleIsGiven_Should_CorrectlyCalculateCircleCenterPosition(VectorRadiusAngleAndResult testcase)
        {
            var calculated2d = CircleCalculations.GetCenterOfCircle(new Vector2(testcase.TheVector.X, testcase.TheVector.Y), testcase.Radius, testcase.Angle);
            var calculated = new Vector3(calculated2d.X, calculated2d.Y, testcase.TheVector.Z);
            Assert.IsTrue(TestVectorsAreEqual(testcase.ExpectedResult, calculated));
        }


        static List<VectorRadiusAngleAndResult> AllCirclePositionTestData = new List<VectorRadiusAngleAndResult>()
        {
            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                result: new Vector3(1000, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, 0, 0),
                radius: new SiDistance(1.05, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                result: new Vector3(0, 1050, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, 0, 0),
                radius: new SiDistance(1.3, DistanceEngUnits.Kilometers),
                angle: new SiAngle(180, AngleEngUnits.Degrees),
                result: new Vector3(-1300, 0, 0)),

            new VectorRadiusAngleAndResult(
                vector: new Vector3(0, 0, 0),
                radius: new SiDistance(1.7, DistanceEngUnits.Kilometers),
                angle: new SiAngle(270, AngleEngUnits.Degrees),
                result: new Vector3(0, -1700, 0))
        };
        
        [Test, TestCaseSource(nameof(AllCirclePositionTestData))]
        public void CircleCalculations_When_CircleAndAngleIsGiven_Should_CorrectlyCalculatePositionOnItsEdgeAtThatAngle(VectorRadiusAngleAndResult testcase)
        {
            var calculated2d = CircleCalculations.GetPositionOnCircle(new Vector2(testcase.TheVector.X, testcase.TheVector.Y), testcase.Radius, testcase.Angle);
            var calculated = new Vector3(calculated2d.X, calculated2d.Y, testcase.TheVector.Z);
            Assert.IsTrue(TestVectorsAreEqual(testcase.ExpectedResult, calculated));
        }


        public class VectorRadiusDoubleAngleTimespanAndResult
        {
            public Vector3 ParentPosition;
            public SiDistance Radius;
            public SiAngle Angle;
            public SiAngle RotationSpeed;
            public TimeSpan Time; 
            public Vector3 ExpectedResult;
            
            public VectorRadiusDoubleAngleTimespanAndResult(Vector3 parentPos, SiDistance radius, SiAngle angle, SiAngle rotation, TimeSpan time, Vector3 result)
            {
                ParentPosition = parentPos;
                Radius = radius;
                Angle = angle;
                RotationSpeed = rotation;
                Time = time;
                ExpectedResult = result;
            }
        }

        static List<VectorRadiusDoubleAngleTimespanAndResult> allOrbitingTests = new List<VectorRadiusDoubleAngleTimespanAndResult>()
        {
            new VectorRadiusDoubleAngleTimespanAndResult(
                parentPos: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                rotation: new SiAngle(1, AngleEngUnits.Degrees),
                time: new TimeSpan(0, 0, 0),
                result: new Vector3(1000, 0, 0)),

            new VectorRadiusDoubleAngleTimespanAndResult(
                parentPos: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                rotation: new SiAngle(1, AngleEngUnits.Degrees),
                time: new TimeSpan(0, 0, 90),
                result: new Vector3(0, 1000, 0)),

            new VectorRadiusDoubleAngleTimespanAndResult(
                parentPos: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                rotation: new SiAngle(1, AngleEngUnits.Degrees),
                time: new TimeSpan(0, 0, 180),
                result: new Vector3(-1000, 0, 0)),

            new VectorRadiusDoubleAngleTimespanAndResult(
                parentPos: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(0, AngleEngUnits.Degrees),
                rotation: new SiAngle(1, AngleEngUnits.Degrees),
                time: new TimeSpan(0, 0, 270),
                result: new Vector3(0, -1000, 0)),

            new VectorRadiusDoubleAngleTimespanAndResult(
                parentPos: new Vector3(0, 0, 0),
                radius: new SiDistance(1, DistanceEngUnits.Kilometers),
                angle: new SiAngle(90, AngleEngUnits.Degrees),
                rotation: new SiAngle(1, AngleEngUnits.Degrees),
                time: new TimeSpan(0, 0, 90),
                result: new Vector3(-1000, 0, 0)),
        };

        [Test, TestCaseSource(nameof(allOrbitingTests))]
        public void CircularOrbit_When_OrbitingAStaticParentForAFixedTimespan_Should_ResultInCorrectnewPosition(VectorRadiusDoubleAngleTimespanAndResult testcase)
        {
            var calculated = SimpleOrbitProcessor.CalculatePositionChangeInReferenceToParent(testcase.ParentPosition, testcase.Radius, testcase.RotationSpeed, testcase.Angle, testcase.Time);
            Assert.IsTrue(TestVectorsAreEqual(testcase.ExpectedResult, calculated));
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
