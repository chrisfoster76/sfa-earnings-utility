using Azure.Identity;
using Microsoft.Extensions.Configuration;
using NServiceBus.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.UI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var appSettings = configuration.Get<AppSettings>()
                ?? throw new InvalidOperationException("appsettings.json could not be bound to AppSettings.");

            if (appSettings.Environments.Count == 0)
                throw new InvalidOperationException("No environments configured in appsettings.json.");

            var cliArgs = ParseArgs(args);
            cliArgs.TryGetValue("--env", out var cliEnv);
            cliArgs.TryGetValue("--uln", out var cliUln);
            cliArgs.TryGetValue("--employer", out var cliEmployer);
            cliArgs.TryGetValue("--employer-type", out var cliEmployerType);
            cliArgs.TryGetValue("--apprenticeship-id", out var cliApprenticeshipId);
            cliArgs.TryGetValue("--transfer-sender", out var cliTransferSender);
            bool oneShot = cliEnv != null && cliUln != null && cliEmployer != null && long.TryParse(cliEmployer, out _)
                && cliEmployerType != null && Enum.TryParse<ApprenticeshipEmployerType>(cliEmployerType, ignoreCase: true, out _)
                && cliApprenticeshipId != null && long.TryParse(cliApprenticeshipId, out _);

            string selectedEnvName;
            string selectedNamespace;

            if (oneShot)
            {
                if (!appSettings.Environments.TryGetValue(cliEnv!, out var ns))
                {
                    Console.Error.WriteLine($"Unknown environment '{cliEnv}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                    Environment.Exit(1);
                    return;
                }
                selectedEnvName = cliEnv!;
                selectedNamespace = ns;
            }
            else
            {
                Console.Clear();
                WriteColor("========================================", ConsoleColor.Cyan);
                WriteColor("  SFA Earnings Utility", ConsoleColor.Cyan);
                WriteColor("========================================", ConsoleColor.Cyan);
                Console.WriteLine();
                WriteColor("Select an environment:", ConsoleColor.White);

                var envNames = appSettings.Environments.Keys.ToList();
                for (int i = 0; i < envNames.Count; i++)
                    WriteColor($"  {i + 1}. {envNames[i]}", ConsoleColor.Yellow);

                Console.WriteLine();
                selectedEnvName = null!;
                selectedNamespace = null!;
                while (true)
                {
                    WriteColor("Enter number: ", ConsoleColor.White, newLine: false);
                    var input = Console.ReadLine()?.Trim();
                    if (int.TryParse(input, out int choice) && choice >= 1 && choice <= envNames.Count)
                    {
                        selectedEnvName = envNames[choice - 1];
                        selectedNamespace = appSettings.Environments[selectedEnvName];
                        break;
                    }
                    WriteColor("Invalid selection, please try again.", ConsoleColor.Red);
                }
            }

            Console.WriteLine();
            WriteColor($"Connecting to {selectedEnvName}...", ConsoleColor.DarkGray);

            LogManager.Use<DefaultFactory>().Level(LogLevel.Fatal);

            var endpointConfiguration = new EndpointConfiguration("Test.Sender");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendOnly();

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.CustomTokenCredential(selectedNamespace, new DefaultAzureCredential());

            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningEventsAs(type => type.Name.EndsWith("Event"))
                       .DefiningCommandsAs(type => type.Namespace != null && type.Name.EndsWith("Command"));

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
            WriteColor("Connected.", ConsoleColor.Green);

            if (oneShot)
            {
                var employerAccountId = long.Parse(cliEmployer!);
                var employerType = Enum.Parse<ApprenticeshipEmployerType>(cliEmployerType!, ignoreCase: true);
                var apprenticeshipId = long.Parse(cliApprenticeshipId!);
                long? transferSenderId = long.TryParse(cliTransferSender, out var ts) ? ts : null;
                Console.WriteLine();
                WriteColor("Sending...", ConsoleColor.DarkGray);
                await SendShortCourseApproval(endpointInstance, cliUln!, employerAccountId, employerType, apprenticeshipId, transferSenderId);
            }
            else
            {
                Console.Clear();
                WriteColor("========================================", ConsoleColor.Cyan);
                WriteColor("  SFA Earnings Utility", ConsoleColor.Cyan);
                WriteColor("========================================", ConsoleColor.Cyan);
                Console.WriteLine();
                WriteColor($"Connected to: {selectedNamespace}", ConsoleColor.Green);
                Console.WriteLine();
                Console.WriteLine("This utility simulates a Short Course Approval by an Employer by sending an ApprenticeshipCreatedEvent via service bus.");
                Console.WriteLine("You will be prompted to enter the ULN and the EmployerAccountId. The ULN should match that of the ShortCourse you are trying to approve.");
                Console.WriteLine();

                while (true)
                {
                    WriteColor("Press Escape to exit, or any other key to send a Short Course Approval...", ConsoleColor.Yellow);

                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    Console.WriteLine();
                    WriteColor("Enter the learner's ULN: ", ConsoleColor.White, newLine: false);
                    var uln = Console.ReadLine() ?? string.Empty;

                    WriteColor("Enter the approving Employer's Account ID (numeric): ", ConsoleColor.White, newLine: false);
                    var employerAccountId = long.Parse(Console.ReadLine() ?? "0");

                    ApprenticeshipEmployerType employerType;
                    while (true)
                    {
                        WriteColor($"Enter Employer Type ({string.Join("/", Enum.GetNames<ApprenticeshipEmployerType>())}): ", ConsoleColor.White, newLine: false);
                        if (Enum.TryParse<ApprenticeshipEmployerType>(Console.ReadLine(), ignoreCase: true, out employerType))
                            break;
                        WriteColor("Invalid value, please try again.", ConsoleColor.Red);
                    }

                    WriteColor("Enter the Apprenticeship ID (numeric): ", ConsoleColor.White, newLine: false);
                    var apprenticeshipId = long.Parse(Console.ReadLine() ?? "0");

                    WriteColor("Enter Transfer Sender ID (or press Enter to skip): ", ConsoleColor.White, newLine: false);
                    var transferSenderInput = Console.ReadLine()?.Trim();
                    long? transferSenderId = long.TryParse(transferSenderInput, out var ts) ? ts : null;

                    Console.WriteLine();
                    WriteColor("Sending...", ConsoleColor.DarkGray);
                    Console.WriteLine();

                    await SendShortCourseApproval(endpointInstance, uln, employerAccountId, employerType, apprenticeshipId, transferSenderId);
                    Console.WriteLine();
                }

                WriteColor("Goodbye.", ConsoleColor.DarkGray);
            }

            await endpointInstance.Stop().ConfigureAwait(false);
        }

        private static async Task SendShortCourseApproval(IEndpointInstance endpointInstance, string uln, long employerAccountId, ApprenticeshipEmployerType employerType, long apprenticeshipId, long? transferSenderId = null)
        {
            var eventMessage = new ApprenticeshipCreatedEvent
            {
                LearningType = LearningType.ApprenticeshipUnit,
                ApprenticeshipId = apprenticeshipId,
                ApprenticeshipEmployerTypeOnApproval = employerType,
                TransferSenderId = transferSenderId,
                ApprenticeshipHashedId = "XYZ123",
                Uln = uln,
                ProviderId = 10005077,
                DateOfBirth = DateTime.Parse("2005-01-14"),
                FirstName = "John",
                LastName = "Smith",
                IsOnFlexiPaymentPilot = true,
                ActualStartDate = DateTime.Parse("2018-08-01"),
                StartDate = DateTime.Parse("2018-08-01"),
                EndDate = DateTime.Parse("2020-07-31"),
                TrainingCode = "21",
                TrainingCourseVersion = "",
                TrainingCourseOption = "",
                TrainingType = ProgrammeType.Standard,
                LegalEntityName = "Mega Corp",
                AccountLegalEntityId = 456,
                AccountId = employerAccountId,
                PriceEpisodes = new PriceEpisode[]
                {
                    new PriceEpisode
                    {
                        Cost = 30000,
                        EndPointAssessmentPrice = 5000,
                        TrainingPrice = 25000,
                        FromDate = DateTime.Parse("2018-08-01"),
                        ToDate = DateTime.Parse("2020-07-31"),
                    }
                }
            };

            await endpointInstance.Publish(eventMessage).ConfigureAwait(false);

            WriteColor($"Short Course Approval sent for ULN {uln} (Employer Account ID: {employerAccountId}).", ConsoleColor.Green);
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].StartsWith("--"))
                    result[args[i]] = args[i + 1];
            return result;
        }

        private static void WriteColor(string text, ConsoleColor color, bool newLine = true)
        {
            Console.ForegroundColor = color;
            if (newLine) Console.WriteLine(text);
            else Console.Write(text);
            Console.ResetColor();
        }
    }
}
