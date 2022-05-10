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
        //Modules
        public List<ExplosionPunchModule> explosionPunchModules = new List<ExplosionPunchModule>();
        public List<BombSmokeModule> bombSmokeModules = new List<BombSmokeModule>();
        public List<BeeModule> beeModules = new List<BeeModule>();
        public List<BeeModule.AttachedBeeModule> attachedBeeModules = new List<BeeModule.AttachedBeeModule>();

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
            if(player == null)
            {
                return;
            }
            if (!player.input[0].thrw || player.dontGrabStuff > 0)
            {
                return;
            }

            float maxColliderDistance = 35f * 35;

            Vector2 PunchVec;
            Vector2 LaserPunchVec;

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
            LaserPunchVec = PunchVec;

            try
            {
                PunchMode punchMode = mode;

                //找到最近的生物(在一定范围内)
                if (player.room.physicalObjects[player.collisionLayer].Count > 0)
                {
                    float maxDist = float.MaxValue;
                    float laserMaxDist = float.MaxValue;
                    BodyChunk closestLaserChunk = null;
                    BodyChunk closestChunck = null;

                    foreach (var physicObj in player.room.physicalObjects[player.collisionLayer])
                    {
                        if (physicObj != player && physicObj is Creature)
                        {
                            bool doGetLaser = false;
                            foreach (var chunck in physicObj.bodyChunks)//检查身体区块
                            {
                                float dist = Custom.DistNoSqrt(chunck.pos, player.mainBodyChunk.pos);
                                if (dist < maxDist && dist < maxColliderDistance * 16 && !(physicObj as Creature).dead)
                                {
                                    maxDist = dist;
                                    closestChunck = chunck;
                                }
                                if(dist < laserMaxDist && dist < maxColliderDistance * 1024 && !(physicObj as Creature).dead && punchMode == PunchMode.LaserPunch)
                                {
                                    Vector2 corner = Custom.RectCollision(player.mainBodyChunk.pos + (chunck.pos - player.mainBodyChunk.pos).normalized * 100000f,player.mainBodyChunk.pos, player.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                                    IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(player.room, player.mainBodyChunk.pos, corner);


                                    if (intVector != null)
                                    {
                                        corner = Custom.RectCollision(corner, player.mainBodyChunk.pos, player.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                                    }

                                    if((corner - player.mainBodyChunk.pos).sqrMagnitude > (chunck.pos - player.mainBodyChunk.pos).sqrMagnitude)
                                    {
                                        laserMaxDist = dist;
                                        closestLaserChunk = chunck;
                                    }
                                }
                            }
                        }
                    }
                    if (closestChunck != null)
                    {
                        PunchVec = (closestChunck.pos - player.mainBodyChunk.pos).normalized;
                    }
                    if(closestLaserChunk != null)
                    {
                        LaserPunchVec = (closestLaserChunk.pos - player.mainBodyChunk.pos).normalized;
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
            Vector2 laserCheckPos = LaserPunchVec + player.mainBodyChunk.pos;

            punchDataPackage.fistPos = colliderCheckPos;
            punchDataPackage.punchVec = PunchVec;
            punchDataPackage.punchLaserVec = LaserPunchVec;
            punchDataPackage.idealBodyChunkAndAppendages = new List<IdealBodyChunkAndAppendage>();

            //检查碰撞
            if (player.room.physicalObjects[player.collisionLayer].Count > 0)
            {
                try
                {
                    foreach (var physicObj in player.room.physicalObjects[player.collisionLayer])
                    {

                        if ((physicObj != player))//近战拳头
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

            Vector2 handAnimVec = PunchVec;

            SpecificPunchReturnValue specificPunchReturnValue;
            switch (mode)
            {
                case PunchMode.NormalPunch:
                    specificPunchReturnValue = NormalPunch(punchDataPackage);
                    break;
                case PunchMode.FastPunch:
                    specificPunchReturnValue = NormalPunch(punchDataPackage, true);
                    break;
                case PunchMode.BombPunch:
                    specificPunchReturnValue = BombPunch(punchDataPackage);
                    break;
                case PunchMode.LaserPunch:
                    specificPunchReturnValue = LaserPunch(punchDataPackage);
                    handAnimVec = LaserPunchVec;
                    break;
                case PunchMode.FriendlyPunch:
                    specificPunchReturnValue = FriendlyPunch(punchDataPackage);
                    break;
                case PunchMode.BeePunch:
                    specificPunchReturnValue = BeePunch(punchDataPackage);
                    break;
                case PunchMode.PuffPunch:
                    specificPunchReturnValue = PuffPunch(punchDataPackage);
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


                slugcatHand1.pos += handAnimVec * 1.65f;
                slugcatHand1.vel += handAnimVec * 10f;

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

        public SpecificPunchReturnValue NormalPunch(PunchDataPackage punchDataPackage,bool isFastPunch = false)
        {
            float damage = 0.1f;

            if (isFastPunch)
            {
                damage = PunchConfigInfo.GetDamage("FastPunch");
            }
            else
            {
                damage = PunchConfigInfo.GetDamage("NormalPunch");
            }

            if(punchDataPackage.idealBodyChunkAndAppendages.Count > 0)
            {
                foreach(var bodyChunkAndAppendage in punchDataPackage.idealBodyChunkAndAppendages)
                {
                    if (bodyChunkAndAppendage.bodyChunk != null || bodyChunkAndAppendage.appendagePos != null)
                    {
                        if (bodyChunkAndAppendage.owner is Creature && bodyChunkAndAppendage.bodyChunk != null)
                        {
                            (bodyChunkAndAppendage.owner as Creature).Violence(player.mainBodyChunk, new Vector2?(punchDataPackage.punchVec), bodyChunkAndAppendage.bodyChunk, null, Creature.DamageType.Blunt, damage, 19f);
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

                        if(player != null)
                        {

                        }
                        Vector2 vector3 = (bodyChunkAndAppendage.bodyChunk != null) ? bodyChunkAndAppendage.bodyChunk.pos : bodyChunkAndAppendage.appendagePos.appendage.OnAppendagePosition(bodyChunkAndAppendage.appendagePos);
                        player.room.PlaySound(SoundID.Rock_Hit_Creature, vector3, punchDataPackage.punchVec.sqrMagnitude * 0.065f, punchDataPackage.punchVec.sqrMagnitude / 20f);

                        player.room.AddObject(new ExplosionSpikes(player.room, vector3 + Custom.DirVec(vector3, punchDataPackage.fistPos) * ((bodyChunkAndAppendage.bodyChunk != null) ? bodyChunkAndAppendage.bodyChunk.rad : 5f), 5, 2f, 4f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
                    }
                }
            }

            SpecificPunchReturnValue value = new SpecificPunchReturnValue();
            if (isFastPunch)
            {
                value.breakFrame = PunchConfigInfo.GetBreak("FastPunch");
            }
            else
            {
                value.breakFrame = PunchConfigInfo.GetBreak("NormalPunch");
            }
            value.velMulti = 1f;

            return value;
        }

        public SpecificPunchReturnValue BombPunch(PunchDataPackage punchDataPackage)
        {
            Vector2 newPos = punchDataPackage.fistPos + punchDataPackage.punchVec;
            Color explodeColor = new Color(1f, 0.4f, 0.3f);

            float damage = PunchConfigInfo.GetDamage("BombPunch");

            //与ScavengerBomb内的代码相同
            Explosion explosion = new Explosion(player.room, player, newPos, 7, 100, damage / 1.5f, damage, damage * 10f, 0.25f, player, 1f, 10, 1f);

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
            value.breakFrame = PunchConfigInfo.GetBreak("BombPunch");

            //玩家加速
            if (player.canJump > 0 && player.room.gravity != 0) { }
            {
                player.mainBodyChunk.vel += 20f * Vector2.up;
            }
            
            return value;
        }

        public SpecificPunchReturnValue LaserPunch(PunchDataPackage punchDataPackage)
        {
            SlugcatHand hand = null;
            if (player.graphicsModule != null)//动画
            {
                PlayerGraphics playerGraphics = player.graphicsModule as PlayerGraphics;
                hand = playerGraphics.hands[attackHand];
            }

            player.room.AddObject(new VE_Laser(punchDataPackage, hand, player,PunchConfigInfo.punchOtherSettings["LaserPunch"])); 

            SpecificPunchReturnValue specificPunchReturnValue;
            specificPunchReturnValue.breakFrame = PunchConfigInfo.GetBreak("LaserPunch");
            specificPunchReturnValue.velMulti = 2f;

            return specificPunchReturnValue;
        }

        public SpecificPunchReturnValue FriendlyPunch(PunchDataPackage punchDataPackage,bool highLevel = true)
        {
            try
            {
                if (punchDataPackage.idealBodyChunkAndAppendages.Count > 0)
                {
                    foreach (var bodyChunkAndAppendage in punchDataPackage.idealBodyChunkAndAppendages)
                    {
                        if (bodyChunkAndAppendage.bodyChunk != null && bodyChunkAndAppendage.owner != null)
                        {
                            if (bodyChunkAndAppendage.owner is Creature && (bodyChunkAndAppendage.owner as Creature).abstractCreature.state != null && (bodyChunkAndAppendage.owner as Creature).abstractCreature.state.socialMemory != null)
                            {
                                if((bodyChunkAndAppendage.owner as Creature).abstractCreature.state.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).like >= 1)
                                {
                                    if ((bodyChunkAndAppendage.owner as Creature).dead && PunchConfigInfo.punchDamageRanges["FriendlyPunch"].y > 0)
                                    {
                                        (bodyChunkAndAppendage.owner as Creature).dead = false;
                                        player.room.AddObject(new VE_FriendlyPunch(bodyChunkAndAppendage.owner as Creature, Color.white, 2f));
                                    }
                                    else
                                    {
                                        player.room.AddObject(new VE_FriendlyPunch(bodyChunkAndAppendage.owner as Creature, Color.green, 0.6f));
                                    }
                                    ((bodyChunkAndAppendage.owner as Creature).State as HealthState).health = Mathf.Clamp(((bodyChunkAndAppendage.owner as Creature).State as HealthState).health + PunchConfigInfo.punchDamageRanges["FriendlyPunch"].y, 0, 1f);
                                    
                                }
                                else
                                {
                                    player.room.AddObject(new VE_FriendlyPunch(bodyChunkAndAppendage.owner as Creature, Color.yellow, 1f));
                                    (bodyChunkAndAppendage.owner as Creature).abstractCreature.state.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).InfluenceLike(PunchConfigInfo.punchDamageRanges["FriendlyPunch"].x);
                                    (bodyChunkAndAppendage.owner as Creature).abstractCreature.state.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).InfluenceTempLike(PunchConfigInfo.punchDamageRanges["FriendlyPunch"].x);
                                    (bodyChunkAndAppendage.owner as Creature).abstractCreature.state.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).InfluenceKnow(PunchConfigInfo.punchDamageRanges["FriendlyPunch"].x / 5f);
                                }
                                if (highLevel)
                                {
                                    if (bodyChunkAndAppendage.owner is Lizard || bodyChunkAndAppendage.owner is Scavenger || bodyChunkAndAppendage.owner is Cicada || bodyChunkAndAppendage.owner is Deer || bodyChunkAndAppendage.owner is GarbageWorm || bodyChunkAndAppendage.owner is JetFish)
                                    {
                                        string matching = "";
                                        if(bodyChunkAndAppendage.owner is Lizard)
                                        {
                                            matching = "Lizards";
                                        }
                                        else if(bodyChunkAndAppendage.owner is Scavenger)
                                        {
                                            matching = "Scavengers";
                                        }
                                        else if (bodyChunkAndAppendage.owner is Cicada)
                                        {
                                            matching = "Cicadas";
                                        }
                                        else if (bodyChunkAndAppendage.owner is Deer)
                                        {
                                            matching = "Deer";
                                        }
                                        else if (bodyChunkAndAppendage.owner is GarbageWorm)
                                        {
                                            matching = "GarbageWorms";
                                        }
                                        else if (bodyChunkAndAppendage.owner is JetFish)
                                        {
                                            matching = "JetFish";
                                        }

                                        player.room.world.game.session.creatureCommunities.InfluenceLikeOfPlayer((CreatureCommunities.CommunityID)System.Enum.Parse(typeof(CreatureCommunities.CommunityID), matching), player.room.world.RegionNumber, player.playerState.playerNumber, 0.5f, 0.75f, 0f);
                                        if (bodyChunkAndAppendage.owner is Scavenger)
                                        {
                                            Scavenger scavenger = bodyChunkAndAppendage.owner as Scavenger;
                                            if (scavenger.AI.outpostModule != null && scavenger.AI.outpostModule.outpost != null)
                                            {
                                                if(scavenger.AI.outpostModule.outpost.worldOutpost.feePayed < 10)
                                                {
                                                    scavenger.AI.outpostModule.outpost.worldOutpost.feePayed += 5;
                                                    player.room.AddObject(new VE_FriendlyPunch(bodyChunkAndAppendage.owner as Creature, new Color(1f, 0, 1f), 0.4f, Vector2.right * 20f));
                                                }
                                            }
                                        }
                                    }

                                }

                                (bodyChunkAndAppendage.owner as Creature).Stun(40);
                           
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }

            SpecificPunchReturnValue specificPunchReturnValue = new SpecificPunchReturnValue();
            specificPunchReturnValue.breakFrame = PunchConfigInfo.GetBreak("FriendlyPunch");
            specificPunchReturnValue.velMulti = 0.5f;

            return specificPunchReturnValue;
        }

        public SpecificPunchReturnValue BeePunch(PunchDataPackage punchDataPackage)
        {
            int count = PunchConfigInfo.GetInt("BeePunch");

            for(int i = 0;i < count; i++)
            {
                SporePlant.Bee bee = new SporePlant.Bee(null, true, punchDataPackage.fistPos, punchDataPackage.punchVec, SporePlant.Bee.Mode.Hunt);
                BeeModule beeModule = new BeeModule(bee, this);

                player.room.AddObject(bee);
                beeModules.Add(beeModule);

                Debug.Log("BeeModules of " + player.ToString() + ":" + beeModules.Count.ToString());
            }

            SpecificPunchReturnValue specificPunchReturnValue = new SpecificPunchReturnValue();
            specificPunchReturnValue.breakFrame = PunchConfigInfo.GetBreak("BeePunch");
            specificPunchReturnValue.velMulti = 1.2f;

            return specificPunchReturnValue;
        }

        public SpecificPunchReturnValue PuffPunch(PunchDataPackage punchDataPackage)
        {
            InsectCoordinator smallInsects = null;
            for (int i = 0; i < player.room.updateList.Count; i++)
            {
                if (player.room.updateList[i] is InsectCoordinator)
                {
                    smallInsects = (player.room.updateList[i] as InsectCoordinator);
                    break;
                }
            }

            int total = PunchConfigInfo.GetInt("PuffPunch");
            float length = total / 20f;

            for (int j = 0; j < total; j++)
            {
                player.room.AddObject(new SporeCloud(punchDataPackage.fistPos, (Custom.RNV() + punchDataPackage.punchVec) * Mathf.Lerp(0, length, (float)j / (float)total), new Color(0.02f, 0.1f, 0.08f), Mathf.Lerp(length / 4f,length / 2f,Random.value), player.abstractCreature, j % 20, smallInsects));
            }

            SpecificPunchReturnValue specificPunchReturnValue = new SpecificPunchReturnValue
            {
                breakFrame = PunchConfigInfo.GetBreak("PuffPunch"),
                velMulti = 1
            };
            return specificPunchReturnValue;
        }

        #region useless
        //public static List<UpdatableAndDeletable> GetUpdatableAndDeletables(int index)
        //{
        //    List<UpdatableAndDeletable> newList = new List<UpdatableAndDeletable>();

        //    if(CatPunchPunch.PunchModules.Count == 0)
        //    {
        //        Debug.Log(newList.Count);
        //        return newList;
        //    }

        //    if(CatPunchPunch.PunchModules[index].player.appendages != null)
        //    {
        //        var collection1 = from physicalObject in CatPunchPunch.PunchModules[index].player.appendages
        //                          where physicalObject != null
        //                          where physicalObject.owner != null
        //                          select physicalObject.owner as UpdatableAndDeletable;
        //        newList.AddRange(collection1);
        //    }

        //    if(CatPunchPunch.PunchModules[index].player.grasps != null)
        //    {
        //        var collecetion2 = from physicalObject in CatPunchPunch.PunchModules[index].player.grasps
        //                           where physicalObject != null
        //                           where physicalObject.grabbed != null
        //                           select physicalObject.grabbed as UpdatableAndDeletable;
        //        newList.AddRange(collecetion2);
        //    }

        //    if(CatPunchPunch.PunchModules[index].explosionPunchModules != null)
        //    {
        //        var collection3 = from explosionModule in CatPunchPunch.PunchModules[index].explosionPunchModules
        //                          select explosionModule.explosion as UpdatableAndDeletable;
        //        newList.AddRange(collection3);
        //    }

        //    if(CatPunchPunch.PunchModules[index].bombSmokeModules != null)
        //    {
        //        var collection4 = from bombSmokeModule in CatPunchPunch.PunchModules[index].bombSmokeModules
        //                          select bombSmokeModule.bombSmoke as UpdatableAndDeletable;
        //        newList.AddRange(collection4);
        //    }

        //    newList.Add(CatPunchPunch.PunchModules[index].player as UpdatableAndDeletable);

        //    Debug.Log(newList.Count);
        //    return newList;
        //}
        #endregion
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
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.DataPearl)
                {
                    DataPearl.AbstractDataPearl pearl = player.objectInStomach as DataPearl.AbstractDataPearl;

                    switch (pearl.dataPearlType)
                    {
                        case DataPearl.AbstractDataPearl.DataPearlType.Misc:
                        case DataPearl.AbstractDataPearl.DataPearlType.Misc2:
                            return PunchMode.FriendlyPunch;
                        case DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl:
                            return PunchMode.FriendlyPunch_highLevel;
                    }
                }
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.SporePlant)
                {
                    return PunchMode.BeePunch;
                }
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.PuffBall)
                {
                    return PunchMode.PuffPunch;
                }
                if(player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.Creature)
                {
                    AbstractCreature creature = player.objectInStomach as AbstractCreature;

                    if(creature.creatureTemplate.type == CreatureTemplate.Type.VultureGrub)
                    {
                        return PunchMode.LaserPunch;
                    }
                    else
                    {
                        return PunchMode.NormalPunch;
                    }
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
            LaserPunch,
            SmokePunch,
            FriendlyPunch,
            FriendlyPunch_highLevel,
            BeePunch,
            PuffPunch,
            TheHandPunch,
            TheWorldPunch,
        }

        public struct PunchDataPackage
        {
            public Vector2 fistPos;
            public Vector2 punchVec;
            public Vector2 punchLaserVec;

            public List<IdealBodyChunkAndAppendage> idealBodyChunkAndAppendages;
        }

        public struct IdealBodyChunkAndAppendage
        {
            public PhysicalObject owner;

            public BodyChunk bodyChunk;
            public PhysicalObject.Appendage.Pos appendagePos;
        }

        public struct LaserLineInfo
        {
            public PhysicalObject owner;

            public BodyChunk bodyChunk;
            public Vector2 laserEndPoint;
            public float distance;
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

    public class BeeModule
    {
        public BeeModule(SporePlant.Bee bee,PunchModule punchModule)
        {
            Bee = bee;
            this.punchModule = punchModule;
        }

        public void Update(bool eu)
        {
            if (Bee == null)
            {
                Destroy();
                return;
            }
            Bee.evenUpdate = eu;

            Bee.inModeCounter++;
            Bee.lastLastLastPos = Bee.lastLastPos;
            Bee.lastLastPos = Bee.lastPos;
            Bee.lastPos = Bee.pos;
            Bee.pos += Bee.vel;
            Bee.vel *= 0.9f;
            Bee.flyDir.Normalize();
            Bee.lastFlyDir = Bee.flyDir;
            Bee.vel += Bee.flyDir * Bee.flySpeed;
            Bee.flyDir += Custom.RNV() * UnityEngine.Random.value * ((Bee.mode != SporePlant.Bee.Mode.LostHive) ? 0.6f : 1.2f);
            Bee.lastBlink = Bee.blink;
            Bee.blink += Bee.blinkFreq;
            Bee.lastBoostTrail = Bee.boostTrail;
            Bee.boostTrail = Mathf.Max(0f, Bee.boostTrail - 0.3f);
            SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(Bee.pos, Bee.lastPos, Bee.vel, 1f, new IntVector2(0, 0), true);
            SharedPhysics.VerticalCollision(Bee.room, terrainCollisionData);
            SharedPhysics.HorizontalCollision(Bee.room, terrainCollisionData);
            Bee.pos = terrainCollisionData.pos;
            Bee.vel = terrainCollisionData.vel;

            Bee.life -= 1f / Bee.lifeTime;

            if (Bee.life < 0.2f * UnityEngine.Random.value)
            {
                Bee.vel.y = Bee.vel.y - Mathf.InverseLerp(0.2f, 0f, Bee.life);
                if (Bee.life <= 0f && (terrainCollisionData.contactPoint.y < 0 || Bee.pos.y < -100f))
                {
                    Destroy();
                    Bee.Destroy();
                }
                Bee.flySpeed = Mathf.Min(Bee.flySpeed, Mathf.Max(0f, Bee.life) * 3f);
                if (Bee.room.water && Bee.pos.y < Bee.room.FloatWaterLevel(Bee.pos.x))
                {
                    Destroy();
                    Bee.Destroy();
                }
                return;
            }
            if (Bee.room.water && Bee.pos.y < Bee.room.FloatWaterLevel(Bee.pos.x))
            {
                Bee.pos.y = Bee.room.FloatWaterLevel(Bee.pos.x) + 1f;
                Bee.vel.y = Bee.vel.y + 1f;
                Bee.flyDir.y = Bee.flyDir.y + 1f;
            }
            if (terrainCollisionData.contactPoint.x != 0)
            {
                Bee.flyDir.x = Bee.flyDir.x - (float)terrainCollisionData.contactPoint.x;
            }
            if (terrainCollisionData.contactPoint.y != 0)
            {
                Bee.flyDir.y = Bee.flyDir.y - (float)terrainCollisionData.contactPoint.y;
            }

            if (Bee.huntChunk != null && Bee.mode != SporePlant.Bee.Mode.Hunt)
            {
                Bee.ChangeMode(SporePlant.Bee.Mode.Hunt);
            }

            if(Bee.huntChunk == null)
            {
                Bee.blinkFreq = Custom.LerpAndTick(Bee.blinkFreq, 0.033333335f, 0.05f, 0.033333335f);
                Bee.flySpeed = Custom.LerpAndTick(Bee.flySpeed, 0.9f, 0.08f, UnityEngine.Random.value / 30f);

                if (UnityEngine.Random.value < 0.0025f)
                {
                    Bee.room.AddObject(new SporePlant.BeeSpark(Bee.pos));
                }
                if (UnityEngine.Random.value < 0.016666668f)
                {
                    Bee.room.PlaySound(SoundID.Spore_Bee_Angry_Buzz, Bee.pos, Custom.LerpMap(Bee.life, 0f, 0.25f, 0.1f, 0.5f) + UnityEngine.Random.value * 0.5f, Custom.LerpMap(Bee.life, 0f, 0.5f, 0.8f, 0.9f, 0.4f));
                }

                if (punchModule.beeModules.Count > 1)
                {
                    SporePlant.Bee bee = (punchModule.beeModules[UnityEngine.Random.Range(0, punchModule.beeModules.Count)]).Bee;
                    if(bee != null)
                    {
                        if (bee != Bee &&  bee.huntChunk != null && Custom.DistLess(Bee.pos, bee.pos,  bee.huntChunk != null ? 300f : 60f) && Bee.room.VisualContact(Bee.pos, bee.pos))
                        {
                            if (bee.huntChunk != null && bee.huntChunk.owner.TotalMass > 0.3f && UnityEngine.Random.value < Bee.CareAboutChunk(bee.huntChunk))
                            {
                                if (Bee.HuntChunkIfPossible(bee.huntChunk))
                                {
                                    return;
                                }
                                if (Vector2.Distance(bee.pos, bee.huntChunk.pos) < Vector2.Distance(Bee.hoverPos, bee.huntChunk.pos))
                                {
                                    Bee.vel += Vector2.ClampMagnitude(bee.pos - Bee.pos, 60f) / 20f * 3f;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Bee.blinkFreq = Custom.LerpAndTick(Bee.blinkFreq, 0.33333334f, 0.05f, 0.033333335f);
                float num3 = Mathf.InverseLerp(-1f, 1f, Vector2.Dot(Bee.flyDir.normalized, Custom.DirVec(Bee.pos, Bee.huntChunk.pos)));
                Bee.flySpeed = Custom.LerpAndTick(Bee.flySpeed, Mathf.Clamp(Mathf.InverseLerp(Bee.huntChunk.rad, Bee.huntChunk.rad + 110f, Vector2.Distance(Bee.pos, Bee.huntChunk.pos)) * 2f + num3, 0.4f, 2.2f), 0.08f, UnityEngine.Random.value / 30f);
                Bee.flySpeed = Custom.LerpAndTick(Bee.flySpeed, Custom.LerpMap(Vector2.Dot(Bee.flyDir.normalized, Custom.DirVec(Bee.pos, Bee.huntChunk.pos)), -1f, 1f, 0.4f, 1.8f), 0.08f, UnityEngine.Random.value / 30f);
                Bee.vel *= 0.9f;
                Bee.flyDir = Vector2.Lerp(Bee.flyDir, Custom.DirVec(Bee.pos, Bee.huntChunk.pos), UnityEngine.Random.value * 0.4f);
                if (UnityEngine.Random.value < 0.033333335f)
                {
                    Bee.room.PlaySound(SoundID.Spore_Bee_Angry_Buzz, Bee.pos, Custom.LerpMap(Bee.life, 0f, 0.25f, 0.1f, 1f), Custom.LerpMap(Bee.life, 0f, 0.5f, 0.8f, 1.2f, 0.25f));
                }
                if (UnityEngine.Random.value < 0.1f && Bee.lastBoostTrail <= 0f && num3 > 0.7f && Custom.DistLess(Bee.pos, Bee.huntChunk.pos, Bee.huntChunk.rad + 150f) && !Custom.DistLess(Bee.pos, Bee.huntChunk.pos, Bee.huntChunk.rad + 50f) && Bee.room.VisualContact(Bee.pos, Bee.huntChunk.pos))
                {
                    Vector2 a = Vector3.Slerp(Custom.DirVec(Bee.pos, Bee.huntChunk.pos), Bee.flyDir.normalized, 0.5f);
                    float num4 = Vector2.Distance(Bee.pos, Bee.huntChunk.pos) - Bee.huntChunk.rad;
                    Vector2 b = Bee.pos + a * num4;
                    if (num4 > 30f && !Bee.room.GetTile(b).Solid && !Bee.room.PointSubmerged(b) && Bee.room.VisualContact(Bee.pos, b))
                    {
                        Bee.boostTrail = 1f;
                        Bee.pos = b;
                        Bee.vel = a * 10f;
                        Bee.flyDir = a;
                        Bee.room.AddObject(new SporePlant.BeeSpark(Bee.lastPos));
                        Bee.room.PlaySound(SoundID.Spore_Bee_Dash, Bee.lastPos);
                        Bee.room.PlaySound(SoundID.Spore_Bee_Spark, Bee.pos, 0.2f, 1.5f);
                    }
                }
                for (int j = 0; j < Bee.huntChunk.owner.bodyChunks.Length; j++)
                {
                    if (Custom.DistLess(Bee.pos, Bee.huntChunk.owner.bodyChunks[j].pos, Bee.huntChunk.owner.bodyChunks[j].rad))
                    {
                        Bee.Attach(Bee.huntChunk.owner.bodyChunks[j]);
                        return;
                    }
                }
                if (!Custom.DistLess(Bee.pos, Bee.huntChunk.pos, Bee.huntChunk.rad + 400f) || (UnityEngine.Random.value < 0.1f && Bee.huntChunk.submersion > 0.8f) || Bee.ObjectAlreadyStuck(Bee.huntChunk.owner) || !Bee.room.VisualContact(Bee.pos, Bee.huntChunk.pos))
                {
                    Bee.huntChunk = null;
                    return;
                }
            }

            if (Bee.huntChunk == null)
            {
                Bee.LookForRandomCreatureToHunt();
            }
        }

        public bool LookForRandomCreatureToHunt()
        {
            if (Bee == null)
            {
                Destroy();
                return false;
            }

            if (Bee.huntChunk != null)
            {
                return false;
            }
            if (Bee.room.abstractRoom.creatures.Count > 0)
            {
                AbstractCreature abstractCreature = Bee.room.abstractRoom.creatures[UnityEngine.Random.Range(0, Bee.room.abstractRoom.creatures.Count)];
                if (abstractCreature.realizedCreature != null && abstractCreature.realizedCreature.room == Bee.room && SporePlant.SporePlantInterested(abstractCreature.realizedCreature.Template.type) && abstractCreature.realizedCreature != punchModule.player)
                {
                    for (int i = 0; i < abstractCreature.realizedCreature.bodyChunks.Length; i++)
                    {
                        if (Custom.DistLess(Bee.pos, abstractCreature.realizedCreature.bodyChunks[i].pos, abstractCreature.realizedCreature.bodyChunks[i].rad))
                        {
                            Bee.Attach(abstractCreature.realizedCreature.bodyChunks[i]);
                            return true;
                        }
                    }
                    return Bee.HuntChunkIfPossible(abstractCreature.realizedCreature.bodyChunks[UnityEngine.Random.Range(0, abstractCreature.realizedCreature.bodyChunks.Length)]);
                }
            }
            if (UnityEngine.Random.value < 0.1f && punchModule.attachedBeeModules.Count > 0 )
            {
                AttachedBeeModule attachedBeeModule = punchModule.attachedBeeModules[UnityEngine.Random.Range(0, punchModule.attachedBeeModules.Count)];
                SporePlant.AttachedBee attachedBee = attachedBeeModule.AttachedBee;

                if (attachedBee == null || attachedBee.slatedForDeletetion)
                {
                    attachedBeeModule.Destroy();
                    return false;
                }
                if (attachedBee.attachedChunk != null)
                {
                    return Bee.HuntChunkIfPossible(attachedBee.attachedChunk.owner.bodyChunks[UnityEngine.Random.Range(0, attachedBee.attachedChunk.owner.bodyChunks.Length)]);
                }
            }
            return false;
        }

        public void Attach(BodyChunk chunk)
        {
            if(Bee == null)
            {
                Destroy();
                return;
            }
            if (Bee.slatedForDeletetion)
            {
                Destroy();
                return;
            }
            SporePlant.AttachedBee attachedBee = new SporePlant.AttachedBee(Bee.room, new AbstractPhysicalObject(Bee.room.world, AbstractPhysicalObject.AbstractObjectType.AttachedBee, null, Bee.room.GetWorldCoordinate(Bee.pos), Bee.room.game.GetNewID()), chunk, Bee.pos, Custom.DirVec(Bee.lastLastPos, Bee.pos), Bee.life, Bee.lifeTime, Bee.boostTrail > 0f);
            AttachedBeeModule attachedBeeModule = new AttachedBeeModule(attachedBee, punchModule);

            punchModule.attachedBeeModules.Add(attachedBeeModule);
            Bee.room.AddObject(attachedBee);
            Debug.Log("AttachedBeeModules of " + punchModule.player.ToString() + ":" + punchModule.attachedBeeModules.Count.ToString());

            Bee.room.PlaySound(SoundID.Spore_Bee_Attach_Creature, chunk);
            Destroy();
            Bee.Destroy();
        }

        public void Destroy()
        {
            if (punchModule.beeModules.Contains(this))
            {
                punchModule.beeModules.Remove(this);
                Debug.Log("BeeModules of " + punchModule.player.ToString() + ":" + punchModule.beeModules.Count.ToString());
            }
        }

        public class AttachedBeeModule
        {
            public AttachedBeeModule(SporePlant.AttachedBee attachedBee,PunchModule punchModule)
            {
                AttachedBee = attachedBee;
                this.punchModule = punchModule;
            }

            public void BreakStinger()
            {
                Destroy();
            }

            public void Destroy()
            {
                if (punchModule.attachedBeeModules.Contains(this))
                {
                    punchModule.attachedBeeModules.Remove(this);
                    Debug.Log("AttachedBeeModules of " + punchModule.player.ToString() + ":" + punchModule.attachedBeeModules.Count.ToString());
                }
            }

            WeakReference _attachedBee;

            public SporePlant.AttachedBee AttachedBee
            {
                get
                {
                    if (_attachedBee.Target == null)
                    {
                        Destroy();
                        return null;
                    }
                    else
                    {
                        return _attachedBee.Target as SporePlant.AttachedBee;
                    }
                }
                set
                {
                    _attachedBee = new WeakReference(value);
                }
            }
            PunchModule punchModule;
        }


        WeakReference _bee;
        public SporePlant.Bee Bee
        {
            get
            {
                if(_bee.Target == null)
                {
                    Destroy();
                    return null;
                }
                else
                {
                    return _bee.Target as SporePlant.Bee;
                }
            }
            set
            {
                _bee = new WeakReference(value);
            }
        }
        PunchModule punchModule;
    }

    public static class ModulePatch
    {
        public static PunchModule GetPunchModule(this Player player)
        {
            if(CatPunchPunch.PunchModules[player.playerState.playerNumber] == null || CatPunchPunch.PunchModules[player.playerState.playerNumber].player != player)
            {
                CatPunchPunch.PunchModules[player.playerState.playerNumber] = new PunchModule(player);
            }
            return CatPunchPunch.PunchModules[player.playerState.playerNumber];
        }
        public static BombSmokeModule GetBombSmokeModule(this BombSmoke bombSmoke)
        {
            for (int i = 0; i < CatPunchPunch.PunchModules.Count(); i++)
            {
                for(int j = CatPunchPunch.PunchModules[i].bombSmokeModules.Count - 1;j >= 0; j--)
                {
                    if(CatPunchPunch.PunchModules[i].bombSmokeModules[j].bombSmoke == bombSmoke)
                    {
                        return CatPunchPunch.PunchModules[i].bombSmokeModules[j];
                    }
                }
            }
            return null;
        }

        public static ExplosionPunchModule GetExplosionPunchModule(this Explosion explosion)
        {
            for(int i = 0;i < CatPunchPunch.PunchModules.Count(); i++)
            {
                if (CatPunchPunch.PunchModules[i] != null && CatPunchPunch.PunchModules[i].explosionPunchModules.Count > 0)
                {
                    for (int j = CatPunchPunch.PunchModules[i].explosionPunchModules.Count - 1; j >= 0; j--)
                    {
                        if (CatPunchPunch.PunchModules[i].explosionPunchModules[j].explosion == explosion)
                        {
                            return CatPunchPunch.PunchModules[i].explosionPunchModules[j];
                        }
                    }
                }
            }
            return null;
        }

        public static BeeModule GetBeeModule(this SporePlant.Bee bee)
        {
            for (int i = 0; i < CatPunchPunch.PunchModules.Count(); i++)
            {
                if (CatPunchPunch.PunchModules[i] != null && CatPunchPunch.PunchModules[i].beeModules.Count > 0)
                {
                    for (int j = CatPunchPunch.PunchModules[i].beeModules.Count - 1; j >= 0; j--)
                    {
                        if (CatPunchPunch.PunchModules[i].beeModules[j].Bee == bee)
                        {
                            return CatPunchPunch.PunchModules[i].beeModules[j];
                        }
                    }
                }
            }
            return null;
        }

        public static BeeModule.AttachedBeeModule GetAttachedBeeModule(this SporePlant.AttachedBee attachedBee)
        {
            for (int i = 0; i < CatPunchPunch.PunchModules.Count(); i++)
            {
                if (CatPunchPunch.PunchModules[i] != null && CatPunchPunch.PunchModules[i].attachedBeeModules.Count > 0)
                {
                    for (int j = CatPunchPunch.PunchModules[i].attachedBeeModules.Count - 1; j >= 0; j--)
                    {
                        if (CatPunchPunch.PunchModules[i].attachedBeeModules[j].AttachedBee == attachedBee)
                        {
                            return CatPunchPunch.PunchModules[i].attachedBeeModules[j];
                        }
                    }
                }
            }
            return null;
        }
    }
}