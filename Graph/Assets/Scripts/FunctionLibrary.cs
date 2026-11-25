// 引入 Unity 引擎核心命名空间（提供 Vector3、Mathf 等基础数学与引擎类型）
using UnityEngine;
// 隐式引入 UnityEngine.Mathf 的所有静态成员和常量（C# using static 语法），
// 这样在代码中可以直接写 PI、Sin、Cos、Sqrt，而无需每次都写 Mathf.PI、Mathf.Sin
using static UnityEngine.Mathf;

// 定义静态函数库类 FunctionLibrary：
// 1. 遵循“关注点分离（Separation of Concerns）”原则，让 Graph 类只负责点的创建与显示，数学公式全部独立到此库中；
// 2. 标记为 static 表示它不继承 MonoBehaviour，不能挂载到物体上，也不能被实例化，仅作为公开静态数学方法的集合（类似 Unity 的 Mathf）
public static class FunctionLibrary
{
	// 定义名为 Function 的公共委托（Delegate）类型：
	// 规定了所有数学曲面函数的统一方法签名——接收两个空间参数坐标 (u, v) 和时间参数 t，返回三维空间坐标 Vector3
	public delegate Vector3 Function (float u, float v, float t);

	// 定义函数名称枚举（Enumeration）：
	// 在 Unity Inspector 面板中会自动渲染为直观的下拉选择菜单；底层默认依次对应整数索引 0, 1, 2, 3, 4, 5, 6
	public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus, Helix, Saddle }

	// 私有的静态委托数组，按与 FunctionName 枚举完全一致的顺序存放各个数学函数方法的引用；
	// 不设为 public 是为了保护数组不被外部代码意外修改
	static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus, Helix, Saddle };

	// 根据传入的枚举名称，从委托数组中取出并返回对应的数学函数委托
	public static Function GetFunction (FunctionName name) {
		// 将枚举显式强制转换为整数 (int) 作为数组下标，直接通过索引返回函数引用，从而替代繁琐的 if-else 判断
		return functions[(int)name];
	}

	// 1. 基础正弦波曲面（Wave）：f(u, v, t) = [u, sin(π(u + v + t)), v]
	public static Vector3 Wave(float u, float v, float t)
	{
		// 声明用于返回的 3D 坐标变量 p（因为下面会对 x、y、z 所有分量逐一赋值，所以无需初始化）
		Vector3 p;
		// X 坐标直接使用参数 u
		p.x = u;
		// Y 坐标由 (u + v + t) 的正弦值决定，结合 u 和 v 会产生沿 XZ 对角线方向推进的波浪
		p.y = Sin(PI * (u + v + t));
		// Z 坐标直接使用参数 v
		p.z = v;
		// 返回计算出的三维表面点坐标
		return p;
	}

	// 2. 多重正弦波叠加曲面（MultiWave）：由三个不同方向、不同频率和速度的正弦波叠加而成
	public static Vector3 MultiWave (float u, float v, float t) {
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标直接使用参数 u
		p.x = u;
		// 第一道波：沿 U（X）轴方向传播的主正弦波，时间系数 0.5f 使它的移动速度减半，振幅为 1
		p.y = Sin(PI * (u + 0.5f * t));
		// 第二道波：叠加沿 V（Z）轴方向传播的次正弦波，乘以 2f 使频率翻倍，乘以 0.5f 使振幅减半（用乘法 0.5f 代替除以 2f 提高效率）
		p.y += 0.5f * Sin(2f * PI * (v + t));
		// 第三道波：叠加沿 UV（XZ）对角线方向传播的正弦波，时间系数 0.25f 使移动速度减为四分之一，振幅为 1
		p.y += Sin(PI * (u + v + 0.25f * t));
		// 归一化高度：三道波振幅之和最大为 1 + 0.5 + 1 = 2.5，乘以常量表达式 (1f / 2.5f) 将 Y 值范围重新缩放到 [-1, 1] 之间
		// （编译器会在编译期直接把常量 1f / 2.5f 计算为单个浮点数，运行时只执行一次乘法，避免运行期除法开销）
		p.y *= 1f / 2.5f;
		// Z 坐标直接使用参数 v
		p.z = v;
		// 返回计算出的三维表面点坐标
		return p;
	}

	// 3. 涟漪曲面（Ripple）：从中心原点向四周扩散且振幅随距离衰减的环形水波
	public static Vector3 Ripple (float u, float v, float t) {
		// 利用勾股定理计算当前参数点 (u, v) 到中心原点的平面距离 d = √(u² + v²)
		float d = Sqrt(u * u + v * v);
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标直接使用参数 u
		p.x = u;
		// 以距离 d 为自变量计算正弦值：4f * d 让定义域内产生多个密集的波峰波谷，减去 t（- t）让波纹随时间从中心向外扩散
		float y = Sin(PI * (4f * d - t));
		// 随距离 d 增大而衰减振幅：除以 (1 + 10d)，使中心波峰最高、越往边缘波纹越平缓
		p.y = y / (1f + 10f * d);
		// Z 坐标直接使用参数 v
		p.z = v;
		// 返回计算出的三维表面点坐标
		return p;
	}

    

	// 4. 扭曲旋转球体（Sphere）：通过参数方程摆脱 XZ 网格限制，构建带旋转扭曲条纹的 UV 球面
	public static Vector3 Sphere (float u, float v, float t) {
		// 计算动态扰动半径 r：基础半径为 0.9，叠加振幅为 0.1 的正弦波；
		// 6f * u 产生垂直经线方向条纹，4f * v 产生水平纬线方向条纹，两者结合并加上时间 t 形成随时间旋转的螺旋扭曲条纹
		float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
		// 计算球体在当前纬度 v 处的水平截面圆半径 s = r * cos(πv / 2)；
		// 当 v 接近 ±1（球体南北极）时，cos(±π/2) = 0，截面圆半径 s 收缩为 0（闭合为极点）
		float s = r * Cos(0.5f * PI * v);
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标：水平截面圆半径 s 乘以 sin(πu)，u ∈ [-1, 1] 刚好覆盖 [-π, π] 一整圈圆周
		p.x = s * Sin(PI * u);
		// Y 坐标：球体半径 r 乘以 sin(πv / 2)，v ∈ [-1, 1] 对应 [-π/2, π/2] 从南极到北极的半圆高度
		p.y = r * Sin(PI * 0.5f * v);
		// Z 坐标：水平截面圆半径 s 乘以 cos(πu)，与 X 坐标配合在 XZ 平面上围成圆形
		p.z = s * Cos(PI * u);
		// 返回计算出的三维球面点坐标
		return p;
	}

	// 5. 扭曲星形圆环面（Torus）：将球体的垂直半圆拉开并闭合为整圆，形成带自旋转扭曲效果的甜甜圈状圆环面
	public static Vector3 Torus (float u, float v, float t) {
		// float r = 1f; // 教程演进初期将球体直接改造为圆环时使用的固定半径
		// 计算主半径（Major Radius）r1（从圆环中心到管道中心的距离）：
		// 基础半径 0.7，叠加随 6u 和 0.5t 变化的正弦波（振幅 0.1），使圆环俯视呈缓慢旋转的六角星形状
		float r1 =  0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
		// 计算次半径/管道半径（Minor Radius）r2（圆环管道本身的粗细半径）：
		// 基础半径 0.15，叠加随 8u + 4v + 2t 变化的正弦波（振幅 0.05），使管道表面产生扭曲旋转的螺旋纹路
		float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
		// 计算当前点在 XZ 平面上距离原点的水平距离 s = r1 + r2 * cos(πv)：
		// 注意这里使用 PI * v 而不是 0.5f * PI * v，使管道截面由半圆变成完整的圆周 [-π, π]，并向外推开主半径 r1 的距离
		float s = r1 + r2 * Cos(PI * v);
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标：由水平距离 s 乘以 sin(πu) 绕 Y 轴旋转成环
		p.x = s * Sin(PI * u);
		// Y 坐标：由次半径 r2 乘以 sin(πv) 决定管道截面圆的上下高度
		p.y = r2 * Sin(PI * v);
		// Z 坐标：由水平距离 s 乘以 cos(πu) 绕 Y 轴旋转成环
		p.z = s * Cos(PI * u);
		// 返回计算出的三维圆环面点坐标
		return p;
	}

	// 6. 螺旋弹簧面（Helix / Spiral）：在圆环面的基础上增加盘旋圈数，并让中心高度随 u 线性爬升，形成可伸缩旋转的三维弹簧
	public static Vector3 Helix (float u, float v, float t) {
		// 弹簧主半径 r1（螺旋中心轴到弹簧管道中心的水平距离）
		float r1 = 0.6f;
		// 弹簧管半径 r2（弹簧管壁本身的粗细半径）
		float r2 = 0.12f;
		// 计算当前点在 XZ 平面上距离中心轴的水平距离 s = r1 + r2 * cos(πv)
		float s = r1 + r2 * Cos(PI * v);
		// 计算螺旋盘旋角度：3f * PI * u 使参数 u ∈ [-1, 1] 绕 Y 轴盘旋 3 整圈（跨度 6π），加上 PI * t 使弹簧整体随时间自转
		float angle = 3f * PI * u + PI * t;
		// 计算弹簧随时间上下压缩与拉伸的动态高度系数（在 0.5 到 0.8 之间随正弦波往复回弹）
		float height = 0.65f + 0.15f * Sin(PI * t);
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标：水平半径 s 乘以螺旋角度的正弦值
		p.x = s * Sin(angle);
		// Y 坐标：在管道截面高度 r2 * sin(πv) 的基础上，加上随 u 线性上升的螺旋轴高度 u * height，将圆环纵向拉开成弹簧
		p.y = u * height + r2 * Sin(PI * v);
		// Z 坐标：水平半径 s 乘以螺旋角度的余弦值
		p.z = s * Cos(angle);
		// 返回计算出的三维螺旋弹簧面点坐标
		return p;
	}

	// 7. 双曲抛物面 / 马鞍面（Saddle / 品客薯片形状）：沿 U 轴与 V 轴弯曲方向相反的经典二次曲面 f(u, v, t) = [u, (u² - v²)sin(πt), v]
	public static Vector3 Saddle (float u, float v, float t) {
		// 声明用于返回的 3D 坐标变量 p
		Vector3 p;
		// X 坐标直接使用参数 u
		p.x = u;
		// Y 坐标由双曲抛物面方程 (u² - v²) 决定：沿 U 轴呈开口向上的抛物线，沿 V 轴呈开口向下的抛物线；
		// 乘以 Sin(PI * t) 使曲面随时间在正向马鞍面与反向马鞍面之间平滑呼吸翻转，且高度始终保持在 [-1, 1] 范围内
		p.y = (u * u - v * v) * Sin(PI * t);
		// Z 坐标直接使用参数 v
		p.z = v;
		// 返回计算出的三维马鞍面点坐标
		return p;
	}
}