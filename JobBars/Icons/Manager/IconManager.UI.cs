﻿using ImGuiNET;
using JobBars.Data;

namespace JobBars.Icons.Manager {
    public partial class IconManager {
        private readonly InfoBox<IconManager> LargeIconInfoBox = new() {
            Label = "Large Text",
            ContentsAction = (IconManager manager) => {
                if (ImGui.Checkbox("Buff icons" + manager.Id, ref JobBars.Config.IconBuffLarge)) {
                    JobBars.IconBuilder.RefreshVisuals();
                    JobBars.Config.Save();
                }

                if (ImGui.Checkbox("Timer icons" + manager.Id, ref JobBars.Config.IconTimerLarge)) {
                    JobBars.IconBuilder.RefreshVisuals();
                    JobBars.Config.Save();
                }
            }
        };

        protected override void DrawHeader() {
            if (ImGui.Checkbox("Icon replacement enabled", ref JobBars.Config.IconsEnabled)) {
                JobBars.Config.Save();
                Reset();
            }
        }

        protected override void DrawSettings() {
            LargeIconInfoBox.Draw(this);
        }

        protected override void DrawItem(IconReplacer[] item, JobIds _) {
            foreach (var icon in item) {
                icon.Draw(Id, SelectedJob);
            }
        }
    }
}