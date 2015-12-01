MoeLoader是一个基于WPF的图片浏览、收集工具。

MoeLoader官网: http://moeloader.sinaapp.com/

**如何开发自定义站点：**

1. 使用Visual Studio创建Class Library类型的项目，将项目属性中的默认命名空间设置为SitePack；

2. 为项目添加引用，将MoeLoader文件夹中的MoeSite.dll添加到引用中；

3. 添加一个类，继承MoeLoader.AbstractImageSite（假设该类命名为SiteSampleImgSite）；

4. 为SiteSampleImgSite类实现必选的SiteUrl、SiteName、ShortName（假设为sis）、GetImages、GetPageString属性和方法；

5. 添加一个类，命名空间为SitePack，类声明为public class SiteProvider，在该类中添加方法public List<MoeLoader.ImageSite> SiteList()，在该方法中返回含有SiteSampleImgSite类实例的List；

6. 在项目中添加一个文件夹，命名为image，在其中添加一个分辨率为16\*16的ico图标文件，重命名为sis.ico（与上面设置的ShortName相同），在它的属性中将Build Action设置为Embedded Resource；

7. 生成项目，将编译好的类库dll文件重命名为SitePackXXX.dll的形式（例如SitePackExt.dll），将重命名后的dll文件放到MoeLoader.exe所在的目录下；

8. 运行MoeLoader，享受你新添加的站点！

PS. 关于MoeLoader接口中的AbstractImageSite、Img类详细使用信息，请参考MoeLoader源代码中的注释；

PS2. 若希望将自定义的站点加入MoeLoader正式版本中，请与我联系

下载自定义站点示例项目：
https://code.google.com/p/moe-loader-v7/downloads/detail?name=SitePackSample.7z
