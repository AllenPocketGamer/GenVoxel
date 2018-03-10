# GenVoxel
### 什么是GenVoxel
* GenVoxel是在Unity3D生成体素地形（类Minecraft）的工具。它主要由两部分组成：
    * 1.体素地形管理器(Component)
        * ![GenVoxel Manager](https://github.com/AllenPocketGamer/GenVoxel/blob/master/Temp%20Show%20Imgs/VoxManager.PNG)
    * 2.地形生成规则编辑器(Editor)
* 地形生成规则编辑器 用于生成 定制的 地形生成规则 资源。然后将定制的资源赋予 体素地形管理器 来生成 定制的体素地形。
### 体素地形管理器 功能
* 1.体素地形数据到网格数据的转换
    * 需要将体素地形数据转换为网格（Mesh)才能显示在计算机。采用的是[GreedyMeshing](https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/)算法
    来网格化体素数据并简化网格（[简化演示](http://www.gedge.ca/dev/2014/08/17/greedy-voxel-meshing)）
* 2.体素地形动态加载
    * 因为GreedyMeshing一个chunk是一个重型操作（GreedyMeshing一个chunk需要60ms,主频2.4GHZ），而且一次需要加载多个（默认为初始加载256个）。所以使用多线程来进行优化。
    * ![Dynamic Load](https://github.com/AllenPocketGamer/GenVoxel/blob/master/Temp%20Show%20Imgs/dynamicLoad.gif)
* 3.体素地形动态修改
    * 实现了简单的对地形进行修改的功能（增/删voxel）。
    * ![Dynamic Modify](https://github.com/AllenPocketGamer/GenVoxel/blob/master/Temp%20Show%20Imgs/dynamicModify.gif)
* 4.体素地形存储/读取
    * 实现了简单的体素地形存储/读取功能。所有生成的体素地形都被存储进入一个二进制文件（扩展名为.wt）。当加载体素地形时，首先检索.wt文件，检索成功则读取数据，若检索失败，则调用地形生成规则生成新的地形。
    
### 地形生成规则编辑器 功能
* (地形生成规则编辑器 仍在完善，具体实现思路参考于 [Polygon Map Generation](http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/))
* (当前的地形生成规则 为 临时使用的 三重柏林噪声)
* 简要描述
  * 通过几个强约束参数（地质年份、海陆分布、水系分布等等）来生成较为真实的地形。
  * 大陆中心到海岸线的海拔将逐级下降，更符合真实情况。
  * 通过地区海拔和海陆分布来计算全球湿度分布和全球温度分度，进而决定生物群落分布。
  * 根据湿度、温度和海拔将更为真实的生成江、河、湖。
 
### 说明
 * 整个项目目前完成度仅有60%左右
 * 地形生成规则编辑器已有基本思路，即将完工
 * 最后演示Demo会使用卡通风格进行渲染
