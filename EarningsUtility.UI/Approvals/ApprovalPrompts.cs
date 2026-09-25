using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.UI.Approvals;

public static class ApprovalPrompts
{
    public static ApprovalRequest Collect()
    {
        LearningType learningType;
        while (true)
        {
            ConsoleWriter.WriteColor("Select approval type (1=Short Course, 2=Apprenticeship): ", ConsoleColor.White, newLine: false);
            var typeInput = Console.ReadLine()?.Trim();
            if (typeInput == "1") { learningType = LearningType.ApprenticeshipUnit; break; }
            if (typeInput == "2") { learningType = LearningType.Apprenticeship; break; }
            ConsoleWriter.WriteColor("Invalid selection, please enter 1 or 2.", ConsoleColor.Red);
        }

        ConsoleWriter.WriteColor("Enter the learner's ULN: ", ConsoleColor.White, newLine: false);
        var uln = Console.ReadLine() ?? string.Empty;

        ConsoleWriter.WriteColor("Enter the approving Employer's Account ID (numeric): ", ConsoleColor.White, newLine: false);
        var employerAccountId = long.Parse(Console.ReadLine() ?? "0");

        ApprenticeshipEmployerType employerType;
        while (true)
        {
            ConsoleWriter.WriteColor($"Enter Employer Type ({string.Join("/", Enum.GetNames<ApprenticeshipEmployerType>())}): ", ConsoleColor.White, newLine: false);
            if (Enum.TryParse<ApprenticeshipEmployerType>(Console.ReadLine(), ignoreCase: true, out employerType))
                break;
            ConsoleWriter.WriteColor("Invalid value, please try again.", ConsoleColor.Red);
        }

        ConsoleWriter.WriteColor("Enter the Apprenticeship ID (numeric): ", ConsoleColor.White, newLine: false);
        var apprenticeshipId = long.Parse(Console.ReadLine() ?? "0");

        ConsoleWriter.WriteColor("Enter the Provider's UKPRN (numeric): ", ConsoleColor.White, newLine: false);
        var ukprn = long.Parse(Console.ReadLine() ?? "0");

        string trainingCode;
        while (true)
        {
            ConsoleWriter.WriteColor("Enter the Training Code: ", ConsoleColor.White, newLine: false);
            trainingCode = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(trainingCode))
                break;
            ConsoleWriter.WriteColor("Training Code cannot be empty, please try again.", ConsoleColor.Red);
        }

        ConsoleWriter.WriteColor("Enter Transfer Sender ID (or press Enter to skip): ", ConsoleColor.White, newLine: false);
        var transferSenderInput = Console.ReadLine()?.Trim();
        long? transferSenderId = long.TryParse(transferSenderInput, out var ts) ? ts : null;

        return new ApprovalRequest(uln, employerAccountId, employerType, apprenticeshipId, learningType, ukprn, trainingCode, transferSenderId);
    }
}
