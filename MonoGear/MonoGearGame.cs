﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Windows.UI.Xaml.Controls;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.Foundation;

using MonoGear.Engine;
using MonoGear.Engine.Audio;
using MonoGear.Entities;

namespace MonoGear
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MonoGearGame : Game
    {
        static MonoGearGame instance;
        /// <summary>
        /// GDM
        /// </summary>
        GraphicsDeviceManager graphics;
        /// <summary>
        /// Main sprite batch
        /// </summary>
        SpriteBatch spriteBatch;
        /// <summary>
        /// Global entity set. Stays between levels and updates first in Update().
        /// </summary>
        HashSet<WorldEntity> globalEntities;
        /// <summary>
        /// Level entity set, gets wiped on level change.
        /// </summary>
        HashSet<WorldEntity> levelEntities;
        /// <summary>
        /// Input object
        /// </summary>
        Input input;
        /// <summary>
        /// Active camera used for rendering
        /// </summary>
        Camera activeCamera;
        /// <summary>
        /// Active level, used for background rendering
        /// </summary>
        Level activeLevel;
        /// <summary>
        /// If set will be loaded the next frame
        /// </summary>
        Level nextLevel;
        /// <summary>
        /// Active Player
        /// </summary>
        Player player;
        LevelListData levelList;

        bool rollingCredits;

        Queue<WorldEntity> spawnQueueLocal;
        Queue<WorldEntity> spawnQueueGlobal;

        public MonoGearGame()
        {
            // Required for static entity/level related methods
            instance = this;

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Window.ClientSizeChanged += (s, e) =>
            {
                if (activeCamera != null)
                {
                    activeCamera.UpdateViewport(graphics.GraphicsDevice.Viewport);
                }
            };
        }

        public static void Restart()
        {
            if(instance != null)
            {
                LoadLevel(instance.activeLevel.Name);
            }
        }

        public static void NextLevel()
        {
            if(instance != null)
            {
                if(instance.levelList.LastLevel() != instance.activeLevel.Name)
                {
                    LoadLevel(instance.levelList.NextLevel());
                }
                else
                {
                    // Roll Credits
                    // Block update
                    instance.rollingCredits = true;
                    // Mute audio?

                    // Roll credits
                    var frame = Windows.UI.Xaml.Window.Current.Content as Frame;
                    frame.Navigate(typeof(CreditsPage));
                }
            }
        }

        private void UpdateDifficulty()
        {
            var sightRange = 295f;
            var runSpeed = 90.0f;
            var walkSpeed = 35.0f;

            var dif = SettingsPage.Difficulty;
            if (dif.Equals(DifficultyLevels.Intern))
            {
                player.DartCount = 5;
                player.Health = 5;
            }
            else if(dif.Equals(DifficultyLevels.Professional))
            {
                player.DartCount = 2;
                player.Health = 3;
                sightRange += 10;
                runSpeed += 10;
                walkSpeed += 10;
            }
            else if(dif.Equals(DifficultyLevels.Veteran))
            {
                player.DartCount = 1;
                player.Health = 2;
                sightRange += 20;
                runSpeed += 20;
                walkSpeed += 20;
            }
            else if(dif.Equals(DifficultyLevels.JamesBond))
            {
                player.DartCount = 0;
                sightRange += 30;
                runSpeed += 30;
                walkSpeed += 30;
            }

            var guards = FindEntitiesWithTag("Guard");
            foreach (var guard in guards)
            {
                var g = guard as Guard;
                if (g != null)
                {
                    g.SightRange = sightRange;
                    g.RunSpeed = runSpeed;
                    g.WalkSpeed = walkSpeed;
                }
            }
            
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            input = new Input();

            levelEntities = new HashSet<WorldEntity>();
            globalEntities = new HashSet<WorldEntity>();

            spawnQueueGlobal = new Queue<WorldEntity>();
            spawnQueueLocal = new Queue<WorldEntity>();

            activeCamera = new Camera(graphics.GraphicsDevice.Viewport);

            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            activeCamera.Zoom = graphics.GraphicsDevice.Viewport.Height / 320;
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load level list
            Task.Run(async () =>
            {
                var sf = await Package.Current.InstalledLocation.TryGetItemAsync(@"Content\Levels\LevelList.xml") as StorageFile;

                using(var stream = await sf.OpenStreamForReadAsync())
                {
                    XmlSerializer xml = new XmlSerializer(typeof(LevelListData));
                    levelList = xml.Deserialize(stream) as LevelListData;
                }
            }).Wait();

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load game over screen
            var gameOver = new GameOver();
            RegisterGlobalEntity(gameOver);

            // Global Entities
            player = new Player();
            RegisterGlobalEntity(player);
            var ui = new GameUI();
            RegisterGlobalEntity(ui);
            var pf = new Pathfinding();
            RegisterGlobalEntity(pf);
			
            // Load first level in the list
            LoadLevel(levelList.Start());
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if(rollingCredits)
            {
                return;
            }

            if(nextLevel != null)
            {
                LoadLevel();
            }

            input.Update();

            // Register newly spawned entities
            while(spawnQueueGlobal.Count > 0)
            {
                RegisterGlobalEntity(spawnQueueGlobal.Dequeue());
            }

            while(spawnQueueLocal.Count > 0)
            {
                RegisterLevelEntity(spawnQueueLocal.Dequeue());
            }

            // Globals go first
            foreach(var entity in globalEntities)
            {
                if(entity.Enabled)
                {
                    entity.Update(input, gameTime);
                }
            }
            foreach(var entity in levelEntities)
            {
                if(entity.Enabled)
                {
                    entity.Update(input, gameTime);
                }
            }

            AudioManager.UpdatePositionalAudio(player);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            var matrix = activeCamera.GetViewMatrix();
            spriteBatch.Begin(transformMatrix: matrix, blendState:BlendState.AlphaBlend, samplerState:SamplerState.PointClamp);

            activeLevel.DrawTiles(spriteBatch, activeCamera);
            activeLevel.DrawBackground(spriteBatch);

            // Sort entities based on z order
            var renderEntities = new List<WorldEntity>();
            renderEntities.AddRange(levelEntities);
            renderEntities.AddRange(globalEntities);
            renderEntities.Sort((ent, other) =>
            {
                return ent.Z.CompareTo(other.Z);
            });

            foreach(var entity in renderEntities)
            {
                if(entity.Visible)
                {
                    entity.Draw(spriteBatch);
                }
            }

            activeLevel.DrawForeground(spriteBatch);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public static void SpawnLevelEntity(WorldEntity entity)
        {
            instance.spawnQueueLocal.Enqueue(entity);
        }

        public static void SpawnGlobalEntity(WorldEntity entity)
        {
            instance.spawnQueueGlobal.Enqueue(entity);
        }

        // Static entity stuff
        /// <summary>
        /// Adds an entity to the level list.
        /// </summary>
        /// <param name="entity">Entity to add</param>
        private void RegisterLevelEntity(WorldEntity entity)
        {
            entity.OnLevelLoaded();
            levelEntities.Add(entity);
        }

        /// <summary>
        /// Adds an entity to the global list.
        /// </summary>
        /// <param name="entity">Entity to add</param>
        private void RegisterGlobalEntity(WorldEntity entity)
        {
            entity.OnLevelLoaded();
            globalEntities.Add(entity);
        }

        /// <summary>
        /// Returns a list of all entities that have the given tag.
        /// </summary>
        /// <param name="tag">Tag to find</param>
        /// <returns>List</returns>
        public static List<WorldEntity> FindEntitiesWithTag(string tag)
        {
            var list = new List<WorldEntity>();
            list.AddRange(instance.levelEntities.Where(
                ent =>
                {
                    return ent.Tag == tag;
                }));
            list.AddRange(instance.globalEntities.Where(
                ent =>
                {
                    return ent.Tag == tag;
                }));

            return list;
        }

        /// <summary>
        /// Returns a list of all entities of type T.
        /// </summary>
        /// <typeparam name="T">Type of entity.</typeparam>
        /// <returns>List</returns>
        public static List<T> FindEntitiesOfType<T>() where T : WorldEntity
        {
            var list = new List<T>();
            list.AddRange(instance.levelEntities.Where(
                ent =>
                {
                    return typeof(T).IsAssignableFrom(ent.GetType());
                }).Cast<T>());
            list.AddRange(instance.globalEntities.Where(
                ent =>
                {
                    return typeof(T).IsAssignableFrom(ent.GetType());
                }).Cast<T>());

            return list;
        }

        public static Level GetCurrentLevel()
        {
            return instance.activeLevel;
        }

        private void LoadLevel()
        {
            if(nextLevel != null)
            {
                activeLevel = nextLevel;
                nextLevel = null;

                // Tell entities that they should stop
                foreach(var e in levelEntities)
                {
                    e.OnLevelUnloaded();
                }

                // Update entities
                instance.levelEntities.Clear();
                var ents = activeLevel.GetEntities();
                foreach(var e in ents)
                {
                    levelEntities.Add(e);
                }

                // Do OnLevelLoaded for all entities
                foreach(var e in instance.levelEntities)
                {
                    e.OnLevelLoaded();
                }

                foreach(var e in instance.globalEntities)
                {
                    e.OnLevelLoaded();
                }

                spawnQueueLocal.Clear();            // Clear local spawn queue to prevent them from appearing in the new level

                UpdateDifficulty();

                // Force GC
                GC.Collect();
            }
        }

        // static level stuff
        public static void LoadLevel(string levelName)
        {
            AudioManager.ClearPositionalAudio();
            AudioManager.ClearGlobalAudio();

            instance.nextLevel = Level.LoadLevel(levelName);
        }

        public static T GetResource<T>(string name)
        {
            return instance.Content.Load<T>(name);
        }
    }
}
