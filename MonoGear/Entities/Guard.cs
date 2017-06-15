﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using MonoGear.Engine;
using MonoGear.Engine.Collisions;
using MonoGear.Engine.Audio;

namespace MonoGear.Entities
{
    class Guard : WorldEntityAnimated, IDestroyable
    {
        private static Texture2D fovSprite;
        private static Color fovColor = new Color(100, 0, 0, 10);

        private static Texture2D alertSprite;
        private static Texture2D searchSprite;
        private static Texture2D sleepSprite;

        public float Health { get; set; }

        public enum State
        {
            Idle,
            Interested,         // Interested in a location, walks to it
            Alerted,            // Alterted to a location, runs to it
            Patrolling,         // Following patrol path
            ToPatrol,           // Pathfinding to patrol path
            ToAlert,            // Pathfinding to alert location
            ToInterest,         // Pathfinding to interest location
            Searching,          // Waiting
            Pursuit,            // Following the player
            Sleeping,           // Sleeping
        }

        public float WalkSpeed { get; set; }
        public float RunSpeed { get; set; }

        private float hearingRange;
        public float SightRange { get; set; }
        private float sightFov;
        private Player player;
        private Vector2 playerPos;
        private PositionalAudio sound;

        public List<Vector2> PatrolPath
        {
            get
            {
                return patrolPath;
            }
            set
            {
                patrolPath = value;
                patrolPathIndex = 0;

                if (state == State.Patrolling)
                {
                    StartPatrol();
                }
            }
        }
        private int patrolPathIndex;

        private List<Vector2> patrolPath;
        private List<Vector2> currentPath;
        private int currentPathIndex;

        private float searchTime;
        private float searchStartTime;

        private float shootTime;
        private float shootStartTime;

        State state;

        public Guard()
        {
            // Speed in units/sec. Right now 1 unit = 1 pixel
            WalkSpeed = 35.0f;
            RunSpeed = 90.0f;
            searchTime = 2.5f;  // sec

            shootTime = 0.4f;

            hearingRange = 75f;
            SightRange = 295f;
            sightFov = 90f;

            TextureAssetName = "Sprites/Guard";

            AnimationLength = 3;
            AnimationCurrentFrame = 1;
            AnimationDelta = 0.05f;
            AnimationPingPong = true;
            AnimationRunning = false;

            Tag = "Guard";

            Z = 100;

            LoadContent();

            Collider = new BoxCollider(this, new Vector2(8));
        }

        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();
            player = MonoGearGame.FindEntitiesWithTag("Player")[0] as Player;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (alertSprite == null)
            {
                alertSprite = MonoGearGame.GetResource<Texture2D>("Sprites/Alert");
            }
            if (searchSprite == null)
            {
                searchSprite = MonoGearGame.GetResource<Texture2D>("Sprites/Searching");
            }
            if (sleepSprite == null)
            {
                sleepSprite = MonoGearGame.GetResource<Texture2D>("Sprites/Sleeping");
            }
            if(fovSprite == null)
            {
                fovSprite = MonoGearGame.GetResource<Texture2D>("Sprites/fov100");
            }
        }

        public override void Update(Input input, GameTime gameTime)
        {
            base.Update(input, gameTime);

            

            //Debug.WriteLine("Guard is " + state);

            if(state == State.Sleeping)
            {
                return;
            }

            if(state == State.Pursuit)
            {
                AnimationRunning = true;
                var target = playerPos;
                if(Vector2.DistanceSquared(Position, target) > 90)
                {
                    // Move towards player
                    Rotation = MathExtensions.VectorToAngle(target - Position);

                    var delta = MathExtensions.AngleToVector(Rotation) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    delta *= RunSpeed;
                    AnimationDelta = 0.05f;
                    Move(delta);
                }
            }
            // Follow current path
            else if(currentPath != null && currentPathIndex >= 0)
            {
                AnimationRunning = true;
                if(currentPathIndex < currentPath.Count)
                {
                    var target = currentPath[currentPathIndex];
                    if(Vector2.DistanceSquared(Position, target) < 8)
                    {
                        currentPathIndex++;
                        if(state == State.Patrolling)  // Loop path when patrolling
                        {
                            if(currentPathIndex >= currentPath.Count)
                            {
                                currentPathIndex = 0;
                            }
                        }
                    }
                    else
                    {
                        // Move down the path
                        Rotation = MathExtensions.VectorToAngle(target - Position);

                        var delta = MathExtensions.AngleToVector(Rotation) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        if(state == State.Alerted || state == State.ToAlert)
                        {
                            delta *= RunSpeed;
                            AnimationDelta = 0.05f;
                        }
                        else
                        {
                            delta *= WalkSpeed;
                            AnimationDelta = 0.1f;
                        }
                        Move(delta);
                    }
                }
                else
                {
                    // Reached end of path or waiting for a new one
                    currentPathIndex = -1;

                    if(state == State.Alerted || state == State.Interested)
                    {
                        StartSearch(gameTime);
                    }
                    else if(state == State.ToPatrol)
                    {
                        currentPath = PatrolPath;
                        currentPathIndex = patrolPathIndex;
                        state = State.Patrolling;
                    }
                }
            }
            // State stuff
            if (state == State.Idle)
            {
                StartPatrol();

                AnimationRunning = false;
                AnimationCurrentFrame = 1;
            }
            else if (state == State.Searching)
            {
                // Wait a little bit at the spot when 'searching'
                if (gameTime.TotalGameTime.TotalSeconds >= searchStartTime + searchTime)
                {
                    state = State.Idle;
                }
                else
                {
                    AnimationRunning = false;
                    AnimationCurrentFrame = 1;
                }
            }

            // We can hear the player but not see him
            if(!CanSee(out playerPos) && CanHear(out playerPos))
            {
                Interest(playerPos);
            }

            if(CanSee(out playerPos))
            {
                // We can see the player
                AnimationRunning = false;
                RunSpeed = 0;
                WalkSpeed = 0;
                if (SettingsPage.Difficulty.Equals(DifficultyLevels.JamesBond))
                {
                    player.Health -= 50;
                }
                else
                {
                    state = State.Pursuit;
                    if (CanSee(out playerPos))
                    {
                        if (gameTime.TotalGameTime.TotalSeconds >= shootStartTime + shootTime)
                        {
                            var bullet = new Bullet(Collider);
                            bullet.Position = Position;
                            bullet.Rotation = Rotation;
                            bullet.Rotation = MathExtensions.VectorToAngle(playerPos - Position);
                            MonoGearGame.SpawnLevelEntity(bullet);
                            var sound = MonoGearGame.GetResource<SoundEffect>("Audio/AudioFX/Gunshot").CreateInstance();
                            sound.Volume = 0.4f *SettingsPage.Volume * SettingsPage.EffectVolume;
                            sound.Play();
                            shootStartTime = (float)gameTime.TotalGameTime.TotalSeconds;
                        }
                    }
                }
            }
            else if(state == State.Pursuit)
            {
                AnimationRunning = true;
                WalkSpeed = 35.0f;
                RunSpeed = 90.0f;
                // Can't see player anymore, but we just followed him so go looking for him
                Alert(playerPos);
            }
            else
            {
                AnimationRunning = true;
                WalkSpeed = 35.0f;
                RunSpeed = 90.0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if(state == State.Sleeping || !Enabled)
            {
                spriteBatch.Draw(sleepSprite, new Vector2(Position.X, Position.Y - 16), sleepSprite.Bounds, Color.White, 0, new Vector2(sleepSprite.Bounds.Size.X, sleepSprite.Bounds.Size.Y) / 2, 1, SpriteEffects.None, 0);
            }
            else if (state == State.ToAlert || state == State.Alerted || state == State.Pursuit)
            {
                spriteBatch.Draw(alertSprite, new Vector2(Position.X, Position.Y - 16), alertSprite.Bounds, Color.White, 0, new Vector2(alertSprite.Bounds.Size.X, alertSprite.Bounds.Size.Y) / 2, 1, SpriteEffects.None, 0);
                // Red FOV
                DrawFOVDebug(spriteBatch, Position, Rotation, player.Position, SightRange, fovColor);
            }
            else if (state == State.Searching || state == State.Interested || state == State.ToInterest)
            {
                spriteBatch.Draw(searchSprite, new Vector2(Position.X, Position.Y - 16), searchSprite.Bounds, Color.White, 0, new Vector2(searchSprite.Bounds.Size.X, searchSprite.Bounds.Size.Y) / 2, 1, SpriteEffects.None, 0);
                // Yellow FOV
                DrawFOVDebug(spriteBatch, Position, Rotation, player.Position, SightRange, new Color(100, 100, 0, 10));
            }
            else
            {
                // Blue FOV
                DrawFOVDebug(spriteBatch, Position, Rotation, player.Position, SightRange, new Color(0, 0, 100, 10));
            }
        }

        public static void DrawFOVDebug(SpriteBatch spriteBatch, Vector2 pos, float rot, Vector2 playerPos, float range)
        {
            DrawFOVDebug(spriteBatch, pos, rot, playerPos, range, fovColor);
        }

        public static void DrawFOVDebug(SpriteBatch spriteBatch, Vector2 pos, float rot, Vector2 playerPos, float range, Color color)
        {
            spriteBatch.Draw(fovSprite, pos, fovSprite.Bounds, color, rot, new Vector2(fovSprite.Bounds.Size.X, fovSprite.Bounds.Size.Y) / 2, range / 50, SpriteEffects.None, 0);

            var dis = Vector2.Distance(pos, playerPos);
            spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), dis.ToString(), pos + new Vector2(-8, 16), Color.Red);
        }

        public void Sleep()
        {
            if (state != State.Sleeping)
            {
                Enabled = false;
                Z = -1;                     // Display below player
                AnimationRunning = false;
                AnimationCurrentFrame = 1;
                state = State.Sleeping;
                sound = AudioManager.AddPositionalAudio(MonoGearGame.GetResource<SoundEffect>("Audio/AudioFX/snoreWhistle"), 1, 150, Position, true);
                sound.Volume = 0.2f;
            }
        }

        private void StartSearch(GameTime gameTime)
        {
            searchStartTime = (float)gameTime.TotalGameTime.TotalSeconds;
            state = State.Searching;
        }

        /// <summary>
        /// Starts guard patrol route if possible, changes state
        /// </summary>
        public void StartPatrol()
        {
            if (PatrolPath == null)
            {
                state = State.Idle;
                return;
            }

            state = State.ToPatrol;
            GoTo(PatrolPath[patrolPathIndex]);
        }

        /// <summary>
        /// Alerts a guard to the specified position and changes state
        /// </summary>
        /// <param name="origin"></param>
        public async void Alert(Vector2 origin)
        {
            if(!Enabled || state == State.ToAlert)
                return;

            if (state == State.Patrolling)
            {
                patrolPathIndex = currentPathIndex;
            }

            if(state != State.Alerted)
            {
                var sound = MonoGearGame.GetResource<SoundEffect>("Audio/AudioFX/Guard_Alert_Sound").CreateInstance();
                sound.Volume = 0.4f * SettingsPage.Volume * SettingsPage.EffectVolume;
                sound.Play();
            }

            state = State.ToAlert;

            await Task.Delay(1000);

            Task.Run(() =>
            {
                Pathfinding.FindPath(Position, origin, (path) =>
                {
                    currentPath = path;
                    currentPathIndex = 0;
                    state = path != null ? State.Alerted : State.Idle;
                    Random rand = new Random();
                    int number = rand.Next(0, 9);
                    if (number == 0)
                    {
                        var sound = MonoGearGame.GetResource<SoundEffect>("Audio/AudioFX/Get_over_here").CreateInstance();
                        sound.Volume = 0.2f * SettingsPage.Volume * SettingsPage.EffectVolume;
                        sound.Play();
                    }
                });
            });
        }

        /// <summary>
        /// Alerts a guard to the specified position and changes state.
        /// Will call Alert if the guard is already alerted.
        /// </summary>
        /// <param name="origin"></param>
        public async void Interest(Vector2 origin)
        {
            if(!Enabled || state == State.ToInterest)
                return;

            // Escalate to an alert if we're already alerted
            if(state == State.Alerted || state == State.ToAlert)
            {
                Alert(origin);
                return;
            }

            if(state == State.Patrolling)
            {
                patrolPathIndex = currentPathIndex;
            }

            if(state != State.Interested)
            {
                var sound = MonoGearGame.GetResource<SoundEffect>("Audio/AudioFX/Guard_Alert_Sound").CreateInstance();
                sound.Volume = 0.4f * SettingsPage.Volume * SettingsPage.EffectVolume;
                sound.Play();
            }

            state = State.ToInterest;
            await Task.Delay(1000);

            Task.Run(() =>
            {
                Pathfinding.FindPath(Position, origin, (path) =>
                {
                    currentPath = path;
                    currentPathIndex = 0;
                    state = path != null ? State.Interested : State.Idle;
                });
            });
        }

        /// <summary>
        /// Makes the guard move to the specified target without changing state
        /// </summary>
        /// <param name="target"></param>
        public void GoTo(Vector2 target)
        {
            Task.Run(() =>
            {
                Pathfinding.FindPath(Position, target, (path) =>
                {
                    currentPath = path;
                    currentPathIndex = 0;
                });
            });
        }

        private bool CanDetect(out Vector2 entityPos)
        {
            if(CanHear(out entityPos))
            {
                return true;
            }
            return CanSee(out entityPos);
        }

        private bool CanSee(out Vector2 entityPos)
        {
            var dis = Vector2.Distance(Position, player.Position);

            //Check if player is within view range
            if (dis < SightRange)
            {
                //Check to see if the guard is looking at the player
                var degrees = Math.Abs(MathHelper.ToDegrees(Rotation) - (90 + MathHelper.ToDegrees(MathExtensions.AngleBetween(Position, player.Position))));
                if (degrees <= (sightFov / 2) || degrees >= (360 - (sightFov / 2)))
                {
                    //Check to see if nothing blocks view of the player
                    Collider hit;
                    bool tilemap;
                    if(Collider.RaycastAny(Position, player.Position, out hit, out tilemap, Tag))
                    {
                        if(hit != null && hit.Entity.Tag.Equals("Player"))
                        {
                            entityPos = hit.Entity.Position;
                            return true;
                        }
                    }   

                }
            }

            entityPos = player.Position;
            return false;
        }

        private bool CanHear(out Vector2 entityPos)
        {
            var dis = Vector2.Distance(Position, player.Position);

            // Check if guard is within hearing range
            if(dis < hearingRange && !player.SneakMode)
            {
                entityPos = player.Position;
                return true;
            }

            entityPos = player.Position;
            return false;
        }

        public void Damage(float damage)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Destroy();
            }
        }

        public void Destroy()
        {
            MonoGearGame.DestroyEntity(this);
        }

        public WorldEntity GetEntity()
        {
            return this;
        }
    }
}
