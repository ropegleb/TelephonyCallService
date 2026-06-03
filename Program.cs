using TelephonyCallService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SessionRepository>();
builder.Services.AddHostedService<CleanupService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/session", (SessionRequest req, SessionRepository repo) =>
{
    var xi = ContactParser.ExtractXi(req.Contact);
    if (xi is null)
        return Results.BadRequest(new { error = "x-i parameter not found in contact header" });

    var (prevFrom, prevCallId) = repo.Upsert(xi, req.From, req.CallId);

    return Results.Ok(new SessionResponse
    {
        LastFrom   = prevFrom   ?? req.From,
        LastCallId = prevCallId ?? req.CallId,
    });
})
.Accepts<SessionRequest>("application/json")
.Produces<SessionResponse>(200)
.Produces(400)
.WithName("PostSession")
.WithTags("Session");

app.Run();
