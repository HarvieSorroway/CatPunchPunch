using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using RWCustom;

namespace CatPunchPunch
{
    public static class FreezeCore
    {
        public static List<UpdatableAndDeletable> updatableAndDeletables_ToUpdate_OneMoreFrame = new List<UpdatableAndDeletable>();

        public static void Patch()
        {
            On.Room.Update += Room_Update;
            On.RoomCamera.SpriteLeaser.Update += SpriteLeaser_Update;
            On.UpdatableAndDeletable.ctor += UpdatableAndDeletable_ctor;
        }

        private static void UpdatableAndDeletable_ctor(On.UpdatableAndDeletable.orig_ctor orig, UpdatableAndDeletable self)
        {
            orig.Invoke(self);
            updatableAndDeletables_ToUpdate_OneMoreFrame.Add(self);
        }

        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            if (Freezing)
            {
                if(self.drawableObject is GraphicsModule)
                {
                    if((self.drawableObject as GraphicsModule).ShouldIUpdateThisFrame())
                    {
                        orig.Invoke(self, timeStacker, rCam, camPos);
                    }
                    else
                    {
                        orig.Invoke(self, 0.00001f, rCam, camPos);
                    }
                }
                else if (self.drawableObject is PhysicalObject)
                {
                    PhysicalObject physicalObject = self.drawableObject as PhysicalObject;
                    if (physicalObject.grabbedBy.Count > 0)
                    {
                        if(physicalObject.ShouldIUpdateThisFrame() || (physicalObject.grabbedBy[0].grabber != null && physicalObject.grabbedBy[0].grabber.ShouldIUpdateThisFrame()))
                        {
                            orig.Invoke(self, timeStacker, rCam, camPos);
                        }
                        else
                        {
                            orig.Invoke(self, 0.00001f, rCam, camPos);
                        }
                    }
                    else
                    {
                        orig.Invoke(self, 0.00001f, rCam, camPos);
                    }
                }

            }
            else
            {
                orig.Invoke(self, timeStacker, rCam, camPos);
            }
        }

        private static void Room_Update(On.Room.orig_Update orig, Room self)
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                FreezeTime();
            }
            else
            {
                ThawTime();
            }

            if (Freezing)
            {
                self.updateIndex = self.updateList.Count - 1;

                if (self.game == null)
                {
                    return;
                }
                if (self.waitToEnterAfterFullyLoaded > 0 && self.fullyLoaded)
                {
                    self.waitToEnterAfterFullyLoaded--;
                }

                while (self.updateIndex >= 0)
                {
                    UpdatableAndDeletable updatableAndDeletable = self.updateList[self.updateIndex];

                    if (updatableAndDeletable.slatedForDeletetion || updatableAndDeletable.room != self)
                    {
                        self.CleanOutObjectNotInThisRoom(updatableAndDeletable);
                    }
                    else
                    {
                        if (updatableAndDeletable.ShouldIUpdateThisFrame())
                        {
                            updatableAndDeletable.Update(self.game.evenUpdate);
                        }

                        if (updatableAndDeletable.slatedForDeletetion || updatableAndDeletable.room != self)
                        {
                            self.CleanOutObjectNotInThisRoom(updatableAndDeletable);
                        }
                        else if (updatableAndDeletable is PhysicalObject)
                        {
                            if (!updatableAndDeletable.ShouldIUpdateThisFrame())
                            {
                                self.updateIndex--;
                                continue;
                            }

                            if ((updatableAndDeletable as PhysicalObject).graphicsModule != null)
                            {
                                (updatableAndDeletable as PhysicalObject).graphicsModule.Update();
                                (updatableAndDeletable as PhysicalObject).GraphicsModuleUpdated(true, self.game.evenUpdate);
                            }
                            else
                            {
                                (updatableAndDeletable as PhysicalObject).GraphicsModuleUpdated(false, self.game.evenUpdate);
                            }
                        }
                    }
                    self.updateIndex--;
                }

                updatableAndDeletables_ToUpdate_OneMoreFrame.Clear();
                updatableAndDeletables_ToUpdate_OneMoreFrame.AddRange(PunchModule.GetUpdatableAndDeletables(0));   

                for (int j = 1; j < self.physicalObjects.Length; j++)
                {
                    for (int k = 0; k < self.physicalObjects[j].Count; k++)
                    {
                        for (int l = k + 1; l < self.physicalObjects[j].Count; l++)
                        {
                            if (Mathf.Abs(self.physicalObjects[j][k].bodyChunks[0].pos.x - self.physicalObjects[j][l].bodyChunks[0].pos.x) < self.physicalObjects[j][k].collisionRange + self.physicalObjects[j][l].collisionRange && Mathf.Abs(self.physicalObjects[j][k].bodyChunks[0].pos.y - self.physicalObjects[j][l].bodyChunks[0].pos.y) < self.physicalObjects[j][k].collisionRange + self.physicalObjects[j][l].collisionRange)
                            {
                                bool flag = false;
                                bool flag2 = false;
                                if (self.physicalObjects[j][k] is Creature && (self.physicalObjects[j][k] as Creature).Template.grasps > 0)
                                {
                                    foreach (Creature.Grasp grasp in (self.physicalObjects[j][k] as Creature).grasps)
                                    {
                                        if (grasp != null && grasp.grabbed == self.physicalObjects[j][l])
                                        {
                                            flag2 = true;
                                            break;
                                        }
                                    }
                                }
                                if (!flag2 && self.physicalObjects[j][l] is Creature && (self.physicalObjects[j][l] as Creature).Template.grasps > 0)
                                {
                                    foreach (Creature.Grasp grasp2 in (self.physicalObjects[j][l] as Creature).grasps)
                                    {
                                        if (grasp2 != null && grasp2.grabbed == self.physicalObjects[j][k])
                                        {
                                            flag2 = true;
                                            break;
                                        }
                                    }
                                }
                                if (!flag2)
                                {
                                    for (int num = 0; num < self.physicalObjects[j][k].bodyChunks.Length; num++)
                                    {
                                        for (int num2 = 0; num2 < self.physicalObjects[j][l].bodyChunks.Length; num2++)
                                        {
                                            if (self.physicalObjects[j][k].bodyChunks[num].collideWithObjects && self.physicalObjects[j][l].bodyChunks[num2].collideWithObjects && Custom.DistLess(self.physicalObjects[j][k].bodyChunks[num].pos, self.physicalObjects[j][l].bodyChunks[num2].pos, self.physicalObjects[j][k].bodyChunks[num].rad + self.physicalObjects[j][l].bodyChunks[num2].rad))
                                            {
                                                float num3 = self.physicalObjects[j][k].bodyChunks[num].rad + self.physicalObjects[j][l].bodyChunks[num2].rad;
                                                float num4 = Vector2.Distance(self.physicalObjects[j][k].bodyChunks[num].pos, self.physicalObjects[j][l].bodyChunks[num2].pos);
                                                Vector2 a = Custom.DirVec(self.physicalObjects[j][k].bodyChunks[num].pos, self.physicalObjects[j][l].bodyChunks[num2].pos);
                                                float num5 = self.physicalObjects[j][l].bodyChunks[num2].mass / (self.physicalObjects[j][k].bodyChunks[num].mass + self.physicalObjects[j][l].bodyChunks[num2].mass);
                                                self.physicalObjects[j][k].bodyChunks[num].pos -= (num3 - num4) * a * num5;
                                                self.physicalObjects[j][k].bodyChunks[num].vel -= (num3 - num4) * a * num5;
                                                self.physicalObjects[j][l].bodyChunks[num2].pos += (num3 - num4) * a * (1f - num5);
                                                self.physicalObjects[j][l].bodyChunks[num2].vel += (num3 - num4) * a * (1f - num5);
                                                if (self.physicalObjects[j][k].bodyChunks[num].pos.x == self.physicalObjects[j][l].bodyChunks[num2].pos.x)
                                                {
                                                    self.physicalObjects[j][k].bodyChunks[num].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                                    self.physicalObjects[j][l].bodyChunks[num2].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                                }
                                                if (!flag)
                                                {
                                                    self.physicalObjects[j][k].Collide(self.physicalObjects[j][l], num, num2);
                                                    self.physicalObjects[j][l].Collide(self.physicalObjects[j][k], num2, num);
                                                }
                                                flag = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                orig.Invoke(self);
            }
        }

        public static void FreezeTime()
        {
            Freezing = true;
            if(CatPunchPunch.PunchModules.Count > 0)
            {
                CatPunchPunch.PunchModules[0].player.mushroomCounter = 10;
                CatPunchPunch.PunchModules[0].player.mushroomEffect = 0f;
            }
        }

        public static void ThawTime()
        {
            Freezing = false;
        }

        static bool Freezing = false;
        

        public static bool ShouldIUpdateThisFrame(this UpdatableAndDeletable updatable)
        {
            return updatableAndDeletables_ToUpdate_OneMoreFrame.Contains(updatable);
        }

        public static bool ShouldIUpdateThisFrame(this GraphicsModule graphicsModule)
        {
            return updatableAndDeletables_ToUpdate_OneMoreFrame.Contains(graphicsModule.owner);
        }
    }
}