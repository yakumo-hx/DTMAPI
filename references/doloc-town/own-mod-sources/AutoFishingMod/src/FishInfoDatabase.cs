using System;

namespace Dlk.DolocAutoFishing
{
    internal sealed class FishInfo
    {
        internal readonly string Name;
        internal readonly string Source;
        internal readonly string MonthsText;
        internal readonly string LocationText;
        internal readonly string WeatherText;
        internal readonly string TimeText;
        internal readonly string UnlockText;
        internal readonly string RodText;
        internal readonly string PriceText;
        internal readonly int[] Months;
        internal readonly string[] WeatherKeys;
        internal readonly int StartMinute;
        internal readonly int EndMinute;
        internal readonly int RequiredRodLevel;
        internal readonly int Price;
        internal readonly string[] Details;

        internal FishInfo(
            string name,
            string source,
            string monthsText,
            string locationText,
            string weatherText,
            string timeText,
            string unlockText,
            string rodText,
            string priceText,
            int[] months,
            string[] weatherKeys,
            int startMinute,
            int endMinute,
            int requiredRodLevel,
            int price,
            string[] details)
        {
            Name = name;
            Source = source;
            MonthsText = monthsText;
            LocationText = locationText;
            WeatherText = weatherText;
            TimeText = timeText;
            UnlockText = unlockText;
            RodText = rodText;
            PriceText = priceText;
            Months = months ?? new int[0];
            WeatherKeys = weatherKeys ?? new string[0];
            StartMinute = startMinute;
            EndMinute = endMinute;
            RequiredRodLevel = requiredRodLevel;
            Price = price;
            Details = details ?? new string[0];
        }

        internal bool MatchesMonth(int month)
        {
            if (Months.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < Months.Length; i++)
            {
                if (Months[i] == month)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool MatchesWeather(string weatherKey)
        {
            if (WeatherKeys.Length == 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(weatherKey))
            {
                return false;
            }

            for (int i = 0; i < WeatherKeys.Length; i++)
            {
                if (string.Equals(WeatherKeys[i], weatherKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool MatchesTime(int minuteOfDay)
        {
            if (StartMinute < 0 || EndMinute < 0)
            {
                return true;
            }

            if (StartMinute <= EndMinute)
            {
                return minuteOfDay >= StartMinute && minuteOfDay <= EndMinute;
            }

            return minuteOfDay >= StartMinute || minuteOfDay <= EndMinute;
        }

        internal bool MatchesRod(int rodLevel)
        {
            return rodLevel >= RequiredRodLevel;
        }
    }

    internal static class FishInfoDatabase
    {
        internal static readonly FishInfo[] All = new FishInfo[]
        {
            new FishInfo("鲤鱼", "仅野外", "无要求", "淡水", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "30 G", new int[0], new string[0], -1, -1, 1, 30, new string[] { "简介", "随处可见，无处不在。", "购买价格", "60 G", "商店", "渔夫的店", "可繁育自", "仅可同类繁殖", "可培育", "锦鲤 闪亮黄金鱼", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "鲤鱼100%" }),
            new FishInfo("虹鳟鱼", "仅野外", "1、2 月", "优质淡水", "无要求", "无要求", "水质核心环境改造", "老旧鱼竿及以上", "75 G", new int[] { 1, 2 }, new string[0], -1, -1, 2, 75, new string[] { "简介", "广泛分布的入侵物种，凶猛的捕食者。", "可繁育自", "仅可同类繁殖", "可培育", "仅可同类繁殖", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "虹鳟鱼100%" }),
            new FishInfo("牙鱼", "仅野外", "3、4 月", "优质淡水", "无要求", "无要求", "水质核心环境改造", "老旧鱼竿及以上", "95 G", new int[] { 3, 4 }, new string[0], -1, -1, 2, 95, new string[] { "简介", "强悍善战的肉食性鱼类，嘴部因利齿永远无法闭合。", "可繁育自", "仅可同类繁殖", "可培育", "恐鱼", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.4% / 30 分钟", "饲料转化率", "0.13% / 饲料量", "非杂交产物", "牙鱼100%" }),
            new FishInfo("鲶鱼", "仅野外", "无要求", "淡水", "无要求", "18:00 ~ 2:55（次日）", "默认解锁", "老旧鱼竿及以上", "85 G", new int[0], new string[0], 1080, 175, 2, 85, new string[] { "简介", "并不能引发地震。", "可繁育自", "仅可同类繁殖", "可培育", "黑背沙丁鱼", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "鲶鱼100%" }),
            new FishInfo("草鱼", "仅野外", "无要求", "淡水", "雨天 雷雨 酸雨", "无要求", "默认解锁", "老旧鱼竿及以上", "50 G", new int[0], new string[] { "RAIN", "THUNDERSTORM", "ACID_RAIN" }, -1, -1, 2, 50, new string[] { "简介", "捞上来时还在嚼着水草，对繁殖条件较为敏感。", "购买价格", "100 G", "商店", "渔夫的店", "可繁育自", "仅可同类繁殖", "可培育", "草叶蛞蝓", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "草鱼75% 纤维25%" }),
            new FishInfo("水生变形虫", "仅野外", "无要求", "淡水（湿地）", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "55 G", new int[0], new string[0], -1, -1, 1, 55, new string[] { "简介", "为了不让凝胶层溶解在水中，发育出了不溶的薄膜层。", "可繁育自", "无", "可培育", "草叶蛞蝓", "鱼卵加工品", "无鱼卵", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.3% / 饲料量", "非杂交产物", "塑料瓶23.46% 垃圾32.26% 旧纸板10.26% 旧电池7.33% 罐头7.33% 瓶装水1.47% 破烂电线14.66% 保温杯0.29% 旧轮胎2.93%" }),
            new FishInfo("鲟鱼", "仅野外", "1、3 月", "优质淡水", "无要求", "无要求", "水质核心环境改造", "老旧鱼竿及以上", "1500 G", new int[] { 1, 3 }, new string[0], -1, -1, 2, 1500, new string[] { "简介", "战前曾因鱼子和栖息地破坏濒临灭绝，在战后几十年内得以恢复。", "可繁育自", "仅可同类繁殖", "可培育", "仅可同类繁殖", "鱼卵加工品", "美味鱼子酱", "鱼卵孵化时间", "3 天", "鱼苗成长时间", "7 天", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.07% / 饲料量", "非杂交产物", "鲟鱼100%" }),
            new FishInfo("泥鳅", "仅野外", "1、2 月", "浅水坑", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "45 G", new int[] { 1, 2 }, new string[0], -1, -1, 1, 45, new string[] { "简介", "水浅的泥泞处总能出现这种惊喜。", "可繁育自", "无", "可培育", "雾隐鳝", "鱼卵加工品", "无鱼卵", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "土块66.67% 沙子33.33%" }),
            new FishInfo("田螺", "仅野外", "3、4 月", "浅水坑", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "35 G", new int[] { 3, 4 }, new string[0], -1, -1, 1, 35, new string[] { "简介", "普普通通的螺类。适合爆炒？", "可繁育自", "无", "可培育", "无", "鱼卵加工品", "无鱼卵", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "土块55.56% 沙子88.89%" }),
            new FishInfo("黄鳝", "仅野外", "无要求", "淡水（湿地）", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "135 G", new int[0], new string[0], -1, -1, 1, 135, new string[] { "简介", "躲藏在泥坑和管道之中，滑不溜秋。", "可繁育自", "仅可同类繁殖", "可培育", "雾隐鳝", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "黄鳝100%" }),
            new FishInfo("狗鱼", "仅野外", "无要求", "淡水（湿地）", "无要求", "6:00 ~ 18:55", "默认解锁", "简易鱼竿及以上", "175 G", new int[0], new string[0], 360, 1135, 1, 175, new string[] { "简介", "是因为嘴大吃得多，所以这么有力量吗？", "可繁育自", "仅可同类繁殖", "可培育", "蓝狗鱼 恐鱼", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "狗鱼100%" }),
            new FishInfo("沙丁鱼", "仅野外", "无要求", "海水", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "30 G", new int[0], new string[0], -1, -1, 1, 30, new string[] { "简介", "永远在成群结队的前行。", "购买价格", "60 G", "商店", "康提基贸易商店", "可繁育自", "仅可同类繁殖", "可培育", "黑背沙丁鱼", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "沙丁鱼100%" }),
            new FishInfo("鲈鱼", "仅野外", "无要求", "海水", "无要求", "无要求", "默认解锁", "老旧鱼竿及以上", "65 G", new int[0], new string[0], -1, -1, 2, 65, new string[] { "简介", "颇受食客欢迎的凶猛肉食鱼类。", "可繁育自", "仅可同类繁殖", "可培育", "渊潜灯笼鲈", "鱼卵加工品", "腌鱼籽", "鱼卵孵化时间", "4 小时", "鱼苗成长时间", "12 小时", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "鲈鱼100%" }),
            new FishInfo("鳕鱼", "仅野外", "1、2 月", "海水", "无要求", "无要求", "默认解锁", "竹鱼竿及以上", "170 G", new int[] { 1, 2 }, new string[0], -1, -1, 3, 170, new string[] { "简介", "引发了一场不流血的战争的罪魁祸首。配合薯条食用更佳？", "可繁育自", "仅可同类繁殖", "可培育", "红鳕鱼", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.1% / 饲料量", "非杂交产物", "鳕鱼100%" }),
            new FishInfo("金枪鱼", "仅野外", "1、2 月", "海水", "无要求", "无要求", "岩芯样本环境改造", "竹鱼竿及以上", "400 G", new int[] { 1, 2 }, new string[0], -1, -1, 3, 400, new string[] { "简介", "快速且迅猛，常规鱼线很难经得起这种大型鱼类的力量。", "可繁育自", "仅可同类繁殖", "可培育", "帝王金枪鱼", "鱼卵加工品", "美味鱼子酱", "鱼卵孵化时间", "1 天", "鱼苗成长时间", "2 天", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.07% / 饲料量", "非杂交产物", "金枪鱼75% 罐头25%" }),
            new FishInfo("鳗鱼", "仅野外", "3、4 月", "海水", "无要求", "无要求", "岩芯样本环境改造", "老旧鱼竿及以上", "155 G", new int[] { 3, 4 }, new string[0], -1, -1, 2, 155, new string[] { "简介", "很长很长很长长长长长。", "可繁育自", "仅可同类繁殖", "可培育", "银光鳗", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "鳗鱼100%" }),
            new FishInfo("荧光鳗鱼", "仅野外", "无要求", "海水", "烈日", "无要求", "岩芯样本环境改造", "老旧鱼竿及以上", "300 G", new int[0], new string[] { "SCORCH_SUN" }, -1, -1, 2, 300, new string[] { "简介", "或许可以拿回去做水族箱的天然灯带。", "可繁育自", "仅可同类繁殖", "可培育", "渊潜灯笼鲈", "鱼卵加工品", "美味鱼子酱", "鱼卵孵化时间", "1 天", "鱼苗成长时间", "2 天", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.07% / 饲料量", "非杂交产物", "荧光鳗鱼100%" }),
            new FishInfo("拟态骨架鱼", "仅野外", "3、4 月", "海水（栈桥下）", "无要求", "无要求", "默认解锁", "老旧鱼竿及以上", "195 G", new int[] { 3, 4 }, new string[0], -1, -1, 2, 195, new string[] { "简介", "战后为了躲过捕捞，进化出骨架拟态的特殊鱼类。", "可繁育自", "仅可同类繁殖", "可培育", "黑夜骨架鱼", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "拟态骨架鱼100%" }),
            new FishInfo("虾", "仅野外", "无要求", "淡水/海水", "无要求", "无要求", "默认解锁", "简易鱼竿及以上", "25 G", new int[0], new string[0], -1, -1, 1, 25, new string[] { "简介", "并不是总是红色的。", "购买价格", "50 G", "商店", "渔夫的店", "可繁育自", "无", "可培育", "无", "鱼卵加工品", "无鱼卵", "饲料食量", "1 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.2% / 饲料量", "非杂交产物", "石头100%" }),
            new FishInfo("鲑鱼", "仅野外", "1 月", "淡水/海水", "无要求", "无要求", "鲑鱼节", "竹鱼竿及以上", "145 G", new int[] { 1 }, new string[0], -1, -1, 3, 145, new string[] { "简介", "带着海洋的营养洄游到上游产卵，前提是没有水坝。", "可繁育自", "仅可同类繁殖", "可培育", "大马哈鱼", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "3 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.1% / 饲料量", "非杂交产物", "鲑鱼100%" }),
            new FishInfo("锦鲤", "野外及养殖", "无要求", "优质淡水", "无要求", "无要求", "水质核心环境改造", "老旧鱼竿及以上", "105 G", new int[0], new string[0], -1, -1, 2, 105, new string[] { "简介", "象征吉祥的观赏鱼类，传说是龙的幼年形态。", "可繁育自", "鲤鱼 ≥ 366.67%按照公式配比鱼缸后，产出锦鲤鱼卵的概率。", "可培育", "闪亮黄金鱼", "鱼卵加工品", "鱼子酱", "鱼卵孵化时间", "12 小时", "鱼苗成长时间", "1 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.3% / 30 分钟", "饲料转化率", "0.15% / 饲料量", "非杂交产物", "锦鲤50% 鲤鱼25% 车票25%" }),
            new FishInfo("闪亮黄金鱼", "野外及养殖", "无要求", "淡水（隐秘处）", "无要求", "无要求", "默认解锁", "老旧鱼竿及以上", "1000 G", new int[0], new string[0], -1, -1, 2, 1000, new string[] { "简介", "发出耀眼的金光！难道真的是黄金做的？", "可繁育自", "鲤鱼 × 6 锦鲤 × 610%按照公式配比鱼缸后，产出闪亮黄金鱼鱼卵的概率。", "可培育", "帝王金枪鱼", "鱼卵加工品", "美味鱼子酱", "鱼卵孵化时间", "2 天", "鱼苗成长时间", "5 天", "饲料食量", "2 / 30 分钟", "鱼缸产出进度贡献", "0.2% / 30 分钟", "饲料转化率", "0.1% / 饲料量", "非杂交产物", "闪亮黄金鱼39.22% 鲤鱼19.61% 锦鲤39.22% 金矿石1.96%" }),
        };

        internal static bool IsDefaultUnlock(FishInfo fish)
        {
            return fish == null ||
                   string.IsNullOrEmpty(fish.UnlockText) ||
                   fish.UnlockText == "默认解锁" ||
                   fish.UnlockText == "无要求";
        }
    }
}
