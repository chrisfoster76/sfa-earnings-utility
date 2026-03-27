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
            string selectedEnvName;
            string selectedNamespace;
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

                Console.WriteLine();
                WriteColor("Sending...", ConsoleColor.DarkGray);
                Console.WriteLine();

                await SendShortCourseApproval(endpointInstance, uln, employerAccountId);
                Console.WriteLine();
            }

            await endpointInstance.Stop().ConfigureAwait(false);
            WriteColor("Goodbye.", ConsoleColor.DarkGray);
        }

        private static async Task SendShortCourseApproval(IEndpointInstance endpointInstance, string uln, long employerAccountId)
        {
            var eventMessage = new ApprenticeshipCreatedEvent
            {
                LearningType = LearningType.ApprenticeshipUnit,
                ApprenticeshipId = 1,
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

        private static void WriteColor(string text, ConsoleColor color, bool newLine = true)
        {
            Console.ForegroundColor = color;
            if (newLine) Console.WriteLine(text);
            else Console.Write(text);
            Console.ResetColor();
        }
    }
}
