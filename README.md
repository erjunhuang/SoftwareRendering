# SoftwareRendering
SoftwareRendering

项目介绍
1:获取模型数据(模型数据终究只是一堆文本数据)

2:绘制三角形
(1)提前面剔除
(2)顶点着色器 模型-世界-观察
(3)视椎体裁剪
(4)透视除法
(5)屏幕映射
(6-1)画点
(6-2)画线(Cohen–Sutherland 裁剪算法 - Bresenham画线算法)
(6-3)画三角形(包含提前深度测试)
(7)像素着色器

3:抗锯齿(SSAA)

![Image text](https://raw.githubusercontent.com/hongmaju/light7Local/master/img/productShow/20170518152848.png)