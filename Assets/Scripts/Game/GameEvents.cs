using System;

public static class GameEvents
{
    public static event Action<bool> OnPowerRestored; // bool is if the cells are placed correctly or wrongly

    public static void TriggerPowerRestored(bool correctCellsPlaced)
    {
        OnPowerRestored?.Invoke(correctCellsPlaced);
    }
}