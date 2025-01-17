// See https://aka.ms/new-console-template for more information
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

// Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
// Hence instrumentation key/ connection string and any changes to default logging level must be specified here.
const string appInsightsConnectionString = "InstrumentationKey=edfb6c67-6cc9-4771-88a5-e12c838c94aa;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=3fed21c1-f27e-4bf3-aa02-27dda886722a";

var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
telemetryConfiguration.ConnectionString = appInsightsConnectionString;
var telemetryClient = new TelemetryClient(telemetryConfiguration);
Console.WriteLine("Application is connected to Application Insights ☁️");

telemetryClient.Context.Cloud.RoleName = "My App";
telemetryClient.Context.Cloud.RoleInstance = Guid.NewGuid().ToString();

// Enable Application Insights internal logging to console
TelemetryDebugWriter.IsTracingDisabled = false;

using var operation = telemetryClient.StartOperation<RequestTelemetry>("big exception");

var properties = new Dictionary<string, string>
        {
            { "Username", Environment.UserName },
            { "Device", Environment.MachineName },
            { "ApplicationVersion", "1.0.0-testing" }
        };

foreach (var arg in properties)
{
    operation.Telemetry.Properties.Add(arg.Key, arg.Value);
}

// To pass a connection string
// - aiserviceoptions must be created
// - set connectionstring on it
// - pass it to AddApplicationInsightsTelemetryWorkerService()

telemetryClient.TrackTrace($"Worker running at: {DateTimeOffset.Now}", SeverityLevel.Information);

// Replace with a name which makes sense for this operation.
using (telemetryClient.StartOperation<RequestTelemetry>("operation"))
{
    telemetryClient.TrackTrace("A sample warning message. By default, logs with severity Warning or higher is captured by Application Insights", SeverityLevel.Warning);
    telemetryClient.TrackTrace("Calling bing.com");
    using var httpClient = new HttpClient();
    var res = await httpClient.GetAsync("https://bing.com");
    telemetryClient.TrackTrace("Calling bing completed with status:" + res.StatusCode);
    telemetryClient.TrackEvent("Bing call event completed");

    // Simulate exception
    // create a 3KB string
    var largeString = new string('a', 6000);
    // create an array of 20 exceptions with long messages
    var exceptions = new List<Exception>();
    for (int i = 0; i < 20; i++)
    {
        exceptions.Add(new InvalidOperationException("Exception " + i + largeString) { Source = "SampleSource" });
    }

    var aggException = new AggregateException("Sample aggregate exception",
        exceptions
    );

    try
    {
        throw exceptions[0];
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine("Logging one of the big exceptions");
        telemetryClient.TrackException(ex);
    }

    try
    {
        throw new Exception("Sample Large exception", aggException);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Logging exception with Aggregate exception with 20 inner exceptions");
        telemetryClient.TrackException(ex);
    }
}

Console.WriteLine("Press any key to exit");
Console.ReadLine();

// Explicitly call Flush() followed by sleep is required in console apps.
// This is to ensure that even if application terminates, telemetry is sent to the back-end.
telemetryClient.Flush();
Task.Delay(5000).Wait();