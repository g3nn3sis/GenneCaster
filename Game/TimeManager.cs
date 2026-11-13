// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;

namespace GenneCaster.Game
{
    internal static class TimeManager
    {
        internal static void SetDay() => SetTime(3);
        internal static void SetNight() => SetTime(0);
        internal static void SetMorning() => SetTime(1);
        internal static void SetEvening() => SetTime(7);
        internal static void StartRain() => SetWeather(BetterDayNightManager.WeatherType.Raining);
        internal static void StopRain() => SetWeather(BetterDayNightManager.WeatherType.None);

        private static void SetTime(int index)
        {
            BetterDayNightManager? manager = BetterDayNightManager.instance;
            if (manager == null)
            {
                Log.Warning("The day/night manager is not loaded yet.");
                return;
            }
            if (manager.currentTimeIndex != index) manager.SetTimeOfDay(index);
        }

        private static void SetWeather(BetterDayNightManager.WeatherType weather)
        {
            BetterDayNightManager? manager = BetterDayNightManager.instance;
            if (manager == null || manager.weatherCycle == null)
            {
                Log.Warning("The day/night manager is not loaded yet.");
                return;
            }
            for (int i = 1; i < manager.weatherCycle.Length; i++)
                manager.weatherCycle[i] = weather;
        }
    }
}
