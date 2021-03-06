﻿using System;
using ImGuiNET;
using Veldrid.Sdl2;
using Pulsar4X.ECSLib;

//using Point = Veldrid.Sdl2.Sdl2Native.SDL_Point;
namespace Pulsar4X.VeldridUI
{


    public class Camera
    {
        internal bool IsGrabbingMap = false;
        internal int MouseFrameIncrementX;
        internal int MouseFrameIncrementY;

        internal bool IsPinnedToEntity { get; private set; }
        internal Guid PinnedEntityGuid;
        PositionDB _entityPosDB;
        Vector4 _camWorldPos = new Vector4();
        public Vector4 CameraWorldPosition
        {
            get
            {
                if (IsPinnedToEntity && _entityPosDB != null)
                    return new Vector4
                    {
                        X = _camWorldPos.X - _entityPosDB.AbsolutePosition_AU.X,
                        Y = -(_camWorldPos.Y - _entityPosDB.AbsolutePosition_AU.Y)
                    };
                else
                    return _camWorldPos;
            }
            set
            {
                if (IsPinnedToEntity)
                {
                    IsPinnedToEntity = false;
                }
                _camWorldPos = value;
            }
        }
                
        //public ImVec2 WorldPosition { get { return _cameraWorldPosition; } }

        public ImVec2 ViewPortCenter { get { return new ImVec2(_viewPort.Size.X * 0.5f, _viewPort.Size.Y * 0.5f); }}

        public ImVec2 ViewPortSize { get { return _viewPort.Size; } }
        public float ZoomLevel { get; set; } = 200;
        public float zoomSpeed { get; set; } = 1.25f;

        public Sdl2Window _viewPort;

        double MAX_ZOOMLEVEL = 1.496e+11;

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera(Sdl2Window viewPort)
        {
            _viewPort = viewPort;
            //_viewPort.SizeChanged += _viewPort_SizeChanged;

        }

        public void PinToEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _entityPosDB = entity.GetDataBlob<PositionDB>();
                _camWorldPos = new Vector4(); //zero on it. 
                IsPinnedToEntity = true;
                PinnedEntityGuid = entity.Guid;
            }
        }

        public void CenterOnEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _camWorldPos = entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            }
        }

        public Point CameraViewCoordinate()
        {
            Point point = new Point();
            point.x = (int)(CameraWorldPosition.X * (ZoomLevel) + ViewPortCenter.x);
            point.y = (int)(CameraWorldPosition.Y * (ZoomLevel) + ViewPortCenter.y);
            return point;
        }
        /// <summary>
        /// THIS MAY RETURN INCORRECT VALUE TODO: Debug/Test this (might need to adjust for CameraWorldPosition)
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(Vector4 worldCoord)
        {
            int x = (int)(worldCoord.X * (ZoomLevel) + ViewPortCenter.x);
            int y = (int)(worldCoord.Y * (ZoomLevel) + ViewPortCenter.y);
            Point viewCoord = new Point() {x = x ,y = y  };

            return viewCoord;
        }


        public Vector4 MouseWorldCoordinate()
        {
            ImVec2 mouseCoord = ImGui.GetMousePos();
            double x = ((mouseCoord.x - ViewPortCenter.x) / ZoomLevel) - CameraWorldPosition.X;
            double y = -(((mouseCoord.y - ViewPortCenter.y) / ZoomLevel) - CameraWorldPosition.Y); 
            return new Vector4(x, y, 0, 0);

        }

        /// <summary>
        /// returns the worldCoordinate of a given View Coordinate 
        /// </summary>
        /// <param name="viewCoordinate"></param>
        /// <returns></returns>
        public Vector4 WorldCoordinate(int viewCoordinateX, int viewCoordinateY)
        {
            double x = ((viewCoordinateX - ViewPortCenter.X) / ZoomLevel) - CameraWorldPosition.X;
            double y = ((viewCoordinateY - ViewPortCenter.Y) / ZoomLevel) - CameraWorldPosition.Y;
            return new Vector4(x, y, 0, 0);
        }


        /// <summary>
        /// Returns the size of an object in view-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public ImVec2 ViewSize(ImVec2 worldSize)
        {
            ImVec2 viewSize = new ImVec2( worldSize.X * ZoomLevel, worldSize.Y * ZoomLevel);
            return viewSize;
        }

        /// <summary>
        /// Returns the Distance in view-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public float ViewDistance(double dist)
        {
            return (float)(dist * ZoomLevel);
        }

        /// <summary>
        /// Returns the Distance in World-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public double WorldDistance(float dist)
        {
            return dist / ZoomLevel;
        }

        /// <summary>
        /// Returns the size of an object in world-Coordinates
        /// </summary>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public ImVec2 WorldSize(ImVec2 viewSize)
        {
            return new ImVec2(viewSize.x / ZoomLevel, viewSize.y / ZoomLevel);
        }


        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// <param name="xOffset">Pans the camera horizontaly relative to offset</param>
        /// <param name="yOffset">Pans the camera verticaly relative to offset</param>
        /// </summary>
        public void WorldOffset(double xOffset, double yOffset)
        {
            if (IsPinnedToEntity)
            {
                _camWorldPos.X -= (float)(xOffset * 1.0f / ZoomLevel);
                _camWorldPos.Y -= (float)(-yOffset * 1.0f / ZoomLevel);
            }
            else
            {
                _camWorldPos.X -= (float)(xOffset * 1.0f / ZoomLevel);
                _camWorldPos.Y -= (float)(yOffset * 1.0f / ZoomLevel);
            }
        }


        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.x - (mouseX - ViewPortCenter.x) * zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.y - (mouseY - ViewPortCenter.y) * zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }


        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
        public void ZoomOut(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);

            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.x - (mouseX - ViewPortCenter.x) / zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.y - (mouseY - ViewPortCenter.y) / zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }

        /// <summary>
        /// Returns the translation matrix for a world position, relative to the camera position
        /// </summary>
        /// <param name="position">Position in World Units</param>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix(ImVec2 position)
        {
            var transformMatrix = new Matrix();
            double x = CameraWorldPosition.X + position.x;
            double y = CameraWorldPosition.Y + position.y;
            transformMatrix.Translate(x, y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }

        /// <summary>
        /// Returns the translation matrix for 0,0, relative to the camera position
        /// </summary>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix()
        {
            var transformMatrix = new Matrix();

            transformMatrix.Translate(CameraWorldPosition.X, CameraWorldPosition.Y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }
    }

    /// <summary>
    /// Cursor crosshair.
    /// Primarily made to debug a problem with getting the world coordinate of the mouse cursor. 
    /// </summary>
    class CursorCrosshair : Icon
    {
        public CursorCrosshair(Vector4 position) : base(position)
        {
            var colour = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };

            PointD point0 = new PointD() { X = -5, Y = 0 };
            PointD point1 = new PointD() { X = +5, Y = 0 };
            Shape shape0 = new Shape() { Points = new PointD[2] { point0, point1 }, Color = colour };

            PointD point2 = new PointD() { X = 0, Y = -5 };
            PointD point3 = new PointD() { X = 0, Y = +5 };
            Shape shape1 = new Shape() { Points = new PointD[2] { point2, point3 }, Color = colour };

            this.Shapes = new System.Collections.Generic.List<Shape>() { shape0, shape1 };
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            WorldPosition = camera.MouseWorldCoordinate();
            base.OnFrameUpdate(matrix, camera);
        }

    }
}
