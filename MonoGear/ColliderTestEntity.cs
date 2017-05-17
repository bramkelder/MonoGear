﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGear
{
    class ColliderTestEntity : WorldEntity
    {
        public ColliderTestEntity()
        {
            TextureAssetName = "Sprites/s_generator";
            Tag = "ColliderTest";

            LoadContent();

            Collider = new BoxCollider(this, Size);

            var fountain = new CircleCollider(new WorldEntity(), 3 * 16);
            fountain.Entity.Position = new Microsoft.Xna.Framework.Vector2(208, 224);
            fountain.Entity.Tag = "Fountain";

            var trigger = new WorldBoxTrigger(fountain.Entity.Position, new Vector2(160), (col, prevColliders, colliders) =>
            {
                foreach (var collider in colliders)
                {
                    if (prevColliders.Contains(collider))
                    {
                        continue;
                    }

                    if (collider.Entity.Tag == "Player")
                    {
                        AudioManager.PlayOnce(ResourceManager.GetManager().GetResource<SoundEffect>("Audio/AudioFX/Guard_Alert_Sound"), 1);
                    }
                }
            });
            MonoGearGame.RegisterGlobalEntity(trigger);
        }
    }
}