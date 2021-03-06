﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGear.Engine;

namespace MonoGear.Entities
{
    public class GameUI : WorldEntity
    {
        private Player player;

        //A list of all the objectives in the current level
        static List<Objective> objectives = new List<Objective>();

        public GameUI()
        {
            Z = int.MaxValue;
        }

        /// <summary>
        /// Method that executes when the level is loaded.
        /// </summary>
        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();

            player = MonoGearGame.FindEntitiesWithTag("Player")[0] as Player;

            //Reset the objectives list and sort in on index
            objectives.Clear();
            objectives.AddRange(MonoGearGame.FindEntitiesOfType<Objective>());
            objectives.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        /// <summary>
        /// Method that completes an objective
        /// </summary>
        /// <param name="obj">The completed objective</param>
        public static void CompleteObjective(Objective obj)
        {
            // Remove the objectives from the list of objectives
            objectives.Remove(obj);
        }

        /// <summary>
        /// Method that updates the game
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="gameTime">GameTime</param>
        public override void Update(Input input, GameTime gameTime)
        {
            base.Update(input, gameTime);
        }

        /// <summary>
        /// Draws the UI.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch</param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            var rect = Camera.main.GetClippingRect();

            #region Draw Health and darts

            var pos = rect.Right - 100;
            for (int i = 0; i < player.Health; i++)
            {
                // Draw a hart
                spriteBatch.Draw(MonoGearGame.GetResource<Texture2D>("Sprites/Heart"), new Vector2(pos, rect.Bottom - 50), Color.White);
                pos += 15;
            }

            // Check if the player has six or less darts.
            if (player.DartCount <= 6)
            {
                pos = rect.Right - 100;
                for (int i = 0; i < player.DartCount; i++)
                {
                    // Draw a dart
                    spriteBatch.Draw(MonoGearGame.GetResource<Texture2D>("Sprites/SleepDart"), new Vector2(pos, rect.Bottom - 32), Color.White);
                    pos += 15;
                }
            }
            else
            {
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "Darts: " + player.DartCount, new Vector2(rect.Right - 100, rect.Bottom - 32), Color.Red);
            }
            #endregion

            #region Draw Vehicle Health

            if (player.CurrentVehicle != null)
            {
                pos = rect.Right - 100;
                var rows = Math.Ceiling(player.CurrentVehicle.Health / 25);
                var healthToDraw = player.CurrentVehicle.Health / 5;
                var h = 5.0f;

                for (int j = 0; j < rows; j++)
                {
                    if (healthToDraw < 5)
                    {
                        h = healthToDraw;
                    }

                    for (int i = 0; i < h; i++)
                    {
                        // Draw a heart
                        spriteBatch.Draw(MonoGearGame.GetResource<Texture2D>("Sprites/Wrench"), new Vector2(pos, rect.Bottom - (70 + (j * 18))), Color.White);
                        pos += 15;
                    }
                    pos = rect.Right - 100;
                    healthToDraw -= 5;
                }
            }

            #endregion

            #region Draw Objective

            // Check if there are any objectives
            if (objectives.Count > 0)
            {
                // Draw a string with the objective text
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "Objective:", new Vector2(rect.Left + 16, rect.Top + 10), Color.LightGray);
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), objectives[0].ToString(), new Vector2(rect.Left + 16, rect.Top + 21), Color.LightGray);
            }
            else
            {
                // Draw a no objective string
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "No objective", new Vector2(rect.Left + 16, rect.Top + 16), Color.LightGray);
            }
            
            #endregion
        }
    }
}
