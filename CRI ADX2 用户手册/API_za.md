
# ============================================================ #
#           criware 更多学习资料
# ============================================================ #

https://www.criware.cn/Home/Goods/study_1_list/cat_id/22/article_id/160.html#

https://www.criware.cn/Home/Goods/study_1_list/cat_id/20/article_id/158.html#\

https://www.criware.cn/public/upload/chm/CRI_ADX2_Tools_Manual_zh_public/criatom_tools_atomcraft_api_refmodule_project.html




# ----------------------------------------- :
# CriAtomEx 类 参考

AttachDspBusSetting()
    添加 dsp 总线设置

DetachDspBusSetting()
    切断 dsp 总线设置

ApplyDspBusSnapshot()
    适用 dsp 总线快照
    

# ----------------------------------------- :
# CriAtomExCategory 类 参考   (类别)
    位于文件: CriAtomEx.cs 内:

SetAisacControl( string name, string controlName, float value )
    指定名称来设置针对 aisac 控制器值;

SetAisacControl( int id, int controlId, float value )
    通过指定 id 指定来设置对类别的 aisac 控制器值


# ----------------------------------------- :
# CriAtomExPlayer 类参考

SetAisacControl( string controlName, float value )
    设置 aisac 控制器值 (指定控制器名)

SetAisacControl( uint controlId, float value )
    设置 aisac 控制器值 (指定控制器id )


# ----------------------------------------- :
# CriAtomExPlayback 结构体 参考

SetNextBlockIndex( int index )
    播放声音 的 块转换


# ----------------------------------------- :
# CriAtomExSequencer 类 参考
    位于文件: CriAtomEx.cs 中;


delegate void EventCbFunc( string eventParamsString )
    序列回调

SetEventCallback( CriAtomExSequencer.EventCbFunc func, string separator="\t" )
    注册序列事件回调
    此函数已废弃

改用 OnCallback();


代码中搜索 CriAtomOutputDeviceObserver





# ----------------------------------------- :
#             名词
# ----------------------------------------- :

# react:
    避让处理, 就是语音出来的时候音乐会变小一点给语音让出空间;


# ducker
    由 音频引擎去自动控制避让, 


# _Hn
 handle 的缩写;


 # pitch:
音调


# "send level": 
指定: 哪个扬声器以哪个音量输出每个声道的声音数据。


# Envelope:
波封（Envelope）是指将一种音色波形的大致轮廓描绘出来用以表示出该音色在音量变化上的特性的参数。
一个波封可以用4种参数来描述，分别是Attack(起音)、Decay(衰减)、Sustain(延持)、与Release(释音)，
四者也就是一般称的“ADSR”。一般音源机或合成器，会提供 Attack 与 Release 的调整参数；
而较专业的合成器，这4种参数则都会提供。


# ASR:
猜测为: Atom Sound Renderer
一种 Sound Renderer Type


# cue:
声音设计师在编辑器中所完成创造的 事件; 
别的软件中称为 event;

https://zhuanlan.zhihu.com/p/46708534


# acb 和 awb 的区别:
都是对 cuesheet 的打包, 区别在于是否是 流文件;



# ----------------------------------------- :
#        CriAtomCueSheet
# ----------------------------------------- :
就是哪个 cur sheet 的 class;



# ----------------------------------------- :
#       CriAtomExAcb
# ----------------------------------------- :
CB and AWB data



# ----------------------------------------- :
#      CriAtomExPlayer
# ----------------------------------------- :
就是所谓的 player 

    示范代码:
	 *  // 创建播放器
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 注册ACF文件
	 *  CriAtomEx.RegisterAcf(null, "sample.acf");
	 *  // 加载ACB文件
	 *  CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, "sample.acb", "sample.awb");
	 *  // 指定要播放的队列的名称
	 *  player.SetCue(acb, "gun_shot");
	 *  // 播放设置的声音数据
	 *  player.Start();


# ----------------------------------------- :
#   CriAtomExPlayback
# ----------------------------------------- :

调用 CriAtomExPlayer.Start() 将返回一个 CriAtomExPlayback 实例;



# ----------------------------------------- :
#    查看 设备当前是否连接了外部 耳机(有线或无线)
# ----------------------------------------- :

CriAtomOutputDeviceObserver.cs

