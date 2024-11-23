//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Params
{
    // 将类名修改为更符合规范的单数形式，同时设置为静态类，方便直接访问其属性，无需实例化
    public static class ChartParams
    {
        // 谱面参数
        // X轴坐标从 -2 到 2
        public static float XaxisMin = -2;
        public static float XaxisMax = 2;

        // Y轴坐标从 0 到 1
        public static float YaxisMin = 0;
        public static float YaxisMax = 1;
    }
}
