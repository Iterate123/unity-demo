// 引入 Unity 引擎核心命名空间（包含 MonoBehaviour、Transform、Vector3、Mathf 等基础类）
using UnityEngine;

// 定义 Graph 类，继承自 MonoBehaviour，使其可以作为组件挂载到 Unity 的 GameObject 上
public class Graph : MonoBehaviour {

	// [SerializeField] 让私有字段也能在 Unity 的 Inspector 面板中显示和赋值
	[SerializeField]
	// 用于实例化采样点的预制体（Prefab）的 Transform 引用
	Transform pointPrefab;
    
	// 同时序列化该字段，并用 [Range(10, 100)] 在 Inspector 中将其限制为 10 到 100 的滑动条
	[SerializeField, Range(10, 100)]
	// 分辨率（生成的点的数量），默认值为 10
	int resolution = 10;

	// [SerializeField, Range(0, 2)]
	[SerializeField]
	FunctionLibrary.FunctionName function;

	// 缓存所有生成的采样点的 Transform 数组，避免在 Update 中每帧重复查找组件
	Transform[] points;

	// Awake 在脚本实例被加载时调用一次（早于 Start），用于初始化生成所有点
	void Awake()
	{
		// 计算每个点之间的步长（也是每个小方块的边长）：将 [-1, 1] 总长度为 2 的区间平分为 resolution 份
		float step = 2f / resolution;
		// 用于复用的局部坐标变量，初始为 (0, 0, 0)
		var position = Vector3.zero;
		// 每个点的缩放大小，等于 (1, 1, 1) * step，保证相邻方块刚好紧挨着不重叠
		var scale = Vector3.one * step;
		// 初始化数组，长度为 resolution，用来存放所有点的引用
		points = new Transform[resolution * resolution];

		// 循环实例化每一个采样点，并设置它们的初始位置、大小和父物体
		for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++) {
			if (x == resolution) {
				x = 0;
				z += 1;
			}
			// 实例化预制体，同时将新生成的 Transform 引用赋值给数组 points[i] 和局部变量 point
			Transform point = points[i] = Instantiate(pointPrefab);
			//points[i] = point;

			// 计算当前点的 X 坐标：
			// + 0.5f 是将方块中心向右偏移半个步长，使所有方块的左右边缘正好严丝合缝地对齐 [-1, 1] 边界；
			// - 1f 是将 [0, 2] 的区间平移到 [-1, 1]
			position.x = (x  + 0.5f) * step - 1f;
			position.z = (z + 0.5f) * step - 1f;
			//position.y = position.x * position.x * position.x; // 静态 f(x) = x³ 曲线（已改到 Update 中动态计算）

			// 将计算好的坐标赋给采样点的局部位置（localPosition）
			point.localPosition = position;
			// 将计算好的缩放赋给采样点的局部缩放（localScale）
			point.localScale = scale;
			// 将当前 Graph 物体的 transform 设为该点的父物体；
			// 第二个参数传 false（worldPositionStays = false）表示保持上面刚设好的局部坐标和局部缩放不变，不让 Unity 自动改写
			point.SetParent(transform, false);
		}

		//Transform point = Instantiate(pointPrefab);
		// point = Instantiate(pointPrefab);   
		// point.localPosition = Vector3.right * 2f;
	}

	// Update 在每一帧都会调用一次，用于让曲线随时间动起来
	void Update () {
		FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
		// 遍历在 Awake 中缓存好的 points 数组
		float time = Time.time;
		for (int i = 0; i < points.Length; i++) {
			// 从数组中直接取出第 i 个点的 Transform 引用（极快的纯 C# 数组访问）
			Transform point = points[i];
			// 先把当前点的 localPosition 拷贝到局部变量 position（因为 C# 中结构体属性不能直接修改单个分量如 point.localPosition.y = ...）
			Vector3 position = point.localPosition;
			// if (function == 0) {
			// position.y = FunctionLibrary.Wave(position.x, time);
			// }
			// else if (function == 1) {
			// 	position.y = FunctionLibrary.MultiWave(position.x, time);
			// }
			// else {
			// 	position.y = FunctionLibrary.Ripple(position.x, time);
			// }
			position.y = f(position.x, position.z, time);
			// 将修改完 Y 坐标的新位置重新赋回给采样点的 localPosition
			point.localPosition = position;
		}
	}
}