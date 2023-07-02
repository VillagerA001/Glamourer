﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Glamourer.Services;
using Glamourer.Structs;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Table;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlockTable : Table<EquipItem>
{
    public UnlockTable(ItemManager items, CustomizationService customizations, ItemUnlockManager itemUnlocks,
        PenumbraChangedItemTooltip tooltip)
        : base("ItemUnlockTable", new ItemList(items),
            new NameColumn(customizations, tooltip) { Label = "Item Name..." },
            new SlotColumn() { Label                        = "Equip Slot" },
            new TypeColumn() { Label                        = "Item Type..." },
            new UnlockDateColumn(itemUnlocks) { Label       = "Unlocked" },
            new ItemIdColumn() { Label                      = "Item Id..." },
            new ModelDataColumn(items) { Label              = "Model Data..." })
    {
        Sortable =  true;
        Flags    |= ImGuiTableFlags.Hideable;
    }

    private sealed class NameColumn : ColumnString<EquipItem>
    {
        private readonly CustomizationService       _customizations;
        private readonly PenumbraChangedItemTooltip _tooltip;

        public override float Width
            => 400 * ImGuiHelpers.GlobalScale;

        public NameColumn(CustomizationService customizations, PenumbraChangedItemTooltip tooltip)
        {
            _customizations =  customizations;
            _tooltip        =  tooltip;
            Flags           |= ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder;
        }

        public override string ToName(EquipItem item)
            => item.Name;

        public override void DrawColumn(EquipItem item, int _)
        {
            var icon = _customizations.AwaitedService.GetIcon(item.IconId);
            ImGui.Image(icon.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            if (ImGui.Selectable(item.Name))
            {
                // TODO link
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _tooltip.Player(out var state))
                _tooltip.ApplyItem(state, item);

            if (ImGui.IsItemHovered() && _tooltip.Player())
                _tooltip.CreateTooltip(item, string.Empty, true);
        }
    }

    private sealed class TypeColumn : ColumnString<EquipItem>
    {
        public override float Width
            => ImGui.CalcTextSize(FullEquipType.CrossPeinHammer.ToName()).X;

        public override string ToName(EquipItem item)
            => item.Type.ToName();

        public override void DrawColumn(EquipItem item, int _)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(item.Type.ToName());
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Type.CompareTo(rhs.Type);
    }

    private sealed class SlotColumn : ColumnFlags<EquipFlag, EquipItem>
    {
        public override float Width
            => ImGui.CalcTextSize("Equip Slotmm").X;

        private EquipFlag _filterValue;

        public SlotColumn()
        {
            AllFlags     = Values.Aggregate((a, b) => a | b);
            _filterValue = AllFlags;
        }

        public override void DrawColumn(EquipItem item, int idx)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ToString(item.Type.ToSlot()));
        }

        public override EquipFlag FilterValue
            => _filterValue;

        protected override IReadOnlyList<EquipFlag> Values
            => new[]
            {
                EquipFlag.Mainhand,
                EquipFlag.Offhand,
                EquipFlag.Head,
                EquipFlag.Body,
                EquipFlag.Hands,
                EquipFlag.Legs,
                EquipFlag.Feet,
                EquipFlag.Ears,
                EquipFlag.Neck,
                EquipFlag.Wrist,
                EquipFlag.RFinger,
            };

        protected override string[] Names
            => new[]
            {
                ToString(EquipSlot.MainHand),
                ToString(EquipSlot.OffHand),
                ToString(EquipSlot.Head),
                ToString(EquipSlot.Body),
                ToString(EquipSlot.Hands),
                ToString(EquipSlot.Legs),
                ToString(EquipSlot.Feet),
                ToString(EquipSlot.Ears),
                ToString(EquipSlot.Neck),
                ToString(EquipSlot.Wrists),
                ToString(EquipSlot.RFinger),
            };

        protected override void SetValue(EquipFlag value, bool enable)
            => _filterValue = enable ? _filterValue | value : _filterValue & ~value;

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Type.ToSlot().CompareTo(rhs.Type.ToSlot());

        public override bool FilterFunc(EquipItem item)
            => _filterValue.HasFlag(item.Type.ToSlot().ToFlag());

        private static string ToString(EquipSlot slot)
            => slot switch
            {
                EquipSlot.MainHand => "Mainhand",
                EquipSlot.OffHand  => "Offhand",
                EquipSlot.Head     => "Head",
                EquipSlot.Body     => "Body",
                EquipSlot.Hands    => "Hands",
                EquipSlot.Legs     => "Legs",
                EquipSlot.Feet     => "Feet",
                EquipSlot.Ears     => "Ears",
                EquipSlot.Neck     => "Neck",
                EquipSlot.Wrists   => "Wrists",
                EquipSlot.RFinger  => "Finger",
                _                  => string.Empty,
            };
    }

    private sealed class UnlockDateColumn : Column<EquipItem>
    {
        private readonly ItemUnlockManager _unlocks;

        public override float Width
            => 110 * ImGuiHelpers.GlobalScale;

        public UnlockDateColumn(ItemUnlockManager unlocks)
            => _unlocks = unlocks;

        public override void DrawColumn(EquipItem item, int idx)
        {
            if (!_unlocks.IsUnlocked(item.Id, out var time))
                return;

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(time == DateTimeOffset.MinValue ? "Always" : time.ToString("g"));
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
        {
            var unlockedLhs = _unlocks.IsUnlocked(lhs.Id, out var timeLhs);
            var unlockedRhs = _unlocks.IsUnlocked(lhs.Id, out var timeRhs);
            var c1          = unlockedLhs.CompareTo(unlockedRhs);
            return c1 != 0 ? c1 : timeLhs.CompareTo(timeRhs);
        }
    }

    private sealed class ItemIdColumn : ColumnString<EquipItem>
    {
        public override float Width
            => 70 * ImGuiHelpers.GlobalScale;

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Id.CompareTo(rhs.Id);

        public override string ToName(EquipItem item)
            => item.Id.ToString();

        public override void DrawColumn(EquipItem item, int _)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtil.RightAlign(item.Id.ToString());
        }
    }

    private sealed class ModelDataColumn : ColumnString<EquipItem>
    {
        private readonly ItemManager _items;

        public override float Width
            => 100 * ImGuiHelpers.GlobalScale;

        public ModelDataColumn(ItemManager items)
            => _items = items;

        public override void DrawColumn(EquipItem item, int _)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtil.RightAlign(item.ModelString);
            if (ImGui.IsItemHovered()
             && item.Type.Offhand().IsOffhandType()
             && _items.ItemService.AwaitedService.TryGetValue(item.Id, false, out var offhand))
            {
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted("Offhand: " + offhand.ModelString);
            }
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Weapon().Value.CompareTo(rhs.Weapon().Value);

        public override bool FilterFunc(EquipItem item)
        {
            if (FilterValue.Length == 0)
                return true;

            if (FilterRegex?.IsMatch(item.ModelString) ?? item.ModelString.Contains(FilterValue, StringComparison.OrdinalIgnoreCase))
                return true;

            if (item.Type.Offhand().IsOffhandType() && _items.ItemService.AwaitedService.TryGetValue(item.Id, false, out var offhand))
                return FilterRegex?.IsMatch(offhand.ModelString)
                 ?? offhand.ModelString.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    private sealed class ItemList : IReadOnlyCollection<EquipItem>
    {
        private readonly ItemManager _items;

        public ItemList(ItemManager items)
            => _items = items;

        public IEnumerator<EquipItem> GetEnumerator()
            => _items.ItemService.AwaitedService.AllItems(true).Select(i => i.Item2).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => _items.ItemService.AwaitedService.TotalItemCount(true);
    }
}