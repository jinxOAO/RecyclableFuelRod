using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UITools;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableFuelRod
{
    public class UIPatchers
    {
        public static GameObject usedFuelObj = null;
        public static GameObject emptyRodIconObj = null;
        public static Image emptyRodIcon = null;
        public static Text emptyRodCount = null;
        public static UIButton emptyRodUIButton = null;
        public static Button emptyRodButton = null;

        public static void InitAll()
        {
            if (usedFuelObj == null)
            {
                Transform grandParent = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2").transform;
                usedFuelObj = new GameObject();
                usedFuelObj.name = "usedFuel";
                usedFuelObj.transform.SetParent(grandParent, false);
                usedFuelObj.AddComponent<Image>();
                usedFuelObj.GetComponent<Image>().sprite = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel").GetComponent<Image>().sprite;
                usedFuelObj.GetComponent<Image>().color = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel").GetComponent<Image>().color;
                usedFuelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
                usedFuelObj.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                usedFuelObj.transform.localPosition = new Vector3(68, -56, 0);
                Transform parent = usedFuelObj.transform;

                GameObject oriIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel/fuel-icon");
                emptyRodIconObj = GameObject.Instantiate(oriIconObj, parent);
                emptyRodIconObj.transform.localScale = Vector3.one;
                emptyRodIcon = emptyRodIconObj.GetComponent<Image>();
                emptyRodIcon.enabled = true;
                emptyRodCount = emptyRodIconObj.transform.Find("cnt-text").GetComponent<Text>();
                emptyRodCount.fontSize = 16; // 12被缩放到0.75后反向改字号缩放回原本大小

                GameObject oriButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel/button");
                GameObject emptyRodButtonObj = GameObject.Instantiate(oriButtonObj, parent);
                emptyRodButtonObj.transform.localScale = Vector3.one;
                emptyRodUIButton = emptyRodButtonObj.GetComponent<UIButton>();
                emptyRodButton = emptyRodButtonObj.GetComponent<Button>();
                emptyRodButton.onClick.RemoveAllListeners();
                emptyRodButton.onClick.AddListener(() => { OnEmptyRodIconClick(); });
            }

        }


        // 为了让鼠标悬停的显示包括空棒
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityBriefInfo), "SetBriefInfo")]
        public static void SetBriefInfoPostPatch(ref EntityBriefInfo __instance, PlanetFactory _factory, int _entityId)
        {
            if (_factory == null)
            {
                return;
            }
            if (_entityId == 0)
            {
                return;
            }
            EntityData[] entityPool = _factory.entityPool;
            InserterComponent[] inserterPool = _factory.factorySystem.inserterPool;
            PowerSystem powerSystem = _factory.powerSystem;
            CargoTraffic cargoTraffic = _factory.cargoTraffic;
            VeinData[] veinPool = _factory.veinPool;
            EntityData entityData = entityPool[_entityId];
            if (entityData.id == 0)
            {
                return;
            }
            if (entityData.powerGenId > 0)
            {
                PowerGeneratorComponent[] genPool = powerSystem.genPool;
                int powerGenId2 = entityData.powerGenId;
                if (RecyclableFuelRod.EmptyRods.Contains((int)genPool[powerGenId2].productHeat))
                {
                    __instance.storage.Add((int)genPool[powerGenId2].productHeat, (int)genPool[powerGenId2].productCount, 0);
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "OnGeneratorIdChange")]
        public static void UIPowerGenIdChangePatch(ref UIPowerGeneratorWindow __instance)
        {
            try
            {
                if (__instance.generatorId == 0 || __instance.factory == null)
                {
                    return;
                }
                PowerGeneratorComponent powerGeneratorComponent = __instance.powerSystem.genPool[__instance.generatorId];
                if (powerGeneratorComponent.id != __instance.generatorId)
                {
                    return;
                }
                int entityId = __instance.powerSystem.genPool[__instance.generatorId].entityId;
                int generatorProtoId = __instance.factory.entityPool[entityId].protoId;
                if (RecyclableFuelRod.RelatedGenerators.Contains(generatorProtoId))
                    usedFuelObj.SetActive(true);
                else
                    usedFuelObj.SetActive(false);
            }
            catch (Exception)
            { }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "_OnUpdate")]
        public static void UIPowerGenWinOnUpdatePatch(ref UIPowerGeneratorWindow __instance)
        {

            if (usedFuelObj.activeSelf)
            {
                var _this = __instance;
                if (_this == null)
                {
                    return;
                }
                if (_this.generatorId == 0 || _this.factory == null)
                {
                    return;
                }
                PowerGeneratorComponent powerGeneratorComponent = _this.powerSystem.genPool[_this.generatorId];
                if (powerGeneratorComponent.id != _this.generatorId)
                {
                    return;
                }
                ItemProto generatorProto = LDB.items.Select((int)_this.factory.entityPool[powerGeneratorComponent.entityId].protoId);
                if (generatorProto == null)
                {
                    return;
                }
                PowerNetwork powerNetwork = _this.powerSystem.netPool[powerGeneratorComponent.networkId];
                _this.powerNetworkDesc.powerNetwork = powerNetwork;
                Assert.NotNull(powerNetwork);
                if (powerNetwork == null)
                {
                    return;
                }
                
                if (powerGeneratorComponent.productHeat > 0)
                {
                    emptyRodIconObj.SetActive(true);
                    ItemProto itemProto = LDB.items.Select((int)powerGeneratorComponent.productHeat);
                    emptyRodIcon.sprite = itemProto.iconSprite;
                    emptyRodCount.text = ((int)powerGeneratorComponent.productCount).ToString();
                    emptyRodUIButton.tips.itemId = itemProto.ID;
                    emptyRodUIButton.tips.itemCount = (int)powerGeneratorComponent.productCount;
                }
                else
                {
                    emptyRodIconObj.SetActive(false);
                    emptyRodCount.text = "";
                    emptyRodUIButton.tips.itemId = 0;
                    emptyRodUIButton.tips.itemCount = 0;
                }
                emptyRodUIButton.tips.itemInc = 0;
                emptyRodUIButton.tips.type = UIButton.ItemTipType.Item;
            }
        }


        public static void OnEmptyRodIconClick()
        {
            UIPowerGeneratorWindow _this = UIRoot.instance.uiGame.generatorWindow;
            if (_this.generatorId == 0 || _this.factory == null || _this.player == null)
            {
                return;
            }
            PowerGeneratorComponent[] genPool = _this.powerSystem.genPool;
            int generatorId = _this.generatorId;
            PowerGeneratorComponent powerGeneratorComponent = _this.powerSystem.genPool[_this.generatorId];
            if (powerGeneratorComponent.id != _this.generatorId)
            {
                return;
            }
            if (powerGeneratorComponent.useFuelPerTick == 0L)
            {
                return;
            }
            int[] fuelArray = ItemProto.fuelNeeds[(int)powerGeneratorComponent.fuelMask];
            int[] array = new int[] { 9451 };
            if (fuelArray == null)
            {
                return;
            }
            for (int i = 0; i < fuelArray.Length; i++) 
            {
                if (fuelArray[i] == 1803 && RecyclableFuelRod.OriRods.Contains(1803))
                    array[i] = 9452; 
            }
            if (_this.player.inhandItemId > 0 && _this.player.inhandItemCount == 0)
            {
                _this.player.SetHandItems(0, 0, 0);
                return;
            }
            if (_this.player.inhandItemId > 0 && _this.player.inhandItemCount > 0)
            {
                if (_this.player.inhandItemId == 1000)
                {
                    return;
                }
                bool flag = false;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == _this.player.inhandItemId)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    UIRealtimeTip.Popup("错误燃料".Translate(), true, 0);
                    return;
                }
                if (_this.player.inhandItemId == (int)powerGeneratorComponent.productHeat || powerGeneratorComponent.productHeat == 0)
                {
                    int num = (int)(100 - powerGeneratorComponent.productCount);
                    if (num < 0)
                    {
                        num = 0;
                    }
                    int num2 = (_this.player.inhandItemCount < num) ? _this.player.inhandItemCount : num;
                    if (num2 <= 0)
                    {
                        UIRealtimeTip.Popup("栏位已满".Translate(), true, 0);
                        return;
                    }
                    int inhandItemCount = _this.player.inhandItemCount;
                    int inhandItemInc = _this.player.inhandItemInc;
                    int num3 = num2;
                    int num4 = inhandItemCount == 0 ? 0 : (int)(inhandItemInc / inhandItemCount);
                    if (powerGeneratorComponent.productHeat == 0)
                    {
                        genPool[generatorId].productHeat = _this.player.inhandItemId;
                    }
                    genPool[generatorId].productCount = genPool[generatorId].productCount + (short)num3; // 这里不能直接用powerGeneratorComponent，因为他是struct，没办法直接改到原本的
                    
                    _this.player.AddHandItemCount_Unsafe(-num3);
                    _this.player.SetHandItemInc_Unsafe(_this.player.inhandItemInc - num4);
                    if (_this.player.inhandItemCount <= 0)
                    {
                        _this.player.SetHandItemId_Unsafe(0);
                        _this.player.SetHandItemCount_Unsafe(0);
                        _this.player.SetHandItemInc_Unsafe(0);
                        return;
                    }
                }
                else
                {
                    if (_this.player.inhandItemCount > 100)
                    {
                        UIRealtimeTip.Popup("不相符的物品gm".Translate(), true, 0);
                        return;
                    }
                    _this.player.SetHandItemId_Unsafe(0);
                    _this.player.SetHandItemCount_Unsafe(0);
                    _this.player.SetHandItemInc_Unsafe(0);
                    if (VFInput.shift || VFInput.control)
                    {
                        int upCount = _this.player.TryAddItemToPackage((int)powerGeneratorComponent.productHeat, (int)powerGeneratorComponent.productCount, 0, false, 0);
                        UIItemup.Up((int)powerGeneratorComponent.productHeat, upCount);
                        genPool[generatorId].productHeat = 0;
                        genPool[generatorId].productCount = 0;
                        return;
                    }
                    _this.player.SetHandItemId_Unsafe((int)powerGeneratorComponent.productHeat);
                    _this.player.SetHandItemCount_Unsafe((int)powerGeneratorComponent.productCount);
                    _this.player.SetHandItemInc_Unsafe(0);
                    genPool[generatorId].productHeat = 0;
                    genPool[generatorId].productCount = 0;
                    return;
                }
            }
            else if (_this.player.inhandItemId == 0 && _this.player.inhandItemCount == 0)
            {
                if (powerGeneratorComponent.productHeat == 0)
                {
                    return;
                }
                if (VFInput.shift || VFInput.control)
                {
                    int upCount2 = _this.player.TryAddItemToPackage((int)powerGeneratorComponent.productHeat, (int)powerGeneratorComponent.productCount, 0, false, 0);
                    UIItemup.Up((int)powerGeneratorComponent.productHeat, upCount2);
                    genPool[generatorId].productHeat = 0;
                    genPool[generatorId].productCount = 0;
                    return;
                }
                _this.player.SetHandItemId_Unsafe((int)powerGeneratorComponent.productHeat);
                _this.player.SetHandItemCount_Unsafe((int)powerGeneratorComponent.productCount);
                _this.player.SetHandItemInc_Unsafe(0);
                genPool[generatorId].productHeat = 0;
                genPool[generatorId].productCount = 0;
            }
        }
    }
}
