

# 禁用Unity标准音频
在项目的音频(Audio)设置中，"Disable Unity Audio"属性可以禁用Unity标准音频。
使用ADX2而不使用Unity标准音频时，禁用Unity标准音频， 可以减少应用程序的CPU负荷和内存使用量。

在某些平台上，使用ADX2音频输出时，必须启用上述的"Disable Unity Audio"。
因此，导入CRIWARE Unity Plugin构建应用程序时，除某些平台外，将强制启用此标记（仅限Unity 5.6及更高版本）。


# 启用Unity标准音频
需要使用Unity标准音频，如Unity标准麦克风输入功能等，请通过以下步骤避免上述标记被插件强制更新。

-- 从 Unity Editor 菜单创建 CriWareBuildPreProcessorPrefs.asset 。

-- 在创建好的资产中禁用"Mute Other Audio"标记。

-- 取消选中项目的音频设置(Edit > Project Settings > Audio)中的"Disable Unity Audio"。





























