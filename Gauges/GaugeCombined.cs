using JobBars.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using JobBars.Data;

namespace JobBars.Gauges {
    public class GaugeCombined : Gauge {
        private float CD;
        private int MaxCharges;
        private Item[] triggerItem;

        public GaugeCombined(string name, float cd, int maxCharges) : base(name) {
            CD = cd;
            MaxCharges = maxCharges;
            DefaultVisual = Visual = new GaugeVisual
            {
                Type = GaugeVisualType.BarDiamondCombo,
                Color = UIColor.LightBlue
            };
        }

        public override void SetupVisual(bool resetValue = true) {
            UI?.SetColor(Visual.Color);
            if (resetValue) {
                if (UI is UIGaugeDiamondCombo combo) {
                    combo.SetMaxValue(MaxCharges);
                    combo.SetDiamondValue(MaxCharges);
                    combo.SetText("0");
                    combo.SetPercent(0);
                }
            }
        }

        public override unsafe void Tick(DateTime time, Dictionary<Item, float> buffDict) {
            foreach(var trigger in Triggers) {
                if (trigger.Type != ItemType.Buff)
                {
                    var adjustedActionId = JobBars.Client.ActionManager.GetAdjustedActionId(trigger.Id);
                    var recastGroup = (int)JobBars.Client.ActionManager.GetRecastGroup(0x01, adjustedActionId) + 1;
                    if (recastGroup == 0 || recastGroup == 58) continue;
                    var recastTimer = JobBars.Client.ActionManager.GetGroupRecastTime(recastGroup);

                    if(recastTimer->IsActive == 1) {
                        //var currentCharges = (int)Math.Floor(recastTimer->Elapsed / CD);
                        var currentTime = recastTimer->Elapsed % CD;
                        var timeLeft = CD - currentTime;

                        if (UI is UIGaugeDiamondCombo combo) {
                            //combo.SetDiamondValue(currentCharges);
                            combo.SetText(((int)timeLeft).ToString());
                            combo.SetPercent((float)currentTime / CD);
                            
                        }
                        //return;
                    }
                }
                else
                {
                    if (buffDict.ContainsKey(trigger))
                    {
                        buffDict.TryGetValue(trigger,out var timeLeft);
                        if (trigger.Id == 0x9999) timeLeft += 10;
                        
                        if (UI is UIGaugeDiamondCombo combo) {
                            //PluginLog.Information("Tick"+timeLeft);
                            //combo.SetDiamondValue(1);
                            combo.SetText(((int)timeLeft).ToString());
                            combo.SetPercent((float)timeLeft / CD);
                        }
                    }
                    else if (UI is UIGaugeDiamondCombo combo)
                    {
                        combo.SetDiamondValue(0);
                    }
                }

                
            }

            foreach (var trigger in triggerItem)
            {
                
                if (trigger.Type != ItemType.Buff) continue;
                if (buffDict.ContainsKey(trigger))
                {
                    buffDict.TryGetValue(trigger,out var timeLeft);
                    if (trigger.Id == 0x9999) timeLeft += 10;
                
                    if (UI is UIGaugeDiamondCombo combo) {
                        combo.SetDiamondValue(1);
                        //combo.SetText(((int)timeLeft).ToString());
                        //combo.SetPercent((float)timeLeft / CD);
                    }
                }
                else if (UI is UIGaugeDiamondCombo combo)
                {
                    combo.SetDiamondValue(0);
                }

                return;
            }

            if (UI is UIGaugeDiamondCombo comboInactive) {
                comboInactive.SetDiamondValue(0);
                comboInactive.SetText("0");
                comboInactive.SetPercent(0);
            }
        }

        public override void ProcessAction(Item action) { }

        public override int GetHeight() {
            return UI == null ? 0 : UI.GetHeight(0);
        }

        public override int GetWidth() {
            return UI == null ? 0 : UI.GetWidth(0);
        }

        // ===== BUILDER FUNCS =====
        public GaugeCombined WithTriggers(Item[] triggers) {
            Triggers = triggers;
            return this;
        }

        public GaugeCombined WithVisual(GaugeVisual visual)
        {
            DefaultVisual = Visual = visual;
            GetVisualConfig();
            return this;
        }

        public GaugeCombined TriggerDiamond(Item[] triggers)
        {
            triggerItem = triggers;
            return this;
        }
    }
}
