using System.Globalization;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.ApprovalsStub;

public static class CocPriceParser
{
    public const string RowFormat = "yyyy-MM-dd:trainingPrice:assessmentPrice";

    public static bool TryParseRow(string? input, out LearningChangeEvent.Price? price, out string error)
    {
        price = null;
        var parts = (input ?? string.Empty).Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            error = $"Expected {RowFormat}, e.g. 2025-09-01:7200:1800.";
            return false;
        }

        if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var effectiveFrom))
        {
            error = $"'{parts[0]}' is not a valid yyyy-MM-dd date.";
            return false;
        }

        if (!TryParseAmount(parts[1], out var training) || !TryParseAmount(parts[2], out var assessment))
        {
            error = "Prices must be numbers of 0 or more, with no thousands separators.";
            return false;
        }

        price = new LearningChangeEvent.Price
        {
            EffectiveFrom = effectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TrainingPrice = training,
            AssessmentPrice = assessment
        };
        error = string.Empty;
        return true;
    }

    public static bool TryParseList(string? input, out List<LearningChangeEvent.Price> prices, out string error)
    {
        prices = new List<LearningChangeEvent.Price>();
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "At least one price is required.";
            return false;
        }

        foreach (var row in input.Split(','))
        {
            if (!TryParseRow(row, out var price, out error))
            {
                prices.Clear();
                return false;
            }
            prices.Add(price!);
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseAmount(string value, out decimal amount) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount >= 0;
}
