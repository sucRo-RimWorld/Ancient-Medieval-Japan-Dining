using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimWorks.Pickle;
using DailyMenu;
using AMJAgriculture.Dining;

namespace AgricultureDiningTests
{
 public sealed class DiningQuickstart:RimWorks.Quickstarts.AbstractQuickstart
 {
  public override TaggedString description {get{return "Agriculture production dining acceptance";}}
  public override int mapSize {get{return 50;}}
  public override string seed {get{return "Agriculture-Dining-20261029";}}
 }
 [PickleSteps] public sealed class ProductionProbe
 {
  static Task Main(Action a){var t=new TaskCompletionSource<bool>();Action run=()=>{try{a();t.SetResult(true);}catch(Exception e){t.SetException(e);}};typeof(PickleContext).Assembly.GetType("RimWorks.Pickle.Runtime.PickleDriver",true).GetMethod("Post",new[]{typeof(Action)}).Invoke(null,new object[]{run});return t.Task;}
  static void Check(bool b,string m){if(!b)throw new Exception(m);Log.Message("AGRI_DINING_CHECK "+m);}
  Map map;Pawn cook;Buffet[] buffets;MenuMap menu;
  Thing Make(string name,int count=1){var t=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name));t.stackCount=count;return t;}
  async Task Until(Func<bool> done,string label,int maximum=12000){bool yes=false;for(int ticks=0;ticks<maximum&&!yes;ticks+=40){await Main(()=>{for(int n=0;n<40;n++){if(done()){yes=true;break;}Find.TickManager.DoSingleTick();}});await Task.Delay(1);}await Main(()=>Check(yes,label));}
  [Then("Agriculture production meals are prepared served and preserved")]
  public async Task Run(PickleContext ctx)
  {
   Bill_Production bill=null;Building_WorkTable fire=null;
   await Main(()=>{
    map=Find.CurrentMap;Check(map!=null,"real map and production assemblies loaded");Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
    foreach(var p in map.mapPawns.AllPawnsSpawned.ToList())p.DeSpawn();
    foreach(var cell in CellRect.CenteredOn(map.Center,10).Cells){foreach(var t in map.thingGrid.ThingsListAt(cell).ToList())t.Destroy();map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);}
    cook=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);GenSpawn.Spawn(cook,map.Center,map);
    cook.needs.food.CurLevelPercentage=1;cook.needs.rest.CurLevelPercentage=1;cook.skills.GetSkill(SkillDefOf.Cooking).Level=15;
    cook.workSettings.EnableAndInitialize();foreach(var w in DefDatabase<WorkTypeDef>.AllDefs)if(!cook.WorkTypeIsDisabled(w))cook.workSettings.SetPriority(w,3);
    cook.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingCategoryDefOf.Foods,true);
    menu=map.GetComponent<MenuMap>();menu.enabled=false;menu.drinksWithMeals=false;
    fire=(Building_WorkTable)ThingMaker.MakeThing(ThingDefOf.Campfire,ThingDefOf.WoodLog);fire.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(fire,map.Center+new IntVec3(-4,0,0),map);fire.TryGetComp<CompRefuelable>().Refuel(50);
    GenPlace.TryPlaceThing(Make("RawRice",40),map.Center+new IntVec3(-3,0,0),map,ThingPlaceMode.Near,out Thing placed);
    var recipe=DefDatabase<RecipeDef>.GetNamed("AMJC_CookMixedGrainMealBulk");bill=(Bill_Production)recipe.MakeNewBill();bill.repeatMode=BillRepeatModeDefOf.RepeatCount;bill.repeatCount=1;bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);bill.ingredientFilter.SetDisallowAll();bill.ingredientFilter.SetAllow(DefDatabase<ThingDef>.GetNamed("RawRice"),true);fire.BillStack.AddBill(bill);
    var work=new WorkGiver_DoBill();work.def=DefDatabase<WorkGiverDef>.GetNamed("DoBillsCookCampfire");var job=work.JobOnThing(cook,fire,true);Check(job!=null,"real Agriculture cooking Bill offered");job.playerForced=true;cook.jobs.StartJob(job,JobCondition.InterruptForced);
   });
   await Until(()=>bill.repeatCount==0,"real DoBill creates Agriculture cooked grains");
   await Main(()=>{
    cook.jobs.EndCurrentJob(JobCondition.InterruptForced,false);fire.BillStack.Delete(bill);
    var meals=map.listerThings.AllThings.Where(t=>t.def.defName=="AMJC_MixedGrainMeal").ToList();
    if(cook.carryTracker.CarriedThing!=null&&cook.carryTracker.CarriedThing.def.defName=="AMJC_MixedGrainMeal"){Thing dropped;cook.carryTracker.TryDropCarriedThing(cook.Position,ThingPlaceMode.Near,out dropped);meals.Add(dropped);}
    Check(meals.Sum(t=>t.stackCount)>=4,"cooking produced four real meals");
    string[] names={"AMJC_MixedGrainMeal","DankPyon_MealMushroomSoup","DankPyon_MealGrilledSkewers"};buffets=new Buffet[3];
    for(int i=0;i<3;i++){buffets[i]=(Buffet)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DailyMenu_Buffet"),ThingDefOf.WoodLog);buffets[i].SetFaction(Faction.OfPlayer);GenSpawn.Spawn(buffets[i],map.Center+new IntVec3(i*2-2,0,4),map);Thing food=i==0?meals.First():Make(names[i],4);AccessTools.Field(typeof(CompFoodPoisonable),"poisonPct").SetValue(food.TryGetComp<CompFoodPoisonable>(),0f);Check(buffets[i].Load(food),"real dish accepted "+names[i]);}
    cook.needs.food.CurLevelPercentage=0.2f;
    var job=(Job)AccessTools.Method(typeof(JobGiver_GetFood),"TryGiveJob").Invoke(new JobGiver_GetFood(),new object[]{cook});
    Check(job!=null&&job.def.defName=="AMJA_TakeMeal"&&job.targetQueueA.Count==3,"normal hunger AI chooses production three-role meal");
    cook.jobs.StartJob(job,JobCondition.InterruptForced);
   });
   await Until(()=>menu.diningRecords.Count>=3,"normal take-and-eat job consumes three real dishes");
   await Main(()=>{
    Check(cook.needs.food.CurLevelPercentage>0.9f,"normal meal restores hunger");
    Check(menu.diningRecords.Select(r=>r.session).Distinct().Count()==1&&menu.diningRecords.All(r=>r.food!="AMJA_MealPlate"),"one meal records each real dish once");
    Check(cook.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("DankPyon_AteSoup")),"MO soup effect survives plate ingestion");
    Log.Message("AGRI_DINING_NATIVE_EAT_PASS");
    cook.jobs.EndCurrentJob(JobCondition.InterruptForced,false);cook.needs.food.CurLevelPercentage=0.2f;
    cook.jobs.StartJob(Meals.MakeJob(cook,cook),JobCondition.InterruptForced);
   });
   await Until(()=>cook.inventory.innerContainer.Any(t=>t.TryGetComp<Plate>()!=null&&t.TryGetComp<Plate>().Dishes.Any()),"interruption fixture has taken actual food");
   Thing recovered=null;
   await Main(()=>{
    cook.jobs.EndCurrentJob(JobCondition.InterruptForced,false);
    recovered=map.listerThings.AllThings.FirstOrDefault(t=>t.TryGetComp<Plate>()!=null&&t.TryGetComp<Plate>().Dishes.Any());
    Check(recovered!=null&&recovered.Spawned,"interrupted take job drops retained food without deleting it");
    float before=FoodUtility.NutritionForEater(cook,recovered);
    float actual=recovered.Ingested(cook,0.03f);Check(actual>0.029f&&actual<0.031f&&!recovered.Destroyed,"partial meal leaves its remainder");
    Check(Math.Abs(FoodUtility.NutritionForEater(cook,recovered)+actual-before)<0.002f,"partial eating conserves nutrition");
    float remaining=FoodUtility.NutritionForEater(cook,recovered);
    string path=System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath,"meal-roundtrip.xml");
    Scribe.saver.InitSaving(path,"MealRoundTrip");Thing saved=recovered;Scribe_Deep.Look(ref saved,"meal");Scribe.saver.FinalizeSaving();recovered.Destroy();
    Thing loaded=null;Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref loaded,"meal");Scribe.loader.FinalizeLoading();
    Check(loaded!=null&&loaded.TryGetComp<Plate>().Dishes.Any()&&Math.Abs(FoodUtility.NutritionForEater(cook,loaded)-remaining)<0.002f,"plate deep save/load retains food and nutrition");
    GenPlace.TryPlaceThing(loaded,cook.Position,map,ThingPlaceMode.Near,out recovered);
    Job eat=JobMaker.MakeJob(JobDefOf.Ingest,recovered);eat.count=1;cook.jobs.StartJob(eat,JobCondition.InterruptForced);
    Log.Message("AGRI_DINING_INTERRUPTION_SAVE_PASS");
   });
   await Until(()=>recovered.Destroyed,"recovered saved plate can be eaten normally");
   Pawn patient=null;Job proxy=null;
   await Main(()=>{
    cook.jobs.EndCurrentJob(JobCondition.InterruptForced,false);cook.needs.food.CurLevelPercentage=1;
    var bed=(Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed,ThingDefOf.WoodLog);bed.SetFaction(Faction.OfPlayer);bed.Medical=true;GenSpawn.Spawn(bed,map.Center+new IntVec3(4,0,-4),map);
    patient=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);GenSpawn.Spawn(patient,bed.GetSleepingSlotPos(0),map);patient.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingCategoryDefOf.Foods,true);patient.needs.food.CurLevelPercentage=0.2f;
    patient.health.AddHediff(HediffDefOf.Anesthetic);patient.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown,bed),JobCondition.InterruptForced);
   });
   await Until(()=>FoodUtility.ShouldBeFedBySomeone(patient),"patient qualifies for native assisted feeding",1000);
   await Main(()=>{
    proxy=ProxyNurse.Make(cook,patient,false);Check(proxy!=null&&proxy.def.defName=="AMJA_TakeMeal","native nurse selects Agriculture meal job");cook.jobs.StartJob(proxy,JobCondition.InterruptForced);
   });
   await Until(()=>patient.needs.food.CurLevelPercentage>0.9f,"nurse collects and feeds one Agriculture plate");
   await Main(()=>{
    Check(menu.diningRecords.Where(r=>r.pawn==patient.GetUniqueLoadID()).Select(r=>r.food).Distinct().Count()==3,"patient meal records three original dishes once");
    Log.Message("AGRI_DINING_PROXY_PASS");
   });
   await Main(()=>{
    cook.jobs.EndCurrentJob(JobCondition.InterruptForced,false);cook.needs.food.CurLevelPercentage=0.2f;
    buffets[1].Destroy();buffets[2].Destroy();
    Job job=Meals.MakeJob(cook,cook);Check(job!=null&&job.targetQueueA.Count==1,"staple-only shortage meal remains available");
    Check(!fire.def.AllRecipes.Any(r=>r.defName=="CookMealSimple"||r.defName=="CookMealSimpleBulk"),"generic simple-meal Bills are retired without deleting saved Defs");
    Log.Message("AGRI_DINING_PRODUCTION_PASS");
   });
  }
 }
}
