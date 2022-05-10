using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using RWCustom;
using UnityEngine;
using OptionalUI;

namespace CatPunchPunch
{
    [BepInPlugin("Harvie.CatPunchPunch", "CatPunchPunch", "1.1.2")]
    public class CatPunchPunch : BaseUnityPlugin
    {
        public static PunchModule[] PunchModules = new PunchModule[1];
        public static CatPunchPunch instance;

        void OnEnable()
        {
            instance = this;
        }
        public OptionInterface LoadOI()
        {
            return new PunchConfig(instance);
        }


        void Start()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;

            //爆炸拳
            On.Explosion.Update += Explosion_Update;
            On.Smoke.BombSmoke.Update += BombSmoke_Update;

            //蜜蜂拳
            On.SporePlant.Bee.Update += Bee_Update;
            On.SporePlant.Bee.LookForRandomCreatureToHunt += Bee_LookForRandomCreatureToHunt;
            On.SporePlant.Bee.Attach += Bee_Attach;
            On.SporePlant.AttachedBee.BreakStinger += AttachedBee_BreakStinger;

            //烟雾拳
            On.DeerAI.Update += DeerAI_Update;
        }

        private void DeerAI_Update(On.DeerAI.orig_Update orig, DeerAI self)
        {
            DeerAIModule deerAIModule = self.GetDeerAIModule();
            if(deerAIModule != null)
            {
                deerAIModule.Update();
            }
            else
            {
                orig.Invoke(self);
            }

        }

        private void Bee_Attach(On.SporePlant.Bee.orig_Attach orig, SporePlant.Bee self, BodyChunk chunk)
        {
            BeeModule bee = self.GetBeeModule();
            if (bee != null)
            {
                bee.Attach(chunk);
            }
            else
            {
                orig.Invoke(self,chunk);
            }
        }

        private bool Bee_LookForRandomCreatureToHunt(On.SporePlant.Bee.orig_LookForRandomCreatureToHunt orig, SporePlant.Bee self)
        {
            BeeModule bee = self.GetBeeModule();
            if (bee != null)
            {
                return bee.LookForRandomCreatureToHunt();
            }
            else
            {
                return orig.Invoke(self);
            }
        }

        private void AttachedBee_BreakStinger(On.SporePlant.AttachedBee.orig_BreakStinger orig, SporePlant.AttachedBee self)
        {
            BeeModule.AttachedBeeModule attachedBeeModule = self.GetAttachedBeeModule();
            if (attachedBeeModule != null)
            {
                attachedBeeModule.BreakStinger();
            }
            orig.Invoke(self);
        }

        private void Bee_Update(On.SporePlant.Bee.orig_Update orig, SporePlant.Bee self, bool eu)
        {
            BeeModule bee = self.GetBeeModule();
            if(bee != null)
            {
                bee.Update(eu);
            }
            else
            {
                orig.Invoke(self, eu);
            }
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
            self.GetPunchModule().Punch();
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self,abstractCreature,world);

            if(self.playerState.playerNumber > PunchModules.Length - 1)
            {
                PunchModule[] olds = PunchModules;
                PunchModules = new PunchModule[self.playerState.playerNumber + 1];

                for(int i = 0;i < olds.Length; i++)
                {
                    PunchModules[i] = olds[i];
                }
            }

            PunchModules[self.playerState.playerNumber] = new PunchModule(self);
        }
    }
}
