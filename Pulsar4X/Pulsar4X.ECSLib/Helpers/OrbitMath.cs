﻿using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// A struct to hold kepler elements without the need to give a 'parent' as OrbitDB does.
    /// </summary>
    public struct KeplerElements
    {
        public double SemiMajorAxis;        //a
        public double SemiMinorAxis;        //b
        public double Eccentricity;         //e
        public double LinierEccentricity;   //ae
        public double Periapsis;            //q
        public double Apoapsis;             //Q
        public double LongdOfAN;                 //Ω (upper case Omega)
        public double ArgumentOfPeriapsis;       //ω (lower case omega)
        public double Inclination;          //i
        public double MeanMotion;           //n
        public double MeanAnomalyAtEpoch;   //M0
        public double TrueAnomalyAtEpoch;   //ν or f or  θ
        //public double Period              //P
        //public double EccentricAnomaly    //E
        public DateTime Epoch;                //
    }

    /// <summary>
    /// Orbit math.
    /// note multiple simular functions for doing the same thing, some of these are untested.
    /// Take care when using unless the function has a decent test in the tests project. 
    /// Some simular functions with simular inputs left in for future performance testing (ie one of the two might be slightly more performant).
    /// </summary>
    public class OrbitMath
    {
        /// <summary>
        /// Kepler elements from velocity and position.
        /// </summary>
        /// <returns>a struct of Kepler elements.</returns>
        /// <param name="parentMass">Mass of the parent object (kg)</param>
        /// <param name="objMass">Mass of the orbiting object object (kg)</param>
        /// <param name="position">Position ralitive to parent (Km)</param>
        /// <param name="velocity">Velocity ralitive to parent (Km/s)</param>
        public static KeplerElements KeplerFromPositionAndVelocity(double parentMass, double objMass, Vector3 position, Vector3 velocity, DateTime epoch)
        {
            double standardGravParam = OrbitMath.CalculateStandardGravityParameterInKm3S2(parentMass, objMass);
            return KeplerFromPositionAndVelocity(standardGravParam, position, velocity, epoch);
        }

        /// <summary>
        /// Kepler elements from velocity and position.
        /// https://github.com/RazerM/orbital/blob/0.7.0/orbital/utilities.py#L284
        /// </summary>
        /// <returns>a struct of Kepler elements.</returns>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="position">Position ralitive to parent (Km)</param>
        /// <param name="velocity">Velocity ralitive to parent (Km/s)</param>
        public static KeplerElements KeplerFromPositionAndVelocity(double standardGravParam, Vector3 position, Vector3 velocity, DateTime epoch)
        {
            KeplerElements ke = new KeplerElements();
			
            Vector3 angularVelocity = CalculateAngularMomentum(position, velocity);
            Vector3 nodeVector = CalculateNode(angularVelocity);
			
            Vector3 eccentVector = EccentricityVector(standardGravParam, position, velocity);
            double eccentricity = eccentVector.Length();

            decimal preciseVelocity = (decimal)velocity.Length();
            double specificOrbitalEnergy = (double)(preciseVelocity * preciseVelocity * 0.5M - (decimal)standardGravParam / (decimal)position.Length());


            double semiMajorAxis;
            double p; //p is where the ellipse or hypobola crosses a line from the focal point 90 degrees from the sma
            if (Math.Abs(eccentricity) > 1) //hypobola
            {
                semiMajorAxis = -(-standardGravParam / (2 * specificOrbitalEnergy)); //in this case the sma is negitive
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else if (Math.Abs(eccentricity) < 1) //ellipse
            {
                semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola
            {
                p = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                semiMajorAxis = double.MaxValue;
            }
            
            double semiMinorAxis = EllipseMath.SemiMinorAxis(semiMajorAxis, eccentricity);
            double linierEccentricity = eccentricity * semiMajorAxis;

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. or pi if counter clockwise orbit. 
  
            if (double.IsNaN(inclination))
                inclination = 0;

            double loANlen = nodeVector.X / nodeVector.Length();
            double longdOfAN = 0;
            if (double.IsNaN(loANlen))
                loANlen = 0;
            else
                loANlen = GMath.Clamp(loANlen, -1, 1);
            if(loANlen != 0)
                longdOfAN = Math.Acos(loANlen); //RAAN or LoAN or Ω
                

            double eccentricAnomoly = GetEccentricAnomalyFromStateVectors2(standardGravParam, semiMajorAxis, position, velocity);

            double trueAnomaly = TrueAnomalyFromEccentricAnomaly(eccentricity, eccentricAnomoly);
            double argOfPeriaps = ArgumentOfPeriapsis2(position, inclination, longdOfAN, trueAnomaly);

            var meanMotion = Math.Sqrt(standardGravParam / Math.Pow(semiMajorAxis, 3));

            var meanAnomaly = eccentricAnomoly - eccentricity * Math.Sin(eccentricAnomoly);

            ke.SemiMajorAxis = semiMajorAxis;
            ke.SemiMinorAxis = semiMinorAxis;
            ke.Eccentricity = eccentricity;

            ke.Apoapsis = EllipseMath.Apoapsis(eccentricity, semiMajorAxis);
            ke.Periapsis = EllipseMath.Periapsis(eccentricity, semiMajorAxis);
            ke.LinierEccentricity = EllipseMath.LinierEccentricity(ke.Apoapsis, semiMajorAxis);
            ke.LongdOfAN = longdOfAN;
            ke.ArgumentOfPeriapsis = argOfPeriaps;
            ke.Inclination = inclination;
            ke.MeanMotion = meanMotion;
            ke.MeanAnomalyAtEpoch = meanAnomaly;
            ke.TrueAnomalyAtEpoch = trueAnomaly;
            ke.Epoch = epoch; //TimeFromPeriapsis(semiMajorAxis, standardGravParam, meanAnomaly);
            //Epoch(semiMajorAxis, semiMinorAxis, eccentricAnomoly, OrbitalPeriod(standardGravParam, semiMajorAxis));

            return ke;
        }

        #region Vector Calculations
        public static Vector3 CalculateAngularMomentum(Vector3 position, Vector3 velocity)
        {
            /*
            * position vector       km
            * velocity              km/sec
            */
            var (X, Y, Z) = Vector3.CrossPrecise(position, velocity);
            return Vector3.Vector3FromDecimals(X, Y, Z);
        }

        public static Vector3 CalculateNode(Vector3 angularVelocity)
        {
            var (X, Y, Z) = Vector3.CrossPrecise(new Vector3(0, 0, 1), angularVelocity);
            return Vector3.Vector3FromDecimals(X, Y, Z);
        }

        public static double CalculateStandardGravityParameter(double orbiterMassInKg, double bodyBeingOrbitedMassInKg)
        {
            // https://en.wikipedia.org/wiki/Standard_gravitational_parameter
            double sgpInM3S2 = GameConstants.Science.GravitationalConstant * (bodyBeingOrbitedMassInKg + orbiterMassInKg);
            double sgpInKm3S2 = sgpInM3S2 / Math.Pow(GameConstants.Units.KmPerAu, 3);
            return sgpInKm3S2;
        }

        public static double CalculateStandardGravityParameterInKm3S2(double orbiterMassInKg, double bodyBeingOrbitedMassInKg)
        {
            // https://en.wikipedia.org/wiki/Standard_gravitational_parameter
            double sgpInM3S2 = GameConstants.Science.GravitationalConstant * (bodyBeingOrbitedMassInKg + orbiterMassInKg);
            double sgpInKm3S2 = sgpInM3S2 / (1000 * 1000 * 1000);
            return sgpInKm3S2;
        }
        #endregion

        #region ArgumentOfPeriapsis
        /*
        public static double ArgumentOfPeriapsis(Vector4 nodeVector, Vector4 eccentVector, Vector4 pos, Vector4 vel, double loAN)
        {
            double eccentricity = eccentVector.Length();
            double argOfPeriaps;
            if (loAN == 0)
            {
                argOfPeriaps = Math.Atan2(eccentVector.Y, eccentVector.X);
                if (Vector4.Cross(pos, vel).Z < 0) //anti clockwise orbit
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }

            else
            {
                double aopLen = Vector4.Dot(nodeVector, eccentVector);
                aopLen = aopLen / (nodeVector.Length() * eccentricity);
                aopLen = GMath.Clamp(aopLen, -1, 1);
                argOfPeriaps = Math.Acos(aopLen);
                if (eccentVector.Z < 0) //anti clockwise orbit.
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }
            return argOfPeriaps;
        }

        public static double ArgumentOfPeriapsis(Vector4 nodeVector, Vector4 eccentricityVector, Vector4 pos, Vector4 vel)
        {
            double aop;
            if (nodeVector.Length() == 0)
            {
                aop = Math.Atan2(eccentricityVector.Y, eccentricityVector.X);
                if(Vector4.Cross(pos, vel).Z < 0)
                    aop = 2 * Math.PI + aop;
            }
            else
            {
                var foo = Vector4.Dot(nodeVector, eccentricityVector);
                var foo2 = nodeVector.Length() * eccentricityVector.Length();
                aop = Math.Acos(foo / foo2);
                if (eccentricityVector.Z < 0)
                    aop = 2 * Math.PI + aop;
            }
            return aop;
        }
        */
        public static double ArgumentOfPeriapsis2(Vector3 pos, double incl, double lan, double trueAnomaly)
        {
            double Sw = 0;
            double Rx = pos.X;
            double Ry = pos.Y;
            double Rz = pos.Z;
            double R = pos.Length();
            double TA = trueAnomaly;
            var Cw = (Rx * Math.Cos(lan) + Ry * Math.Sin(lan)) / R;

            if (incl == 0 || incl == Math.PI)
            { Sw = (Ry * Math.Cos(lan) - Rx * Math.Sin(lan)) / R; }
            else
            { Sw = Rz / (R * Math.Sin(incl)); }

            var W = Math.Atan2(Sw, Cw) - TA;
            if (W < 0) { W = 2 * Math.PI + W; }

            return W;
        }

        #endregion

        #region EccentricityVector

        /// <summary>
        /// https://en.wikipedia.org/wiki/Eccentricity_vector
        /// </summary>
        /// <returns>The vector.</returns>
        /// <param name="sgp">StandardGravParam.</param>
        /// <param name="position">Position, ralitive to parent (Km)</param>
        /// <param name="velocity">Velocity, ralitive to parent (Km)</param>
        public static Vector3 EccentricityVector(double sgp, Vector3 position, Vector3 velocity)
        {
            Vector3 angularMomentum = Vector3.Cross(position, velocity);
            Vector3 foo1 = Vector3.Cross(velocity, angularMomentum) / sgp;
            var foo2 = position / position.Length();
            return foo1 - foo2;
        }

        public static Vector3 EccentricityVector2(double sgp, Vector3 position, Vector3 velocity)
        {
            var speed = velocity.Length();
            var radius = position.Length();
            var foo1 = (speed * speed - sgp / radius) * position ;
            var foo2 = Vector3.Dot(position, velocity) * velocity;
            var foo3 = (foo1 - foo2) / sgp;
            return foo3;
        }

        #endregion

        #region TrueAnomaly

        /*
        /// <summary>
        /// The True Anomaly in radians
        /// </summary>
        /// <returns>The anomaly.</returns>
        /// <param name="position">Position.</param>
        /// <param name="loP">Lo p.</param>
        public static double TrueAnomaly(Vector4 position, double loP)
        {
            return Math.Atan2(position.Y, position.X) - loP; 
        }
        */
        /// <summary>
        /// The True Anomaly in radians
        /// https://en.wikipedia.org/wiki/True_anomaly#From_state_vectors
        /// </summary>
        /// <returns>The True Anomaly in radians</returns>
        /// <param name="eccentVector">Eccentricity vector.</param>
        /// <param name="position">Position ralitive to parent</param>
        /// <param name="velocity">Velocity ralitive to parent</param>
        public static double TrueAnomaly(Vector3 eccentVector, Vector3 position, Vector3 velocity)
        {
            var dotEccPos = Vector3.Dot(eccentVector, position);
            var talen = eccentVector.Length() * position.Length();
            talen = dotEccPos / talen;
            talen = GMath.Clamp(talen, -1, 1);
            var trueAnomoly = Math.Acos(talen);

            if (Vector3.Dot(position, velocity) < 0)
                trueAnomoly = Math.PI * 2 - trueAnomoly;

            return trueAnomoly;
        }
        /*
        public static double TrueAnomaly2(Vector4 eccentVector, Vector4 position, Vector4 velocity)
        {
            var r = Math.Sqrt(Vector4.Dot(position, position));
            var b = new Vector4{X = -eccentVector.Y, Y = eccentVector.X};
            var cos_TA = Vector4.Dot(position, eccentVector) / r;
            var sin_TA = Vector4.Dot(position, b) / r;
            double trueAnomoly = 0;
            if (sin_TA >= 0)
            {
                trueAnomoly = Math.Acos(cos_TA);
            }
            else if (sin_TA < 0)
            {
                trueAnomoly = 2 * Math.PI - Math.Acos(cos_TA);
            }
            return trueAnomoly;
        }
        */

        /// <summary>
        /// The True Anomaly in radians
        /// </summary>
        /// <returns>The True Anomaly in radians</returns>
        /// <param name="sgp">Sgp.</param>
        /// <param name="position">Position.</param>
        /// <param name="velocity">Velocity.</param>
        public static double TrueAnomaly(double sgp, Vector3 position, Vector3 velocity)
        {
            var H = Vector3.Cross(position, velocity).Length();
            var R = position.Length();
            var q = Vector3.Dot(position, velocity);  // dot product of r*v
            var TAx = H * H / (R * sgp) - 1;
            var TAy = H * q / (R * sgp);
            var TA = Math.Atan2(TAy, TAx);
            return TA;
        }

        public static double TrueAnomalyFromEccentricAnomaly(double eccentricity, double eccentricAnomaly)
        {
            var x = Math.Sqrt(1 - Math.Pow(eccentricity, 2)) * Math.Sin(eccentricAnomaly);
            var y = Math.Cos(eccentricAnomaly) - eccentricity;
            return Math.Atan2(x, y);
        }

        public static double TrueAnomalyFromEccentricAnomaly2(double eccentricity, double eccentricAnomaly)
        {
            var x = Math.Cos(eccentricAnomaly) - eccentricity;
            var y = 1 - eccentricity * Math.Cos(eccentricAnomaly);
            return Math.Acos(x / y);
        }

        /// <summary>
        /// This does not apear to work correctly. don't know why.
        /// </summary>
        /// <returns>The anomaly from eccentric anomaly3.</returns>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="eccentricAnomaly">Eccentric anomaly.</param>
        /// 
        /*
        public static double TrueAnomalyFromEccentricAnomaly3(double eccentricity, double eccentricAnomaly)
        {
            var x = Math.Sqrt(1 - Math.Pow(eccentricity, 2)) * Math.Sin(eccentricAnomaly);
            var y = 1 - eccentricity * Math.Cos(eccentricAnomaly);
            return Math.Asin(x / y);
        }
        */
        /*
        public static double GetTrueAnomaly(Vector4 eccentricityVector, Vector4 positionVector)
        {
            var a = Vector4.Dot(eccentricityVector, positionVector);
            var h = eccentricityVector.Length() * positionVector.Length();
            var ta = Math.Acos(a / h);
            double b = 0;

            if(a/b > 1 || a/b < 1)
            {
                b = Math.Sqrt(a * a + b * b);
                ta = Math.Atan(b / a);
            }
            return ta;
        }
        */

        #endregion

        #region VelocityAndSpeed;
        /*
    /// <summary>
    /// 2d Velocity vector in polar coordinates.
    /// </summary>
    /// <returns>item1 is speed (distance/s), item2 is heading in radians.</returns>
    /// <param name="sgp">Sgp.</param>
    /// <param name="position">Position.</param>
    /// <param name="sma">Sma.</param>
    /// <param name="eccentricity">Eccentricity.</param>
    /// <param name="loP">Lo p.</param>
    public static Tuple<double, double> PreciseOrbitalVelocityPolarCoordinate(double sgp, Vector4 position, double sma, double eccentricity, double loP)
    {
        var radius = position.Length();
        var spd = PreciseOrbitalSpeed(sgp, radius, sma);

        double linierEcc = EllipseMath.LinierEccentricityFromEccentricity(sma, eccentricity);

        double referenceToPosAngle = Math.Atan2(position.X, -position.Y); //we switch x and y here so atan2 works in the y direction. 

        double anglef = loP - referenceToPosAngle;

        //find angle alpha using law of cos: (a^2 + b^2 - c^2) / 2ab
        double sideA = radius;
        double sideB = 2 * sma - radius;
        double sideC = 2 * linierEcc;
        double alpha = Math.Acos((sideA * sideA + sideB * sideB - sideC * sideC) / (2 * sideA * sideB));

        double angle = Math.PI - (referenceToPosAngle + ((Math.PI - alpha) * 0.5));

        return new Tuple<double, double>(spd, angle);
    }*/

        /// <summary>
        /// This returns the heading mesured from the periapsis (AoP) in radians
        /// Add the LoP to this to get the true heading in a 2d orbit. 
        /// </summary>
        /// <returns>The orbital velocity polar coordinate.</returns>
        /// <param name="sgp">Sgp.</param>
        /// <param name="position">Position.</param>
        /// <param name="semiMajorAxis">Semi major axis.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="trueAnomaly">True anomaly.</param>
        public static (double speed, double heading) InstantaneousOrbitalVelocityPolarCoordinate(double sgp, Vector3 position, double semiMajorAxis, double eccentricity, double trueAnomaly)
        {
            var radius = position.Length();
            var spd = InstantaneousOrbitalSpeed(sgp, radius, semiMajorAxis);

            var heading = HeadingFromPeriaps(position, eccentricity, semiMajorAxis, trueAnomaly);

            return (spd, heading);
        }

        /// <summary>
        /// This returns the heading mesured from the periapsis (AoP) and is on the plane of the object
        /// Add the LoP to this to get the true heading in a 2d orbit. 
        /// </summary>
        /// <returns>The from periaps.</returns>
        /// <param name="pos">Position.</param>
        /// <param name="eccentcity">Eccentcity.</param>
        /// <param name="semiMajorAxis">Semi major axis.</param>
        /// <param name="trueAnomaly">True anomaly.</param>
        public static double HeadingFromPeriaps(Vector3 pos, double eccentcity, double semiMajorAxis, double trueAnomaly)
        {

            double r = pos.Length();
            double a = semiMajorAxis;
            double e = eccentcity;
            double k = r / a;
            double f = trueAnomaly;

            double bar = ((2 - 2 * e * e) / (k * (2 - k))) - 1 ;
            double foo = GMath.Clamp(bar, - 1, 1);
            double alpha = Math.Acos(foo);
            if (trueAnomaly > Math.PI || trueAnomaly < 0)
                alpha = -alpha;
            double heading = f + ((Math.PI - alpha) / 2);
            return heading;

        }

        /// <summary>
        /// vector ralitive to AoP and on the plane of the object
        /// </summary>
        /// <returns>The orbital vector ralitive to the Argument Of Periapsis</returns>
        /// <param name="sgp">Standard Grav Perameter. in AU</param>
        /// <param name="position">Ralitive Position.</param>
        /// <param name="sma">SemiMajorAxis</param>
        public static Vector2 InstantaneousOrbitalVelocityVector(double sgp, Vector3 position, double sma, double eccentricity, double trueAnomaly)
        {
            (double speed, double angle) = InstantaneousOrbitalVelocityPolarCoordinate(sgp, position, sma, eccentricity, trueAnomaly);
            var v = new Vector2()
            {
                Y = Math.Cos(angle) * speed,
                X = Math.Sin(angle) * speed
            };

            if (double.IsNaN(v.X) || double.IsNaN(v.Y))
                throw new Exception("Result is NaN");

            return v;
        }



        /// <summary>
        /// returns the speed for an object of a given mass at a given radius from a body. this is the vis-viva calculation
        /// </summary>
        /// <returns>The orbital speed, ralitive to the parent</returns>
        /// <param name="standardGravParameter">standardGravParameter.</param>
        /// <param name="distance">Radius.</param>
        /// <param name="semiMajAxis">Semi maj axis.</param>
        public static double InstantaneousOrbitalSpeed(double standardGravParameter, double distance, double semiMajAxis)
        {
            var spd = Math.Sqrt(standardGravParameter * (2 / distance - 1 / semiMajAxis));
            if (double.IsNaN(spd))
                throw new Exception("Speed Result is NaN");
            return spd;
        }

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future. 
        /// Fairly slow and inefficent. 
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        public static double Hackspeed(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            return Distance.DistanceBetween(pos1, pos2);
        }

        public static double HackVelocityHeading(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            Vector3 vector = pos2 - pos1;
            double heading = Math.Atan2(vector.Y, vector.X);
            return heading;
        }

        public static Vector3 HackVelocityVector(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));
            //double speed = Distance.DistanceBetween(pos1, pos2);
            return pos2 - pos1;
        }

        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit. 
        /// </summary>
        /// <returns>The orbital velocity in au.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInAU(OrbitDB orbit)
        {
            double a = orbit.SemiMajorAxis;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.OrbitalPeriod.TotalSeconds;
            double peremeter = Math.PI * (3* (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return peremeter  / orbitalPerodSeconds;
        }


        #endregion

        #region EccentricAnomaly



        /// <summary>
        /// Gets the eccentric anomaly.
        /// This can take a number of itterations to calculate so may not be fast. 
        /// </summary>
        /// <returns>The eccentric anomaly.</returns>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="currentMeanAnomaly">Current mean anomaly.</param>
        public static double GetEccentricAnomalyNewtonsMethod(double eccentricity, double currentMeanAnomaly)
        {
            
            //Kepler's Equation
            const int numIterations = 1000;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*                   E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *                        1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                e[i + 1] = e[i] - (e[i] - eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - eccentricity * Math.Cos(e[i]));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                throw new Exception("Non-convergence of Newton's method while calculating Eccentric Anomaly.");
            }

            return e[i - 1];
        }

        public static double GetEccentricAnomalyNewtonsMethod2(double eccentricity, double currentMeanAnomaly)
        {
            double eca = currentMeanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = 0;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - currentMeanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }
            return eca;
        }

        public static double GetEccentricAnomalyFromTrueAnomaly(double ν, double eccentricity)
        {
            return Math.Acos((Math.Cos(ν) + eccentricity) / (1 + eccentricity * Math.Cos(ν)));
        }

        public static double GetEccentricAnomalyFromStateVectors(Vector3 position, double semiMajAxis, double linierEccentricity, double aop)
        {
            var x = (position.X * Math.Cos(-aop)) - (position.Y * Math.Sin(-aop));
            x = linierEccentricity + x;
            double foo = GMath.Clamp(x / semiMajAxis, -1, 1); //because sometimes we were getting a floating point error that resulted in numbers infinatly smaller than -1
            return Math.Acos(foo);
        }

        public static double GetEccentricAnomalyFromStateVectors2(double sgp, double semiMajorAxis, Vector3 position, Vector3 velocity)
        {
            var radius = position.Length();
            var q = Vector3.Dot(position, velocity);
            var Ex = 1 - radius / semiMajorAxis;
            var Ey = q / Math.Sqrt(semiMajorAxis * sgp);
            var u = Math.Atan2(Ey, Ex); // eccentric anomoly 
            return u;
        }

        #endregion

        #region MeanAnomaly

        public static double GetMeanAnomaly(double eccentricity, double eccentricAnomaly)
        {
            return eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly);
        }

        /// <summary>
        /// Calculates CurrentMeanAnomaly
        /// </summary>
        /// <returns>The mean anomaly.</returns>
        /// <param name="meanAnomalyAtEpoch">InRadians.</param>
        /// <param name="meanMotion">InRadians/s.</param>
        /// <param name="secondsFromEpoch">Seconds from epoch.</param>
        public static double GetMeanAnomalyFromTime(double meanAnomalyAtEpoch, double meanMotion, double secondsFromEpoch)
        {
            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = meanAnomalyAtEpoch;
            // Add nT
            currentMeanAnomaly += meanMotion * secondsFromEpoch;
            // Large nT can cause meanAnomaly to go past 2*Pi. Roll it down. It shouldn't, because timeSinceEpoch should be tapered above, but it has.
            currentMeanAnomaly = currentMeanAnomaly % (Math.PI * 2);
            return currentMeanAnomaly;
        }

        /// <summary>
        /// Untested
        /// </summary>
        /// <returns>The hypobolic mean anomaly.</returns>
        /// <param name="hypobolicEccentricAnomaly">Hypobolic eccentric anomaly.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double GetHypobolicMeanAnomaly(double hypobolicEccentricAnomaly, double eccentricity)
        {
            return eccentricity * Math.Sinh(hypobolicEccentricAnomaly) - hypobolicEccentricAnomaly; 
        }

        #endregion

        #region Positions:

        /// <summary>
        /// Gets the position of an intersect between an orbit and a circle(radius)
        /// </summary>
        /// <returns>The from radius.</returns>
        /// <param name="radius">Radius.</param>
        /// <param name="semiLatusRectum">Semi latus rectum.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static Vector3 PositionFromRadius(double radius, double semiLatusRectum, double eccentricity)
        {
            double θ = AngleAtRadus(radius, semiLatusRectum, eccentricity);
            var x = radius * Math.Cos(θ);
            var y = radius * Math.Sin(θ);
            return new Vector3() { X = x, Y = y };
        }


        public static Vector3 GetRalitivePosition(double lofAN, double aoP, double incl, double trueAnomaly, double radius)
        {
            double angle = trueAnomaly + aoP;
            double x = Math.Cos(lofAN) * Math.Cos(angle) - Math.Sin(lofAN) * Math.Sin(angle) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angle) + Math.Cos(lofAN) * Math.Sin(angle) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angle);

            return new Vector3(x, y, z) * radius;
        }

        /// <summary>
        /// Another way of getting position, untested, currently unused, copied from somehwere on the net. 
        /// Untested
        /// </summary>
        /// <returns>The position.</returns>
        /// <param name="combinedMass">Combined mass.</param>
        /// <param name="semiMajAxis">Semi maj axis.</param>
        /// <param name="meanAnomaly">Mean anomaly.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="aoP">AngleOfPeriapsis.</param>
        /// <param name="loAN">LongditudeOfAccendingNode.</param>
        /// <param name="i">I don't even remmebr what this is inclination maybe</param>
        public static Vector3 Pos(double combinedMass, double semiMajAxis, double meanAnomaly, double eccentricity, double aoP, double loAN, double i)
        {
            var G = 6.6725985e-11;


            double eca = meanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = 0;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - meanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }

            var ceca = Math.Cos(eca);
            var seca = Math.Sin(eca);
            e1 = semiMajAxis * Math.Sqrt(Math.Abs(1 - eccentricity * eccentricity));
            var xw = semiMajAxis * (ceca - eccentricity);
            var yw = e1 * seca;

            var edot = Math.Sqrt((G * combinedMass) / semiMajAxis) / (semiMajAxis * (1 - eccentricity * ceca));
            var xdw = -semiMajAxis * edot * seca;
            var ydw = e1 * edot * ceca;

            var Cw = Math.Cos(aoP);
            var Sw = Math.Sin(aoP);
            var co = Math.Cos(loAN);
            var so = Math.Sin(loAN);
            var ci = Math.Cos(i);
            var si = Math.Sin(i);
            var swci = Sw * ci;
            var cwci = Cw * ci;
            var pX = Cw * co - so * swci;
            var pY = Cw * so + co * swci;
            var pZ = Sw * si;
            var qx = -Sw * co - so * cwci;
            var qy = -Sw * so + co * cwci;
            var qz = Cw * si;

            return new Vector3()
            {
                X = xw * pX + yw * qx,
                Y = xw * pY + yw * qy,
                Z = xw * pZ + yw * qz
            };
        }


        #endregion

        #region Time

        public static double GetOrbitalPeriodInSeconds(double sgp, double semiMajAxis)
        {
            return 2 * Math.PI * Math.Sqrt(Math.Pow(semiMajAxis, 3) / sgp);
        }
        public static TimeSpan GetOrbitalPeriodAsTimeSpan(double sgp, double SemiMajorAxis)
        {
            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            TimeSpan period;
            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(SemiMajorAxis, 3) / sgp);
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                period = TimeSpan.MaxValue;
            }
            else
            {
                period = TimeSpan.FromSeconds(orbitalPeriod);
            }
            return period;
        }

        /// <summary>
        /// Returns the TimeFromPeriapsis
        /// </summary>
        /// <returns>time from periapsis.</returns>
        /// <param name="semiMaj">Semi maj.</param>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="currentMeanAnomaly">Mean anomaly current.</param>
        public static double TimeFromPeriapsis(double semiMaj, double standardGravParam, double currentMeanAnomaly)
        {
            return Math.Pow((Math.Pow(semiMaj, 3) / standardGravParam), 0.5) * currentMeanAnomaly;
        }

        /// <summary>
        /// Alternate way to get TimeFromPeriapsis
        /// Doesn't work with Hypobolic orbits due to period being undefined. 
        /// </summary>
        /// <returns>The epoch.</returns>
        /// <param name="semiMaj">Semi maj.</param>
        /// <param name="semiMin">Semi minimum.</param>
        /// <param name="eccentricAnomaly">Eccentric anomaly.</param>
        /// <param name="Period">Period.</param>
        public static double TimeFromPeriapsis(double semiMaj, double semiMin, double eccentricAnomaly, double Period)
        {

            double areaOfEllipse = semiMaj * semiMin * Math.PI;
            double eccentricAnomalyArea = EllipseMath.AreaOfEllipseSector(semiMaj, semiMaj, 0, eccentricAnomaly); //we get the area as if it's a circile. 
            double trueArea = semiMin / semiMaj * eccentricAnomalyArea; //we then multiply the result by a fraction of b / a
            //double areaOfSegment = EllipseMath.AreaOfEllipseSector(semiMaj, semiMin, 0, lop + trueAnomaly);

            double t = Period * (trueArea / areaOfEllipse);

            return t;

        }

        public static double GetLongditudeOfPeriapsis(double inclination, double aoP, double loAN)
        {
            double lop;
            if (inclination > Math.PI * 0.5 && inclination < Math.PI * 1.5) 
            {

                lop = loAN - aoP;
            }
            else
            {

                lop = loAN + aoP;
            }
            return lop;
        }


        /// <summary>
        /// Incorrect/Incomplete Unfinished DONOTUSE
        /// </summary>
        /// <returns>The to radius from periapsis.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="radiusAU">Radius au.</param>
        public static double TimeToRadiusFromPeriapsis(OrbitDB orbit, double radiusAU)
        {
            throw new NotImplementedException();
            var a = orbit.SemiMajorAxis;
            var e = orbit.Eccentricity;
            var p = EllipseMath.SemiLatusRectum(a, e);
            var angle = AngleAtRadus(radiusAU, p, e);
            //var meanAnomaly = CurrentMeanAnomaly(orbit.MeanAnomalyAtEpoch, meanMotion, )
            return TimeFromPeriapsis(a, orbit.GravitationalParameterAU, orbit.MeanAnomalyAtEpoch);
        }

        #endregion


        /// <summary>
        /// Gets the soi radius of a given body
        /// </summary>
        /// <returns>The SOI radius in whatever units you feed the semiMajorAxis.</returns>
        /// <param name="semiMajorAxis">Semi major axis of the smaller body ie the earth around the sun</param>
        /// <param name="mass">Mass of the smaller body ie the earth</param>
        /// <param name="parentMass">Parent mass. ie the sun</param>
        public static double GetSOI(double semiMajorAxis, double mass, double parentMass)
        {
            return semiMajorAxis * Math.Pow((mass / parentMass), 0.4);
        }

        /// <summary>
        /// works with ellipse and hyperabola. Plucked from: http://www.bogan.ca/orbits/kepler/orbteqtn.html
        /// </summary>
        /// <returns>The radius from the focal point for a given angle</returns>
        /// <param name="angle">Angle.</param>
        /// <param name="semiLatusRectum">Semi latus rectum.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double RadiusAtAngle(double angle, double semiLatusRectum, double eccentricity)
        {
            return semiLatusRectum / (1 + eccentricity * Math.Cos(angle));
        }

        /// <summary>
        /// works with ellipse and hyperabola. Plucked from: http://www.bogan.ca/orbits/kepler/orbteqtn.html
        /// </summary>
        /// <returns>The angle from the focal point for a given radius</returns>
        /// <param name="radius">Radius.</param>
        /// <param name="semiLatusRectum">Semi latus rectum.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double AngleAtRadus(double radius, double semiLatusRectum, double eccentricity)
        {
            //r = p / (1 + e * cos(θ))
            //1 + e * cos(θ) = p/r
            //((p / r) -1) / e = cos(θ)
            return Math.Acos((semiLatusRectum / radius - 1) / eccentricity);
        }

        /// <summary>
        /// Returns the LoP in 2d space (a retrograde orbits aop will be sign switched)
        /// </summary>
        /// <returns>The of periapsis2d.</returns>
        /// <param name="loAN">Lo an.</param>
        /// <param name="aoP">Ao p.</param>
        /// <param name="inclination">Inclination.</param>
        public static double LonditudeOfPeriapsis2d(double loAN, double aoP, double inclination)
        {
            if (inclination > Math.PI * 0.5 && inclination < Math.PI * 1.5)
            {
                aoP = -aoP;
            }
            return loAN + aoP;
        }

        /// <summary>
        /// Tsiolkovsky's rocket equation.
        /// </summary>
        /// <returns>deltaV</returns>
        /// <param name="wetMass">Wet mass.</param>
        /// <param name="dryMass">Dry mass.</param>
        /// <param name="specificImpulse">Specific impulse.</param>
        public static double TsiolkovskyRocketEquation(double wetMass, double dryMass, double specificImpulse)
        {
            double ve = specificImpulse * 9.8;
            double deltaV = ve * Math.Log(wetMass / dryMass);
            return deltaV;
        }



    }
}
