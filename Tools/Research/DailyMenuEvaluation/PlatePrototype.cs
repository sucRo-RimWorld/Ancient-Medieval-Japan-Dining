// Disposable feasibility probe. Core meal defs stand in for three roles;
// this is not a production AMJ food classification or approved balance.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using DailyMenu;

namespace AmjDailyProbe {
 public class PlateProps:CompProperties {public PlateProps(){compClass=typeof(Plate);}}
 public class Plate:ThingComp,IThingHolder {
  ThingOwner<Thing> contents;
  public Plate(){contents=new ThingOwner<Thing>(this,false,LookMode.Deep);}
  public ThingOwner GetDirectlyHeldThings(){return contents;}
  public void GetChildHolders(List<IThingHolder> output){ThingOwnerUtility.AppendThingHoldersFromThings(output,contents);}
  public IEnumerable<Thing> Dishes {get{return contents;}}
  public void Add(Thing t){contents.TryAdd(t,false);}
  public override void PostExposeData(){base.PostExposeData();Scribe_Deep.Look(ref contents,"plateDishes",this);}
  public override void PostDestroy(DestroyMode mode,Map previousMap){contents.ClearAndDestroyContents();base.PostDestroy(mode,previousMap);}
 }
 public static class PlatePrototype {
  public static bool Enabled;
  public static int Assembled,OuterIngest,InternalIngest;
  public static void Install(){new Harmony("amj.daily.probe.plate").PatchAll(typeof(PlatePrototype).Assembly);Enabled=true;}
  public static int Role(ThingDef d){return d.defName=="MealSimple"?0:d.defName=="MealFine"?1:d.defName=="MealLavish"?2:99;}
  public static void Wrap(JobDriver driver){
   var field=AccessTools.Field(typeof(BuffetJob),"basket");var basket=(List<Thing>)field.GetValue(driver);
   var dishes=basket.Where(t=>t!=null&&!t.Destroyed).ToList();if(dishes.Count==0)return;
   var thing=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("AMJ_ProbePlate"));var plate=thing.TryGetComp<Plate>();
   foreach(var dish in dishes){driver.pawn.inventory.innerContainer.Remove(dish);plate.Add(dish);}
   if(!driver.pawn.inventory.innerContainer.TryAdd(thing,false))throw new Exception("probe plate inventory insertion failed");
   basket.Clear();basket.Add(thing);Assembled++;Log.Message("AMJ_DAILY_PLATE_ASSEMBLED dishes="+dishes.Count);
  }
 }
 [HarmonyPatch(typeof(BuffetFoodJob),"Postfix")] public static class SelectRoles {
  static void Postfix(Pawn pawn,ref Job __1){
   if(!PlatePrototype.Enabled||__1==null||__1.def.defName!="DailyMenu_TakeBuffet")return;
   var available=pawn.Map.GetComponent<MenuMap>().Buffets.Where(b=>b.Sample(pawn)!=null&&!b.IsForbidden(pawn)&&pawn.CanReserveAndReach(b,PathEndMode.Touch,Danger.Deadly)).ToList();
   var chosen=available.Where(b=>PlatePrototype.Role(b.selectedFood)<99).GroupBy(b=>PlatePrototype.Role(b.selectedFood)).OrderBy(g=>g.Key).Select(g=>g.OrderBy(b=>b.Position.DistanceToSquared(pawn.Position)).First()).ToList();
   if(chosen.Any(b=>PlatePrototype.Role(b.selectedFood)==0))__1.targetQueueA=chosen.Select(b=>new LocalTargetInfo(b)).ToList();
  }
 }
 [HarmonyPatch(typeof(BuffetJob),"MakeNewToils")] public static class Assemble {
  static void Postfix(BuffetJob __instance,ref IEnumerable<Toil> __result){if(PlatePrototype.Enabled)__result=WithPlate(__result,__instance);}
  static IEnumerable<Toil> WithPlate(IEnumerable<Toil> original,BuffetJob driver){
   Toil previous=null;foreach(var toil in original){if(previous!=null)yield return previous;previous=toil;}
   yield return Toils_General.Do(()=>PlatePrototype.Wrap(driver));if(previous!=null)yield return previous;
  }
 }
 [HarmonyPatch(typeof(FoodUtility),"NutritionForEater")] public static class PlateNutrition {
  static void Postfix(Pawn __0,Thing __1,ref float __result){var c=__1==null?null:__1.TryGetComp<Plate>();if(c!=null)__result=c.Dishes.Sum(t=>FoodUtility.NutritionForEater(__0,t));}
 }
 [HarmonyPatch(typeof(Thing),"Ingested")] public static class PlateIngest {
  static bool Prefix(Thing __instance,Pawn ingester,float nutritionWanted,ref float __result){
   var c=__instance.TryGetComp<Plate>();if(c==null)return true;
   PlatePrototype.OuterIngest++;float sum=0;
   foreach(var t in c.Dishes.ToList()) {if(nutritionWanted-sum<=0.001f)break;PlatePrototype.InternalIngest++;sum+=t.Ingested(ingester,nutritionWanted-sum);}
   __result=sum;__instance.Destroy();return false;
  }
 }
 [HarmonyPatch(typeof(MealTracking),"Postfix")] public static class AvoidWrapperRecord {
  static bool Prefix(Thing __0){return __0==null||__0.TryGetComp<Plate>()==null;}
 }
}
