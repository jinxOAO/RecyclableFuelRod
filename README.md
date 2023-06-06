# Recyclable Fuel Rods

After a mini fusion power station burning a deuteron fuel rod (or an artificial star burning an antimatter fuel rod), it will produce an empty deuteron fuel rod (or an empty antimatter fuel rod) back. You can take them out by sorters(no need to set a filter). And you can use that to make new full rod by only adding fuels in empty rods. Also works on mecha. 

New recipes:   
Deuteron fuel rod reperfusion: 1 Empty deuteron fuel rod + 10 Deuterium -> 1 Deuteron fuel rod   (in page 1 by default)  
Antimatter fuel rod reperfusion: 1 Empty antimatter fuel rod + 6 Antimatter + 6 Hydrogen -> 1 Antimatter fuel rod   (in page 1 by default)    
1 Titanium alloy + 1 Super-magnetic ring -> 2 Empty dueteron fuel rod    (in page 3 by default)   
1 Titanium alloy + 1 Annihilation constraint sphere -> 2 Empty Animatter fuel rod   (in page 3 by default)    

This mod won't work on Hydrogen fuel rods. 

### Installation (If Manually)

1. Install BepInEx.   
2. Install LDBTool and CommonAPI. (You must have this mod!) Thanks to xiaoye97 for providing such convenient tools.   
3. Drag RecyclableFuelRod.dll into "Dyson Sphere Program/BepInEx/plugins/"   
4. Drag "recycleicons" into the same folder of the RecyclableFuelRod.dll  

### Mod conflicts

Problems may arise when other mods that create new items/recipes use the following IDs:   
RecipeProto.ID: 458,459,460,461   
ItemProto.ID: 9451,9452   
StringProto.ID: 10559 - 10567   

Problems may arise when other mods that create new items/recipes use the following GridIndex:   
1606,1607 / 1611,1612 / 3611,3612   

### ChangeLog

v2.0.0: 
 - Rewrite the main logic. Now there are no more restrictions on how you place your power generators. You can put it side by side if you want, line them up with sorters, just like the vanilla game. And the sorter that connects 2 generators will automatically choose which rod(full or empty) to deliver. Of cource you can also set a filter to control the sorters if you want.
 - Now the full rods and empty rods can be stored in the generator at the same time, and you can see/take out/put in them on the UI window. The amount of fuel that can be stored in each power station has been restored to match the original limit of the vanilla game.  


<details>
  <summary> Click to view all changelog </summary>  

the versions before v1.1.9 have some limitations when you build the generators:  
--After installing this mod, the mini fusion power station and artificial star can only storage one fuel rod (full or empty) now (if by sorter). If you put more than one rods by your hand, the full-fuel rods will not become empty rods except for the last one.   
--In that case, you can no longer build "mini fusion power stations or artificial stars" side by side (Does not affect thermal power stations). Because these two kinds of power stations can only get fuel from the conveyor belt, but not from other power stations. And in order to take out the empty fuel rods, you also need to build conveyor belts to transport the empty fuel rods away (but not necessary, see the next note) .   
--To prevent the power station from being unable to receive new fuel rod due to the blockage of empty fuel rods, If a power station ran out off fuel inside, and there is still empty rod which have not been taken away, The power station will still accept new full-fuel rod, and the original empty rod will disappear. 

v1.1.8 and v1.1.9: Use CommonAPI instead of MoreProtoPages to select the empty rods. The MoreProtoPages mod has been deprecated.

v1.1.7: Add the recipe to make the empty rod. You can see it in the 3rd page if you install the MoreProtoPages mod.   
          Fix the problem that you can't find empty rods in ILS or PLS. Now you can select them in the 3rd page.    
          Reduced time consumption of reperfusion recipes to match the vanilla recipes.  

v1.1.6: Now the mecha will return an empty rod when it burns out a fuel rod. Fix two description text errors.

v1.1.5: Update for game version 0.9.24.11182. (2022-1-20). After you update this mod, you MUST DELETE the temp fix .dll file, if you've downloaded it from my github. (named RFRTempPatch.dll) Because this two dll files are in conflict with each other.

v1.1.4: Modify the recipe of Antimatter fuel rod reperfusion(Antimatter & Hydrogen: 10 to 6) to make it consistent with the game update. 

v1.1.3: Correct a description text error. 

v1.1.2: Fix a bug that you can not select the empty rods as cargo in the ILS or PLS. 

v1.1.0 & v1.1.1: Fix a bug that may cause errors and abnormal performance when manually crafting deuteron fuel rods. 

v1.0.1: Fix a bug that may cause the abnormal consumption speed (5x) of the fuel rods. 
</details>

# 可回收燃料棒

在一个迷你聚变发电站（或小太阳）开始烧一个氘核燃料棒（或一个反物质燃料棒）时，它会产出一个空的氘核燃料棒（或一个空的反物质燃料棒）。这个空燃料棒可以被分拣器取出。你可以用这个空的燃料棒，通过反复添加燃料制作新的燃料棒循环使用。对放入机甲的燃料棒也有效。

新配方如下：   
氘核燃料棒再灌注：1空的氘核燃料棒 + 10重氢 -> 1氘核燃料棒   （默认在第一页）  
反物质燃料棒再灌注：1空的反物质燃料棒 + 6反物质 + 6氢气 -> 1反物质燃料棒    （默认在第一页）  
1钛合金 + 1超级磁场环 -> 2空的氘核燃料棒  （默认在第三页）  
1钛合金 + 1湮灭约束球 -> 2空的反物质燃料棒  （默认在第三页）  

--这个mod对液氢燃料棒不起作用   
 

### 安装（如果手动安装）

1. 安装 BepInEx框架。   
2. 安装 LDBTool和CommonAPI（必须安装这个mod）感谢宵夜97提供了方便的工具。   
3. 将RecyclableFuelRod.dll放入 "Dyson Sphere Program/BepInEx/plugins/"文件夹内    
4. 将recycleicons文件拖入与RecyclableFuelRod.dll同一个文件夹  

### Mod冲突

当其他创造新物品/配方的mod使用了以下ID时，可能会产生问题：   
RecipeProto.ID: 458,459,460,461   
ItemProto.ID: 9451,9452   
StringProto.ID: 10559 - 10567   
当其他创造新物品/配方的mod使用了以下位置时，可能会产生问题：   
1606,1607 / 1611,1612 / 3611,3612   

### 更新

v2.0.0: 
 - 重写了主要逻辑，移除了所有摆放发电站的限制。现在你可以把他们并排连接、用分拣器串成一串等等随便摆放。连接两个发电站的分拣器将自动选择运送的燃料棒类型（空的或者满的），无需设置过滤物品（当然你想设置也可以）。  
 - 现在空棒和满棒可以同时存储在发电站中，你还可以在UI面板里面看到他们的数量，取出或放入这些物品。发电站可储存的燃料数量上限也恢复成了和原本游戏的限制一致。 

<details>
  <summary> 点击查看全部更新历史 </summary>  

在v1.1.9以前的版本，使用这个mod有诸多限制：  
--安装这个mod后，迷你聚变发电站和小太阳最多只能储存一个燃料棒（或一个空的燃料棒），如果是被分拣器抓进来的话。如果你非要手动一下子塞入多个燃料棒，那么这些燃料棒都不能产生空的燃料棒，除了最后一个。   
--在这种情况下，你不再能并排建造迷你聚变发电站和小太阳（不影响燃烧发电站），因为这两种发电站都只能从传送带上获取燃料，而不能从其他发电站获取燃料。而且为了取出空的燃料棒，你还需要配置一条输出传送带将空的燃料棒运走（但不是必须的，见下一条）。   
--为了防止空燃料棒堵塞导致发电站无法接受新的燃料棒，如果发电站内部的燃料被消耗干净，而空燃料棒仍然没有被运走，发电站还是会接受新的满燃料棒，但是上次的空燃料棒会消失掉。  

v1.1.8 - v1.1.9: 使用CommonAPI来选择空的燃料棒。原本的mod MoreProtoPages已被弃用。

v1.1.7: 新增了制造空燃料棒的配方，他们默认在第三页，因此你需要安装MoreProtoPages mod来看到他们。  
          修复了物流塔中找不到空燃料棒的问题，现在你可以在第三页选择空燃料棒来作为物流塔的货物。  
          减少再灌注配方的时间消耗来与游戏本体配方相匹配。  

v1.1.6: 现在，机甲烧尽燃料棒后也会返还一个空的燃料棒。更正了两处表述文本的错误。

v1.1.5: 更新以适配游戏版本0.9.24.11182（游戏更新于2022-1-20）请注意，如果你之前从我的github下载过这个mod的临时修复版本，请务必删除那个dll文件（名字叫RFRTempPatch.dll）！！！因为二者是冲突的。

v1.1.4: 修改反物质燃料棒再灌注的配方（反物质和氢气消耗10 -> 6），使其与游戏更新一致。

v1.1.3: 更正一个描述文本错误。

v1.1.2: 修复一个bug，该bug曾导致你无法在物流站里面选择空的燃料棒。

v1.1.0 & v1.1.1: 修复一个bug，该bug曾导致手动合成氘核燃料棒时的不正确表现，以及ArgumentOutOfRange（曾与QTools不兼容，目前已修复）或IndexOutOfRange报错。

v1.0.1: 修复一个bug，该bug曾导致发电站以5倍的速度消耗燃料棒。
</details>