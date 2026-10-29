using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using RimWorks.Pickle;
using DailyMenu;

namespace AmjDailyProbe {
 public class DailyQuickstart : RimWorks.Quickstarts.AbstractQuickstart {
  public override TaggedString description { get { return "Disposable Daily Menu assessment"; } }
  public override int mapSize { get { return 50; } }
  public override string seed { get { return "AMJ-DailyMenu-20261028"; } }
 }
 [PickleSteps] public class Steps {
  static Task Main(Action a) {
   var t=new TaskCompletionSource<bool>();
   Action run=delegate {try {a();t.SetResult(true);}catch(Exception e){t.SetException(e);}};
   typeof(PickleContext).Assembly.GetType("RimWorks.Pickle.Runtime.PickleDriver",true).GetMethod("Post",new[]{typeof(Action)}).Invoke(null,new object[]{run});
   return t.Task;
  }
  static void Check(bool value,string message) {if(!value)throw new Exception(message);Log.Message("AMJ_DAILY_CHECK "+message);}
  [Then("Daily Menu is assessed through real food jobs")]
  public async Task Assess(PickleContext ctx) {
   Pawn p=null; Map map=null; MenuMap state=null; Buffet[] vats=null; float initial=0; int initialTick=0; int maxPortions=0; var ingested=new HashSet<string>();
   await Main(delegate {
    map=Find.CurrentMap; Check(map!=null,"native map loaded"); Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
    foreach(var pawn in map.mapPawns.AllPawnsSpawned.ToList())pawn.DeSpawn();
    var center=map.Center;
    foreach(var cell in CellRect.CenteredOn(center,8).Cells) {foreach(var t in map.thingGrid.ThingsListAt(cell).ToList())t.Destroy();map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);}
    p=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer); GenSpawn.Spawn(p,center,map);
    p.needs.food.CurLevelPercentage=0.2f; if(p.needs.rest!=null)p.needs.rest.CurLevelPercentage=1;
    if(p.foodRestriction!=null)p.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingCategoryDefOf.Foods,true);
    state=map.GetComponent<MenuMap>();state.enabled=false;state.drinksWithMeals=false;
    string[] foods={"MealSimple","MealFine","MealLavish"}; vats=new Buffet[3];
    for(int i=0;i<3;i++) {
     vats[i]=(Buffet)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DailyMenu_Buffet"),ThingDefOf.WoodLog);vats[i].SetFaction(Faction.OfPlayer);
     GenSpawn.Spawn(vats[i],center+new IntVec3(i*2-2,0,3),map);
     var food=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(foods[i]));food.stackCount=3;
     food.TryGetComp<CompIngredients>().RegisterIngredient(DefDatabase<ThingDef>.GetNamed("RawRice"));
     var rot=food.TryGetComp<CompRottable>();rot.RotProgress=1234;
     Check(vats[i].Load(food),"buffet accepts "+foods[i]);
    }
    initial=vats.Sum(v=>v.Nutrition); initialTick=Find.TickManager.TicksGame;
    Job job=null; BuffetFoodJob.Postfix(p,ref job);
    Check(job!=null&&job.targetQueueA.Count==3,"automatic selection chooses three different dishes");
    job.playerForced=true;p.jobs.StartJob(job,JobCondition.InterruptForced);
   });
   bool done=false;
   for(int batch=0;batch<120&&!done;batch++) {
    await Main(delegate {
     for(int n=0;n<40;n++) {
      maxPortions=Math.Max(maxPortions,p.inventory.innerContainer.Count(t=>t.TryGetComp<Portion>()!=null&&t.TryGetComp<Portion>().amount>=0));
      foreach(var record in state.diningRecords.Where(r=>r.pawn==p.GetUniqueLoadID()))ingested.Add(record.food);
      if(ingested.Count==3){done=true;break;}
      Find.TickManager.DoSingleTick();
     }
    });
    await Task.Delay(1);
   }
   await Main(delegate {
    Check(done,"three dishes consumed by actual pawn jobs");
    Check(maxPortions>=3,"baseline carries three separate portions rather than one plate");
    var records=state.diningRecords.Where(r=>r.pawn==p.GetUniqueLoadID()).ToList();
    Check(records.Select(r=>r.session).Distinct().Count()==1,"three dishes share one dining session");
    float consumed=records.Sum(r=>r.nutrition), removed=initial-vats.Sum(v=>v.Nutrition);
    Check(Math.Abs(consumed-removed)<0.002f,"buffet deduction matches recorded consumed nutrition");
    Check(p.needs.food.CurLevelPercentage>0.9f,"native ingestion satisfies hunger");
    Check(p.needs.mood.thoughts.memories.Memories.Any(t=>t.def.defName=="DailyMenu_BalancedMeal"),"multi-dish dining mood applied");
    Log.Message("AMJ_DAILY_RESULT nutrition="+consumed+" removed="+removed+" portions="+maxPortions+" elapsedTicks="+(Find.TickManager.TicksGame-initialTick));
    p.jobs.EndCurrentJob(JobCondition.InterruptForced,false);
    p.needs.food.CurLevelPercentage=0.2f;
    vats[1].Destroy();vats[2].Destroy();Job fallback=null;BuffetFoodJob.Postfix(p,ref fallback);
    Check(fallback!=null&&fallback.targetQueueA.Count==1,"one available dish still offers a meal job");
    var sample=vats[0].Dispense(p,0.12f);
    Check(sample!=null&&Math.Abs(FoodUtility.NutritionForEater(p,sample)-0.12f)<0.001f,"fractional serving retains requested nutrition");
    Check(sample.TryGetComp<CompIngredients>().ingredients.Contains(DefDatabase<ThingDef>.GetNamed("RawRice")),"portion retains ingredient identity");
    Check(sample.TryGetComp<CompRottable>().RotProgress>=1234,"portion retains existing rot progress");
    sample.Destroy();Log.Message("AMJ_DAILY_BASELINE_PASS");
   });
   await Main(delegate {
    PlatePrototype.Install();state.diningRecords.Clear();p.needs.food.CurLevelPercentage=0.2f;
    vats[0].Destroy();vats=new Buffet[4];
    string[] names={"MealLavish","MealFine","MealSimple","MealFine"};
    for(int i=0;i<4;i++) {
     vats[i]=(Buffet)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DailyMenu_Buffet"),ThingDefOf.WoodLog);vats[i].SetFaction(Faction.OfPlayer);
     GenSpawn.Spawn(vats[i],map.Center+new IntVec3(i*2-3,0,3),map);
     var food=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(names[i]));food.stackCount=3;
     food.TryGetComp<CompIngredients>().RegisterIngredient(DefDatabase<ThingDef>.GetNamed("RawRice"));
     Check(vats[i].Load(food),"prototype loads dish "+names[i]);
    }
    Job j=null;BuffetFoodJob.Postfix(p,ref j);
    Check(j!=null&&j.targetQueueA.Count==3,"role patch selects one of each role despite duplicate dish");
    Check(((Buffet)j.targetQueueA[0].Thing).selectedFood.defName=="MealSimple","staple role is selected first");
    initial=vats.Sum(v=>v.Nutrition);j.playerForced=true;p.jobs.StartJob(j,JobCondition.InterruptForced);
   });
   done=false;maxPortions=0;
   for(int batch=0;batch<120&&!done;batch++) {
    await Main(delegate {for(int n=0;n<40;n++) {
     int plates=p.inventory.innerContainer.Count(t=>t.TryGetComp<Plate>()!=null);maxPortions=Math.Max(maxPortions,plates);
     if(PlatePrototype.OuterIngest>0){done=true;break;}Find.TickManager.DoSingleTick();
    }});await Task.Delay(1);
   }
   await Main(delegate {
    Check(done&&PlatePrototype.Assembled==1&&PlatePrototype.OuterIngest==1,"single assembled plate consumed through one native Ingest job");
    Check(PlatePrototype.InternalIngest==3,"plate forwards original three food ingestion effects once each");
    Check(maxPortions==1,"pawn carries one assembled plate");
    var records=state.diningRecords.Where(r=>r.pawn==p.GetUniqueLoadID()).ToList();
    Check(records.Count==3&&records.All(r=>r.food!="AMJ_ProbePlate"),"wrapper does not duplicate Daily Menu consumption records");
    Check(Math.Abs(records.Sum(r=>r.nutrition)-(initial-vats.Sum(v=>v.Nutrition)))<0.002f,"plate preserves nutrition accounting");
    Check(p.needs.food.CurLevelPercentage>0.9f,"one-plate native job satisfies hunger");
    Log.Message("AMJ_DAILY_PLATE_PROTOTYPE_PASS");
   });
  }
 }
}
