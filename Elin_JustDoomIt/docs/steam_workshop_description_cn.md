### Just Doom It

在Fortune Bell接待处旁边放了一台能玩DOOM的街机。

每次 `START OVER` / `CONTINUE` 时只支付一次固定入场费 `100` 来开启奖励会话。
击杀和秘密奖励会立刻到账；基础每杀奖励 `70` 会随着连杀每次 `+35` 递增。
每杀奖励上限取决于难度；受伤不会扣已获得奖励，只会重置连杀奖励；通关额外 `+1000`。
同一会话后续地图不会再次收费。死亡或按 `ESC` 中断时，也不会再额外结算或没收奖励。
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

- DOOM兼容引擎：[ManagedDoom](https://github.com/sinshu/managed-doom)
- 游戏数据：[FreeDoom](https://freedoom.github.io/) 的 `freedoom1.wad`
- 街机柜通过CWL（Custom Whatever Loader）作为自定义物品添加

#### 许可协议

- ManagedDoom：GPLv2 或更高版本
- FreeDoom (`freedoom1.wad`)：BSD 3-Clause 协议

分发包内附带 `LICENSES/` 文件夹，包含以下文件：

- LICENSES/FreeDoom-BSD-3-Clause.txt
- LICENSES/FreeDoom-CREDITS.txt
- ManagedDoom-GPL-2.0.txt

所有附带资源均在各自许可协议允许的范围内进行再分发。
衷心感谢各项目的开发者和社区将这些优秀资产开源共享。
详情请参阅附带的 README.md。
