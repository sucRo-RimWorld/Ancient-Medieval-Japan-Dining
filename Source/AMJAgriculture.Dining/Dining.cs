using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DailyMenu;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AMJAgriculture.Dining
{
    public enum MealRole { None, Staple, Soup, Side }
    public sealed class MealRoleExtension : DefModExtension { public MealRole role; }

    public sealed class DiningMod : Mod
    {
        internal static Harmony Patches;
        public DiningMod(ModContentPack content) : base(content)
        {
            Patches = new Harmony("sucro.agriculture.dining");
            Patches.PatchAll(typeof(DiningMod).Assembly);
        }
    }

    // The supplied 0.3.7 DLL calls a helper "Prepare", a reserved Harmony
    // callback. Bypass ONLY its startup class, installing its actual patches
    // individually without ever treating that helper as a prepare callback.
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), "CallAll")]
    internal static class SupplyCompatibility
    {
        private static bool installed;
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.Method(typeof(RuntimeHelpers), "RunClassConstructor", new[] { typeof(RuntimeTypeHandle) });
            MethodInfo replacement = AccessTools.Method(typeof(SupplyCompatibility), "RunConstructor");
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original)) { instruction.opcode = OpCodes.Call; instruction.operand = replacement; }
                yield return instruction;
            }
        }
        private static void RunConstructor(RuntimeTypeHandle handle)
        {
            Type type = Type.GetTypeFromHandle(handle);
            if (type.FullName != "DailyMenuSupply.Startup") { RuntimeHelpers.RunClassConstructor(handle); return; }
            Type bad = type.Assembly.GetType("DailyMenuSupply.ProcessingSupply");
            MethodInfo helper = bad == null ? null : AccessTools.DeclaredMethod(bad, "Prepare");
            if (helper == null) { RuntimeHelpers.RunClassConstructor(handle); return; }
            if (installed) return;
            foreach (Type patch in type.Assembly.GetTypes().Where(t => t.IsDefined(typeof(HarmonyPatch), false)))
            {
                if (patch == bad || patch.FullName == "DailyMenuSupply.StandardDrinkSupply")
                    DiningMod.Patches.Patch(patch == bad ? AccessTools.Method(typeof(Processing), "Plan") : AccessTools.Method(typeof(Planner), "PlanDrugs"),
                        prefix: new HarmonyMethod(AccessTools.DeclaredMethod(patch, "Prefix")));
                else DiningMod.Patches.CreateClassProcessor(patch).Patch();
            }
            installed = true;
            Log.Message("[AMJ Agriculture] Daily Menu Supply compatibility applied; provider files unchanged.");
        }
    }

    // Author-approved acceptance-only exception. Do not mutate the live Def or
    // copy Cheat Menu's per-item stat offsets during portion splitting.
    [HarmonyPatch(typeof(FoodState), "Unsupported")]
    internal static class CheatMenuAcceptance
    {
        private static readonly MethodInfo Clone = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Prefix(ref ThingDef __0)
        {
            if (__0 == null || __0.comps == null || !__0.comps.Exists(IsCheatOffsets)) return;
            ThingDef copy = (ThingDef)Clone.Invoke(__0, null);
            copy.comps = new List<CompProperties>(__0.comps);
            copy.comps.RemoveAll(IsCheatOffsets);
            __0 = copy;
        }

        private static bool IsCheatOffsets(CompProperties props)
        {
            return props != null && props.compClass != null &&
                props.compClass.FullName == "Cheat_Menu.CompCheatStatOffsets";
        }
    }

    public static class Meals
    {
        public static MealRole Role(ThingDef def)
        { var extension = def == null ? null : def.GetModExtension<MealRoleExtension>(); return extension == null ? MealRole.None : extension.role; }
        public static List<Buffet> Select(Pawn worker, Pawn eater)
        {
            var choices = eater.Map.GetComponent<MenuMap>().Buffets
                .Where(b => b.Sample(eater) != null && Role(b.selectedFood) != MealRole.None &&
                    !b.IsForbidden(worker) && worker.CanReserveAndReach(b, PathEndMode.Touch, Danger.Deadly))
                .GroupBy(b => Role(b.selectedFood)).OrderBy(g => g.Key)
                .Select(g => g.OrderBy(b => b.Position.DistanceToSquared(worker.Position)).First()).ToList();
            return choices.Any(b => Role(b.selectedFood) == MealRole.Staple) ? choices : new List<Buffet>();
        }
        public static Job MakeJob(Pawn worker, Pawn eater)
        {
            var choices = Select(worker, eater);
            if (choices.Count == 0) return null;
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("AMJA_TakeMeal"), choices[0], eater);
            job.targetQueueA = choices.Select(b => new LocalTargetInfo(b)).ToList();
            return job;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
    internal static class ChooseMeal
    {
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("daily.menu.manager")]
        private static void Postfix(Pawn pawn, ref Job __result)
        {
            if (pawn.Map == null || !pawn.RaceProps.Humanlike || pawn.Downed || pawn.Drafted ||
                pawn.needs.food == null || pawn.needs.food.CurCategory < HungerCategory.Hungry) return;
            if (__result != null && __result.def == JobDefOf.Ingest && __result.targetA.Thing != null &&
                __result.targetA.Thing.TryGetComp<Plate>() != null) return;
            if (__result != null && __result.def != JobDefOf.Ingest && __result.def.defName != "DailyMenu_TakeBuffet") return;
            Job meal = Meals.MakeJob(pawn, pawn);
            if (meal != null) __result = meal;
        }
    }

    [HarmonyPatch(typeof(ProxyNurse), "Make")]
    internal static class ProxyMeal
    {
        private static void Postfix(Pawn worker, Pawn eater, ref Job __result)
        {
            if (__result == null) return; // retain upstream prisoner/medical eligibility
            Job job = Meals.MakeJob(worker, eater);
            if (job != null) __result = job;
        }
    }

    public sealed class PlateProperties : CompProperties
    { public PlateProperties() { compClass = typeof(Plate); } }

    public sealed class Plate : ThingComp, IThingHolder
    {
        private ThingOwner<Thing> contents;
        public Plate() { contents = new ThingOwner<Thing>(this, false, LookMode.Deep); }
        public IEnumerable<Thing> Dishes { get { return contents; } }
        public ThingOwner GetDirectlyHeldThings() { return contents; }
        public void GetChildHolders(List<IThingHolder> output) { ThingOwnerUtility.AppendThingHoldersFromThings(output, contents); }
        public override bool AllowStackWith(Thing other) { return false; }
        public void Add(Thing dish)
        {
            if (!contents.TryAdd(dish, false)) throw new InvalidOperationException("Cannot hold meal dish");
            var ingredients = dish.TryGetComp<CompIngredients>();
            if (ingredients != null) foreach (ThingDef ingredient in ingredients.ingredients)
                parent.TryGetComp<CompIngredients>().RegisterIngredient(ingredient);
            SyncRot();
        }
        public bool SafeFor(Pawn eater)
        { return contents.Count > 0 && contents.All(d => d.IngestibleNow && Rules.Allowed(eater, d)); }
        private void SyncRot()
        {
            var rot = parent.TryGetComp<CompRottable>();
            foreach (Thing dish in contents)
            {
                var source = dish.TryGetComp<CompRottable>();
                if (source == null) continue;
                float left = Math.Max(0f, source.PropsRot.TicksToRotStart - source.RotProgress);
                rot.RotProgress = Math.Max(rot.RotProgress, rot.PropsRot.TicksToRotStart - left);
            }
        }
        public override void CompTick() { contents.DoTick(); SyncRot(); }
        public override void PostExposeData()
        { base.PostExposeData(); Scribe_Deep.Look(ref contents, "mealDishes", this); if (contents == null) contents = new ThingOwner<Thing>(this, false, LookMode.Deep); }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        { contents.ClearAndDestroyContents(); base.PostDestroy(mode, previousMap); }
        public override string CompInspectStringExtra()
        { return string.Join("\n", contents.Select(d => d.LabelCap + " (" + FoodUtility.NutritionForEater(null, d).ToString("0.00") + ")").ToArray()); }
        public float Eat(Pawn eater, float wanted)
        {
            if (!SafeFor(eater)) return 0f;
            float total = 0;
            foreach (Thing original in contents.ToList())
            {
                float available = FoodUtility.NutritionForEater(eater, original);
                float take = Math.Min(available, wanted - total);
                if (take <= 0.001f) break;
                Thing eaten = original;
                if (take < available - 0.0001f)
                {
                    eaten = ThingMaker.MakeThing(original.def);
                    foreach (ThingComp comp in ((ThingWithComps)original).AllComps) comp.PostSplitOff(eaten);
                    var ing = original.TryGetComp<CompIngredients>();
                    if (ing != null) foreach (ThingDef ingredient in ing.ingredients) eaten.TryGetComp<CompIngredients>().RegisterIngredient(ingredient);
                    var rot = original.TryGetComp<CompRottable>();
                    if (rot != null) eaten.TryGetComp<CompRottable>().RotProgress = rot.RotProgress;
                    float raw = original.TryGetComp<Portion>().amount;
                    eaten.TryGetComp<Portion>().amount = raw * take / available;
                    original.TryGetComp<Portion>().amount = raw - eaten.TryGetComp<Portion>().amount;
                    contents.TryAdd(eaten, false);
                }
                total += eaten.Ingested(eater, take);
            }
            if (contents.Count == 0 || contents.All(t => t.Destroyed)) parent.Destroy();
            return total;
        }
    }

    [HarmonyPatch(typeof(FoodUtility), "NutritionForEater")]
    internal static class PlateNutrition
    {
        private static void Postfix(Pawn __0, Thing __1, ref float __result)
        { Plate plate = __1 == null ? null : __1.TryGetComp<Plate>(); if (plate != null) __result = plate.Dishes.Sum(d => FoodUtility.NutritionForEater(__0, d)); }
    }
    [HarmonyPatch(typeof(FoodUtility), "WillEat", new Type[] { typeof(Pawn), typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool) })]
    internal static class PlateDiet
    {
        private static void Postfix(Pawn p, Thing food, ref bool __result)
        { Plate plate = food == null ? null : food.TryGetComp<Plate>(); if (__result && plate != null) __result = plate.SafeFor(p); }
    }
    [HarmonyPatch(typeof(Thing), "Ingested")]
    internal static class IngestPlate
    {
        private static bool Prefix(Thing __instance, Pawn ingester, float nutritionWanted, ref float __result)
        { Plate plate = __instance.TryGetComp<Plate>(); if (plate == null) return true; __result = plate.Eat(ingester, nutritionWanted); return false; }
    }
    [HarmonyPatch(typeof(MealTracking), "Postfix")]
    internal static class NoDoubleRecord
    { private static bool Prefix(Thing __0) { return __0 == null || __0.TryGetComp<Plate>() == null; } }

    public sealed class TakeMealJob : JobDriver
    {
        private Thing plate;
        private float goal;
        private float totalWeight;
        private string session;
        private Pawn Eater { get { return job.targetB.Pawn ?? pawn; } }
        public override void ExposeData()
        { base.ExposeData(); Scribe_References.Look(ref plate, "assembledMeal"); Scribe_Values.Look(ref goal, "mealGoal"); Scribe_Values.Look(ref totalWeight, "mealWeight"); Scribe_Values.Look(ref session, "mealSession"); }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Eater != pawn && !pawn.Reserve(Eater, job, 1, -1, null, errorOnFailed)) return false;
            foreach (LocalTargetInfo target in job.targetQueueA)
                if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;
            return true;
        }
        private void Drop(IntVec3 at)
        {
            if (plate == null || plate.Destroyed || !pawn.inventory.innerContainer.Contains(plate)) return;
            Thing dropped;
            pawn.inventory.innerContainer.TryDrop(plate, at, Map, ThingPlaceMode.Near, out dropped);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Eater == null || Eater.Dead || !Eater.Spawned || Eater.Map != Map);
            AddFinishAction(condition => { if (condition != JobCondition.Succeeded && pawn.Spawned) Drop(pawn.Position); });
            var targets = job.targetQueueA.ToList();
            yield return Toils_General.Do(() =>
            {
                if (plate != null) return;
                goal = Eater.needs.food.NutritionWanted;
                totalWeight = targets.Sum(t => { var b = t.Thing as Buffet; return b != null && Meals.Role(b.selectedFood) == MealRole.Staple ? 3f : 1f; });
                session = Eater.GetUniqueLoadID() + "_amj_" + job.loadID;
                plate = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("AMJA_MealPlate"));
                pawn.inventory.innerContainer.TryAdd(plate, false);
            });
            foreach (LocalTargetInfo target in targets)
            {
                LocalTargetInfo current = target;
                yield return Toils_General.Do(() => job.targetA = current);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
                yield return Toils_General.Wait(60);
                yield return Toils_General.Do(() =>
                {
                    var buffet = job.targetA.Thing as Buffet;
                    if (buffet == null || !buffet.Spawned || buffet.IsForbidden(pawn)) return;
                    Plate packed = plate.TryGetComp<Plate>();
                    MealRole role = Meals.Role(buffet.selectedFood);
                    if (role != MealRole.Staple && !packed.Dishes.Any()) return;
                    float budget = Math.Min(goal, Eater.needs.food.NutritionWanted);
                    float used = packed.Dishes.Sum(d => FoodUtility.NutritionForEater(Eater, d));
                    if (budget - used <= 0.001f) return;
                    Thing dish = buffet.Dispense(Eater, Math.Min(budget - used, goal * (role == MealRole.Staple ? 3f : 1f) / Math.Max(1f, totalWeight)));
                    if (dish == null) return;
                    dish.TryGetComp<Portion>().session = session;
                    dish.TryGetComp<Portion>().goal = goal;
                    packed.Add(dish);
                });
            }
            if (Eater != pawn)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
                yield return Toils_General.Wait(180);
            }
            yield return Toils_General.Do(() =>
            {
                if (!plate.TryGetComp<Plate>().Dishes.Any()) { plate.Destroy(); return; }
                if (Eater == pawn)
                {
                    Job eat = JobMaker.MakeJob(JobDefOf.Ingest, plate); eat.count = 1;
                    pawn.jobs.jobQueue.EnqueueFirst(eat);
                }
                else if (FoodUtility.ShouldBeFedBySomeone(Eater))
                {
                    float actual = plate.Ingested(Eater, Eater.needs.food.NutritionWanted);
                    Eater.needs.food.CurLevel += actual;
                    if (!plate.Destroyed) Drop(Eater.Position);
                }
                else Drop(Eater.Position);
            });
        }
    }
}
