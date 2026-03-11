### Just Doom It

在Fortune Bell接待处旁边放了一台能玩DOOM的街机。

每张地图开始时选择 `LOW / MID / HIGH` 的 RATE。
`LOW` 偏稳定，`MID` 偏标准，`HIGH` 是适合长图无伤连胜的大倍率梦想档。
击杀奖励不会立刻到账，而是累积进本地图专用的未兑现奖池；受伤会损失部分奖池并重置连杀加成，只有通关时才会 `CASH OUT`。
发现秘密时，也会向同一个未兑现奖池固定追加 `+500`。
死亡时会兑现本地图的未兑现奖池，而按 `ESC` 中断时仍会全部损失。
简单来说，这是一台杀气很重的老虎机。
你可以去赌场玩，也可以把它偷回家，放牧场里、卧室里，随你。

支持 JP / EN / CN

#### 操作方式

- 移动：`W / A / S / D`（方向键也行）
- 瞄准 / 射击：`鼠标移动` / `左键`
- 使用 / 开门：`Space` / `E`
- 冲刺：`Shift`
- 切换武器：`1 - 7` / `鼠标滚轮`
- 退出：`ESC`

#### 常见问题

Q. 卸载安全吗？
A. 安全。只影响自定义物品，卸载后街机会变成炼金灰。

Q. 为什么？
A. 治疗二十一点读档疲劳的特效药。

Q. 还能回去种田吗？
A. 开过BFG之后再拿锄头，总觉得少了点什么。

#### 技术组成

- DOOM兼容引擎：[DoomNetFrameworkEngine](https://github.com/mahach666/DoomNetFrameworkEngine)
- 游戏数据：[FreeDoom](https://freedoom.github.io/) 的 `freedoom1.wad`
- 街机柜通过CWL（Custom Whatever Loader）作为自定义物品添加

#### 许可协议

- DoomNetFrameworkEngine：MIT 协议
- FreeDoom (`freedoom1.wad`)：BSD 3-Clause 协议

分发包内附带 `LICENSES/` 文件夹，包含以下文件：

- LICENSES/FreeDoom-BSD-3-Clause.txt
- LICENSES/FreeDoom-CREDITS.txt
- DoomNetFrameworkEngine-MIT.txt

所有附带资源均在各自许可协议允许的范围内进行再分发。
衷心感谢各项目的开发者和社区将这些优秀资产开源共享。
详情请参阅附带的 README.md。
