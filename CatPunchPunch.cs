using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using RWCustom;
using UnityEngine;

namespace CatPunchPunch
{
    [BepInPlugin("Harvie.CatPunchPunch", "CatPunchPunch", "1.0.0")]
    public class CatPunchPunch : BaseUnityPlugin
    {
        public static List<PunchModule> PunchModules = new List<PunchModule>();

        public static CatPunchPunch instance;

        void Start()
        {
            instance = this;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;

            On.Explosion.Update += Explosion_Update;
            On.Smoke.BombSmoke.Update += BombSmoke_Update;


			FreezeCore.Patch();
            //On.Room.Update += Room_Update;
            //On.RoomCamera.SpriteLeaser.Update += SpriteLeaser_Update;
        }

        private void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
			if(self.drawableObject is GraphicsModule)
            {
				if(!PunchModule.GetUpdatableAndDeletables(0).Contains((self.drawableObject as GraphicsModule).owner) && Input.GetKey(KeyCode.Tab))
                {
					orig.Invoke(self, 0, rCam, camPos);
					return;
                }
            }
			orig.Invoke(self, timeStacker, rCam, camPos);
        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {

        }

        private void BombSmoke_Update(On.Smoke.BombSmoke.orig_Update orig, Smoke.BombSmoke self, bool eu)
        {
            BombSmokeModule module = self.GetBombSmokeModule();

            orig.Invoke(self, eu);

            if(module != null)
            {
                module.Update(eu);
            }
        }

        private void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
        {
            ExplosionPunchModule module = self.GetExplosionPunchModule();

            if(module != null)
            {
                module.Update(eu);
            }
            else
            {
                orig.Invoke(self, eu);
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if(PunchModules[0] != null)
            {
                PunchModules[0].Punch();
            }
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self,abstractCreature,world);
            PunchModules.Clear();
            PunchModules.Add(new PunchModule(self));
        }
    }
}
