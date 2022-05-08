using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionalUI;
using BepInEx;
using UnityEngine;
using RWCustom;
using Menu;
using Random = UnityEngine.Random;

namespace CatPunchPunch
{
    public static class PunchConfigInfo
    {
        public static string version = "1.1.1";

        public static Dictionary<string, Vector2> punchDamageRanges = new Dictionary<string, Vector2>();
        public static Dictionary<string, IntVector2> punchIntRanges = new Dictionary<string, IntVector2>();
        public static Dictionary<string, int> punchBreakFrames = new Dictionary<string, int>();
        public static Dictionary<string, string> punchOtherSettings = new Dictionary<string, string>();

        public static float GetDamage(string punchType)
        {
            if (punchDamageRanges.ContainsKey(punchType))
            {
                Debug.Log("GetDamage:" + punchType + " : " + punchDamageRanges[punchType].x.ToString() + "," + punchDamageRanges[punchType].y.ToString());
                return Mathf.Lerp(punchDamageRanges[punchType].x, punchDamageRanges[punchType].y, Random.value);
            }
            else
            {
                return 0.5f;
            }
        }

        public static int GetInt(string punchType)
        {
            if (punchIntRanges.ContainsKey(punchType))
            {
                return Random.Range(punchIntRanges[punchType].x, punchIntRanges[punchType].y + 1);
            }
            else
            {
                return 1;
            }
        }

        public static int GetBreak(string punchType)
        {
            if (punchBreakFrames.ContainsKey(punchType))
            {
                return punchBreakFrames[punchType];
            }
            else
            {
                return 20;
            }
        }
    }

    public class PunchConfig : OptionInterface
    {
        List<IPunchInfoBox> punchInfoBoxes = new List<IPunchInfoBox>();

        public PunchConfig(BaseUnityPlugin baseUnityPlugin) : base(baseUnityPlugin)
        {

        }

        public override void Initialize()
        {
            punchInfoBoxes.Clear();
            base.Initialize();

            Tabs = new OpTab[1];
            Tabs[0] = new OpTab("CatPunchPunch_Tab0");

            //标题和信息介绍
            OpLabel title = new OpLabel(new Vector2(300 - 100,550),new Vector2(200,30), "CatPunchPunch",FLabelAlignment.Center, true) { color = Color.white};
            OpLabel version = new OpLabel(50, 500, "Version:" + PunchConfigInfo.version);
            OpLabel author = new OpLabel(510, 500, "Harvie");

            OpScrollBox opScrollBox = new OpScrollBox(new Vector2(50,50),new Vector2(500,450),900f);

            Tabs[0].AddItems(title, version, author, opScrollBox);

            PunchInfoBox NormalPunch_Box = new PunchInfoBox(opScrollBox, "NormalPunch", new Vector2(0, 0), new Vector2(0.1f, 1.1f), new Vector2(0.2f, 1.2f), new IntVector2(1, 60), 0.1f, 0.2f, 5);
            PunchInfoBox FastPunch_Box = new PunchInfoBox(opScrollBox, "FastPunch", new Vector2(0, 125), new Vector2(0.1f, 1.1f), new Vector2(0.2f, 1.2f), new IntVector2(1, 60), 0.1f, 0.3f, 2, "Symbol_Rock");
            PunchInfoBox BombPunch_Box = new PunchInfoBox(opScrollBox, "BombPunch", new Vector2(0, 250), new Vector2(1f, 10f), new Vector2(2f, 20f), new IntVector2(5, 100), 1.5f, 2.5f, 40, "Symbol_StunBomb");
            PunchInfoBox FriendlyPunch_Box = new PunchInfoBox(opScrollBox, "FriendlyPunch", new Vector2(0, 375), new Vector2(-1f, 1f), new Vector2(-1f, 1f), new IntVector2(5, 100), 0.5f, 0.1f, 30, "Symbol_Pearl", "BreakFrame", "Favor", "Heal",false);
            PunchInfoBox_IntRange BeePunch_Box = new PunchInfoBox_IntRange(opScrollBox, "BeePunch", new Vector2(0, 500), new IntVector2(1, 10), new IntVector2(1, 20), new IntVector2(5, 100), 4, 8, 30, "Symbol_SporePlant","BreakFrame","LowBee","HighBee");

            FastPunch_Box.MakeAndAddElement();
            NormalPunch_Box.MakeAndAddElement();
            BombPunch_Box.MakeAndAddElement();
            FriendlyPunch_Box.MakeAndAddElement();
            BeePunch_Box.MakeAndAddElement();

            punchInfoBoxes.Add(NormalPunch_Box);
            punchInfoBoxes.Add(FastPunch_Box);
            punchInfoBoxes.Add(BombPunch_Box);
            punchInfoBoxes.Add(FriendlyPunch_Box);
            punchInfoBoxes.Add(BeePunch_Box);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            foreach(var box in punchInfoBoxes)
            {
                box.Update(dt);
            }
        }

        public override void Signal(UItrigger trigger, string signal)
        {
            base.Signal(trigger, signal);

            foreach(var box in punchInfoBoxes)
            {
                box.Signal(signal);
            }
        }

        public override void ConfigOnChange()
        {
            Dictionary<string, Vector2> temp_DamageRange = PunchConfigInfo.punchDamageRanges;
            Dictionary<string, int> temp_BreakFrame = PunchConfigInfo.punchBreakFrames;
            Dictionary<string, string> temp_OtherSetting = PunchConfigInfo.punchOtherSettings;
            Dictionary<string, IntVector2> temp_IntRange = PunchConfigInfo.punchIntRanges;

            foreach (var pair in config)
            {
                string[] part = pair.Key.Split('_');
                if(part[0] == "CatPunchPunch")
                {
                    if (!PunchConfigInfo.punchDamageRanges.ContainsKey(part[1]))
                    {
                        temp_DamageRange.Add(part[1], Vector2.one);
                        temp_BreakFrame.Add(part[1], 5);
                        temp_OtherSetting.Add(part[1], "");
                        temp_IntRange.Add(part[1], new IntVector2(2, 3));
                    }

                    Debug.Log(part[1] + ":" + part[2] + ":" + pair.Value);

                    if(part[2] == "lowRange")
                    {
                        temp_DamageRange[part[1]] = new Vector2(float.Parse(pair.Value), temp_DamageRange[part[1]].y);
                    }
                    else if(part[2] == "highRange")
                    {
                        temp_DamageRange[part[1]] = new Vector2(temp_DamageRange[part[1]].x, float.Parse(pair.Value));
                    }
                    else if(part[2] == "lowRangeInt")
                    {
                        temp_IntRange[part[1]] = new IntVector2(int.Parse(pair.Value), temp_IntRange[part[1]].y);
                    }
                    else if (part[2] == "highRangeInt")
                    {
                        temp_IntRange[part[1]] = new IntVector2(temp_IntRange[part[1]].x, int.Parse(pair.Value));
                    }
                    else if(part[2] == "break")
                    {
                        temp_BreakFrame[part[1]] = int.Parse(pair.Value);
                    }
                    else
                    {
                        temp_OtherSetting[part[1]] = pair.Value;
                    }
                }

                PunchConfigInfo.punchDamageRanges = temp_DamageRange;
                PunchConfigInfo.punchBreakFrames = temp_BreakFrame;
                PunchConfigInfo.punchOtherSettings = temp_OtherSetting;
                PunchConfigInfo.punchIntRanges = temp_IntRange;
            }
        }

        public class PunchInfoBox : IPunchInfoBox
        {
            public PunchInfoBox(OpScrollBox owner, string PunchType,Vector2 pos,Vector2 lowRange,Vector2 highRange,IntVector2 breakRange,float defaultLow,float defaultHigh,int defaultBreak,string FElement = "None",string breakTitle = "BreakFrame",string lowRangeTitle = "LowDamage",string highRangeTitle = "HighDamage",bool limHigh = true)
            {
                this.punchType = PunchType;
                this.pos = pos;
                this.lowRange = lowRange;
                this.highRange = highRange;
                this.breakRange = breakRange;
                this.defaultBreak = defaultBreak;
                this.defaultLow = defaultLow;
                this.defaultHigh = defaultHigh;
                this.owner = owner;
                this.FElement = FElement;
                this.highRangeTitle = highRangeTitle;
                this.lowRangeTitle = lowRangeTitle;
                this.breakTitle = breakTitle;
                this.limHigh = limHigh;
            }

            public void MakeAndAddElement()
            {
                OpRect opRect = new OpRect(pos + Vector2.one * 10f, new Vector2(500f - 40f, 115));
                OpLabel title = new OpLabel(pos.x + 20f, pos.y + 60f + 35f, punchType,true) { color = Color.white};

                pos.x += 20f;

                OpLabel title_low = new OpLabel(pos.x + 30f, pos.y + 15f, lowRangeTitle);
                OpLabel title_hight = new OpLabel(pos.x + 30f, pos.y + 30f + 15f, highRangeTitle);
                OpLabel title_break = new OpLabel(pos.x + 30f, pos.y + 60f + 15f, breakTitle);

                opSlider_lowRange = new OpSliderFloat(pos + Vector2.one * 10 + Vector2.right * 110, "CatPunchPunch_" + punchType + "_lowRange", lowRange,200, false, defaultLow);
                OpLabel opLabel_lowRange = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f, defaultLow.ToString());
                labels.Add(opLabel_lowRange);

                opSlider_highRange = new OpSliderFloat(pos + Vector2.one * 10 + Vector2.right * 110 + Vector2.up * 30f, "CatPunchPunch_" + punchType + "_highRange", highRange, 200, false, defaultHigh);
                OpLabel opLabel_highRange = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f + 30f, defaultHigh.ToString());
                labels.Add(opLabel_highRange);

                opSlider_break = new OpSlider(pos + Vector2.one * 10 + Vector2.right * 110 + Vector2.up * 60f, "CatPunchPunch_" + punchType + "_break", breakRange,200, false, defaultBreak);
                OpLabel opLabel_break = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f + 60f, defaultBreak.ToString());
                sliders.Add(opSlider_break);
                labels.Add(opLabel_break);


                opButton_Reset = new OpSimpleButton(new Vector2(pos.x + 10 + 60 + 250 + 85f, pos.y + 20f), new Vector2(35f, 20f), "CatPunchPunch_" + punchType + "_reset", "reset");
                owner.AddItems(opRect,title, title_low, title_hight, title_break,opSlider_lowRange, opLabel_lowRange, opSlider_highRange, opLabel_highRange, opSlider_break, opLabel_break, opButton_Reset);

                if (FElement != "None")
                {
                    OpImage opImage = new OpImage(pos + Vector2.up * 20, FElement);
                    owner.AddItems(opImage);
                }
            }

            public virtual void Update(float dt)
            {
                if (firstUpdate)
                {
                    if (menuLabels.Count == 0)
                    {
                        foreach (var obj in opSlider_lowRange.subObjects)
                        {
                            if (obj is MenuLabel)
                            {
                                menuLabels.Add(obj as MenuLabel);
                            }
                        }
                        foreach (var obj in opSlider_highRange.subObjects)
                        {
                            if (obj is MenuLabel)
                            {
                                menuLabels.Add(obj as MenuLabel);
                            }
                        }
                        foreach (var obj in opSlider_break.subObjects)
                        {
                            if (obj is MenuLabel)
                            {
                                menuLabels.Add(obj as MenuLabel);
                            }
                        }
                    }
                    
                    firstUpdate = false;
                }

                foreach (var menuLabel in menuLabels)
                {
                    menuLabel.label.isVisible = false;
                }

                if (opSlider_lowRange.valueFloat > opSlider_highRange.valueFloat && limHigh)
                {
                    opSlider_lowRange.valueFloat = opSlider_highRange.valueFloat;
                }

                labels[0].text = String.Format("{0:F2}", opSlider_lowRange.valueFloat);
                labels[1].text = String.Format("{0:F2}", opSlider_highRange.valueFloat);
                labels[2].text = opSlider_break.valueInt.ToString();
            }

            public virtual void Signal(string signal)
            {
                string[] parts = signal.Split('_');

                if(parts[0] == "CatPunchPunch")
                {
                    if(parts[1] == punchType && parts[2] == "reset")
                    {
                        opSlider_lowRange.valueFloat = defaultLow;
                        opSlider_highRange.valueFloat = defaultHigh;
                        opSlider_break.valueInt = defaultBreak;
                    }
                }
            }

            OpScrollBox owner;

            List<MenuLabel> menuLabels = new List<MenuLabel>();
            List<OpSlider> sliders = new List<OpSlider>();
            List<OpLabel> labels = new List<OpLabel>();

            OpSimpleButton opButton_Reset;

            string punchType;
            Vector2 pos;
            Vector2 lowRange;
            Vector2 highRange;
            IntVector2 breakRange;
            float defaultLow;
            float defaultHigh;
            int defaultBreak;
            string FElement;

            string lowRangeTitle;
            string highRangeTitle;
            string breakTitle;

            OpSliderFloat opSlider_lowRange;
            OpSliderFloat opSlider_highRange;
            OpSlider opSlider_break;

            bool firstUpdate = true;
            bool limHigh;
        }

        public class PunchInfoBox_IntRange : IPunchInfoBox
        {
            public PunchInfoBox_IntRange(OpScrollBox owner, string PunchType, Vector2 pos, IntVector2 lowRange, IntVector2 highRange, IntVector2 breakRange, int defaultLow, int defaultHigh, int defaultBreak, string FElement = "None",string breakTitle = "BreakFrame", string lowRangeTitle = "LowDamage", string highRangeTitle = "HighDamage",bool limHigh = true)
            {
                this.punchType = PunchType;
                this.pos = pos;
                this.lowRange = lowRange;
                this.highRange = highRange;
                this.breakRange = breakRange;
                this.defaultBreak = defaultBreak;
                this.defaultLow = defaultLow;
                this.defaultHigh = defaultHigh;
                this.owner = owner;
                this.FElement = FElement;
                this.highRangeTitle = highRangeTitle;
                this.lowRangeTitle = lowRangeTitle;
                this.breakTitle = breakTitle;
                this.limHigh = limHigh;
            }

            public void MakeAndAddElement()
            {
                OpRect opRect = new OpRect(pos + Vector2.one * 10f, new Vector2(500f - 40f, 115));
                OpLabel title = new OpLabel(pos.x + 20f, pos.y + 60f + 35f, punchType, true) { color = Color.white };

                pos.x += 20f;

                OpLabel title_low = new OpLabel(pos.x + 30f, pos.y + 15f, "LowDamage");
                OpLabel title_hight = new OpLabel(pos.x + 30f, pos.y + 30f + 15f, "HighDamage");
                OpLabel title_break = new OpLabel(pos.x + 30f, pos.y + 60f + 15f, "breakFrames");

                OpSlider opSlider_lowRange = new OpSlider(pos + Vector2.one * 10 + Vector2.right * 110, "CatPunchPunch_" + punchType + "_lowRangeInt", lowRange, 200f / (float)(lowRange.y - lowRange.x), false, defaultLow);
                OpLabel opLabel_lowRange = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f, defaultLow.ToString());
                sliders.Add(opSlider_lowRange);
                labels.Add(opLabel_lowRange);

                OpSlider opSlider_highRange = new OpSlider(pos + Vector2.one * 10 + Vector2.right * 110 + Vector2.up * 30f, "CatPunchPunch_" + punchType + "_highRangeInt", highRange, 200f / (float)(highRange.y - highRange.x), false, defaultHigh);
                OpLabel opLabel_highRange = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f + 30f, defaultHigh.ToString());
                sliders.Add(opSlider_highRange);
                labels.Add(opLabel_highRange);

                OpSlider opSlider_break = new OpSlider(pos + Vector2.one * 10 + Vector2.right * 110 + Vector2.up * 60f, "CatPunchPunch_" + punchType + "_break", breakRange, 200f / (float)(breakRange.y - breakRange.x), false, defaultBreak);
                OpLabel opLabel_break = new OpLabel(pos.x + 10 + 60 + 250 + 10f, pos.y + 15f + 60f, defaultBreak.ToString());
                sliders.Add(opSlider_break);
                labels.Add(opLabel_break);


                opButton_Reset = new OpSimpleButton(new Vector2(pos.x + 10 + 60 + 250 + 85f, pos.y + 20f), new Vector2(35f, 20f), "CatPunchPunch_" + punchType + "_reset", "reset");
                owner.AddItems(opRect, title, title_low, title_hight, title_break, opSlider_lowRange, opLabel_lowRange, opSlider_highRange, opLabel_highRange, opSlider_break, opLabel_break, opButton_Reset);

                if (FElement != "None")
                {
                    OpImage opImage = new OpImage(pos + Vector2.up * 20, FElement);
                    owner.AddItems(opImage);
                }
            }

            public void Signal(string signal)
            {
                string[] parts = signal.Split('_');

                if (parts[0] == "CatPunchPunch")
                {
                    if (parts[1] == punchType && parts[2] == "reset")
                    {
                        sliders[0].valueInt = defaultLow;
                        sliders[1].valueInt = defaultHigh;
                        sliders[2].valueInt = defaultBreak;
                    }
                }
            }

            public void Update(float dt)
            {
                if (firstUpdate)
                {
                    if (menuLabels.Count == 0)
                    {
                        foreach (var slider in sliders)
                        {
                            foreach (var obj in slider.subObjects)
                            {
                                if (obj is MenuLabel)
                                {
                                    menuLabels.Add(obj as MenuLabel);
                                }
                            }
                        }
                    }
                    firstUpdate = false;
                }

                foreach (var menuLabel in menuLabels)
                {
                    menuLabel.label.isVisible = false;
                }

                if (sliders[0].valueInt > sliders[1].valueInt && limHigh)
                {
                    sliders[0].valueInt = sliders[1].valueInt;
                }

                labels[0].text = sliders[0].valueInt.ToString();
                labels[1].text = sliders[1].valueInt.ToString();
                labels[2].text = sliders[2].valueInt.ToString();
            }

            OpScrollBox owner;

            List<MenuLabel> menuLabels = new List<MenuLabel>();
            List<OpSlider> sliders = new List<OpSlider>();
            List<OpLabel> labels = new List<OpLabel>();

            OpSimpleButton opButton_Reset;

            string punchType;
            Vector2 pos;
            IntVector2 lowRange;
            IntVector2 highRange;
            IntVector2 breakRange;
            int defaultLow;
            int defaultHigh;
            int defaultBreak;
            string FElement;

            string lowRangeTitle;
            string highRangeTitle;
            string breakTitle;

            bool firstUpdate = true;
            bool limHigh;
        }

        public interface IPunchInfoBox
        {
            void MakeAndAddElement();
            void Update(float dt);
            void Signal(string signal);
        }
    }
}
