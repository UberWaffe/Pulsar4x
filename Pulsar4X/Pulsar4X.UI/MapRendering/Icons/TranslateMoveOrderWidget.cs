﻿using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using static SDL2.SDL;

namespace Pulsar4X.VeldridUI
{
    public class TranslateMoveOrderWidget : IDrawData
    {
        public SDL_Color TransitLineColor = new SDL_Color() { r = 0, g = 255, b = 255, a = 100 };

        Entity _movingEntity;
        OrbitDB _movingEntityCurrentOrbit;
        DateTime _currentDateTime;
        DateTime _transitLeaveDateTime;

        PositionDB _parentPositionDB;

        Entity _targetEntity;

        //List<Icon> _icons = new List<Icon>();

        GlobalUIState _state;

        TransitIcon _departIcon;
        TransitIcon _arriveIcon;

        PositionDB _targetPositionDB;

        Vector4 _transitLeavePositionRalitive; //ralitive to the parentBody
        Vector4 _transitArrivePosition;

        SDL_Point[] _linePoints;

        double _progradeAngle;
        double _arrivePntRadius;

        public TranslateMoveOrderWidget(GlobalUIState state, Entity orderingEntity)
        {
            _state = state;
            _currentDateTime = _state.CurrentSystemDateTime;

            _movingEntity = orderingEntity;

            Setup();
        }

        void Setup()
        {
            _movingEntityCurrentOrbit = _movingEntity.GetDataBlob<OrbitDB>();
            _transitLeaveDateTime = _currentDateTime;

            //_transitLeavePosition = OrbitProcessor.GetAbsolutePosition_AU(_movingEntityCurrentOrbit, _transitLeaveDateTime);
            _parentPositionDB = _movingEntityCurrentOrbit.Parent.GetDataBlob<PositionDB>();
            _departIcon = TransitIcon.CreateDepartIcon(_parentPositionDB);
            OnPhysicsUpdate();
            //_icons.Add(new TransitIcon(_targetPositionDB, _transitArrivePosition));
        }

        public void SetDepartDateTime(DateTime dateTime)
        {
            if (dateTime > _currentDateTime)
                _transitLeaveDateTime = dateTime;
            else
                _transitLeaveDateTime = _currentDateTime;
            OnPhysicsUpdate();
        }



        public void SetArrivalTarget(Entity targetEntity)
        {
            _targetEntity = targetEntity;
            _targetPositionDB = _targetEntity.GetDataBlob<PositionDB>();

            _arriveIcon = TransitIcon.CreateArriveIcon(_targetPositionDB);
            //these are ralitive to thier respective bodies, for the initial default, copying the position shoul be fine.
            //however a better default would djust the distance from the target to get a circular orbit and
            //check if it's above minimum and that the resulting orbit is within soi 
             
            OnPhysicsUpdate();
        }

        public void SetArrivalPosition(Vector4 ralitiveWorldPosition)
        {
            _transitArrivePosition = ralitiveWorldPosition;
            _arriveIcon.SetTransitPostion(_transitArrivePosition);
        }

        public void SetArrivalRadius(double radius)
        {
            _arrivePntRadius = radius;
            _arriveIcon.SetTransitPostion(_progradeAngle, radius);
        }




        public void OnPhysicsUpdate()
        {
            _currentDateTime = _state.CurrentSystemDateTime;
            if (_transitLeaveDateTime < _currentDateTime)
                _transitLeaveDateTime = _currentDateTime;


            _transitLeavePositionRalitive = OrbitProcessor.GetPosition_AU(_movingEntityCurrentOrbit, _transitLeaveDateTime);


            _progradeAngle = Math.Atan2(_transitLeavePositionRalitive.Y , _transitLeavePositionRalitive.X);
            //OrbitProcessor.PreciseOrbitalVector(
            _progradeAngle -= Math.PI ;
            _departIcon.ProgradeAngle = _progradeAngle;
            _departIcon.SetTransitPostion(_transitLeavePositionRalitive);


            if (_arriveIcon != null)
            {
                _arriveIcon.ProgradeAngle = _progradeAngle;
                _arriveIcon.SetTransitPostion(_progradeAngle, _arrivePntRadius);

            }
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            _departIcon.OnFrameUpdate(matrix, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.OnFrameUpdate(matrix, camera);

                var camerapoint = camera.CameraViewCoordinate();

                _linePoints = new SDL_Point[2];

                var depart = matrix.Transform(_departIcon.WorldPosition.X, _departIcon.WorldPosition.Y);
                _linePoints[0] = new SDL_Point()
                {
                    x = (depart.x + camerapoint.x),
                    y = (depart.y + camerapoint.y)
                };

                var arrive = matrix.Transform(_arriveIcon.WorldPosition.X, _arriveIcon.WorldPosition.Y);
                _linePoints[1] = new SDL_Point()
                {
                    x = (arrive.x + camerapoint.x),
                    y = (arrive.y + camerapoint.y)
                };




            }

        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            _departIcon.Draw(rendererPtr, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.Draw(rendererPtr, camera);
                //draw the transitLine
                SDL_SetRenderDrawColor(rendererPtr, TransitLineColor.r, TransitLineColor.g, TransitLineColor.b, TransitLineColor.a);
                SDL_RenderDrawLine(rendererPtr, _linePoints[0].x, _linePoints[0].y, _linePoints[1].x, _linePoints[1].y);

            }
        }
    }



    public class TransitIcon : Icon
    {
        public SDL_Color PrimaryColour = new SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
        public SDL_Color VectorColour = new SDL_Color() { r = 255, g = 0, b = 255, a = 255 };

        public double ProgradeAngle = 0;
        double _arrivePntRadius;
        PositionDB _parentPosition;
        //PositionDB _myPosition;

        //DateTime TransitDateTime;
        //Vector4 _transitPosition;



        private TransitIcon(PositionDB parentPos) : base(parentPos)
        {
            _parentPosition = parentPos;
            positionByDB = true;
            Setup();
        }

        public static TransitIcon CreateArriveIcon(PositionDB targetPosition)
        {
            var icon = new TransitIcon(targetPosition);
            icon.CreateCheverons(0, -13);
            return icon;
        }

        public static TransitIcon CreateDepartIcon(PositionDB targetPosition)
        {
            var icon = new TransitIcon(targetPosition);
            icon.CreateCheverons(0, 11);
            return icon;
        }

        void Setup()
        {

            Shapes = new List<Shape>(5);
            CreateProgradeArrow();

            Shape dot = new Shape()
            {
                Points = CreatePrimitiveShapes.Circle(0, 0, 3, 6),
                Color = PrimaryColour
            };
            Shape circle = new Shape()
            {
                Points = CreatePrimitiveShapes.Circle(0, 0, 8, 12),
                Color = PrimaryColour
            };

            //Shapes[0] = vectorArrow; 
            //Shapes[1] = dot;
            //Shapes[2] = circle;
            //Shapes[3] = chevron;
            //Shapes[4] = chevron2;
            Shapes.Add(dot);
            Shapes.Add(circle);

        }

        void CreateCheverons(int x, int y)
        {
            PointD[] chevronPoints1 = new PointD[3];
            chevronPoints1[0] = new PointD() { X = x - 4, Y = y + 3 };
            chevronPoints1[1] = new PointD() { X = x + 0, Y = y - 3 };
            chevronPoints1[2] = new PointD() { X = x + 4, Y = y + 3 };
            Shape chevron = new Shape()
            {
                Points = chevronPoints1,
                Color = PrimaryColour
            };
            PointD[] chevronPoints2 = new PointD[3];
            chevronPoints2[0] = new PointD() { X = x - 4, Y = y + 7 };
            chevronPoints2[1] = new PointD() { X = x + 0, Y = y + 1 };
            chevronPoints2[2] = new PointD() { X = x + 4, Y = y + 7 };
            Shape chevron2 = new Shape()
            {
                Points = chevronPoints2,
                Color = PrimaryColour
            };

            Shapes.Add(chevron);
            Shapes.Add(chevron2);
        }

        void CreateProgradeArrow()
        {


            PointD[] pnts = CreatePrimitiveShapes.CreateArrow(24);
            List<PointD> arrowPoints = new List<PointD>(pnts.Length);
            foreach (var point in pnts)
            {
                double x = point.X * Math.Cos(ProgradeAngle) + point.Y * Math.Sin(ProgradeAngle);
                double y = point.X * -Math.Sin(ProgradeAngle) + point.Y * Math.Cos(ProgradeAngle);
                arrowPoints.Add(new PointD() { X = x, Y = y });
            }

            Shape vectorArrow = new Shape()
            {
                Points = arrowPoints.ToArray(),
                Color = VectorColour
            };

            if (Shapes.Count < 1)
                Shapes.Add(vectorArrow);
            else
                Shapes[0] = vectorArrow;

        }



        /// <summary>
        /// Sets the transit postion.
        /// </summary>
        /// <param name="transitPositionOffset">Transit position offset, this is the world position ralitive to the parent body</param>
        public void SetTransitPostion(Vector4 transitPositionOffset)
        {
            _worldPosition = transitPositionOffset;
            OnPhysicsUpdate();
        }

        /// <summary>
        /// Sets the transit position from the prograde Angle and distance from the body. 
        /// </summary>
        /// <param name="progradeAngle">Prograde angle.</param>
        /// <param name="radius_AU">Radius au.</param>
        public void SetTransitPostion(double progradeAngle, double radius_AU)
        {
            ProgradeAngle = progradeAngle;
            _arrivePntRadius = radius_AU;
            var theta = progradeAngle + Math.PI * 0.5;
            _worldPosition.X = Math.Sin(theta) * radius_AU;
            _worldPosition.Y = Math.Cos(theta) * radius_AU;
            OnPhysicsUpdate();
        }


        public override void OnPhysicsUpdate()
        {
            base.OnPhysicsUpdate();
            CreateProgradeArrow();
        }


    }
}
