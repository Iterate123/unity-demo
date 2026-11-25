// 引入 Unity 引擎核心命名空间（包含 MonoBehaviour、Transform、Vector3、Time 等基础类）
using UnityEngine;

// 定义 Graph 类，继承自 MonoBehaviour，使其可以作为组件挂载到 Unity 的 GameObject 上
public class Graph : MonoBehaviour {

	// [SerializeField] 让私有字段也能在 Unity 的 Inspector 面板中显示和配置
	[SerializeField]
	// 用于实例化采样点的预制体（Prefab）的 Transform 引用
	Transform pointPrefab;
    
	// 同时序列化该字段，并用 [Range(10, 100)] 在 Inspector 中将其限制为 10 到 100 的滑动条
	[SerializeField, Range(10, 100)]
	// 网格每行/每列的分辨率（采样点数量），默认值为 10（二维网格总点数为 resolution * resolution）
	int resolution = 10;

	// [SerializeField, Range(0, 2)] // 教程早期使用整数滑动条（0~2）选择函数的写法，现已被下面的枚举下拉菜单替代
	// 将函数选择字段序列化到 Inspector 面板，因为类型是枚举，Unity 会自动将其渲染为下拉菜单
	[SerializeField]
	// 当前需要在图表中显示的数学曲面函数名称（引用自 FunctionLibrary 中的 FunctionName 枚举）
	FunctionLibrary.FunctionName function;

	public enum TransitionMode { Cycle, Random }

	[SerializeField]
	TransitionMode transitionMode;

	[SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	// 缓存所有生成的采样点的 Transform 一维数组，避免在 Update 中每帧重复查找组件
	Transform[] points;
	float duration;

	bool transitioning;

	FunctionLibrary.FunctionName transitionFunction;

	// Awake 在脚本实例被加载时调用一次（早于 Start），用于初始化生成所有采样点并设置缩放与父节点
	void Awake()
	{
		// 计算每个点之间的步长（也是每个小方块的边长）：将 [-1, 1] 总长度为 2 的区间平分为 resolution 份
		float step = 2f / resolution;
		// 用于复用的局部坐标变量，初始为 (0, 0, 0)（进入三维参数曲面阶段后，位置完全由 Update 动态计算，因此不再在 Awake 中设置初始位置）
		//var position = Vector3.zero;
		// 每个点的缩放大小，等于 (1, 1, 1) * step，保证在 [-1, 1] 区间内相邻方块刚好紧挨着不重叠
		var scale = Vector3.one * step;
		// 初始化数组：从一维线条升级为二维网格（Grid）后，总点数变为 resolution 的平方（resolution * resolution）
		points = new Transform[resolution * resolution];

		// 循环实例化每一个采样点，并设置它们的大小和父物体（因为 X 和 Z 也会随参数曲面动态变化，位置留到 Update 中统一计算）
		for (int i = 0; i < points.Length; i++) {
			// 教程中间阶段（将点排成固定 XZ 网格）在 Awake 中追踪行号 z 和列号 x 的代码，现已移至 Update 中：
			// if (x == resolution) {
			// 	x = 0;
			// 	z += 1;
			// }
			// 实例化预制体，同时将新生成的 Transform 引用赋值给数组元素 points[i] 和局部变量 point
			Transform point = points[i] = Instantiate(pointPrefab);
			//points[i] = point;

			// 计算当前点的 X 和 Z 坐标（教程中间阶段在 Awake 中固定网格坐标的代码，现已在 Update 中改为计算参数 u 和 v）：
			// + 0.5f 是将方块中心偏移半个步长，使所有方块的外边缘正好严丝合缝地对齐 [-1, 1] 边界；
			// - 1f 是将 [0, 2] 的区间平移到 [-1, 1]
			// position.x = (x + 0.5f) * step - 1f;
			// position.z = (z + 0.5f) * step - 1f;
			// position.y = position.x * position.x * position.x; // 第一节教程中的静态 f(x) = x³ 曲线

			// 将计算好的坐标赋给采样点的局部位置（localPosition）（现已移至 Update 中每帧更新）
			// point.localPosition = position;
			// 将计算好的缩放赋给采样点的局部缩放（localScale）
			point.localScale = scale;
			// 将当前 Graph 物体的 transform 设为该点的父物体；
			// 第二个参数传 false（worldPositionStays = false）表示保持上面刚设好的局部坐标和局部缩放不变，不让 Unity 自动换算改写
			point.SetParent(transform, false);
		}

		// 第一节教程最开始测试生成单个点时的历史代码：
		//Transform point = Instantiate(pointPrefab);
		// point = Instantiate(pointPrefab);   
		// point.localPosition = Vector3.right * 2f;
	}

	// Update 在每一帧都会调用一次，用于根据所选数学函数让三维曲面随时间动态变化
	void Update () {
		duration += Time.deltaTime;
		if (transitioning)
		{
			if (duration >= transitionDuration) {
				duration -= transitionDuration;
				transitioning = false;
			}
		}
		else if (duration >= functionDuration) {
			duration -= functionDuration;
			transitioning = true;
			transitionFunction = function;
			//function = FunctionLibrary.GetNextFunctionName(function);
			PickNextFunction();
		}
		if (transitioning) {
			UpdateFunctionTransition();
		}
		else {
			UpdateFunction();
		}
	}
	
	void PickNextFunction () {
		function = transitionMode == TransitionMode.Cycle ?
			FunctionLibrary.GetNextFunctionName(function) :
			FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}

	// 正常显示阶段（非过渡期）每帧更新所有点的位置
	void UpdateFunction () {
		// 根据 Inspector 中选中的枚举值 function，从 FunctionLibrary 获取对应的数学函数委托引用 f
		FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
		// 获取当前游戏已运行的时间（秒），作为数学函数的动画时间参数 t（在循环外获取一次以减少属性调用开销）
		float time = Time.time;
		// 在 Update 中实时根据当前 resolution 计算步长 step（这样在运行模式下拖动 resolution 滑块也能动态拉伸/压缩参数区间）
		float step = 2f / resolution;
		// 预先计算第一行（z = 0）对应的纵向参数坐标 v，后续只在换行（z 改变）时才重新计算 v，避免每次循环重复计算
		float v = 0.5f * step - 1f;
		// 遍历所有采样点：i 为数组总索引，x 为当前列索引（每步自增），z 为当前行索引（仅在每行结束时自增）
		// 使用单层循环而非嵌套双层循环，可以确保即使在运行模式下修改了 resolution，也始终能遍历并更新数组中的每一个点
		for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++) {
			// 当列索引 x 达到每行点数 resolution 时，说明当前行已排满，需要换行
			if (x == resolution) {
				// 将列索引 x 重置回行首 0
				x = 0;
				// 将行索引 z 加 1，进入下一行
				z += 1;
				// 仅在行索引 z 发生变化时，重新计算新的纵向参数坐标 v ∈ (-1, 1)
				v = (z + 0.5f) * step - 1f;
			}
			// 根据当前列索引 x 计算横向参数坐标 u ∈ (-1, 1)
			float u = (x + 0.5f) * step - 1f;
			//float v = (z + 0.5f) * step - 1f; // 未优化前在每次循环中都计算 v 的写法（现已优化到循环外和 if 换行块内）
			// 调用委托函数 f(u, v, time) 直接计算出三维空间坐标 Vector3，并赋值给当前采样点的 localPosition
			points[i].localPosition = f(u, v, time);
		}
	}

	// 两个函数切换时的平滑过渡变形阶段（transitioning == true）每帧更新所有点的位置
	void UpdateFunctionTransition () {
		FunctionLibrary.Function
			from = FunctionLibrary.GetFunction(transitionFunction),
			to = FunctionLibrary.GetFunction(function);
		float progress = duration / transitionDuration;
		// 获取当前游戏已运行的时间（秒），作为数学函数的动画时间参数 t（在循环外获取一次以减少属性调用开销）
		float time = Time.time;
		// 在 Update 中实时根据当前 resolution 计算步长 step（这样在运行模式下拖动 resolution 滑块也能动态拉伸/压缩参数区间）
		float step = 2f / resolution;
		// 预先计算第一行（z = 0）对应的纵向参数坐标 v，后续只在换行（z 改变）时才重新计算 v，避免每次循环重复计算
		float v = 0.5f * step - 1f;
		// 遍历所有采样点：i 为数组总索引，x 为当前列索引（每步自增），z 为当前行索引（仅在每行结束时自增）
		// 使用单层循环而非嵌套双层循环，可以确保即使在运行模式下修改了 resolution，也始终能遍历并更新数组中的每一个点
		for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++) {
			// 当列索引 x 达到每行点数 resolution 时，说明当前行已排满，需要换行
			if (x == resolution) {
				// 将列索引 x 重置回行首 0
				x = 0;
				// 将行索引 z 加 1，进入下一行
				z += 1;
				// 仅在行索引 z 发生变化时，重新计算新的纵向参数坐标 v ∈ (-1, 1)
				v = (z + 0.5f) * step - 1f;
			}
			// 根据当前列索引 x 计算横向参数坐标 u ∈ (-1, 1)
			float u = (x + 0.5f) * step - 1f;
			//float v = (z + 0.5f) * step - 1f; // 未优化前在每次循环中都计算 v 的写法（现已优化到循环外和 if 换行块内）
			// 调用委托函数 f(u, v, time) 直接计算出三维空间坐标 Vector3，并赋值给当前采样点的 localPosition
			points[i].localPosition = FunctionLibrary.Morph(
				u, v, time, from, to, progress
			);

			// 以下为升级到三维参数曲面（Vector3 返回值）之前、仅修改 position.y 时的历史演进代码：
			// 从数组中直接取出第 i 个点的 Transform 引用
			Transform point = points[i];
			// 先把当前点的 localPosition 拷贝到局部变量 position（因为 C# 中结构体属性不能直接修改单个分量如 point.localPosition.y = ...）
			Vector3 position = point.localPosition;
			// 教程早期尚未使用委托（Delegate）时，用 if-else 根据整数索引分别调用不同函数的历史代码：
			// if (function == 0) {
			// 	position.y = FunctionLibrary.Wave(position.x, time);
			// }
			// else if (function == 1) {
			// 	position.y = FunctionLibrary.MultiWave(position.x, time);
			// }
			// else {
			// 	position.y = FunctionLibrary.Ripple(position.x, time);
			// }
			// 教程中期使用返回 float 的委托 f(x, z, t) 仅计算 Y 坐标的历史代码：
			//position.y = f(position.x, position.z, time);
			// 将修改完 Y 坐标的新位置重新赋回给采样点的 localPosition：
			//point.localPosition = position;
		}
	}
}