

//// 判定面上的点键类
//public class Tap : MonoBehaviour
//{
//    // 点键自身的X轴坐标
//    public float startX;
//    // 点键的大小（可用于碰撞检测范围等设置，根据实际需求调整）
//    public float noteSize;
//    // 与判定面相关联的引用，用于获取判定面的Y轴坐标信息
//    public JudgePlane associatedPlane;

//    // 根据关联的判定面更新点键的Y轴坐标，使其与所在子判定面的Y轴坐标一致
//    void Update()
//    {
//        float currentTime = Time.time;
//        float yAxisCoordinate = GetCurrentYAxisCoordinate(currentTime);
//        transform.position = new Vector3(startX, yAxisCoordinate, 0f);
//    }

//    // 检测玩家是否点击到点键的方法（这里简单示例，可根据实际需求完善）
//    public bool IsClicked()
//    {
//        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
//        {
//            // 获取点击位置的世界坐标
//            Vector3 clickPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
//            // 比对点击位置与点键位置，判断是否在碰撞体范围内
//        }

//        return false;
//    }

//    // 根据当前时间获取点键所在子判定面的当前Y轴坐标
//    private float GetCurrentYAxisCoordinate(float currentTime)
//    {
//        foreach (JudgePlane.SubJudgePlane subPlane in associatedPlane.SubJudgePlaneList)
//        {
//            if (currentTime >= subPlane.startT && currentTime <= subPlane.endT)
//            {
//                // 根据Y轴变化函数类型计算当前子判定面的Y轴坐标
//                return JudgePlane.CalculateYAxisPosition(currentTime, subPlane.startT, subPlane.startY, subPlane.endT, subPlane.endY, subPlane.yAxisFunction);
//            }
//        }

//        // 如果不在任何有效子判定面时间范围内，返回一个默认值（这里可根据实际情况调整处理方式）
//        return 0f;
//    }
//}


//// 持续按键（Hold）类，代表整个Hold，由多个子Hold组成
//public class Hold
//{
//    // 用于存储子Hold参数的列表
//    private List<SubHold> subHoldList = new List<SubHold>();

//    // 与判定面相关联的引用，用于获取判定面的Y轴坐标信息
//    public JudgePlane associatedPlane;

//    // 内部类，用于表示子Hold的参数结构
//    public class SubHold
//    {
//        public float startT;
//        public float startXLeft;
//        public float startXRight;
//        public float endT;
//        public float endXLeft;
//        public float endXRight;
//        public Utility.TransFunctionType XLeftFunction;
//        public Utility.TransFunctionType XRightFunction;

//        public SubHold(float startTime, float startXLeftVal, float startXRightVal, float endTime, float endXLeftVal,
//                             float endXRightVal, Utility.TransFunctionType XLeftFunc, Utility.TransFunctionType XRightFunc)
//        {
//            startT = startTime;
//            startXLeft = startXLeftVal;
//            startXRight = startXRightVal;
//            endT = endTime;
//            endXLeft = endXLeftVal;
//            endXRight = endXRightVal;
//            XLeftFunction = XLeftFunc;
//            XRightFunction = XRightFunc;
//        }
//    }

//    // 方法用于向子Hold参数列表中添加子Hold的参数
//    public void AddSubHold(float startTime, float startXStart, float startXEnd, float endTime, float endXStart,
//                                 float endXEnd, Utility.TransFunctionType startXFunction, Utility.TransFunctionType endXFunction)
//    {
//        subHoldList.Add(new SubHold(startTime, startXStart, startXEnd, endTime, endXStart, endXEnd, startXFunction, endXFunction));
//    }

//    // 检测玩家是否成功按住了这个Hold，这里简化逻辑，仅判断是否在正确时间内按住了对应的区域（实际可更细化）
//    public bool IsHeldCorrectly()
//    {
//        float currentTime = Time.time;
//        bool isHeld = false;

//        foreach (SubHold subParams in subHoldList)
//        {
//            if (currentTime >= subParams.startTime && currentTime <= subParams.endTime)
//            {
//                // 这里可进一步添加详细检测逻辑，比如检测手指是否在对应的X轴坐标范围内等
//                isHeld = true;
//            }
//        }

//        return isHeld;
//    }

//    // 根据当前时间更新Hold的可视化表示（这里假设你有对应的可视化组件来显示Hold，暂未详细实现）
//    public void UpdateVisualization()
//    {
//        foreach (SubHold subParams in subHoldList)
//        {
//            if (Time.time >= subParams.startT && Time.time <= subParams.endT)
//            {
//                // 根据对应的函数计算当前子Hold的起始和结束X轴坐标
//                float currentXLeft = CalculateXAxisPosition(Time.time, subParams.startT, subParams.startXLeft, subParams.endT, subParams.endXLeft, subParams.XLeftFunction);
//                float currentXRight = CalculateXAxisPosition(Time.time, subParams.startT, subParams.startXRight, subParams.endT, subParams.endXRight, subParams.XRightFunction;

//                // 这里可将计算得到的坐标传递给可视化组件来更新显示（暂未详细实现）
//            }
//        }
//    }

//    // 计算指定子Hold在给定时间下根据不同函数类型的X轴当前位置
//    private float CalculateXAxisPosition(float currentTime, float startTime, float startVal, float endTime, float endVal, Utility.TransFunctionType functionType)
//    {
//        return CalculatePosition(currentTime, startTime, startVal, endTime, endVal, functionType);
//    }
//}

//// 滑动键（Slide）类
//public class Slide : MonoBehaviour
//{
//    // 滑动键的开始时间
//    public float startT;
//    // 滑动键在X轴的坐标（这里简化为一个坐标，可根据实际情况调整具体含义，比如中心坐标等）
//    public float startX;
//    // 与判定面相关联的引用，用于获取判定面在对应时间的Y轴坐标信息
//    public JudgePlane associatedPlane;

//    // 检测玩家是否在正确的时间段内触摸到了滑动键对应的判定区（仅针对移动端触摸检测）
//    public bool IsTouched()
//    {
//        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved && Time.time >= startT && Time.time <= startT + 0.1f)
//        {
//            // 获取触摸位置的世界坐标
//            Vector3 touchPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

//            // 判断触摸位置的X轴坐标是否接近滑动键的X轴坐标以及是否在关联的判定面上（可根据实际调整容差范围）
//            if (Mathf.Abs(touchPosition.x - startX) < 0.1f && touchPosition.y >= associatedPlane.startY && touchPosition.y <= associatedPlane.endY)
//            {
//                return true;
//            }
//        }

//        return false;
//    }
//}

//// 划键（Flick）类
//public class Flick : MonoBehaviour
//{
//    // 划键的开始时间
//    public float startT;
//    // 划键在X轴的坐标（这里简化为一个坐标，可根据实际情况调整具体含义，比如中心坐标等）
//    public float startX;
//    // 与判定面相关联的引用，用于获取判定面在对应时间的Y轴坐标信息
//    public JudgePlane associatedPlane;
//    // 用于记录划键操作的滑动方向向量（支持360度任意方向）
//    public Vector2 flickDirectionVector;

//    // 检测玩家是否在正确的时间点击并向正确方向滑动了划键对应的判定区（仅针对移动端触摸检测）
//    public bool IsFlickedCorrectly()
//    {
//        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
//        {
//            // 获取点击位置的世界坐标
//            Vector3 clickPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

//            // 判断点击位置是否在划键的坐标范围内以及是否在关联的判定面上
//            if (collider.bounds.Contains(clickPosition) && clickPosition.y >= associatedPlane.startY && clickPosition.y <= associatedPlane.endY)
//            {
//                // 记录点击开始时的触摸位置
//                Vector2 startTouchPosition = Input.GetTouch(0).position;
//                // 等待触摸移动阶段，获取滑动后的触摸位置
//                while (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
//                {
//                    Vector2 endTouchPosition = Input.GetTouch(0).position;
//                    // 计算滑动方向向量
//                    flickDirectionVector = (endTouchPosition - startTouchPosition).normalized;
//                }

//                return Time.time >= startT && Time.time <= startT + 0.1f;
//            }
//        }

//        return false;
//    }
//}

//// 星星（Star）类
//public class Star : MonoBehaviour
//{
//    // 星星头（Star Head）的激活时间
//    public float starHeadT;
//    // 用于存储子星星（subStar）参数的列表
//    private List<SubStar> subStarList = new List<SubStar>();

//    // 内部类，用于表示子星星（subStar）的参数结构
//    public class SubStar
//    {
//        public float starTrackStartT;
//        public float starTrackEndT;
//        public Vector2 startPosition;
//        public Vector2 endPosition;
//        public Utility.TrackFunctionType trackFunction;

//        public SubStar(float startT, float endT, Vector2 startPos, Vector2 endPos, Utility.TrackFunctionType func)
//        {
//            starTrackStartT = startT;
//            starTrackEndT = endT;
//            startPosition = startPos;
//            endPosition = endPos;
//            trackFunction = func;
//        }
//    }

//    // 方法用于向子星星参数列表中添加子星星的参数
//    public void AddSubStar(float starTrackStartT, float starTrackEndT, Vector2 startPosition, Vector2 endPosition, TrackFunctionType trackFunction)
//    {
//        subStarList.Add(new SubStar(starTrackStartT, starTrackEndT, startPosition, endPosition, trackFunction));
//    }

//    // 更新方法，用于在每一帧检查星星的状态和玩家操作，根据时间推进更新星星位置等
//    void Update()
//    {
//        if (Time.time >= starHeadT)
//        {
//            foreach (SubStarParams subStar in subStarList)
//            {
//                if (Time.time >= subStar.starTrackStartT && Time.time <= subStar.starTrackEndT)
//                {
//                    // 根据不同的函数类型计算当前子星星的位置
//                    Vector2 currentPosition = CalculatePosition(Time.time, subStar);
//                    transform.position = currentPosition;

//                    // 检测玩家是否触摸到了星星（这里简单示例触摸检测逻辑，可根据实际完善）
//                    if (IsTouched())
//                    {
//                        // 可在这里添加更多针对触摸到星星后的逻辑，比如触发特效、记录操作成功等
//                    }
//                }
//            }
//        }
//    }

//    // 根据给定时间和子星星参数计算当前子星星的位置
//    private Vector2 CalculatePosition(float currentTime, SubStar subStar)
//    {
//        return Utility.CalculatePosition(currentTime, subStar);
//    }

//    // 检测玩家是否触摸到了星星（仅针对移动端触摸检测，简单示例，可根据实际需求完善）
//    public bool IsTouched()
//    {
//        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
//        {
//            // 获取触摸位置的世界坐标
//            Vector3 touchPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

//            // 判断触摸位置是否在星星的位置范围内（可根据实际调整容差范围）
//            if (Vector2.Distance(touchPosition, transform.position) < 0.1f)
//            {
//                return true;
//            }
//        }

//        return false;
//    }
//}

