namespace Bonus.Application.Common;

public static class BonusRounding
{
    public static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal RoundPoints(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
