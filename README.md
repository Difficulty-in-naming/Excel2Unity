# Excel2Unity

该库已经废弃请移步到#https://github.com/pk27602017/Excel2Sqlite

About
===
- 非常简单的使用
- 只需简单的配置几个参数
- 不需要任何代码
- 完全自动化

Get Started
===
- Clone Git 仓库
- 将所有文件导入到Unity当中
- 打开Assets/Editor/ConfigGenerate.cs
- 根据需求设定这几个变量[mNameSpace][AssetSavePath][LoadPath][ScriptSavePath][CodeTemplatePath]
- 打开CodeTemplate.txt修改成你想要生成的代码模板的样子
- 最后将Excel表格拖入[LoadPath]目录下,插件便会自动生成对应的Asset到AssetSavePath目录下,脚本则会自动生成在ScriptSavePath目录下
- 之后每当Excel有修改亦或者是有新加入都会自动重新生成对应的Asset
- 如果Excel生成失败请根据Console打印的错误进行修改然后点击Tools/ConfigGenerate/生成上次出错的Excel文件即可
