﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xna.Framework.Audio;

namespace MonoGear
{
    class GameOver : WorldEntity
    {
        private bool gameOver;
        private Player player;
        private Texture2D gameOverSprite;

        public GameOver()
        {
            TextureAssetName = "Sprites/blank";
            Tag = "GameOverScreen";
            gameOver = false;
            Visible = false;

            Z = 999;

            LoadContent();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (gameOverSprite == null)
            {
                gameOverSprite = ResourceManager.GetManager().GetResource<Texture2D>("Sprites/gameover");
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            

            base.Draw(spriteBatch);
            if (gameOver)
            {
                var clipRect = Camera.main.GetClippingRect();
                spriteBatch.Draw(gameOverSprite, clipRect, Color.White);
            }
        }

        public void EnableGameOver()
        {
            player = MonoGearGame.FindEntitiesWithTag("Player")[0] as Player;
            gameOver = true;
            Position = player.Position;
            Visible = true;
            player.Enabled = false;
            AudioManager.Clear();
            AudioManager.PlayOnce(ResourceManager.GetManager().GetResource<SoundEffect>("Audio/AudioFX/Wasted_sound"), 1);
        }

        public void DisableGameOver()
        {
            player = MonoGearGame.FindEntitiesWithTag("Player")[0] as Player;
            gameOver = false;
            Visible = false;
            player.Enabled = true;

        }

        public override void Update(Input input, GameTime gameTime)
        {
            base.Update(input, gameTime);

            if (gameOver)
            {
                if(input.IsKeyPressed(Keys.Space))
                {
                    MonoGearGame.Restart();
                    DisableGameOver();
                }
            }
        }
    }
}

