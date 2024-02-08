using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using static System.Net.Mime.MediaTypeNames;

namespace rancidBirds
{
    public class Game1 : Game
    {
        private SpriteBatch _spriteBatch;
        private readonly GraphicsDeviceManager _graphics;
        Vector3 camTarget;
        Vector3 camPosition;
        Matrix projectionMatrix;
        Matrix viewMatrix;
        Matrix worldMatrix;
        MouseState mouse;
        KeyboardState keyboard;
        SpriteFont arial;
        Model model;
        float pointInDirection = 0;
        float headBob = 0;
        float bobIntensity = 0;
        float fov = 0;
        float delta = 0;
        bool mousePressed = false;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            IsMouseVisible = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            IsFixedTimeStep = false;
        }
        protected override void Initialize()
        {
            base.Initialize();

            camTarget = new Vector3(0f, 0f, 0f);
            camPosition = new Vector3(0f, 0f, -20f);

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                // FOV in radians (degrees being converted to radians)
                MathHelper.ToRadians(90f), GraphicsDevice.DisplayMode.AspectRatio,
                // Render limit for objects that are too close
                // ex. 0 = all objects are rendered, 1 = objects that are 0 away from the camera will not be rendered
                0.001f,
                // Render distance
                1000f
                );
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            worldMatrix = Matrix.CreateWorld(camTarget, Vector3.Forward, Vector3.Up);
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            model = Content.Load<Model>("MonoCube");
            arial = Content.Load<SpriteFont>("arial");
        }
        protected override void Update(GameTime gameTime)
        {
            keyboard = Keyboard.GetState();
            mouse = Mouse.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            delta = 1 / deltaTime;
            float speedX = 0;
            float speedZ = 0;
            float direction = 0;
            bool moving = false;
            List<float> speedXTotals = new();
            List<float> speedZTotals = new();
            List<float> directionTotals = new();
            headBob += deltaTime;
            pointInDirection += (mouse.X - _graphics.GraphicsDevice.Viewport.Width / 2) * -0.2f;
            Mouse.SetPosition(_graphics.GraphicsDevice.Viewport.Width / 2, 0);
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
                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov + 90), GraphicsDevice.DisplayMode.AspectRatio, 0.001f, 1000f);
            void Move(float speedX, float speedZ)
            {
                speedXTotals.Add(speedX);
                speedZTotals.Add(speedZ);
                moving = true;
            }
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();
            if (keyboard.IsKeyDown(Keys.W))
                Move(13, 0);
            if (keyboard.IsKeyDown(Keys.A))
                Move(0, -10.5f);
            if (keyboard.IsKeyDown(Keys.S))
                Move(-8.5f, 0);
            if (keyboard.IsKeyDown(Keys.D))
                Move(0, 10.5f);
            if (headBob > 628.31)
                headBob -= 628.31f;
            foreach (float f in speedXTotals)
                speedX += f;
            speedX /= speedXTotals.Count;
            foreach (float f in speedZTotals)
                speedZ += f;
            speedZ /= speedZTotals.Count;
            if (speedX > 0)
                directionTotals.Add(pointInDirection);
            if (speedX < 0)
                directionTotals.Add(pointInDirection - 180);
            if (speedZ > 0)
                directionTotals.Add(pointInDirection - 90);
            if (speedZ < 0)
                directionTotals.Add(pointInDirection + 90);
            foreach (float f in directionTotals)
            {
                direction += f;
            }
            direction /= directionTotals.Count;
            if (keyboard.IsKeyDown(Keys.A) && keyboard.IsKeyDown(Keys.S))
            {
                direction = pointInDirection + 135;
                if (keyboard.IsKeyDown(Keys.D))
                    direction = pointInDirection + 180;
            }
            float speed = (Math.Abs(speedX) + Math.Abs(speedZ)) / 1.1f;
            if (speed != 0 && moving)
            {
                float camX = (float)(Math.Sin(MathHelper.ToRadians(direction)) * speed) * deltaTime;
                float camZ = (float)(Math.Cos(MathHelper.ToRadians(direction)) * speed) * deltaTime;
                camPosition.X += camX;
                camPosition.Z += camZ;
                float camXZ = deltaTime * (float)Math.Sqrt(camX * camX + camZ * camZ);
                headBob += deltaTime * 9;
                bobIntensity = 2;
            }
            else
            {
                bobIntensity = 0;
            }
            camPosition.Y = (float)((Math.Sin(headBob - 1.57) + 1) * (0.1 + bobIntensity * 0.175));
            camTarget.X = (float)(Math.Sin(MathHelper.ToRadians(pointInDirection)) * 20 + camPosition.X);
            camTarget.Z = (float)(Math.Cos(MathHelper.ToRadians(pointInDirection)) * 20 + camPosition.Z);
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            model.Draw(worldMatrix, viewMatrix, projectionMatrix);
            model.Draw(Matrix.CreateWorld(new Vector3(0f, 0.5f, 5f), Vector3.Forward, Vector3.Up), viewMatrix, projectionMatrix);
            model.Draw(Matrix.CreateWorld(new Vector3(5f, 1f, 2.5f), Vector3.Forward, Vector3.Up), viewMatrix, projectionMatrix);
            _spriteBatch.Begin();
            Vector2 position = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
            _spriteBatch.DrawString(arial, delta.ToString(), position, Color.Black, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0.5f);
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
