using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatPunchPunch
{
    public class VE_Laser : CosmeticSprite
    {
        PunchModule.PunchDataPackage punchDataPackage;
        SlugcatHand hand;
        Player player;
        public VE_Laser(PunchModule.PunchDataPackage punchDataPackage, SlugcatHand hand,Player player)
        {
            this.punchDataPackage = punchDataPackage;
            this.hand = hand;
            this.player = player;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];

            laser = new CustomFSprite("Futile_White");
            laser.shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"];

            flash = new FSprite("Futile_White", true);
            flash.shader = rCam.game.rainWorld.Shaders["FlatLight"];

            flash_E = new FSprite("Futile_White", true);
            flash_E.shader = rCam.game.rainWorld.Shaders["FlatLight"];

            light = new LightSource(hand.pos, false, Color.red, this);
            player.room.AddObject(light);

            light_E = new LightSource(hand.pos, false, Color.red, this);
            player.room.AddObject(light_E);

            sLeaser.sprites[0] = laser;
            sLeaser.sprites[1] = flash;
            sLeaser.sprites[2] = flash_E;

            AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (hand == null)
            {
                sLeaser.sprites[0].isVisible = false;
                return;
            }
            else
            {
                sLeaser.sprites[0].isVisible = true;
            }

            Vector2 fromPos = hand.pos;
            Vector2 dir = punchDataPackage.punchLaserVec.normalized;

            (sLeaser.sprites[0] as CustomFSprite).verticeColors[0] = Custom.RGB2RGBA(currentColor, currentColor.a);
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[1] = Custom.RGB2RGBA(currentColor, currentColor.a);
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[2] = Custom.RGB2RGBA(currentColor, currentColor.a);
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[3] = Custom.RGB2RGBA(currentColor, currentColor.a);


            Vector2 corner = Custom.RectCollision(fromPos, fromPos + dir * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, fromPos, corner);
            
            
            if (intVector != null)
            {
                corner = Custom.RectCollision(corner, fromPos, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
            }

            //激光检测
            PhysicalObject owner = null;
            BodyChunk closestLaserChunck = null;
            float maxLaserDist = float.MaxValue;
            try
            {
                foreach (var physicObj in player.room.physicalObjects[player.collisionLayer])
                {
                    if ((physicObj != player) && physicObj is Creature)
                    {
                        if (physicObj.bodyChunks.Length > 0)
                        {
                            foreach (var chunck in physicObj.bodyChunks)//检查身体区块
                            {
                                float lineDist = Mathf.Abs(Custom.DistanceToLine(chunck.pos, hand.pos, punchDataPackage.punchLaserVec + hand.pos));
                                float dist = Custom.Dist(chunck.pos, hand.pos);

                                if (lineDist < chunck.rad && dist < maxLaserDist)
                                {
                                    Vector2 directionVec1 = chunck.pos - hand.pos;
                                    Vector2 directionVec2 = punchDataPackage.punchLaserVec;

                                    if (Vector2.Dot(directionVec1, directionVec2) > 0)
                                    {
                                        closestLaserChunck = chunck;
                                        owner = physicObj;
                                        maxLaserDist = dist;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogException(new NullReferenceException("At block1-1"));
                Debug.LogException(e);
                goto JumpOver;
            }

            try
            {
                if (closestLaserChunck != null)
                {
                    if ((corner - hand.pos).magnitude >= maxLaserDist)
                    {
                        corner = Custom.ClosestPointOnLine(hand.pos, punchDataPackage.punchLaserVec + hand.pos, closestLaserChunck.pos); ;

                        dir = (punchDataPackage.punchLaserVec).normalized;

                        if (owner is Creature && currentColor.a > 0.3f)
                        {
                            if(owner is Player)
                            {
                                (owner as Player).SetKillTag(player.abstractCreature);
                                (owner as Player).Die();
                            }
                            else
                            {
                                (owner as Creature).Violence(closestLaserChunck, new Vector2?(punchDataPackage.punchVec.normalized * 1.2f), closestLaserChunck, null, Creature.DamageType.Explosion, 0.02f * currentColor.a * Mathf.Pow(owner.TotalMass,0.7f), 19f);
                                (owner as Creature).SetKillTag(player.abstractCreature);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogException(new NullReferenceException("At block1-2"));
                Debug.LogException(e);
                goto JumpOver;
            }

            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(0, fromPos - dir * 4f + Custom.PerpendicularVector(dir) * 0.5f - camPos);
            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(1, fromPos - dir * 4f - Custom.PerpendicularVector(dir) * 0.5f - camPos);
            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(2, corner - Custom.PerpendicularVector(dir) * 0.5f - camPos);
            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(3, corner + Custom.PerpendicularVector(dir) * 0.5f - camPos);

            sLeaser.sprites[1].x = fromPos.x - camPos.x;
            sLeaser.sprites[1].y = fromPos.y - camPos.y;
            sLeaser.sprites[1].color = currentColor;
            sLeaser.sprites[1].alpha = Mathf.Pow(currentColor.a, 0.5f);
            sLeaser.sprites[1].scale = Mathf.Pow(currentColor.a, 1.2f) * 1.5f;

            sLeaser.sprites[2].x = corner.x - camPos.x;
            sLeaser.sprites[2].y = corner.y - camPos.y;
            sLeaser.sprites[2].color = currentColor;
            sLeaser.sprites[2].alpha = Mathf.Pow(currentColor.a, 0.5f);
            sLeaser.sprites[2].scale = Mathf.Pow(currentColor.a, 1.2f) * 1.5f;

            light.setPos = new Vector2?(hand.pos);
            light.setAlpha = new float?(Mathf.Pow(currentColor.a, 0.5f));
            light.setRad = new float?(Mathf.Lerp(50f, 120f, Mathf.Pow(currentColor.a, 1.2f)));

            light_E.setPos = new Vector2?(corner);
            light_E.setAlpha = new float?(Mathf.Pow(currentColor.a, 0.5f));
            light_E.setRad = new float?(Mathf.Lerp(50f, 120f, Mathf.Pow(currentColor.a, 1.2f)));

            JumpOver:

            currentColor = Color.Lerp(lastColor, new Color(0, 0, 0, 0),0.02f);
            lastColor = currentColor;

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (currentColor.a < 0.1f)
            {
                Destroy();
            }
        }

        public override void Destroy()
        {
            player = null;
            hand = null;
            punchDataPackage = new PunchModule.PunchDataPackage();

            light.Destroy();
            light = null;
            light_E.Destroy();
            light_E = null;

            laser.isVisible = false;
            flash.isVisible = false;
            flash_E.isVisible = false;
            laser.RemoveFromContainer();
            flash.RemoveFromContainer();
            flash_E.RemoveFromContainer();
            base.Destroy();
        }

        public CustomFSprite laser;
        public FSprite flash;
        public FSprite flash_E;

        public LightSource light;
        public LightSource light_E;

        public Color currentColor = Color.red;
        public Color lastColor = Color.red;
    }

    public class VE_FriendlyPunch : CosmeticSprite
    {
        public VE_FriendlyPunch(Creature creature,Color color,float cof,Vector2? bias = null)
        {
            current_Pos = creature.mainBodyChunk.pos + (bias.HasValue ? bias.Value : Vector2.zero);
            last_Pos = creature.mainBodyChunk.pos + (bias.HasValue ? bias.Value : Vector2.zero);
            aim_Pos = creature.mainBodyChunk.pos + 60f * Vector2.up + (bias.HasValue ? bias.Value : Vector2.zero);

            sColor = color;
            sCof = cof;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];

            plus_x = new FSprite("pixel", true) { scaleX = 30 * sCof, scaleY = 5 * sCof, color = sColor };
            plus_y = new FSprite("pixel", true) { scaleX = 5 * sCof, scaleY = 30 * sCof, color = sColor };

            sLeaser.sprites[0] = plus_x;
            sLeaser.sprites[1] = plus_y;

            AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            plus_x.SetPosition(current_Pos - camPos);
            plus_y.SetPosition(current_Pos - camPos);

            plus_x.alpha = current_A;
            plus_y.alpha = current_A;

            current_Pos = Vector2.Lerp(last_Pos, aim_Pos, 0.05f);
            last_Pos = current_Pos;

            current_A = Mathf.Lerp(last_A, 0, 0.05f);
            last_A = current_A;

            if (current_A < 0.05f)
            {
                Destroy();
            }

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void Destroy()
        {
            plus_x.isVisible = false;
            plus_x.RemoveFromContainer();
            plus_y.isVisible = false;
            plus_y.RemoveFromContainer();
        }
        public Vector2 current_Pos;
        public Vector2 last_Pos;
        public Vector2 aim_Pos;

        public float current_A = 1f;
        public float last_A = 1f;

        public FSprite plus_x;
        public FSprite plus_y;

        Color sColor;
        float sCof;
    }
}
