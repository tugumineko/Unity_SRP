# Unity_SRP

本项目基于 [wlgys8/SRPLearn](https://github.com/wlgys8/SRPLearn) 仓库扩展而来。  
原项目旨在使用 Unity 可编程渲染管线（Scriptable Render Pipeline）从零构建渲染器，适合边做边学。

我在原项目基础上进行了一些改动和实验，并且增加了我自己的一些实验性的美术效果。
Unity版本: 2022.3.53

> ⚠️ 本项目为个人学习与实验用途，非正式生产方案，建议配合原仓库学习参考。

---

## 我的改动内容

- Age-Of-Sail-Deferred-Rendering-Pipeline
  - 参考论文:  [Real-time non-photorealistic animation for immersive storytelling in “Age of Sail”](https://www.sciencedirect.com/science/article/pii/S2590148619300123#sec0015)


---

## 原项目信息（SRPLearn）

> # SRPLearn
>
> 基于Unity可编程渲染管线造轮子。 主要目的是为了边写边学习，不考虑平台兼容和性能优化等问题。
>
> Unity版本: 2020.3.17
>
> 注意，项目不需要用到`com.unity.render-pipelines.universal`，可以将其从包依赖中删除。
>
> ## 目录
>
> - [创建渲染管线，绘制Cube](https://github.com/wlgys8/SRPLearn/wiki/Hello)
> - [支持平行光 - BlinnPhong光照模型](https://github.com/wlgys8/SRPLearn/wiki/DirLight)
> - [平行光投影 - Shadow Mapping](https://github.com/wlgys8/SRPLearn/wiki/MainLightShadow)
> - [半透明物体渲染](https://github.com/wlgys8/SRPLearn/wiki/Transparent)
> - [阴影优化 - Cascade Shadow Mapping](https://github.com/wlgys8/SRPLearn/wiki/CascadeShadowMapping)
> - [点光源支持 - PointLight](https://github.com/wlgys8/SRPLearn/wiki/PointLight)
> - PCF软阴影
>   - [理论部分 - PCF优化采样算法](https://github.com/wlgys8/SRPLearn/wiki/PCFSampleOptimize)
>   - [SRP实现](https://github.com/wlgys8/SRPLearn/wiki/ShadowPCF)
> - Shadow Bias
>   - [理论部分 - 自适应Bias算法](https://github.com/wlgys8/SRPLearn/wiki/ShadowBias)
>   - SRP实现
> - 抗锯齿
>   - [几种抗锯齿方式总结](https://github.com/wlgys8/SRPLearn/wiki/AntiAliasSummary)
>   - [FXAA详细算法](https://github.com/wlgys8/SRPLearn/wiki/FXAA)
>   - [MSAA抗锯齿SRP实现](https://github.com/wlgys8/SRPLearn/wiki/MSAA_Implement)
>   - [FXAA抗锯齿SRP实现](https://github.com/wlgys8/SRPLearn/wiki/FXAA_Implement)
> - [CSM阴影混合过渡](https://github.com/wlgys8/SRPLearn/wiki/CSMBlend)
> - [PBR实现](https://github.com/wlgys8/SRPLearn/wiki/PBR)
> - [Tile Based Deferred Shading](https://github.com/wlgys8/SRPLearn/wiki/DeferredShading)
> - 待补充


---

## 📌 致谢

本项目继承自：
- 原作者 GitHub：[@wlgys8](https://github.com/wlgys8)
- 原始项目地址：[SRPLearn](https://github.com/wlgys8/SRPLearn)

感谢其高质量的 SRP 教程与注释，为我在 SRP 学习路上提供了宝贵的帮助 🙏


