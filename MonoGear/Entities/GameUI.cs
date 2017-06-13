﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using MonoGear.Engine;

namespace MonoGear.Entities
{
    public class GameUI : WorldEntity
    {
        private Player player;
        private bool showAllObjective;

        static List<Objective> objectives = new List<Objective>();

        public GameUI()
        {
            showAllObjective = false;

            Z = Int32.MaxValue;
        }

        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();

            player = MonoGearGame.FindEntitiesWithTag("Player")[0] as Player;
            objectives.AddRange(MonoGearGame.FindEntitiesOfType<Objective>());
            objectives.Sort((a, b) => a.index.CompareTo(b.index));
        }

        public static void CompleteObjective(Objective obj)
        {
            objectives.Remove(obj);
        }

        public override void Update(Input input, GameTime gameTime)
        {
            base.Update(input, gameTime);

            if (input.IsButtonPressed(Input.Button.Interact))
                showAllObjective = !showAllObjective;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var rect = Camera.main.GetClippingRect();

            #region Draw Health and darts
            var pos = rect.Right - 100;
            var rows = Math.Ceiling(player.Health / 5);
            var healthToDraw = player.Health;
            var h = 5.0f;

            for (int j = 0; j < rows; j++)
            {
                if (healthToDraw < 5)
                    h = healthToDraw;

                for (int i = 0; i < h; i++)
                {
                    spriteBatch.Draw(MonoGearGame.GetResource<Texture2D>("Sprites/Heart"), new Vector2(pos, rect.Bottom - (50 + (j * 18))), Color.White);
                    pos += 15;
                }
                pos = rect.Right - 100;
                healthToDraw -= 5;
            }

            if (player.DartCount <= 6)
            {
                pos = rect.Right - 100;
                for (int i = 0; i < player.DartCount; i++)
                {
                    spriteBatch.Draw(MonoGearGame.GetResource<Texture2D>("Sprites/SleepDart"), new Vector2(pos, rect.Bottom - 32), Color.White);
                    pos += 15;
                }
            }
            else
            {
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "Darts: " + player.DartCount, new Vector2(rect.Right - 100, rect.Bottom - 32), Color.Red);
            }
            #endregion

            #region Draw Objective

            if (objectives.Count > 0)
            {
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "Objective:", new Vector2(rect.Left + 16, rect.Top + 10), Color.LightGray);
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), objectives[0].ToString(), new Vector2(rect.Left + 16, rect.Top + 21), Color.LightGray);
            }
            else
            {
                spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), "No objective", new Vector2(rect.Left + 16, rect.Top + 16), Color.LightGray);
            }

            if (showAllObjective)
            {
                float top = rect.Top + 32;
                for (int i = 1; i < objectives.Count; i++)
                {
                    spriteBatch.DrawString(MonoGearGame.GetResource<SpriteFont>("Fonts/Arial"), objectives[i].ToString(), new Vector2(rect.Left + 16, top), Color.LightGray);
                    top += 11;
                }
            }
            
            #endregion
        }
    }
}