https://www.criware.cn/public/upload/chm/CRIWARE_Unity_Plugin_Manual_smartphone_zh_public/atom4u_atomcompo.html

需要正确配置脚本的 Script Execution Order（脚本执行顺序） "Execution Order(执行顺序)"，才能让各个组件正常工作。

# CRI Atom Source
这是用作声源的对象。
可以从 Atom Browser window 中创建该对象。

创建CRI Atom Source的方法有以下两种。
根据不同情况，选择其中之一。
-1- 创建为单独的"游戏对象"（Game Object）。
-2- 可以以"组件"（Component）形式粘贴至另一个游戏对象。

# CRI Atom Listener（收听者）
需要设置音频的收听者对象，才能使用3D定位应用声音效果。
（不使用3D定位时，无需配置该对象。）

详细信息请参照 Cri Atom Listener 。

# CRI Atom
CRI Atom是用于加载诸如全局声音设置和Cue Sheet之类的数据的对象。
通过将其作为组件添加到场景中的对象，可以在Inspector上设置要加载到场景中的数据。

见表;

[备注]
通过在该对象中注册，可以在场景中使用多个Cue Sheet。

注意:
如果CRI Atom Source在CRI Atom处理开始前运行，则将由于在该时间参照Cue Sheet的原因导致无法正常运行。 在游戏场景中使用CRI Atom功能时，请执行以下步骤：
- 应用程序开始后，请将CRI Atom和CRI Atom Source配置到目标场景中。
- 设置Execution Order，在CriAtomSource.cs之前执行CriAtom.cs。
- 启用Don't Destroy On Load复选框，以在场景转换后仍然保持初始化状态。

# CRI Atom Server
CRI Atom Server是控制整个声音播放的对象。
使用ADX2播放声音时，需要将其配置在场景中，但是在初始化库时会自动将其创建为名为"CRIWARE"的对象，因此用户通常无须另行创建。

注意:
库已初始化时，则CRI Atom Server必须始终存在而不被销毁。
因此我们建议您进行以下设置之一，以防止破坏"CRIWARE"对象。
- 将 CRIWARE Library Initializer 和 CRI Atom 作为组件添加到名为"CRIWARE"的对象中，并启用"DontDestroyOnLoad"标记。

- 在游戏启动场景中调用以下处理。
/* Create CRI Atom Server instance. */
CriAtomServer.CreateInstance();
/* Prevent destruction of CRIWARE object. */
DontDestroyOnLoad(CriWare.managerObject);


# CRI Atom Transceiver
这是在3D定位中实现空间声音连接功能"3D收发器"的对象。
关于3D收发器的详细信息，请参照 空间声音连接功能 "3D收发器" 。
3D收发器功能需要与 CRI Atom Region 一起使用。


# CRI Atom Region
此为声源、收听者和收发器组的"3D区域"的对象。
CRI Atom Region没有设置项目，通过指定已粘贴此组件的GameObject， 可以确定CriAtomSource、CriAtomListener和CriAtomTransceiver所属的3D区域。









































