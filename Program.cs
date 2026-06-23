using TelephonyCallService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SessionRepository>();
builder.Services.AddHostedService<CleanupService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/from", (PostFromRequest req, SessionRepository repo) =>
{
    var xi = ContactParser.ExtractXi(req.Contact);
    if (xi is null)
        return Results.BadRequest(new { error = "x-i parameter not found in contact header" });

    repo.Save(xi, req.From);
    return Results.Ok();
})
.Accepts<PostFromRequest>("application/json")
.Produces(200)
.Produces(400)
.WithName("PostFrom")
.WithTags("From");

app.MapGet("/from", (string contact, SessionRepository repo) =>
{
    var xi = ContactParser.ExtractXi(contact);
    if (xi is null)
        return Results.BadRequest(new { error = "x-i parameter not found in contact header" });

    var from = repo.GetFrom(xi);
    return Results.Ok(new { from = from ?? string.Empty });
})
.Produces<object>(200)
.Produces(400)
.WithName("GetFrom")
.WithTags("From");

app.Run();
