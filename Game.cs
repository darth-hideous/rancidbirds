using Microsoft.Xna.Framework;
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
        Vector3 handPosition;
        Matrix projectionMatrix;
        Matrix viewMatrix;
        MouseState mouse;
        KeyboardState keyboard;
        readonly Matrix[] handMatrixArray = new Matrix[3];
        Model model;
        Model hands;

        static private readonly float collisionMulti = 1.2f;
        float pointInDirection = 0;
        float bulletPositionX;
        float bulletPositionZ;
        float headBob = 0;
        float bobIntensity = 0;
        float fov = 0;
        bool mousePressed = false;
        float sliding = 0;

        List<float> bulletExpiration = new();
        List<Matrix> bulletPositions = new();

        readonly Vector3[] modelPositions = {
            new(2, 0, 0),
            new(4, 0, 0),
            new(6, 0, 0),
            new(8, 0, 0),
            new(10, 0, 0),
            new(2, 0, 2),
            new(4, 0, 2),
            new(6, 0, 2),
            new(8, 0, 2),
            new(2, 0, 4),
            new(4, 0, 4),
            new(6, 0, 4),
            new(8, 0, 4),
            new(10, 0, 4),
        };
        readonly Vector3[] modelMoveSpeed = {
            new(-0.2f, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, -0.2f),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
            new(0, 0, 0),
        };

        public Game()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "assets";
            Window.AllowUserResizing = true;
            IsMouseVisible = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = false;
            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = true;
        }
        protected override void Initialize()
        {
            base.Initialize();
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);


            renderTarget = new RenderTarget2D(
                GraphicsDevice,
                480,
                270,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);

            camTarget = new Vector3(0f, 0f, 0f);
            camPosition = new Vector3(0f, 0f, -20f);
            handPosition = new Vector3(0, 0, 0);

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                // FOV in radians (degrees being converted to radians)
                MathHelper.ToRadians(90f), GraphicsDevice.DisplayMode.AspectRatio,
                // Render limit for objects that are too close
                // ex. 0 = all objects are rendered, 1 = objects that are 0 away from the camera will not be rendered
                0.01f,
                // Render distance
                20f
                );
            handMatrixArray[2] = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60f), GraphicsDevice.DisplayMode.AspectRatio, 0.01f, 1000f);
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            model = Content.Load<Model>("MonoCube");
            hands = Content.Load<Model>("BulletHole");
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
            int spinKeyboard = 90;
            if (keyboard.IsKeyDown(Keys.Up))
                spinKeyboard *= 5;
            if (keyboard.IsKeyDown(Keys.Down))
                spinKeyboard /= 5;
            if (keyboard.IsKeyDown(Keys.Left))
                pointInDirection += spinKeyboard * deltaTime;
            if (keyboard.IsKeyDown(Keys.Right))
                pointInDirection -= spinKeyboard * deltaTime;
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            if (mouse.LeftButton == ButtonState.Pressed && !mousePressed)
            {
                mousePressed = true;
                fov = 30f;
                bulletPositionX = camPosition.X;
                bulletPositionZ = camPosition.Z;
                float bulletShiftX = (float)(Math.Sin(MathHelper.ToRadians(pointInDirection)) * 0.5f);
                float bulletShiftZ = (float)(Math.Cos(MathHelper.ToRadians(pointInDirection)) * 0.5f);
                bool checkForBulletCollision(Vector3 bulletPosition)
                {
                    foreach (Vector3 vector3 in modelPositions)
                    {
                        if (bulletPosition.X <= vector3.X + 1 &&
                        bulletPosition.Z <= vector3.Z + 1 &&
                        bulletPosition.X >= vector3.X - 1 &&
                        bulletPosition.Z >= vector3.Z - 1)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                for (int i = 0; i < 40; i++)
                {
                    bulletPositionX += bulletShiftX;
                    bulletPositionZ += bulletShiftZ;
                    if (checkForBulletCollision(new Vector3(bulletPositionX, 0, bulletPositionZ)))
                    {
                        for (int j = 0; j < 40; j++)
                        {
                            bulletPositionX -= bulletShiftX / 40;
                            bulletPositionZ -= bulletShiftZ / 40;
                            if (!checkForBulletCollision(new Vector3(bulletPositionX, 0, bulletPositionZ)))
                                break;
                        }
                        break;
                    }
                }
                bulletPositions.Add(Matrix.CreateTranslation(new Vector3(bulletPositionX,
                    camPosition.Y / 2 + (headBob % 1 / 3)+ (pointInDirection % 1 / 3), bulletPositionZ)));
                bulletExpiration.Add(1f);
            }
            else
            {
                if (mouse.LeftButton == ButtonState.Released)
                    mousePressed = false;
            }
            int count_Temporary = bulletExpiration.Count;
            for (int expireCycle = 0; expireCycle < count_Temporary; expireCycle++)
            {
                bulletExpiration[expireCycle] = bulletExpiration[expireCycle] - deltaTime / 20;
                if (bulletExpiration[expireCycle] < 0 || bulletExpiration.Count > 20)
                {
                    bulletExpiration.RemoveAt(expireCycle);
                    bulletPositions.RemoveAt(expireCycle);
                    expireCycle -= 1;
                    count_Temporary -= 1;
                }
            }
            fov -= deltaTime * 120;
            if (fov < 0)
                fov = 0;
            if (Math.Round(fov, 0) != 0)
                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(((float)((Math.Sin((10 * fov / 30 / (Math.PI * 1.0133)) + Math.PI * 3 / 2) + 1) / 2) *
                    10
                    ) + 90), GraphicsDevice.DisplayMode.AspectRatio,
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
            double speed = (((((direction % 360) - 180) * ((direction % 360) - 180) + 138600) / 1800) + 9 * Math.Cos(direction / 9119 / (Math.PI / 500))) / 8;

            for (int i = 0; i < modelPositions.Length; i++)
            {
                Vector3 mp = modelPositions[i];
                Vector3 mm = modelMoveSpeed[i];
                modelPositions[i] = new(mp.X + mm.X * deltaTime,
                    mp.Y + mm.Y * deltaTime,
                    mp.Z + mm.Z * deltaTime);
            }

            if ((speed != 0 && moving) || Math.Round(bobIntensity * 2) != 0)
            {
                if (moving)
                {
                    sliding = direction;
                    bobIntensity += 8 * deltaTime;
                    if (bobIntensity > 1)
                        bobIntensity = 1;
                }
                else
                {
                    direction = sliding;
                    speed = (((((direction % 360) - 180) * ((direction % 360) - 180) + 138600) / 1800) + 9 * Math.Cos(direction / 9119 / (Math.PI / 500))) / 8;
                }
                float camX = (float)(Math.Sin(MathHelper.ToRadians(direction + pointInDirection)) * speed * Ease(bobIntensity)) * deltaTime;
                float camZ = (float)(Math.Cos(MathHelper.ToRadians(direction + pointInDirection)) * speed * Ease(bobIntensity)) * deltaTime;
                camPosition.X += camX;
                camPosition.Z += camZ;
                Vector3 checkForCollision()
                {
                    int cycle = 0;
                    foreach (Vector3 vector3 in modelPositions)
                    {
                        if (camPosition.X <= vector3.X + collisionMulti &&
                        camPosition.Z <= vector3.Z + collisionMulti &&
                        camPosition.X >= vector3.X - collisionMulti &&
                        camPosition.Z >= vector3.Z - collisionMulti)
                        {
                            return modelMoveSpeed[cycle];
                        }
                        cycle++;
                    }
                    return new(0, -1, 0);
                }
                Vector3 temporary_Index = checkForCollision();
                Vector3 short_ = new(0, -1, 0);
                if (temporary_Index != short_)
                {
                    camX -= temporary_Index.X * deltaTime;
                    camZ -= temporary_Index.Z * deltaTime;
                    camPosition.X -= camX;
                    temporary_Index = checkForCollision();
                    if (temporary_Index != short_)
                    {
                        camPosition.X += camX;
                        camPosition.Z -= camZ;
                        temporary_Index = checkForCollision();
                        if (temporary_Index != short_)
                        {
                            camPosition.X -= camX;
                        }
                    }
                }
                headBob += deltaTime * 9;
            }
            camPosition.Y = (float)((Math.Sin(headBob - 1.57) + 1) * (0.1 +
                Ease(bobIntensity)
                * 0.175));
            handPosition.Y = (float)((Math.Sin(headBob - 1.57) + 1) * (0.1 +
                Ease(bobIntensity)
                * 0.175));
            camTarget.X = (float)(Math.Sin(MathHelper.ToRadians(pointInDirection)) * 20 + camPosition.X);
            camTarget.Z = (float)(Math.Cos(MathHelper.ToRadians(pointInDirection)) * 20 + camPosition.Z);
            handPosition.X = (float)(Math.Sin(MathHelper.ToRadians(pointInDirection)) * 0f + camPosition.X);
            handPosition.Z = (float)(Math.Cos(MathHelper.ToRadians(pointInDirection)) * 0f + camPosition.Z);
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);


            handMatrixArray[0] = Matrix.CreateScale(1f, 1f, 1f) * Matrix.CreateRotationY(MathHelper.ToRadians(pointInDirection))
                * Matrix.CreateTranslation(new Vector3(handPosition.X, handPosition.Y, handPosition.Z));
            handMatrixArray[1] = Matrix.CreateLookAt(camPosition, new Vector3(camTarget.X, camPosition.Y, camTarget.Z), Vector3.Up);


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
                    float temporaryShaderLightTracker = (float)((Math.Sin((10 * fov / 30 / (Math.PI * 1.0133)) + Math.PI * 3 / 2) + 1) / 2) * 4;
                    effect.EmissiveColor = new Vector3(temporaryShaderLightTracker, temporaryShaderLightTracker, temporaryShaderLightTracker);
                    effect.FogEnabled = true;
                    effect.FogColor = Color.Black.ToVector3();
                    effect.FogStart = 5f;
                    effect.FogEnd = 20f;

                    effect.View = viewMatrix;
                    effect.World = Matrix.CreateWorld(new Vector3(camPosition.X, 0, camPosition.Z), Vector3.Forward, Vector3.Up);
                    effect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }

            // model.Draw(worldMatrix, viewMatrix, projectionMatrix);
            foreach (Vector3 vector3 in modelPositions)
            {
                model.Draw(Matrix.CreateWorld(vector3, Vector3.Forward, Vector3.Up), viewMatrix, projectionMatrix);
            }

            //
            // <draw hands>
            // 

            // bullets
            int bulletPosCount = bulletExpiration.Count;
            for (int matrixCycle = 0; matrixCycle < bulletPosCount; matrixCycle++)
            {
                float float_f = (float)((Math.Sin((10 * bulletExpiration[matrixCycle] / (Math.PI * 1.0133)) + Math.PI * 3 / 2) + 1) / 2) * 0.05f;
                if (matrixCycle == 0)
                {
                    foreach (ModelMesh mesh in hands.Meshes)
                    {
                        foreach (BasicEffect effect in mesh.Effects.Cast<BasicEffect>())
                        {
                            effect.LightingEnabled = true;
                            float temporaryShaderLightTracker = (float)((Math.Sin((10 * fov / 30 / (Math.PI * 1.0133)) + Math.PI * 3 / 2) + 1) / 2);
                            effect.Alpha = 1.2f - temporaryShaderLightTracker;

                            effect.View = viewMatrix;
                            effect.World = Matrix.CreateScale(float_f, float_f, float_f)
                                * bulletPositions[matrixCycle];
                            effect.Projection = projectionMatrix;
                        }
                        mesh.Draw();
                    }
                }
                else
                {
                    hands.Draw(Matrix.CreateScale(float_f, float_f, float_f)
                        * bulletPositions[matrixCycle],
                        viewMatrix,
                        projectionMatrix);
                }
            }


            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            // hands, weapons, etc. positioned with these values :
            hands.Draw(handMatrixArray[0], handMatrixArray[1], handMatrixArray[2]);


            //
            // </draw hands>
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
