﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace MonoGear
{
    class Player : WorldEntityAnimated
    {
        public float Speed { get; set; }
        private SoundEffectInstance walkingSound;

        public Player() : base()
        {
            // Speed in units/sec. Right now 1 unit = 1 pixel
            Speed = 100.0f;
            TextureAssetName = "Sprites/guardsheet";

            AnimationLength = 3;
            AnimationCurrentFrame = 1;
            AnimationDelta = 0.1f;
            AnimationPingPong = true;

            Tag = "Player";

            LoadContent();

            Collider = new BoxCollider(this, new Vector2(8));
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            walkingSound = ResourceManager.GetManager("Global").GetResource<SoundEffect>("Audio/AudioFX/Running On Grass").CreateInstance();
        }

        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();

            var ents = MonoGearGame.FindEntitiesWithTag("PlayerSpawnPoint");
            if(ents.Count > 0)
            {
                Position = new Vector2(ents[0].Position.X, ents[0].Position.Y);
            }
        }

        public override void Update(Input input, GameTime gameTime)
        {
            if(!Enabled)
                return;

            base.Update(input, gameTime);

            var dx = 0.0f;
            var dy = 0.0f;
            if(input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left))
                dx -= Speed;
            if(input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right))
                dx += Speed;
            if(input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up))
                dy -= Speed;
            if(input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down))
                dy += Speed;

            var delta = new Vector3(dx, dy, 0);
            if(delta.LengthSquared() > Speed * Speed)
            {
                delta.Normalize();
                delta *= Speed;
            }

            if(delta.LengthSquared() > 0)
            {
                Rotation = (float)(Math.Atan2(delta.Y, delta.X) - Math.PI * 0.5);
                AnimationRunning = true;
                AudioManager.GlobalAudioPlay(walkingSound);
            }
            else
            {
                AnimationRunning = false;
                AnimationCurrentFrame = 1;
                AudioManager.GlobalAudioStop(walkingSound);
            }

            // Check collisions
            var prevPos = Position;
            var deltaX = new Vector2(delta.X, 0);
            var deltaY = new Vector2(0, delta.Y);

            Position += deltaX * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(Collider.CollidesAny())
            {
                Position = prevPos;
            }
            prevPos = Position;
            Position += deltaY * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(Collider.CollidesAny())
            {
                Position = prevPos;
            }

            Camera.main.Position = new Vector2(Position.X, Position.Y);
        }
    }
}

