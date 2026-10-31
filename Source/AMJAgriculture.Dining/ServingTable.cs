using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AMJAgriculture.Dining
{
    // Vanilla storage handles hauling. Only fractional leftovers are held internally.
    public class ServingTable : Building_Storage, IThingHolder
    {
        public ThingDef selectedFood;
        private ThingOwner<Thing> portions;
        public ServingTable() { portions = new ThingOwner<Thing>(this, false, LookMode.Deep); }
        public ThingOwner GetDirectlyHeldThings() { return portions; }
        public void GetChildHolders(List<IThingHolder> output) { ThingOwnerUtility.AppendThingHoldersFromThings(output, portions); }
        public IEnumerable<Thing> Stock
        {
            get
            {
                foreach (Thing thing in portions) yield return thing;
                if (!Spawned) yield break;
                foreach (IntVec3 cell in this.AllSlotCells())
                    foreach (Thing thing in Map.thingGrid.ThingsListAt(cell))
                        if (thing.def.category == ThingCategory.Item) yield return thing;
            }
        }
        public void SelectFood(ThingDef food)
        {
            selectedFood = food;
            GetStoreSettings().filter.SetDisallowAll();
            if (food != null) GetStoreSettings().filter.SetAllow(food, true);
            // A previous choice's fractions remain recoverable when changing the menu.
            if (Spawned)
                foreach (Thing old in portions.Where(t => t.def != food).ToList())
                    portions.TryDrop(old, Position, Map, ThingPlaceMode.Near, out Thing dropped);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            SelectFood(selectedFood);
            // Old buffet stock becomes ordinary accessible storage, including doctor/warden food searches.
            portions.TryDropAll(Position, Map, ThingPlaceMode.Near);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            // These names also read retained stock in the old AMJ-used buffet.
            Scribe_Defs.Look(ref selectedFood, "buffetFood");
            Scribe_Deep.Look(ref portions, "buffetContents", this);
            if (portions == null) portions = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }
        protected override void Tick()
        {
            base.Tick(); portions.DoTick();
        }
        public Thing Sample(Pawn eater, Pawn worker)
        {
            if (selectedFood == null || Meals.Role(selectedFood) == MealRole.None) return null;
            return Stock.OrderBy(t => FoodPortions.Raw(t)).FirstOrDefault(t => t.def == selectedFood && t.TryGetComp<Portion>() != null &&
                (t.TryGetComp<CompFoodPoisonable>() == null || t.TryGetComp<CompFoodPoisonable>().PoisonPercent <= 0) &&
                FoodPortions.Allowed(eater, t, worker) && !t.IsForbidden(worker) &&
                (!t.Spawned || worker.CanReserve(t)));
        }
        public Thing Dispense(Pawn eater, Pawn worker, float wanted)
        {
            Thing source = Sample(eater, worker);
            if (source == null || wanted <= 0) return null;
            float available = FoodUtility.NutritionForEater(eater, source);
            float raw = FoodPortions.Raw(source);
            if (available <= 0 || raw <= 0) return null;
            float taken = Math.Min(wanted, available);
            Thing unit = source.SplitOff(1);
            unit.holdingOwner?.Remove(unit);
            if (taken < available - 0.0001f)
            {
                Thing remainder = FoodPortions.Copy(unit);
                remainder.TryGetComp<Portion>().amount = raw * (1f - taken / available);
                remainder.TryGetComp<Portion>().session = null;
                remainder.TryGetComp<Portion>().goal = 0;
                // Keep leftovers visible to vanilla food search and hauling.
                if (!GenPlace.TryPlaceThing(remainder, Position, Map, ThingPlaceMode.Near)) portions.TryAdd(remainder, false);
            }
            unit.TryGetComp<Portion>().amount = raw * taken / available;
            return unit;
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            yield return new Command_Action {
                defaultLabel = selectedFood == null ? "AMJD_SelectFood".Translate() : selectedFood.LabelCap,
                defaultDesc = "AMJD_SelectFoodDesc".Translate(),
                action = () => {
                    var options = new List<FloatMenuOption> { new FloatMenuOption("AMJD_ClearFood".Translate(), () => SelectFood(null)) };
                    foreach (ThingDef food in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => Meals.Role(d) != MealRole.None).OrderBy(d => d.label))
                    { ThingDef choice = food; options.Add(new FloatMenuOption(choice.LabelCap, () => SelectFood(choice))); }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }
        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "AMJD_SelectedFood".Translate(selectedFood?.LabelCap.ToString() ?? "-") +
                "\n" + "AMJD_StoredNutrition".Translate(Stock.Sum(t => FoodPortions.Raw(t) * t.stackCount).ToString("0.00"));
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned) portions.TryDropAll(Position, Map, ThingPlaceMode.Near);
            base.Destroy(mode);
        }
    }
}
// Save-name adapter only. This is newly written AMJ code, not a vendor implementation.
namespace DailyMenu
{
    public sealed class Buffet : AMJAgriculture.Dining.ServingTable { }
}
