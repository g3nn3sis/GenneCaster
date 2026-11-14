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

        internal sealed class Snapshot
        {
            internal int TimeIndex;
            internal BetterDayNightManager.WeatherType[] WeatherCycle = Array.Empty<BetterDayNightManager.WeatherType>();
        }

        internal static bool TryReadEnvironment(out int timeIndex, out bool raining)
        {
            BetterDayNightManager? manager = BetterDayNightManager.instance;
            timeIndex = 0;
            raining = false;
            if (manager == null) return false;
            timeIndex = manager.currentTimeIndex;
            BetterDayNightManager.WeatherType[]? cycle = manager.weatherCycle;
            int weather = manager.currentWeatherIndex;
            raining = cycle != null && weather >= 0 && weather < cycle.Length && cycle[weather] == BetterDayNightManager.WeatherType.Raining;
            return true;
        }

        internal static Snapshot? Capture()
        {
            BetterDayNightManager? manager = BetterDayNightManager.instance;
            if (manager == null) return null;
            return new Snapshot
            {
                TimeIndex = manager.currentTimeIndex,
                WeatherCycle = manager.weatherCycle != null ? (BetterDayNightManager.WeatherType[])manager.weatherCycle.Clone() : Array.Empty<BetterDayNightManager.WeatherType>(),
            };
        }

        internal static void Apply(int timeIndex, bool raining)
        {
            if (BetterDayNightManager.instance == null) return;
            SetTime(timeIndex);
            SetWeather(raining ? BetterDayNightManager.WeatherType.Raining : BetterDayNightManager.WeatherType.None);
        }

        internal static void Restore(Snapshot snapshot)
        {
            BetterDayNightManager? manager = BetterDayNightManager.instance;
            if (manager == null) return;
            SetTime(snapshot.TimeIndex);
            if (manager.weatherCycle == null || manager.weatherCycle.Length != snapshot.WeatherCycle.Length) return;
            Array.Copy(snapshot.WeatherCycle, manager.weatherCycle, snapshot.WeatherCycle.Length);
        }

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
