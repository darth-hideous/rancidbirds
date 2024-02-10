﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rancidBirds
{
    public class Game : Microsoft.Xna.Framework.Game
    {
        // BasicEffect effect;
        RenderTarget2D renderTarget;
        SpriteBatch _spriteBatch;
        readonly GraphicsDeviceManager _graphics;
        Vector3 camTarget;
        Vector3 camPosition;
        Matrix projectionMatrix;
        Matrix viewMatrix;
        Matrix worldMatrix;
        MouseState mouse;
        KeyboardState keyboard;
        Model model;
        // Model floorPlane;
        float pointInDirection = 0;
        float headBob = 0;
        float bobIntensity = 0;
        float fov = 0;
        bool mousePressed = false;


        readonly Vector3[] modelPositions = {
            new(2, 0, 0), 
            new(4, 0, 0), 
            new(6, 0, 0),
            new(8, 0, 0),
            new(10, 0, 0)
        };

        public Game()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "assets";
            Window.AllowUserResizing = true;
            IsMouseVisible = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = true;
        }
        protected override void Initialize()
        {
            base.Initialize();


            renderTarget = new RenderTarget2D(
                GraphicsDevice,
                480,
                270,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);

            camTarget = new Vector3(0f, 0f, 0f);
            camPosition = new Vector3(0f, 0f, -20f);

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                // FOV in radians (degrees being converted to radians)
                MathHelper.ToRadians(90f), GraphicsDevice.DisplayMode.AspectRatio,
                // Render limit for objects that are too close
                // ex. 0 = all objects are rendered, 1 = objects that are 0 away from the camera will not be rendered
                0.001f,
                // Render distance
                20f
                );
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            worldMatrix = Matrix.CreateWorld(camTarget, Vector3.Forward, Vector3.Up);
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            model = Content.Load<Model>("MonoCube");
            // floorPlane = Content.Load<Model>("floor_temp");
        }
        protected override void Update(GameTime gameTime)
        {
            keyboard = Keyboard.GetState();
            mouse = Mouse.GetState();
            double Ease(double easingVal) => (Math.Sin((10 * easingVal / (Math.PI * 1.0133)) + Math.PI * 3 / 2) + 1) / 2;
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float direction = 0;
            bool moving = false;
            headBob += deltaTime;
            pointInDirection += (mouse.X - GraphicsDevice.Viewport.Width / 2) * -0.2f;
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            if (mouse.LeftButton == ButtonState.Pressed && !mousePressed)
            {
                mousePressed = true;
                fov = 30f;
            }
            else
            {
                if (mouse.LeftButton == ButtonState.Released)
                    mousePressed = false;
            }
            fov -= deltaTime * 120;
            if (fov < 0)
                fov = 0;
            if (Math.Round(fov, 0) != 0)
                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov + 90), GraphicsDevice.DisplayMode.AspectRatio,
                    0.001f, 1000f);
            void KeyboardMovement()
            {
                List<float> directionTotals = new();
                void Move(int direction)
                {
                    directionTotals.Add(direction);
                    moving = true;
                }
                if (keyboard.IsKeyDown(Keys.Escape))
                    Exit();
                if (keyboard.IsKeyDown(Keys.W))
                    Move(0);
                if (keyboard.IsKeyDown(Keys.A))
                    Move(90);
                if (keyboard.IsKeyDown(Keys.S))
                    Move(180);
                if (keyboard.IsKeyDown(Keys.D))
                    Move(270);
                foreach (float f in directionTotals)
                {
                    direction += f;
                }
                direction /= directionTotals.Count;
                if (directionTotals.Count == 0)
                    direction = 0;
                if (keyboard.IsKeyDown(Keys.W) && keyboard.IsKeyDown(Keys.D))
                    direction = 315;
                if (keyboard.IsKeyDown(Keys.W) && keyboard.IsKeyDown(Keys.S))
                    moving = false;
                if (keyboard.IsKeyDown(Keys.A) && keyboard.IsKeyDown(Keys.D))
                    moving = false;
            }
            KeyboardMovement();
            if (headBob > 628.31)
                headBob -= 628.31f;
            bobIntensity -= 4 * deltaTime;
            if (bobIntensity < 0)
                bobIntensity = 0;
            double speed = (((((direction%360)-180)*((direction%360)-180)+138600)/1800)+9*Math.Cos(direction/9119/(Math.PI/500)))/8;
            if (speed!= 0 && moving)
            {
                bobIntensity += 8 * deltaTime;
                if (bobIntensity > 1)
                    bobIntensity = 1;
                float camX = (float)(Math.Sin(MathHelper.ToRadians(direction + pointInDirection)) * speed * Ease(bobIntensity)) * deltaTime;
                float camZ = (float)(Math.Cos(MathHelper.ToRadians(direction + pointInDirection)) * speed * Ease(bobIntensity)) * deltaTime;
                camPosition.X += camX;
                camPosition.Z += camZ;
                foreach (Vector3 vector3 in modelPositions)
                {
                    if (Math.Round(camPosition.X / 2) == vector3.X / 2 && Math.Round(camPosition.Z / 2) == vector3.Z / 2)
                    {
                        camPosition.X -= camX;
                        camPosition.Z -= camZ;
                        bobIntensity = 0;
                    }
                }
                headBob += deltaTime * 9;
            }
            camPosition.Y = (float)((Math.Sin(headBob - 1.57) + 1) * (0.1 + 
                Ease(bobIntensity)
                * 0.175));
            camTarget.X = (float)(Math.Sin(MathHelper.ToRadians(pointInDirection)) * 20 + camPosition.X);
            camTarget.Z = (float)(Math.Cos(MathHelper.ToRadians(pointInDirection)) * 20 +     camPosition.Z);
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            //
            // <draw scene>
            //

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects.Cast<BasicEffect>())
                {
                    effect.LightingEnabled = true;
                    effect.DiffuseColor = new Vector3(1, 0.8f, 0.9f);
                    effect.AmbientLightColor = new Vector3(1.2f, 1, 1);
                    effect.EmissiveColor = new Vector3(0, 0, 0);
                    effect.FogEnabled = true;
                    effect.FogColor = Color.Black.ToVector3();
                    effect.FogStart = 5f;
                    effect.FogEnd = 20f;

                    effect.View = viewMatrix;
                    effect.World = worldMatrix;
                    effect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }

            // model.Draw(worldMatrix, viewMatrix, projectionMatrix);
            foreach (Vector3 vector3 in modelPositions)
            {
                model.Draw(Matrix.CreateWorld(vector3, Vector3.Forward, Vector3.Up), viewMatrix, projectionMatrix);
            }
            // floorPlane.Draw(Matrix.CreateWorld(new Vector3(camPosition.X, -1f, camPosition.Z), Vector3.Forward, Vector3.Up),
            //     viewMatrix, projectionMatrix);

            //
            // </draw scene>
            //

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.White);

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.Default,
                        RasterizerState.CullCounterClockwise);

            _spriteBatch.Draw(renderTarget, new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth,
                        GraphicsDevice.PresentationParameters.BackBufferHeight), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}