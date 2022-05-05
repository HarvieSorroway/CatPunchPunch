using Noise;
using RWCustom;
using Smoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatPunchPunch
{
    public class PunchModule
    {
        public List<ExplosionPunchModule> explosionPunchModules = new List<ExplosionPunchModule>();
        public List<BombSmokeModule> bombSmokeModules = new List<BombSmokeModule>();
        public PunchModule(Player player)
        {
            this.player = player;
        }

        public void Punch()
        {
            if(Cool > 0)
            {
                Cool--;
                return;
            }
            if (!player.input[0].thrw || player.dontGrabStuff > 0)
            {
                return;
            }

            float maxColliderDistance = 35f * 35;

            Vector2 PunchVec;

            //Get PunchPackage
            PunchDataPackage punchDataPackage = new PunchDataPackage();

            if (player.room.gravity == 0)
            {
                PunchVec = (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized;
            }
            else
            {
                PunchVec = (new Vector2(player.flipDirection, 0)).normalized;
            }

            try
            {
                if (player.room.physicalObjects[player.collisionLayer].Count > 0)
                {
                    float maxDist = float.MaxValue;
                    BodyChunk closestChunck = null;

                    foreach (var physicObj in player.room.physicalObjects[player.collisionLayer])
                    {
                        if (physicObj != player && physicObj is Creature)
                        {
                            foreach (var chunck in physicObj.bodyChunks)//检查身体区块
                            {
                                float dist = Custom.DistNoSqrt(chunck.pos, player.mainBodyChunk.pos);
                                if (dist < maxDist && dist < maxColliderDistance * 16)
                                {
                                    maxDist = dist;
                                    closestChunck = chunck;
                                }
                            }
                        }
                    }
                    if (closestChunck != null)
                    {
                        PunchVec = (closestChunck.pos - player.mainBodyChunk.pos).normalized;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogException(new NullReferenceException("At Block 1"));
                Debug.LogException(e);
            }

            PunchVec *= (float)Random.Range(15, 20);

            Vector2 colliderCheckPos = PunchVec + player.mainBodyChunk.pos;

            punchDataPackage.fistPos = colliderCheckPos;
            punchDataPackage.punchVec = PunchVec;
            punchDataPackage.idealBodyChunkAndAppendages = new List<IdealBodyChunkAndAppendage>();

            //检查碰撞
            if (player.room.physicalObjects[player.collisionLayer].Count > 0)
            {
                try
                {
                    foreach (var physicObj in player.room.physicalObjects[player.collisionLayer])
                    {
                        if ((physicObj != player) && physicObj.CollideWithObjects)
                        {
                            BodyChunk closestChunck = null;
                            PhysicalObject.Appendage.Pos closestAppendagePos = null;

                            float maxDist = float.MaxValue;
                            try
                            {
                                if (physicObj.bodyChunks.Length > 0)
                                {
                                    foreach (var chunck in physicObj.bodyChunks)//检查身体区块
                                    {
                                        float dist = Custom.DistNoSqrt(chunck.pos, colliderCheckPos);
                                        if (dist < maxDist && dist < maxColliderDistance)
                                        {
                                            maxDist = dist;
                                            closestChunck = chunck;
                                        }
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                Debug.LogException(new NullReferenceException("At Block 3"));
                                Debug.LogException(e);
                            }

                            if (closestChunck == null && physicObj.appendages != null && physicObj.appendages.Count > 0)//检查附着物
                            {
                                maxDist = float.MaxValue;

                                if (physicObj.appendages.Count > 0)
                                {
                                    try
                                    {
                                        foreach (var appendage in physicObj.appendages)
                                        {
                                            if (appendage.canBeHit)
                                            {
                                                for (int i = 0; i < appendage.segments.Length; i++)
                                                {
                                                    float dist = Custom.DistNoSqrt(appendage.segments[i], colliderCheckPos);

                                                    if (dist < maxDist && dist < maxColliderDistance)
                                                    {
                                                        closestAppendagePos = new PhysicalObject.Appendage.Pos(appendage, i - 1, (i < appendage.segments.Length - 1 && i > 1) ? 0f : Custom.Dist(appendage.segments[i - 1], appendage.segments[i]));
                                                        maxDist = dist;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogException(new NullReferenceException("At Block 4"));
                                        Debug.LogException(e);
                                    }
                                }
                            }

                            IdealBodyChunkAndAppendage idealBodyChunkAndAppendage = new IdealBodyChunkAndAppendage();
                            idealBodyChunkAndAppendage.owner = physicObj;

                            idealBodyChunkAndAppendage.bodyChunk = closestChunck;
                            idealBodyChunkAndAppendage.appendagePos = closestAppendagePos;

                            punchDataPackage.idealBodyChunkAndAppendages.Add(idealBodyChunkAndAppendage);
                            continue;
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.LogException(new NullReferenceException("At Block 2"));
                    Debug.LogException(e);
                }
            }

            SpecificPunchReturnValue specificPunchReturnValue;
            switch (mode)
            {
                case PunchMode.NormalPunch:
                    specificPunchReturnValue = NormalPunch(punchDataPackage);
                    break;
                case PunchMode.FastPunch:
                    specificPunchReturnValue = NormalPunch(punchDataPackage, 4);
                    break;
                case PunchMode.BombPunch:
                    specificPunchReturnValue = BombPunch(punchDataPackage);
                    break;
                case PunchMode.TheHandPunch:
                    specificPunchReturnValue = NormalPunch(punchDataPackage);
                    break;
                default:
                    specificPunchReturnValue = NormalPunch(punchDataPackage);
                    break;
            }

            if (player.graphicsModule != null)//动画
            {
                PlayerGraphics playerGraphics = player.graphicsModule as PlayerGraphics;
                SlugcatHand slugcatHand1 = playerGraphics.hands[attackHand];

                slugcatHand1.mode = Limb.Mode.Dangle;


                slugcatHand1.pos += PunchVec * 1.65f;
                slugcatHand1.vel += PunchVec * 10f;

                playerGraphics.LookAtPoint(player.mainBodyChunk.pos + PunchVec, 99999f);
                attackHand = 1 - attackHand;
            }

            if (player.room.gravity != 0)
            {
                player.mainBodyChunk.vel += PunchVec * 0.15f * specificPunchReturnValue.velMulti;
                player.bodyChunks[1].vel -= PunchVec * 0.15f * specificPunchReturnValue.velMulti;
            }


            Cool = specificPunchReturnValue.breakFrame;
        }

        public SpecificPunchReturnValue NormalPunch(PunchDataPackage punchDataPackage,int breakFrame = 10)
        {
            if(punchDataPackage.idealBodyChunkAndAppendages.Count > 0)
            {
                foreach(var bodyChunkAndAppendage in punchDataPackage.idealBodyChunkAndAppendages)
                {
                    if (bodyChunkAndAppendage.bodyChunk != null || bodyChunkAndAppendage.appendagePos != null)
                    {
                        if (bodyChunkAndAppendage.owner is Creature && bodyChunkAndAppendage.bodyChunk != null)
                        {
                            (bodyChunkAndAppendage.owner as Creature).Violence(player.mainBodyChunk, new Vector2?(punchDataPackage.punchVec), bodyChunkAndAppendage.bodyChunk, null, Creature.DamageType.Blunt, 0.4f * Mathf.Pow(Random.value * 1.1f, 8f), 19f);
                        }
                        else
                        {
                            if (bodyChunkAndAppendage.bodyChunk != null)
                            {
                                bodyChunkAndAppendage.bodyChunk.vel += Vector2.ClampMagnitude(punchDataPackage.punchVec / bodyChunkAndAppendage.owner.TotalMass, 10f);
                            }
                        }

                        if (bodyChunkAndAppendage.appendagePos != null)
                        {
                            (bodyChunkAndAppendage.owner as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(bodyChunkAndAppendage.appendagePos, punchDataPackage.punchVec * 0.007f);
                        }

                        if (mode == PunchMode.NormalPunch)
                        {
                            Vector2 vector3 = (bodyChunkAndAppendage.bodyChunk != null) ? bodyChunkAndAppendage.bodyChunk.pos : bodyChunkAndAppendage.appendagePos.appendage.OnAppendagePosition(bodyChunkAndAppendage.appendagePos);
                            player.room.PlaySound(SoundID.Rock_Hit_Creature, vector3, punchDataPackage.punchVec.sqrMagnitude * 0.065f, punchDataPackage.punchVec.sqrMagnitude / 20f);

                            player.room.AddObject(new ExplosionSpikes(player.room, vector3 + Custom.DirVec(vector3, punchDataPackage.fistPos) * ((bodyChunkAndAppendage.bodyChunk != null) ? bodyChunkAndAppendage.bodyChunk.rad : 5f), 5, 2f, 4f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
                        }
                    }
                }
            }

            SpecificPunchReturnValue value = new SpecificPunchReturnValue();
            value.breakFrame = breakFrame;
            value.velMulti = 1f;

            return value;
        }

        public SpecificPunchReturnValue BombPunch(PunchDataPackage punchDataPackage)
        {
            Vector2 newPos = punchDataPackage.fistPos + punchDataPackage.punchVec;
            Color explodeColor = new Color(1f, 0.4f, 0.3f);

            //与ScavengerBomb内的代码相同
            Explosion explosion = new Explosion(player.room, player, newPos, 7, 100, 3, 2, 30, 0.25f, player, 1f, 10, 1f);

            explosionPunchModules.Add(new ExplosionPunchModule(explosion, this));
            //Debug.Log("[CatPunch]Total explosionPunchModule:" + explosionPunchModules.Count.ToString());

            player.room.AddObject(explosion);
            player.room.AddObject(new SootMark(player.room, newPos, 80f, true));
            player.room.AddObject(new ShockWave(newPos, 330f, 0.045f, 5));
            player.room.AddObject(new Explosion.ExplosionLight(newPos, 280f, 1f, 7, explodeColor));
            player.room.AddObject(new Explosion.ExplosionLight(newPos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            player.room.AddObject(new ExplosionSpikes(player.room, newPos, 14, 30f, 9f, 7f, 170f, explodeColor));
            player.room.PlaySound(SoundID.Bomb_Explode, player.mainBodyChunk);

            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (player.room.GetTile(newPos + a * 20f).Solid)
                {
                    if (!player.room.GetTile(newPos - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    player.room.AddObject(new Spark(newPos + a * Mathf.Lerp(30f, 60f, UnityEngine.Random.value), a * Mathf.Lerp(7f, 38f, UnityEngine.Random.value) + Custom.RNV() * 20f * UnityEngine.Random.value, Color.Lerp(explodeColor, new Color(1f, 1f, 1f), UnityEngine.Random.value), null, 11, 28));
                }
                player.room.AddObject(new Explosion.FlashingSmoke(newPos + a * 40f * UnityEngine.Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(UnityEngine.Random.value, 2f)), 1f + 0.05f * UnityEngine.Random.value, new Color(1f, 1f, 1f), explodeColor, UnityEngine.Random.Range(3, 11)));
            }

            //获取玩家的手
            PlayerGraphics playerGraphics = null;
            if(player.graphicsModule != null)
            {
                playerGraphics = player.graphicsModule as PlayerGraphics;
            }

            BombSmoke smoke = new BombSmoke(player.room, newPos, null,Color.black);

            //添加module
            bombSmokeModules.Add(new BombSmokeModule(smoke, this, playerGraphics != null ? playerGraphics.hands[attackHand] : null, (int)Random.Range(30, 90)));
            player.room.AddObject(smoke);
           
            //点燃烟雾
            for (int k = 0; k < 4; k++)
            {
                smoke.EmitWithMyLifeTime(newPos + Custom.RNV(), Custom.RNV() * UnityEngine.Random.value * 17f);
            }
            //晃动镜头
            player.room.ScreenMovement(new Vector2?(newPos), default(Vector2), 1.3f);

            SpecificPunchReturnValue value = new SpecificPunchReturnValue();
            value.velMulti = 3f;
            value.breakFrame = 40;

            return value;
        }

        public static List<UpdatableAndDeletable> GetUpdatableAndDeletables(int index)
        {
            List<UpdatableAndDeletable> newList = new List<UpdatableAndDeletable>();

            if(CatPunchPunch.PunchModules.Count == 0)
            {
                Debug.Log(newList.Count);
                return newList;
            }

            if(CatPunchPunch.PunchModules[index].player.appendages != null)
            {
                var collection1 = from physicalObject in CatPunchPunch.PunchModules[index].player.appendages
                                  where physicalObject != null
                                  where physicalObject.owner != null
                                  select physicalObject.owner as UpdatableAndDeletable;
                newList.AddRange(collection1);
            }

            if(CatPunchPunch.PunchModules[index].player.grasps != null)
            {
                var collecetion2 = from physicalObject in CatPunchPunch.PunchModules[index].player.grasps
                                   where physicalObject != null
                                   where physicalObject.grabbed != null
                                   select physicalObject.grabbed as UpdatableAndDeletable;
                newList.AddRange(collecetion2);
            }

            if(CatPunchPunch.PunchModules[index].explosionPunchModules != null)
            {
                var collection3 = from explosionModule in CatPunchPunch.PunchModules[index].explosionPunchModules
                                  select explosionModule.explosion as UpdatableAndDeletable;
                newList.AddRange(collection3);
            }
            
            if(CatPunchPunch.PunchModules[index].bombSmokeModules != null)
            {
                var collection4 = from bombSmokeModule in CatPunchPunch.PunchModules[index].bombSmokeModules
                                  select bombSmokeModule.bombSmoke as UpdatableAndDeletable;
                newList.AddRange(collection4);
            }

            newList.Add(CatPunchPunch.PunchModules[index].player as UpdatableAndDeletable);

            Debug.Log(newList.Count);
            return newList;
        }

        WeakReference _player;
        public Player player
        {
            get
            {
                if (_player.Target == null)
                {
                    throw new NullReferenceException("PlayerHasBeenDestroy");
                }
                else
                {
                    return _player.Target as Player;
                }
            }
            set
            {
                _player = new WeakReference(value);
            }
        }
        PunchMode mode
        {
            get
            {
                if(player.objectInStomach == null)
                {
                    return PunchMode.NormalPunch;
                }
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.Rock)
                {
                    return PunchMode.FastPunch;
                }
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb)
                {
                    return PunchMode.BombPunch;
                }
                return PunchMode.NormalPunch;
            }
        }

        int attackHand = 0;
        public int Cool = 0;

        public enum PunchMode
        {
            NormalPunch,
            FastPunch,
            BombPunch,
            SmokePunch,
            ScavPunch,
            TheHandPunch,
            TheWorldPunch,
        }

        public struct PunchDataPackage
        {
            public Vector2 fistPos;
            public Vector2 punchVec;

            public List<IdealBodyChunkAndAppendage> idealBodyChunkAndAppendages;
        }

        public struct IdealBodyChunkAndAppendage
        {
            public PhysicalObject owner;

            public BodyChunk bodyChunk;
            public PhysicalObject.Appendage.Pos appendagePos;
        }

        public struct SpecificPunchReturnValue
        {
            public int breakFrame;
            public float velMulti;
        }
    }

    public class ExplosionPunchModule
    {
        public ExplosionPunchModule(Explosion explosion,PunchModule punchModule)
        {
            this.explosion = explosion;
            this.module = punchModule;
        }

        public void Update(bool eu)
        {
            explosion.evenUpdate = eu;

            if (!explosion.explosionReactorsNotified)
            {
                explosion.explosionReactorsNotified = true;
                for (int i = 0; i < explosion.room.updateList.Count; i++)
                {
                    if (explosion.room.updateList[i] is Explosion.IReactToExplosions)
                    {
                        (explosion.room.updateList[i] as Explosion.IReactToExplosions).Explosion(explosion);
                    }
                }
                if (explosion.room.waterObject != null)
                {
                    explosion.room.waterObject.Explosion(explosion);
                }
                if (explosion.sourceObject != null)
                {
                    explosion.room.InGameNoise(new InGameNoise(explosion.pos, explosion.backgroundNoise * 2700f, explosion.sourceObject, explosion.backgroundNoise * 6f));
                }
            }
            explosion.room.MakeBackgroundNoise(explosion.backgroundNoise);
            float num = explosion.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)explosion.lifeTime, (float)explosion.frame) * 3.1415927f));
            for (int j = 0; j < explosion.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < explosion.room.physicalObjects[j].Count; k++)
                {
                    if (explosion.sourceObject != explosion.room.physicalObjects[j][k] && !explosion.room.physicalObjects[j][k].slatedForDeletetion && explosion.room.physicalObjects[j][k] != module.player)
                    {
                        float num2 = 0f;
                        float num3 = float.MaxValue;
                        int num4 = -1;
                        for (int l = 0; l < explosion.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            float num5 = Vector2.Distance(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos);
                            num3 = Mathf.Min(num3, num5);
                            if (num5 < num)
                            {
                                float num6 = Mathf.InverseLerp(num, num * 0.25f, num5);
                                if (!explosion.room.VisualContact(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos))
                                {
                                    num6 -= 0.5f;
                                }
                                if (num6 > 0f)
                                {
                                    explosion.room.physicalObjects[j][k].bodyChunks[l].vel += explosion.PushAngle(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos) * (explosion.force / explosion.room.physicalObjects[j][k].bodyChunks[l].mass) * num6;
                                    explosion.room.physicalObjects[j][k].bodyChunks[l].pos += explosion.PushAngle(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos) * (explosion.force / explosion.room.physicalObjects[j][k].bodyChunks[l].mass) * num6 * 0.1f;
                                    if (num6 > num2)
                                    {
                                        num2 = num6;
                                        num4 = l;
                                    }
                                }
                            }
                        }
                        if (explosion.room.physicalObjects[j][k] == explosion.killTagHolder)
                        {
                            num2 *= explosion.killTagHolderDmgFactor;
                        }
                        if (explosion.deafen > 0f && explosion.room.physicalObjects[j][k] is Creature && explosion.room.physicalObjects[j][k] != module.player)
                        {
                            (explosion.room.physicalObjects[j][k] as Creature).Deafen((int)Custom.LerpMap(num3, num * 1.5f * explosion.deafen, num * Mathf.Lerp(1f, 4f, explosion.deafen), 650f * explosion.deafen, 0f));
                        }
                        if (num4 > -1)
                        {
                            if (explosion.room.physicalObjects[j][k] is Creature && explosion.room.physicalObjects[j][k] != module.player)
                            {
                                int num7 = 0;
                                while ((float)num7 < Math.Min(Mathf.Round(num2 * explosion.damage * 2f), 8f))
                                {
                                    Vector2 p = explosion.room.physicalObjects[j][k].bodyChunks[num4].pos + Custom.RNV() * explosion.room.physicalObjects[j][k].bodyChunks[num4].rad * UnityEngine.Random.value;
                                    explosion.room.AddObject(new WaterDrip(p, Custom.DirVec(explosion.pos, p) * explosion.force * UnityEngine.Random.value * num2, false));
                                    num7++;
                                }
                                if (explosion.killTagHolder != null && explosion.room.physicalObjects[j][k] != explosion.killTagHolder)
                                {
                                    (explosion.room.physicalObjects[j][k] as Creature).SetKillTag(explosion.killTagHolder.abstractCreature);
                                }
                                (explosion.room.physicalObjects[j][k] as Creature).Violence(null, null, explosion.room.physicalObjects[j][k].bodyChunks[num4], null, Creature.DamageType.Explosion, num2 * explosion.damage / ((!((explosion.room.physicalObjects[j][k] as Creature).State is HealthState)) ? 1f : ((float)explosion.lifeTime)), num2 * explosion.stun);
                                if (explosion.minStun > 0f)
                                {
                                    (explosion.room.physicalObjects[j][k] as Creature).Stun((int)(explosion.minStun * Mathf.InverseLerp(0f, 0.5f, num2)));
                                }
                                if ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule != null && (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts != null)
                                {
                                    for (int m = 0; m < (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts.Length; m++)
                                    {
                                        (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos += explosion.PushAngle(explosion.pos, (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * explosion.force * 5f;
                                        (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].vel += explosion.PushAngle(explosion.pos, (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * explosion.force * 5f;
                                        if ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] is Limb)
                                        {
                                            ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] as Limb).mode = Limb.Mode.Dangle;
                                        }
                                    }
                                }
                            }
                            explosion.room.physicalObjects[j][k].HitByExplosion(num2, explosion, num4);
                        }
                    }
                }
            }
            explosion.frame++;
            if (explosion.frame > explosion.lifeTime)
            {
                explosion.Destroy();
                module.explosionPunchModules.Remove(this);
                Debug.Log("[CatPunch]Total explosionPunchModule:" + module.explosionPunchModules.Count.ToString());
            }
        }

        WeakReference _explosion;
        public Explosion explosion
        {
            get
            {
                if(_explosion.Target == null)
                {

                    return null;
                }
                else
                {
                    return _explosion.Target as Explosion;
                }
            }
            set
            {
                _explosion = new WeakReference(value);
            }
        }

        PunchModule module;
    }

    public class BombSmokeModule
    {
        public int lifeTime;

        public BombSmokeModule(BombSmoke bombSmoke,PunchModule module,SlugcatHand fireHand,int lifeTime)
        {
            this.bombSmoke = bombSmoke;
            this.module = module;
            this.lifeTime = lifeTime;
            this.fireHand = fireHand;
        }

        public void Update(bool eu)
        {
            lifeTime--;

            if(fireHand != null)
            {
                bombSmoke.pos = fireHand.pos;
            }

            if(lifeTime <= 0)
            {
                bombSmoke.Destroy();
                module.bombSmokeModules.Remove(this);
            }
        }

        WeakReference _bombSmoke;
        public BombSmoke bombSmoke
        {
            get
            {
                if (_bombSmoke.Target == null)
                {

                    return null;
                }
                else
                {
                    return _bombSmoke.Target as BombSmoke;
                }
            }
            set
            {
                _bombSmoke = new WeakReference(value);
            }
        }
        public PunchModule module;
        public SlugcatHand fireHand;
    }



    public static class ModulePatch
    {
        public static BombSmokeModule GetBombSmokeModule(this BombSmoke bombSmoke)
        {
            foreach(var punchModule in CatPunchPunch.PunchModules)
            {
                if(punchModule.bombSmokeModules.Count > 0)
                {
                    foreach(var bombSmokeModule in punchModule.bombSmokeModules)
                    {
                        if(bombSmokeModule.bombSmoke == bombSmoke)
                        {
                            return bombSmokeModule;
                        }
                    }
                }
            }
            return null;
        }

        public static ExplosionPunchModule GetExplosionPunchModule(this Explosion explosion)
        {
            foreach (var punchModule in CatPunchPunch.PunchModules)
            {
                if (punchModule.explosionPunchModules.Count > 0)
                {
                    foreach (var explosionPunchModule in punchModule.explosionPunchModules)
                    {
                        if (explosionPunchModule.explosion == explosion)
                        {
                            return explosionPunchModule;
                        }
                    }
                }
            }
            return null;
        }
    }
}