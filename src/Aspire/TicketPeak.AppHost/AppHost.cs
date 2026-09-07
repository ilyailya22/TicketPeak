IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Phase 0 orchestrates a single project. The containerised data stores (SQL, Redis, Mongo,
// RabbitMQ) join here from Phase 2 onward, which is when Docker becomes a hard requirement.
builder.AddProject<Projects.TicketPeak_Api>("api")
       .WithHttpHealthCheck("/health/ready");

builder.Build().Run();
