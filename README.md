# GenVoxel

### 什么是GenVoxel
  * GenVoxel是在Unity3D生成体素地形（类Minecraft）的工具。它主要由两部分组成：
    * 1.体素地形管理器(Component)（以下有图）
    * 2.地形生成规则编辑器(Editor)
### GenVoxel功能
  * 1.体素地形数据到网格数据的转换
    * 需要将体素地形数据转换为网格（Mesh)才能显示在计算机。采用的是[GreedyMeshing](https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/)算法
    来网格化体素数据并简化网格（[简化演示](http://www.gedge.ca/dev/2014/08/17/greedy-voxel-meshing)）。
    * 体素地形动态加载
      * 因为GreedyMeshing一个chunk是一个重型操作（GreedyMeshing一个chunk需要60ms,主频2.4GHZ），而且一次需要加载多个（默认为初始加载256个）。所以使用
    多线程来进行优化。（以下有图）
    * 体素地形动态修改
      * 实现了简单的对地形进行修改的功能（增/删voxel）。（以下有图）
    * 体素地形存储/读取
      * 实现了简单的体素地形存储/读取功能。所有生成的体素地形都被存储进入一个二进制文件（扩展名为.wt）。当加载体素地形时，首先检索.wt文件，检索成功则
      读取数据，若检索失败，则调用地形生成规则生成新的地形。
    
### 当前版本
