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
        private Item[] diamondTrigger;
        private GaugeVisual otherVisual;
        private Item[] otherTriggers;
        private float otherCD;

        public GaugeCombined(string name, float cd) : base(name) {
            CD = cd;
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
                    combo.SetMaxValue(1);
                    combo.SetDiamondValue(1);
                    combo.SetText("0");
                    combo.SetPercent(0);
                }
            }
        }

        unsafe float CheckCD(uint id)
        {
            var adjustedActionId = JobBars.Client.ActionManager.GetAdjustedActionId(id);
            var recastGroup = (int)JobBars.Client.ActionManager.GetRecastGroup(0x01, adjustedActionId) + 1;
            if (recastGroup == 0 || recastGroup == 58) return 0x9999;
            var recastTimer = JobBars.Client.ActionManager.GetGroupRecastTime(recastGroup);
            if (recastTimer->IsActive == 1)
            {
                var currentTime = recastTimer->Elapsed % recastTimer->Total;
                var timeLeft = recastTimer->Total - currentTime;
                return timeLeft;
            }
            return 0;
        }

        public override void Tick(DateTime time, Dictionary<Item, float> buffDict) {
            if (UI is UIGaugeDiamondCombo combo)
            {
                
                foreach (var trigger in diamondTrigger)
                {

                    if (trigger.Type == ItemType.Action)
                    {
                        var timeleft = CheckCD(trigger.Id);
                        combo.SetDiamondValue(timeleft == 0 ? 1 : 0);
                    }
                    else
                    {
                        buffDict.TryGetValue(trigger, out var timeleft);
                        combo.SetDiamondValue(timeleft == 0 ? 0 : 1);
                    }

                }

                var percent = 0f;
                var timeLeft = 0f;
                foreach (var trigger in Triggers)
                {
                    var left = 0f;
                    if (trigger.Type == ItemType.Action)
                    {
                        left = CheckCD(trigger.Id);
                    }
                    else
                    {
                        buffDict.TryGetValue(trigger, out left);
                        if (trigger.Id == 1224 && left > 0) left += 10f;
                    }

                    if (left > 0)
                    {
                        timeLeft = left;
                        percent = timeLeft / CD;
                    }
                }

                foreach (var trigger in otherTriggers)
                {

                    var left = 0f;
                    if (trigger.Type == ItemType.Action)
                    {
                        left = CheckCD(trigger.Id);
                    }
                    else
                    {
                        buffDict.TryGetValue(trigger, out left);
                    }

                    if (left < timeLeft && left != 0 || timeLeft == 0)
                    {
                        timeLeft = left;
                        percent = left / otherCD;
                    }

                }
                combo.SetText(timeLeft.ToString("0.0"));
                combo.SetPercent(percent);

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

        public GaugeCombined OtherTriggers(Item[] triggers,float cd) {
            otherTriggers = triggers;
            otherCD = cd;
            return this;
        }

        public GaugeCombined WithVisual(GaugeVisual visual)
        {
            DefaultVisual = Visual = visual;
            GetVisualConfig();
            return this;
        }

        public GaugeCombined DiamondTrigger(Item[] triggers)
        {
            diamondTrigger = triggers;
            return this;
        }
    }
}
