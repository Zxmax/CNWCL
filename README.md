# CNWCL 是一个分析wcl日志的网站
[在线使用](http://120.26.47.184/)

## 使用说明:
### 第一步
在页面中输入wcl的某次活动的报告ID,

比如 “https://cn.warcraftlogs.com/reports/ntc2pvV4CaPL9RNM” 中的 “ntc2pvV4CaPL9RNM” 就是报告ID,

然后敲下回车等待页面刷新会得到下面的页面:

![fights](https://github.com/Zxmax/CNWCL/tree/master/CNWCL/Photo/1.jpg?raw=true)

### 第二步
第一步得到的页面中显示了本次活动的所有boss尝试以及击杀，找到想要数据分析的战斗，然后有两个可以使用的功能：

#### 角色技能分析:
此项功能会跳转到选择角色的页面，会列出参与此次战斗的成员，鼠标移动到想要查看的成员，然后选择分析分析该角色就可以跳转到分析页面

![friends](https://github.com/Zxmax/CNWCL/tree/master/CNWCL/Photo/3.jpg?raw=true)

分析页面包括了该名角色的技能施法数量以及占比，然后还提供了wcl前100与他天赋盟约相同的第一名的施法数量以及占比供参考

![friend](https://github.com/Zxmax/CNWCL/tree/master/CNWCL/Photo/2.jpg?raw=true)

#### 战斗爆发以及boss关键技能分析:
此项功能能够列出此次战斗中boss的技能释放时间以及所有团员的爆发技能，团减技能，自保技能，功能性技能的施放时间，导出excel之后可以自行整出时间轴。

主要用处就是选择一个和自己团队差不多的配置的log，然后可以参考这个log来进行副本的开荒，节省下减伤等技能的分配磨合。

点击此功能之后还需要输入一个“请输入要统计的最短技能的cd(秒)”，这个代表了要统计的多少cd的爆发技能，这边推荐是90s，就会统计大部分职业的爆发了，如果需要统计60s一次的爆发就输入60s。

![time](https://github.com/Zxmax/CNWCL/tree/master/CNWCL/Photo/4.jpg?raw=true)

最后得到的效果图如下:

![erupt](https://github.com/Zxmax/CNWCL/tree/master/CNWCL/Photo/5.jpg?raw=true)



