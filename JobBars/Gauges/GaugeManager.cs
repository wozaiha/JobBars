﻿using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using JobBars.Data;
using JobBars.PartyList;
using JobBars.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace JobBars.Gauges {
    public unsafe partial class GaugeManager {
        public static GaugeManager Manager { get; private set; }
        public DalamudPluginInterface PluginInterface;

        public Dictionary<JobIds, Gauge[]> JobToGauges;
        public JobIds CurrentJob = JobIds.OTHER;
        public Gauge[] CurrentGauges => JobToGauges.TryGetValue(CurrentJob, out var gauges) ? gauges : JobToGauges[JobIds.OTHER];

        public IntPtr TargetAddress;

        public GaugeManager(DalamudPluginInterface pi) {
            Manager = this;
            PluginInterface = pi;
            TargetAddress = PluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? FF 50 ?? 48 85 DB", 3);

            if (!Configuration.Config.GaugesEnabled) UIBuilder.Builder.HideGauges();
            Init();
        }

        public void SetJob(JobIds job) {

            //===== CLEANUP OLD =======
            foreach (var gauge in CurrentGauges) {
                gauge.UI?.Cleanup();
                gauge.UI = null;
            }
            UIBuilder.Builder.HideAllGauges();
            UIIconManager.Manager.Reset();

            //====== SET UP NEW =======
            CurrentJob = job;
            int idx = 0;
            foreach (var gauge in CurrentGauges) {
                gauge.UI = GetUI(idx, gauge.GetVisualType());
                gauge.SetupUI();
                idx++;
            }
            SetPositionScale();
        }

        public void SetPositionScale() {
            UIBuilder.Builder.SetGaugePosition(Configuration.Config.GaugePosition);
            UIBuilder.Builder.SetGaugeScale(Configuration.Config.GaugeScale);

            int totalPosition = 0;
            foreach (var gauge in CurrentGauges.OrderBy(g => g.Order).Where(g => g.Enabled)) {
                if (Configuration.Config.GaugeSplit) { // SPLIT
                    gauge.UI.SetSplitPosition(Configuration.Config.GetGaugeSplitPosition(gauge.Name));
                }
                else {
                    if (Configuration.Config.GaugeHorizontal) { // HORIZONTAL
                        gauge.UI.SetPosition(new Vector2(totalPosition, gauge.UI.GetHorizontalYOffset()));
                        totalPosition += gauge.Width;
                    }
                    else { // VERTICAL
                        int xPosition = Configuration.Config.GaugeAlignRight ? 160 - gauge.Width : 0;
                        gauge.UI.SetPosition(new Vector2(xPosition, totalPosition));
                        totalPosition += gauge.Height;
                    }
                }
            }
        }

        public void Reset() {
            SetJob(CurrentJob);
        }

        public void ResetJob(JobIds job) {
            if (job == CurrentJob) SetJob(job);
        }

        public void PerformAction(Item action) {
            if (!Configuration.Config.GaugesEnabled) return;
            foreach (var gauge in CurrentGauges.Where(x => x.DoProcessInput())) {
                gauge.ProcessAction(action);
            }
        }

        public void Tick(PList party, bool inCombat) {
            if (!Configuration.Config.GaugesEnabled) return;

            if (Configuration.Config.GaugesHideOutOfCombat) {
                if (inCombat) UIBuilder.Builder.ShowGauges();
                else UIBuilder.Builder.HideGauges();
            }

            Dictionary<Item, BuffElem> BuffDict = new();
            var currentTime = DateTime.Now;

            int ownerId = (int)PluginInterface.ClientState.LocalPlayer.ActorId;

            AddBuffs(PluginInterface.ClientState.LocalPlayer, ownerId, BuffDict);

            var prevEnemy = GetPreviousEnemyTarget();
            if (prevEnemy != null) {
                AddBuffs(prevEnemy, ownerId, BuffDict);
            }

            if (CurrentJob == JobIds.SCH && inCombat) { // only need this to catch excog for now
                foreach (var pMember in party) {
                    if (pMember == null) continue;

                    foreach (var actor in PluginInterface.ClientState.Actors) {
                        if (actor == null) continue;
                        if (actor.ActorId == pMember.ActorId) {
                            AddBuffs(actor, ownerId, BuffDict);
                        }
                    }
                }
            }

            foreach (var gauge in CurrentGauges) {
                if (!gauge.DoProcessInput()) { continue; }
                gauge.Tick(currentTime, BuffDict);
            }
            UIIconManager.Manager.Tick();
        }

        private static UIElement GetUI(int idx, GaugeVisualType type) {
            return type switch {
                GaugeVisualType.Arrow => UIBuilder.Builder.Arrows[idx],
                GaugeVisualType.Bar => UIBuilder.Builder.Gauges[idx],
                GaugeVisualType.Diamond => UIBuilder.Builder.Diamonds[idx],
                GaugeVisualType.BarDiamondCombo => new UIGaugeDiamondCombo(
                    UIBuilder.Builder.Gauges[idx],
                    UIBuilder.Builder.Diamonds[idx]
                ), // kind of scuffed, but oh well
                _ => null,
            };
        }

        private static void AddBuffs(Actor actor, int ownerId, Dictionary<Item, BuffElem> buffDict) {
            if (actor == null) return;
            if (actor is Chara charaActor) {
                foreach (var status in charaActor.StatusEffects) {
                    if (status.OwnerId == ownerId) {
                        buffDict[new Item {
                            Id = (uint)status.EffectId,
                            Type = ItemType.Buff
                        }] = new BuffElem {
                            Duration = status.Duration > 0 ? status.Duration : status.Duration * -1,
                            StackCount = status.StackCount
                        };
                    }
                }
            }
        }

        private Actor GetPreviousEnemyTarget() {
            var actorAddress = Marshal.ReadIntPtr(TargetAddress + 0xF0);
            if (actorAddress == IntPtr.Zero) return null;

            return PluginInterface.ClientState.Actors.CreateActorReference(actorAddress);
        }
    }

    public struct BuffElem {
        public float Duration;
        public byte StackCount;
    }
}
