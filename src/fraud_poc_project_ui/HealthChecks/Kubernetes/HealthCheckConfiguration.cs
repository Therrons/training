using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace HealthChecks.Kubernetes
{
    public static class HealthCheckConfiguration
    {
        // the below hooks up to the values.yaml in the HELM chart under 'livenessProbe | readinessProbe'

        public static WebApplication ConfigureHealthChecks(this WebApplication app)
        {
            // The application is ready to serve traffic.
            app.MapHealthChecks("/health/liveness", new HealthCheckOptions() { });

            // All the dependencies are ready.
            app.MapHealthChecks("/health/readiness", new HealthCheckOptions { });

            return app;
        }
    }
}
