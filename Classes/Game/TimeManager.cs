using TMPro;
using UnityEngine;

namespace GenneCaster.Classes.Game
{
    internal class TimeManager
    {
        public static void SetTime(string timetoSet)
        {
            int time = timetoSet.ToIntArray()[0];
            BetterDayNightManager.instance.SetTimeOfDay(time);
        }
        public static void SetDay() => SetTimeInternal(3);
        public static void SetNight() => SetTimeInternal(0);
        public static void SetMorning() => SetTimeInternal(1);
        public static void SetEvening() => SetTimeInternal(7);
        private static void SetTimeInternal(int time)
        {
            if (BetterDayNightManager.instance.currentTimeIndex == time) return;
            BetterDayNightManager.instance.SetTimeOfDay(time);
        }
        public static void StartRain()
        {
            var weather = BetterDayNightManager.instance.weatherCycle;
            for (int i = 1; i < weather.Length; i++)
                weather[i] = BetterDayNightManager.WeatherType.Raining;
        }
        public static void StopRain()
        {
            var weather = BetterDayNightManager.instance.weatherCycle;
            for (int i = 1; i < weather.Length; i++)
                weather[i] = BetterDayNightManager.WeatherType.None;
        }
    }
}
