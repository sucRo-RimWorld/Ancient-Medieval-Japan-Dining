using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AMJAgriculture.Dining
{
    public sealed class PortionProperties : CompProperties
    { public PortionProperties() { compClass = typeof(Portion); } }

    // Absolute raw nutrition per unit, not a second edible item or ingredient.
    public sealed class Portion : ThingComp
    {
        public float amount = -1f;
        public string session;
        public float goal;
        public override void PostExposeData()
        {
            base.PostExposeData();
            // Retain old portion field names to read the food inside existing trays.
            Scribe_Values.Look(ref amount, "buffetNutrition", -1f);
            Scribe_Values.Look(ref session, "diningSession");
            Scribe_Values.Look(ref goal, "diningGoal");
        }
        public override void PostSplitOff(Thing piece)
        {
            Portion other = piece.TryGetComp<Portion>();
            if (other == null) return;
            other.amount = amount; other.session = session; other.goal = goal;
        }
        public override bool AllowStackWith(Thing other)
        {
            Portion p = other.TryGetComp<Portion>();
            return p != null && amount < 0 && p.amount < 0 && session == null && p.session == null;
        }
    }

    [StaticConstructorOnStartup]
    internal static class RegisterPortions
    {
        static RegisterPortions()
        {
            foreach (ThingDef food in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (food.ingestible == null || food.IsDrug || food.defName == "AMJA_MealPlate" ||
                    (food.ingestible.foodType & FoodTypeFlags.Meal) == 0) continue;
                if (food.comps == null) food.comps = new List<CompProperties>();
                if (!food.comps.Any(c => c.compClass == typeof(Portion))) food.comps.Add(new PortionProperties());
            }
        }
    }

    public static class FoodPortions
    {
        public static bool Allowed(Pawn eater, Thing food, Pawn worker = null)
        {
            if (eater?.needs?.food == null || food == null || !food.IngestibleNow) return false;
            var policy = eater.foodRestriction?.GetCurrentRespectedRestriction(worker ?? eater);
            return (policy == null || policy.Allows(food)) && FoodUtility.WillEat(eater, food, worker);
        }
        public static float Raw(Thing food)
        {
            Portion part = food.TryGetComp<Portion>();
            return part != null && part.amount >= 0 ? part.amount : food.GetStatValue(StatDefOf.Nutrition);
        }
        public static Thing Copy(Thing original)
        {
            Thing copy = ThingMaker.MakeThing(original.def, original.Stuff);
            foreach (ThingComp comp in ((ThingWithComps)original).AllComps) comp.PostSplitOff(copy);
            var ingredients = original.TryGetComp<CompIngredients>();
            if (ingredients != null)
                foreach (ThingDef ingredient in ingredients.ingredients) copy.TryGetComp<CompIngredients>()?.RegisterIngredient(ingredient);
            var rot = original.TryGetComp<CompRottable>();
            if (rot != null && copy.TryGetComp<CompRottable>() != null) copy.TryGetComp<CompRottable>().RotProgress = rot.RotProgress;
            var quality = original.TryGetComp<CompQuality>();
            if (quality != null) copy.TryGetComp<CompQuality>()?.SetQuality(quality.Quality, ArtGenerationContext.Colony);
            copy.HitPoints = original.HitPoints;
            return copy;
        }
    }

    [HarmonyPatch(typeof(FoodUtility), "NutritionForEater")]
    internal static class PortionNutrition
    {
        private static void Postfix(Thing food, ref float __result)
        {
            Portion part = food?.TryGetComp<Portion>();
            if (part == null || part.amount < 0) return;
            float full = food.GetStatValue(StatDefOf.Nutrition);
            if (full > 0) __result = __result * part.amount / full;
        }
    }

    public sealed class MealRecord : IExposable
    {
        public string pawn, session, food;
        public int tick;
        public float nutrition, goal;
        public void ExposeData()
        {
            Scribe_Values.Look(ref pawn, "pawn"); Scribe_Values.Look(ref session, "session");
            Scribe_Values.Look(ref food, "food"); Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref nutrition, "nutrition"); Scribe_Values.Look(ref goal, "goal");
        }
    }

    public sealed class MealLedger : MapComponent
    {
        public List<MealRecord> records = new List<MealRecord>();
        public MealLedger(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref records, "amjdMeals", LookMode.Deep);
            if (records == null) records = new List<MealRecord>();
        }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 6000 == 0) Prune();
        }
        private void Prune() { records.RemoveAll(r => Find.TickManager.TicksGame - r.tick > 6000); }
        public void Record(Pawn eater, Thing food, float actual)
        {
            Portion portion = food.TryGetComp<Portion>();
            if (portion == null || string.IsNullOrEmpty(portion.session) || actual <= 0) return;
            Prune();
            records.Add(new MealRecord { pawn = eater.GetUniqueLoadID(), session = portion.session,
                food = food.def.defName, nutrition = actual, goal = portion.goal, tick = Find.TickManager.TicksGame });
            // Keep the previous provisional variety threshold/reward; role-based balance is separate work.
            int count = records.Where(r => r.pawn == eater.GetUniqueLoadID() && r.session == portion.session)
                .GroupBy(r => r.food).Count(g => g.Sum(r => r.nutrition) >= Math.Max(0.02f, portion.goal * 0.15f));
            if (count < 2 || eater.needs.mood == null) return;
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("AMJD_VariedMeal");
            eater.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thought);
            eater.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thought, count >= 3 ? 1 : 0));
        }
    }

    [HarmonyPatch(typeof(Thing), "Ingested")]
    internal static class RecordMeal
    {
        private static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            // A tray has no Portion, so only its actual food contributes.
            if (ingester?.MapHeld != null && __instance.TryGetComp<Portion>() != null)
                ingester.MapHeld.GetComponent<MealLedger>().Record(ingester, __instance, __result);
        }
    }
}
