using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using xiaoye97;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using System.Reflection.Emit;
using CommonAPI;
using CommonAPI.Systems;
using System.IO;

namespace RecyclableFuelRod
{
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dsp.common-api.CommonAPI", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("Gnimaerd.DSP.plugin.RecyclableFuelRod", "RecyclableFuelRod", "1.1")]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(TabSystem))]
    public class RecyclableFuelRod : BaseUnityPlugin
    {
        private Sprite iconAntiInject;
        private Sprite iconDeutInject;
        private Sprite iconEptA;
        private Sprite iconEptD;
        public static ConfigEntry<bool> AntiFuelRecycle;
        public static List<int> OriRods;
        public static List<int> EmptyRods;
        public static List<int> RelatedGenerators;
        public static string GUID = "Gnimaerd.DSP.plugin.RecyclableFuelRod";
        public static string MODID = "FractionateEverything";
        public static int pagenum = 3;

        public static ResourceData resources;

        public void Awake()
        {
            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {

                using (ProtoRegistry.StartModLoad(GUID))
                {
                    //Initilize new instance of ResourceData class.
                    resources = new ResourceData(GUID, "Recycle", pluginfolder); // Make sure that the keyword you are using is not used by other mod authors.
                    resources.LoadAssetBundle("recycleicons"); // Load asset bundle located near your assembly
                                                              //resources.ResolveVertaFolder(); // Call this to resolver verta folder. You don't need to call this if you are not using .verta files 
                    ProtoRegistry.AddResource(resources); // Add your ResourceData to global list of resources
                    pagenum = TabSystem.RegisterTab($"{MODID}:{MODID}Tab", new TabData("FractionateTab", "Assets/Recycle/add1"));
                }
            }
            catch (Exception)
            {

                pagenum = TabSystem.RegisterTab($"{MODID}:{MODID}Tab", new TabData("FractionateTab", "Assets/Recycle/add1"));
            }


            AntiFuelRecycle = Config.Bind<bool>("config", "AntiFuelRecycle", true, "Turn this to false to deactivate recyclable Antimatter Fuel Rod. 设置为false来停用反物质燃料棒的循环使用。");

            OriRods = new List<int> { 1802 };
            EmptyRods = new List<int> { 9451 };
            RelatedGenerators = new List<int> { 2211 };

            if (RecyclableFuelRod.AntiFuelRecycle.Value)
            {
                OriRods.Add(1803);
                EmptyRods.Add(9452);
                RelatedGenerators.Add(2210);
            }

            LDBTool.PreAddDataAction += AddTranslateDInj;
            LDBTool.PreAddDataAction += AddTranslateEptD;
            LDBTool.PreAddDataAction += AddDeutRods;

            if (true)
            {
                LDBTool.PreAddDataAction += AddTranslateAInj;
                LDBTool.PreAddDataAction += AddTranslateEptA;
                LDBTool.PostAddDataAction += AddAntiRods;
            }

            //var harmony = new Harmony("Gnimaerd.DSP.plugin.RecyclableFuelRod.patch");
            //harmony.PatchAll(typeof(Patch_Mecha_GenerateEnergy));
            Harmony.CreateAndPatchAll(typeof(RecyclableFuelRod));
            Harmony.CreateAndPatchAll(typeof(UIPatchers));

        }

        public void Start()
        {
            UIPatchers.InitAll();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), "InsertInto")]
        public static bool InsertIntoPatch(PlanetFactory __instance, int entityId, int itemId, ref byte itemCount, ref byte itemInc, out byte remainInc, ref int __result)
        {
            remainInc = itemInc;
            int beltId = __instance.entityPool[entityId].beltId;
            if (beltId > 0)
            {
                return true;
            }

            int powerGenId = __instance.entityPool[entityId].powerGenId;
            int protoId_h = __instance.entityPool[entityId].protoId;
            int[] array = __instance.entityNeeds[entityId];
            if (powerGenId > 0)
            {
                PowerGeneratorComponent[] genPool = __instance.powerSystem.genPool;
                if (!RelatedGenerators.Contains(protoId_h))//如果不是相关的发电厂建筑，则不patch，执行原函数
                {
                    return true;
                }
                Mutex obj = __instance.entityMutexs[entityId];
                lock (obj)
                {
                    if (itemId == (int)genPool[powerGenId].fuelId)
                    {
                        if (genPool[powerGenId].fuelCount < 10)
                        {
                            PowerGeneratorComponent[] array4 = genPool;
                            int num12 = powerGenId;
                            array4[num12].fuelCount = (short)(array4[num12].fuelCount + (short)itemCount);
                            PowerGeneratorComponent[] array5 = genPool;
                            int num13 = powerGenId;
                            array5[num13].fuelInc = (short)(array5[num13].fuelInc + (short)itemInc);
                            remainInc = 0;
                            __result = (int)itemCount;
                            return false;
                        }
                        __result = 0;
                        return false;
                    }
                    else if (genPool[powerGenId].fuelId == 0 && !EmptyRods.Contains(itemId)) // fuel是空的，且爪子正准备送进来的不是空棒（那就是满棒呗
                    {
                        array = ItemProto.fuelNeeds[(int)genPool[powerGenId].fuelMask];
                        if (array == null || array.Length == 0)
                        {
                            __result = 0;
                            return false;
                        }
                        for (int j = 0; j < array.Length; j++)
                        {
                            if (array[j] == itemId)
                            {
                                genPool[powerGenId].SetNewFuel(itemId, (short)itemCount, (short)itemInc);
                                remainInc = 0;
                                __result = (int)itemCount;
                                return false;
                            }
                        }
                        __result = 0;
                        return false;
                    }
                    // 爪子里是空棒待送入（这里没有对发电厂的类型和抓来的空棒类型作判断可能会发生空的氘核燃料棒被试图或者真的抓入小太阳里，但这种情况无需特殊处理来避免卡死，毕竟谁会串联核电站和小太阳呢？就算串联了，卡死和不卡死都没区别，都是无法工作的。
                    // 针对可能的把空氘棒放入小太阳然后下次小太阳消耗黑棒的时候骗取黑棒（+1的同时会改productHeat）的问题，已在GenEnergyByFuelPatch中做了防备
                    // 这里暂时不判断数量了
                    else if (EmptyRods.Contains(itemId) && (genPool[powerGenId].productHeat == 0 || genPool[powerGenId].productHeat == itemId)) 
                    {
                        PowerGeneratorComponent[] array4 = genPool;
                        int num12 = powerGenId;
                        array4[num12].productCount = (short)(array4[num12].productCount + (short)itemCount);
                        PowerGeneratorComponent[] array5 = genPool;
                        int num13 = powerGenId;
                        array5[num13].productHeat = itemId;
                        remainInc = 0;
                        __result = (int)itemCount;
                        return false;
                    }
                }
                __result = 0;
                return false;


            }
            else
            {
                return true;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), "GenEnergyByFuel")]
        public static bool GenEnergyByFuelPatch(ref PowerGeneratorComponent __instance, long energy, ref int[] consumeRegister)
        {
            if (!OriRods.Contains(__instance.fuelId))
            {
                return true;
            }
            //long num = energy * __instance.useFuelPerTick / __instance.genEnergyPerTick;

            long num = __instance.productive ? (energy * __instance.useFuelPerTick * 40L / (__instance.genEnergyPerTick * (long)Cargo.incFastDivisionNumerator[(int)__instance.fuelIncLevel])) : (energy * __instance.useFuelPerTick / __instance.genEnergyPerTick);
            num = ((energy > 0L && num == 0L) ? 1L : num);
            if (__instance.fuelEnergy > num)
            {
                __instance.fuelEnergy -= num;
                return false;
            }

            //（以下的燃料棒泛指任何燃料）
            //fuelEnergy是已经进入发电厂内部的燃料可供消耗的能量（就是外面的一圈橙色圈圈指示的能量，是已经被发电厂吞掉的燃料棒，还未完全消耗掉的）
            //fuelId是能看到物品图表的，在电厂物品栏内还未被吞掉的燃料棒的Id
            //fuelHeat就是上面那个等待被发电厂吞掉的燃料棒的单个物品的能量
            //fuelCount就是发电厂里暂存的上述燃料棒的数量

            __instance.curFuelId = 0;
            if (__instance.fuelCount > 0)
            {

                int num2 = (int)(__instance.fuelInc / __instance.fuelCount);
                //Console.WriteLine("fuleinc is " + __instance.fuelInc.ToString() + "  and num2 ori is " + num2.ToString());
                num2 = ((num2 > 0) ? ((num2 > 10) ? 10 : num2) : 0);
                __instance.fuelInc -= (short)num2;
                __instance.productive = LDB.items.Select((int)__instance.fuelId).Productive;
                if (__instance.productive)
                {
                    __instance.fuelIncLevel = (byte)num2;
                    num = energy * __instance.useFuelPerTick * 40L / (__instance.genEnergyPerTick * (long)Cargo.incFastDivisionNumerator[(int)__instance.fuelIncLevel]);
                }
                else
                {
                    __instance.fuelIncLevel = (byte)num2;
                    num = energy * __instance.useFuelPerTick / __instance.genEnergyPerTick;
                }
                long num3 = num - __instance.fuelEnergy;
                __instance.fuelEnergy = __instance.fuelHeat - num3;
                __instance.curFuelId = __instance.fuelId;
                __instance.fuelCount -= 1;
                consumeRegister[(int)__instance.fuelId]++;

                // 这里为什么把空棒的Id存在productHeat里而不是productId里面呢？是因为一些游戏逻辑会根据productId是不是0做出（或不做）一些别的操作，导致bug（一个bug就是空棒没被抓出去的时候，满棒不会消耗，这个bug在PowerSystem.GameTick中调用的.GenEnergyByFuel的前置条件式productId==0），因此只能勉为其难存储在productHeat中了，看起来很离谱
                // 坏处就是鼠标悬停的UI就没办法显示空棒及其数量了，因此后面还要patch一下EntityBriefInfo里面的SetBriefInfo来让这个productHeat也作为可被认作是storage的id之一返回
                if (__instance.fuelId == 1802)
                {
                    if (__instance.productHeat != 9451) // 为了防止玩家把空的氘棒抓入小太阳然后用增量骗取空的反物质棒（或反过来），所以检测到空棒存储的id不符时，清零productCount，下同
                        __instance.productCount = 0;
                    __instance.productHeat = 9451;
                    __instance.productCount += 1;
                    
                }
                else if (__instance.fuelId == 1803 && OriRods.Contains(1803))
                {
                    if (__instance.productHeat != 9452) // 为了防止玩家把空的氘棒抓入小太阳然后用增量骗取空的反物质棒，所以检测到空棒存储的id不符时，清零productCount
                        __instance.productCount = 0;
                    __instance.productHeat = 9452;
                    __instance.productCount += 1;
                }


                if (__instance.fuelCount == 0)
                {
                    __instance.fuelId = 0;
                    __instance.fuelHeat = 0L;
                }
                if (__instance.fuelEnergy < 0L)
                {
                    __instance.fuelEnergy = 0L;
                    return false;
                }
            }

            else
            {
                __instance.fuelEnergy = 0L;
                __instance.productive = false;
            }

            return false;



        }



        /// <summary>
        /// 这个函数的prepatch必须保证filter为0时优先抓取满棒，防止为了运送空棒而耽误燃料运送而停电
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="filter"></param>
        /// <param name="inc"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), "PickFuelFrom")]
        public static bool PickFuelFromPatch(ref PowerGeneratorComponent __instance, ref int __result, int filter, out int inc)
        {

            inc = 0;
            if (!EmptyRods.Contains((int)__instance.productHeat))
            {
                return true;
            }
            if (filter == 0)
            {
                if (__instance.fuelId > 0 && __instance.fuelCount > 5 && (filter == 0 || filter == (int)__instance.fuelId)) // 这里其实是原有逻辑，在filter为0时优先执行抓取满燃料棒的工作
                {
                    if (__instance.fuelInc > 0)
                    {
                        inc = (int)(__instance.fuelInc / __instance.fuelCount);
                    }
                    __instance.fuelInc -= (short)inc;
                    __instance.fuelCount -= 1;
                    __result = (int)__instance.fuelId;
                    return false;
                }
            }

            // 代码能进行到这里代表filter不是0或者没能成功抓取满棒（否则就return false了），那么尝试抓取空棒
            // 这里记得要支持filter == -1的情况，是为了跳过抓取满棒的判定，因此抓取空棒这里要为filter<0的情况放行，当然也要兼顾filter==0但是前面抓满棒的判定没过（比如满棒来源的储量不够5个不能抓满棒）的情况
            // 但是有一种情况，来源的空棒是氘空棒，插入的目标发电机是小太阳（只能接受反物质空棒），这就麻烦了，这种情况下乱抓会导致爪子在送过去的时候卡住，这种情况我认为不需要在insert的方法里面再针对性地处理一下（即使该爪子是两用：同时运满棒和空棒的话，其本身也不能把满棒正确地从核电站运进小太阳，这就是玩家的建造错误，所以不需要处理卡住的状况）
            // 这里为什么把空棒的Id存在productHeat里而不是productId里面呢？是因为一些游戏逻辑会根据productId是不是0做出（或不做）一些别的操作，导致bug（一个bug就是空棒没被抓出去的时候，满棒不会消耗，这个bug在PowerSystem.GameTick中调用的.GenEnergyByFuel的前置条件式productId==0），因此只能勉为其难存储在productHeat中了，看起来很离谱
            if (EmptyRods.Contains((int)__instance.productHeat) && (filter <=0 || filter == (int)__instance.productHeat)) 
            {
                __instance.productCount -= 1;
                __result = (int)__instance.productHeat;
                if (__instance.productCount == 0)
                {
                    __instance.productHeat = 0;
                }
                return false;
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), "PickFrom")]
        public static bool PickFromPatch(ref PlanetFactory __instance, ref int __result, int entityId, int offset, int filter, int[] needs, out byte stack, out byte inc)
        {
            stack = 1;
            inc = 0;
            int beltId = __instance.entityPool[entityId].beltId;
            if (beltId > 0)
            {
                return true;
            }

            int powerGenId = __instance.entityPool[entityId].powerGenId;
            if (powerGenId > 0 && RelatedGenerators.Contains(__instance.entityPool[entityId].protoId))//这里删掉了offset的判断
            {
                Mutex obj = __instance.entityMutexs[entityId];
                lock (obj)
                {
                    // 如果爪子的另一端连接了另一个发电机，这里不判断该发电机的类型是否相符了，一般不会有人把两个不同类的发电机连起来。即使真的有人这样做了，正常的游戏逻辑也无法把燃料放进去，所以不需要判断了。
                    if (offset > 0 && __instance.powerSystem.genPool[offset].id == offset)
                    {
                        if (__instance.powerSystem.genPool[offset].fuelCount <= 8)
                        {
                            int num3;
                            int result3 = __instance.powerSystem.genPool[powerGenId].PickFuelFrom(filter, out num3);
                            inc = (byte)num3;
                            __result = result3;
                            return false;
                        }
                    }
                    // 能进行到这里，代表前面没成功return false，代表爪子的另一端不是发电机，或者爪子另一端的燃料棒数量足够（9及以上）那么这种情况下，只抓空棒
                    if (EmptyRods.Contains(filter) || filter == 0)
                    {
                        int num3;
                        int result3 = __instance.powerSystem.genPool[powerGenId].PickFuelFrom(-1, out num3); // 这里传入filter为-1告诉pickFuelFrom的patch，不要抓满的燃料棒，只能抓空棒
                        inc = (byte)num3;
                        __result = result3;
                        return false;
                    }
                }
            }
            else
            {
                return true;
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mecha), "GenerateEnergy")]
        public static bool MechaGenerateEnergyPatch(ref Mecha __instance, double dt)
        {
            //与发电厂的逻辑不同，机甲是在燃料棒烧光后返还空燃料棒，这是为了能够尽早return true来适配另一个返还空电池的mod，否则会使该mod失效。另一方面，我层尝试类似该mod修改IL代码的方法，但并未成功。
            //_h是为了区分原版变量，我自己加的后缀
            double num2_h = 1.0;
            ItemProto itemProto_h = (__instance.reactorItemId > 0) ? LDB.items.Select(__instance.reactorItemId) : null;
            if (itemProto_h != null)
            {
                num2_h = (double)(itemProto_h.ReactorInc + 1f);
                if (__instance.reactorItemInc > 0)
                {
                    if (itemProto_h.Productive)
                    {
                        num2_h *= 1.0 + Cargo.incTableMilli[__instance.reactorItemInc];
                    }
                    else
                    {
                        num2_h *= 1.0 + Cargo.accTableMilli[__instance.reactorItemInc];
                    }
                }
            }
            double num3_h = __instance.coreEnergyCap - __instance.coreEnergy;
            double num4_h = __instance.reactorPowerGen * num2_h * dt;
            if (num4_h > num3_h)
            {
                num4_h = num3_h;
            }
            //以下是核心逻辑，如果当前帧应该生成的能量>能量池剩余能量，按理说应该消耗新的燃料，这代表着上一个燃料已经耗尽，于是如果上一个燃料是氘核/反物质燃料棒，则返还空棒
            //由于到此为止并未对机甲内部能量做任何修改，执行完毕后直接返回原函数
            if (__instance.reactorEnergy < num4_h && OriRods.Contains(__instance.reactorItemId))
            {
                int v;
                int outinc;
                if ((v = __instance.player.package.AddItemStacked(__instance.reactorItemId - 1802 + 9451, 1, __instance.reactorItemInc, out outinc)) != 0)
                {
                    UIItemup.Up(__instance.reactorItemId - 1802 + 9451, v);
                }
            }

            return true;

            //以下是本来想直接覆盖的原函数，逻辑是每次刚开始消耗燃料棒就返还空燃料棒，但是会影响其他mod（指RecycleAccumulator）因此放弃该方案
            /*
            __instance.ClearEnergyChange();
            __instance.ClearChargerDevice();
            double num = __instance.corePowerGen * dt;
            __instance.coreEnergy += num;
            if (__instance.coreEnergy > __instance.coreEnergyCap)
            {
                __instance.coreEnergy = __instance.coreEnergyCap;
            }
            __instance.MarkEnergyChange(0, num);
            double num2 = 1.0;
            ItemProto itemProto = (__instance.reactorItemId > 0) ? LDB.items.Select(__instance.reactorItemId) : null;
            if (itemProto != null)
            {
                num2 = (double)(itemProto.ReactorInc + 1f);
                if (__instance.reactorItemInc > 0)
                {
                    if (itemProto.Productive)
                    {
                        num2 *= 1.0 + Cargo.incTableMilli[__instance.reactorItemInc];
                    }
                    else
                    {
                        num2 *= 1.0 + Cargo.accTableMilli[__instance.reactorItemInc];
                    }
                }
            }
            double num3 = __instance.coreEnergyCap - __instance.coreEnergy;
            double num4 = __instance.reactorPowerGen * num2 * dt;
            if (num4 > num3)
            {
                num4 = num3;
            }
            while (__instance.reactorEnergy < num4)
            {
                int num5 = 0;
                int num6 = 1;
                int num7;
                __instance.reactorStorage.TakeTailItems(ref num5, ref num6, out num7, false);
                if (num6 <= 0 || num5 <= 0)
                {
                    __instance.reactorItemId = 0;
                    __instance.reactorItemInc = 0;
                    break;
                }
                __instance.AddConsumptionStat(num5, num6, __instance.player.nearestFactory);
                __instance.reactorItemId = num5;
                ItemProto itemProto2 = LDB.items.Select(num5);
                __instance.reactorItemInc = ((num7 > 10) ? 10 : num7);
                //下面返还空燃料棒
                if (OriRods.Contains(num5))
                {
                    int v;
                    int outinc;
                    if ((v = __instance.player.package.AddItemStacked(num5 - 1802 + 9451, 1, __instance.reactorItemInc, out outinc)) != 0)
                    {
                        UIItemup.Up(num5 - 1802 + 9451, v);
                    }
                }
                if (itemProto2 != null)
                {
                    __instance.reactorEnergy += (double)itemProto2.HeatValue * (1.0 + (itemProto2.Productive ? Cargo.incTableMilli[__instance.reactorItemInc] : 0.0));
                }
                __instance.player.controller.gameData.history.AddFeatureValue(2100000 + num5, num6);
            }
            if (__instance.reactorEnergy > 0.0)
            {
                __instance.MarkEnergyChange(1, __instance.reactorPowerGen * num2 * dt);
                if (num4 > __instance.reactorEnergy)
                {
                    num4 = __instance.reactorEnergy;
                }
                __instance.coreEnergy += num4;
                __instance.reactorEnergy -= num4;
            }
            return false;
            */
        }




        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), "EnergyCap_Fuel")]
        public static bool EnergyCap_Fuel_Patch(ref PowerGeneratorComponent __instance, ref long __result)
        {
            long num = ((__instance.fuelCount <= 0 || __instance.fuelId == 1101 ) && __instance.fuelEnergy < __instance.useFuelPerTick) ? (__instance.fuelEnergy * __instance.genEnergyPerTick / __instance.useFuelPerTick) : __instance.genEnergyPerTick;
            __instance.capacityCurrentTick = num;
            __result = __instance.capacityCurrentTick;
            return false;
        }
        */
        void AddDeutRods()
        {

            var icondesc = ProtoRegistry.GetDefaultIconDesc(new Color(1f, 1f, 1f), new Color(0f, 1f, 1f), new Color(0, 0.2f, 0.3f), new Color(0, 0.4f, 0.5f));
            ItemProto DRod = ProtoRegistry.RegisterItem(9451, "空的氘核燃料棒", "空的氘核燃料棒描述", "Assets/Recycle/EmptyDeut", 611 + pagenum * 1000, 30, EItemType.Product, icondesc);
            //DRod._iconSprite = iconEptD;
            //Traverse.Create(DRod).Field("_iconSprite").SetValue(iconEptD);

            var icondesc2 = ProtoRegistry.GetDefaultIconDesc(new Color(1f, 1f, 1f), new Color(0f, 1f, 1f), new Color(0, 0.2f, 0.3f), new Color(0, 0.4f, 0.5f));
            ItemProto ARod = ProtoRegistry.RegisterItem(9452, "空的反物质燃料棒", "空的反物质燃料棒描述", "Assets/Recycle/EmptyAnti", 612 + pagenum * 1000, 30, EItemType.Product, icondesc2);
            //ARod._iconSprite = iconEptA;
            //Traverse.Create(ARod).Field("_iconSprite").SetValue(iconEptA);

            RecipeProto DRodRefill = ProtoRegistry.RegisterRecipe(458, ERecipeType.Assemble, 360, new int[] { 9451, 1121 }, new int[] { 1, 10 }, new int[] { 1802 }, new int[] { 1 }, "氘核燃料棒再灌注描述", 1416, 1611, "氘核燃料棒再灌注", "Assets/Recycle/DeutInject");
            DRodRefill.Explicit = true;
            //DRodRefill._iconSprite = iconDeutInject;
            //Traverse.Create(DRodRefill).Field("_iconSprite").SetValue(iconDeutInject);

            RecipeProto DRodRecipe = ProtoRegistry.RegisterRecipe(460, ERecipeType.Assemble, 720, new int[] { 1107, 1205 }, new int[] { 1, 1 }, new int[] { 9451 }, new int[] { 2 }, "空的氘核燃料棒描述", 1416, 611 + pagenum * 1000, "空的氘核燃料棒", "Assets/Recycle/EmptyDeut");
            //DRodRecipe._iconSprite = iconEptD;
            //Traverse.Create(DRodRecipe).Field("_iconSprite").SetValue(iconEptD);

            RecipeProto ARodRefill = ProtoRegistry.RegisterRecipe(459, ERecipeType.Assemble, 720, new int[] { 1122, 1120, 9452 }, new int[] { 6, 6, 1 }, new int[] { 1803 }, new int[] { 1 }, "反物质燃料棒再灌注描述", 1145, 1612, "反物质燃料棒再灌注", "Assets/Recycle/AntiInject");
            ARodRefill.Explicit = true;
            //ARodRefill._iconSprite = iconAntiInject;
            //Traverse.Create(ARodRefill).Field("_iconSprite").SetValue(iconAntiInject);

            RecipeProto ARodRecipe = ProtoRegistry.RegisterRecipe(461, ERecipeType.Assemble, 1440, new int[] { 1107, 1403 }, new int[] { 1, 1 }, new int[] { 9452 }, new int[] { 2 }, "空的反物质燃料棒描述", 1145, 612 + pagenum * 1000, "空的反物质燃料棒", "Assets/Recycle/EmptyAnti");
            //ARodRecipe._iconSprite = iconEptA;
            //Traverse.Create(ARodRecipe).Field("_iconSprite").SetValue(iconEptA);

            ProtoRegistry.RegisterString("不相符的物品gm", "Item not match.", "物品不相符。");
            return;

            var oriRecipe = LDB.recipes.Select(41);
            var oriItem = LDB.items.Select(1802);

            //D
            var DInjectRecipe = oriRecipe.Copy();
            var EptDRodRecipe = oriRecipe.Copy();
            var EptDRod = oriItem.Copy();

            DInjectRecipe.ID = 458;
            DInjectRecipe.Explicit = true;
            DInjectRecipe.Name = "氘核燃料棒再灌注";
            DInjectRecipe.name = "氘核燃料棒再灌注".Translate();
            DInjectRecipe.Description = "氘核燃料棒再灌注描述";
            DInjectRecipe.description = "氘核燃料棒再灌注描述".Translate();
            DInjectRecipe.Items = new int[] { 9451, 1121 };
            DInjectRecipe.ItemCounts = new int[] { 1, 10 };
            DInjectRecipe.Results = new int[] { 1802 };
            DInjectRecipe.ResultCounts = new int[] { 1 };
            DInjectRecipe.GridIndex = 1611;
            DInjectRecipe.TimeSpend = 360;
            //DInjectRecipe.SID = "2509";
            //DInjectRecipe.sid = "2509".Translate();
            Traverse.Create(DInjectRecipe).Field("_iconSprite").SetValue(iconDeutInject);
            DInjectRecipe.preTech = LDB.techs.Select(1416);


            EptDRodRecipe.ID = 460;
            EptDRodRecipe.Name = "空的氘核燃料棒";
            EptDRodRecipe.name = "空的氘核燃料棒".Translate();
            EptDRodRecipe.Description = "空的氘核燃料棒描述";
            EptDRodRecipe.description = "空的氘核燃料棒描述".Translate();
            EptDRodRecipe.Items = new int[] { 1107, 1205 };
            EptDRodRecipe.ItemCounts = new int[] { 1, 1 };
            EptDRodRecipe.Results = new int[] { 9451 };
            EptDRodRecipe.ResultCounts = new int[] { 2 };
            EptDRodRecipe.GridIndex = 611 + pagenum * 1000;
            EptDRodRecipe.TimeSpend = 720;
            Traverse.Create(EptDRodRecipe).Field("_iconSprite").SetValue(iconEptD);

            EptDRod.ID = 9451;
            EptDRod.Name = "空的氘核燃料棒";
            EptDRod.name = "空的氘核燃料棒".Translate();
            EptDRod.Description = "空的氘核燃料棒描述";
            EptDRod.description = "空的氘核燃料棒描述".Translate();
            EptDRod.GridIndex = 611 + pagenum * 1000;
            EptDRod.HeatValue = 0L;

            EptDRod.handcraft = EptDRodRecipe;
            EptDRod.handcrafts = new List<RecipeProto> { EptDRodRecipe };
            EptDRod.maincraft = EptDRodRecipe;
            EptDRod.recipes = new List<RecipeProto> { EptDRodRecipe };

            EptDRod.makes = new List<RecipeProto> { DInjectRecipe };
            Traverse.Create(EptDRod).Field("_iconSprite").SetValue(iconEptD);


            LDBTool.PostAddProto(ProtoType.Item, EptDRod);
            LDBTool.PostAddProto(ProtoType.Recipe, DInjectRecipe);
            LDBTool.PostAddProto(ProtoType.Recipe, EptDRodRecipe);

            oriItem.recipes.Add(DInjectRecipe);

        }


        void AddAntiRods()
        {
            return;
            var oriRecipe = LDB.recipes.Select(44);
            var oriItem = LDB.items.Select(1803);

            //D
            var AInjectRecipe = oriRecipe.Copy();
            var EptARodRecipe = oriRecipe.Copy();
            var EptARod = oriItem.Copy();

            AInjectRecipe.ID = 459;
            AInjectRecipe.Explicit = true;
            AInjectRecipe.Name = "反物质燃料棒再灌注";
            AInjectRecipe.name = "反物质燃料棒再灌注".Translate();
            AInjectRecipe.Description = "反物质燃料棒再灌注描述";
            AInjectRecipe.description = "反物质燃料棒再灌注描述".Translate();
            AInjectRecipe.Items = new int[] { 1122, 1120, 9452 };
            AInjectRecipe.ItemCounts = new int[] { 6, 6, 1 };
            AInjectRecipe.Results = new int[] { 1803 };
            AInjectRecipe.ResultCounts = new int[] { 1 };
            AInjectRecipe.GridIndex = 1612;
            AInjectRecipe.TimeSpend = 720;
            //AInjectRecipe.SID = "2509";
            //AInjectRecipe.sid = "2509".Translate();
            Traverse.Create(AInjectRecipe).Field("_iconSprite").SetValue(iconAntiInject);
            AInjectRecipe.preTech = LDB.techs.Select(1145);

            EptARodRecipe.ID = 461;
            EptARodRecipe.Name = "空的反物质燃料棒";
            EptARodRecipe.name = "空的反物质燃料棒".Translate();
            EptARodRecipe.Description = "空的反物质燃料棒描述";
            EptARodRecipe.description = "空的反物质燃料棒描述".Translate();
            EptARodRecipe.Items = new int[] { 1107, 1403 };
            EptARodRecipe.ItemCounts = new int[] { 1, 1 };
            EptARodRecipe.Results = new int[] { 9452 };
            EptARodRecipe.ResultCounts = new int[] { 2 };
            EptARodRecipe.GridIndex = 612 + pagenum * 1000;
            EptARodRecipe.TimeSpend = 1440;
            Traverse.Create(EptARodRecipe).Field("_iconSprite").SetValue(iconEptA);


            EptARod.ID = 9452;
            EptARod.Name = "空的反物质燃料棒";
            EptARod.name = "空的反物质燃料棒".Translate();
            EptARod.Description = "空的反物质燃料棒描述";
            EptARod.description = "空的反物质燃料棒描述".Translate();
            EptARod.GridIndex = 612 + pagenum * 1000;
            EptARod.HeatValue = 0L;

            EptARod.handcraft = EptARodRecipe;
            EptARod.handcrafts = new List<RecipeProto> { EptARodRecipe };
            EptARod.maincraft = EptARodRecipe;
            EptARod.recipes = new List<RecipeProto> { EptARodRecipe };

            EptARod.makes = new List<RecipeProto> { AInjectRecipe };
            Traverse.Create(EptARod).Field("_iconSprite").SetValue(iconEptA);


            LDBTool.PostAddProto(ProtoType.Item, EptARod);
            LDBTool.PostAddProto(ProtoType.Recipe, AInjectRecipe);
            LDBTool.PostAddProto(ProtoType.Recipe, EptARodRecipe);

            oriItem.recipes.Add(AInjectRecipe);

        }



        void AddTranslateDInj()
        {
            StringProto recipeName = new StringProto();
            StringProto desc = new StringProto();
            recipeName.ID = 10559;
            recipeName.Name = "氘核燃料棒再灌注";
            recipeName.name = "氘核燃料棒再灌注";
            recipeName.ZHCN = "氘核燃料棒再灌注";
            recipeName.ENUS = "Deuteron fuel rod reperfusion";
            recipeName.FRFR = "Deuteron fuel rod reperfusion";

            desc.ID = 10560;
            desc.Name = "氘核燃料棒再灌注描述";
            desc.name = "氘核燃料棒再灌注描述";
            desc.ZHCN = "使用重氢填充空的氘核燃料棒。";
            desc.ENUS = "Fill empty deuteron fuel rods with deuterium.";
            desc.FRFR = "Fill empty deuteron fuel rods with deuterium.";


            LDBTool.PreAddProto(ProtoType.String, recipeName);
            LDBTool.PreAddProto(ProtoType.String, desc);
        }

        void AddTranslateEptD()
        {
            StringProto itemName = new StringProto();
            StringProto desc2 = new StringProto();

            itemName.ID = 10561;
            itemName.Name = "空的氘核燃料棒";
            itemName.name = "空的氘核燃料棒";
            itemName.ZHCN = "空的氘核燃料棒";
            itemName.ENUS = "Empty deuteron fuel rod";
            itemName.FRFR = "Empty deuteron fuel rod";

            desc2.ID = 10562;
            desc2.Name = "空的氘核燃料棒描述";
            desc2.name = "空的氘核燃料棒描述";
            desc2.ZHCN = "这有啥可描述的？它本来是个氘核燃料棒，然后用光了……就变成这样了。";
            desc2.ENUS = "It was originally a deuteron fuel rod, and then ran out, and it became like this:)";
            desc2.FRFR = "It was originally a deuteron fuel rod, and then ran out, and it became like this:)";

            LDBTool.PreAddProto(ProtoType.String, itemName);
            LDBTool.PreAddProto(ProtoType.String, desc2);
        }

        void AddTranslateAInj()
        {
            StringProto recipeName = new StringProto();
            StringProto desc = new StringProto();
            recipeName.ID = 10563;
            recipeName.Name = "反物质燃料棒再灌注";
            recipeName.name = "反物质燃料棒再灌注";
            recipeName.ZHCN = "反物质燃料棒再灌注";
            recipeName.ENUS = "Anitimatter fuel rod reperfusion";
            recipeName.FRFR = "Anitimatter fuel rod reperfusion";

            desc.ID = 10564;
            desc.Name = "反物质燃料棒再灌注描述";
            desc.name = "反物质燃料棒再灌注描述";
            desc.ZHCN = "使用氢和反物质氢装填空的反物质燃料棒。";
            desc.ENUS = "Use hydrogen and antimatter hydrogen to fill empty antimatter fuel rods.";
            desc.FRFR = "Use hydrogen and antimatter hydrogen to fill empty antimatter fuel rods.";


            LDBTool.PreAddProto(ProtoType.String, recipeName);
            LDBTool.PreAddProto(ProtoType.String, desc);
        }

        void AddTranslateEptA()
        {
            StringProto itemName = new StringProto();
            StringProto desc2 = new StringProto();
            StringProto tabname = new StringProto();

            itemName.ID = 10565;
            itemName.Name = "空的反物质燃料棒";
            itemName.name = "空的反物质燃料棒";
            itemName.ZHCN = "空的反物质燃料棒";
            itemName.ENUS = "Empty antimatter fuel rods";
            itemName.FRFR = "Empty antimatter fuel rods";

            desc2.ID = 10566;
            desc2.Name = "空的反物质燃料棒描述";
            desc2.name = "空的反物质燃料棒描述";
            desc2.ZHCN = "这是一个空的，反物质燃料棒。";
            desc2.ENUS = "This is an empty, anti-matter fuel rod. ";
            desc2.FRFR = "This is an empty, anti-matter fuel rod. ";

            tabname.ID = 10567;
            tabname.Name = "FractionateTab";
            tabname.name = "FractionateTab";
            tabname.ZHCN = "分馏";
            tabname.ENUS = "Fract/Rods";
            tabname.FRFR = "Fract/Rods";


            LDBTool.PreAddProto(ProtoType.String, itemName);
            LDBTool.PreAddProto(ProtoType.String, desc2);
            LDBTool.PreAddProto(tabname);
        }
    }

    /*
    //根据RecycleAccumulator by tanukinomori修改 但是我没学会，改了也没实现功能，难受
    [HarmonyPatch(typeof(Mecha), "GenerateEnergy")]
    class Patch_Mecha_GenerateEnergy
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.
                MatchForward(true,
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Mul),
                    new CodeMatch(OpCodes.Stloc_S),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Ble_Un),
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Stloc_S),
                    new CodeMatch(OpCodes.Br),
                    new CodeMatch(OpCodes.Ldc_I4_0));// 94
            var bakOpcode = matcher.Opcode;
            var bakOperand = matcher.Operand;
            matcher.
                SetAndAdvance(OpCodes.Ldarg_0, null).
                InsertAndAdvance(Transpilers.EmitDelegate<Action<Mecha>>(
                    mecha => {
                        if (mecha.reactorItemId == 0)
                        {
                            Console.WriteLine("ZERO ID, NOW RETURN");
                            return;
                        }
                        if (!RecyclableFuelRod.OriRods.Contains(mecha.reactorItemId))
                        {
                            Console.WriteLine("NOT CONTAINED, NOW RETURN");
                            return;
                        }
                        Console.WriteLine("MECH return activated");
                        int v;
                        int outinc;
                        if ((v = mecha.player.package.AddItemStacked(mecha.reactorItemId - 1802 + 9451, 1, mecha.reactorItemInc, out outinc)) != 0)
                        {
                            UIItemup.Up(mecha.reactorItemId - 1802 + 9451, v);
                        }
                    })).
                InsertAndAdvance(new CodeInstruction(bakOpcode, bakOperand));
            return matcher.InstructionEnumeration();
        }
    }
    */

}