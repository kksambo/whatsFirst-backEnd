using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ApplicationDb"));

// Add CORS policy to allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();


const string TWILIO_ACCOUNT_SID = "ACb96c08ad37433c682164c7e2b651e96a";
const string TWILIO_AUTH_TOKEN = "a935a74cb50697ffdfdac39f72b9b4d4";
const string TWILIO_PHONE_NUMBER = "+17439106215";



TwilioClient.Init(TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN);



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use the CORS policy
app.UseCors("AllowAll");

app.MapGet("/", () =>
{
    var student = new Student("John Doe", 20);
    return Results.Ok(student);
});

app.MapPost("/student", async (Student student, AppDbContext dbContext) =>
{
    dbContext.Students.Add(student);
    await dbContext.SaveChangesAsync();
    return Results.Ok(student);
});

app.MapGet("/students", async (AppDbContext dbContext) =>
{
    var students = await dbContext.Students.ToListAsync();
    return Results.Ok(students);
});

app.MapGet("/callSummarySize", async (AppDbContext dbContext) =>
{
    var callSummary = await dbContext.CallSumary.ToListAsync();
    return Results.Ok(callSummary.Count);
});

app.MapGet("/callSummary", async (AppDbContext dbContext) =>
{
    var callSummary = await dbContext.CallSumary.ToListAsync();
    return Results.Ok(callSummary);
});

app.MapPost("/callSummary", async (CallSummary callSummary, AppDbContext dbContext) =>
{
    dbContext.CallSumary.Add(callSummary);
    await dbContext.SaveChangesAsync();
    return Results.Ok(callSummary);
});

app.MapGet("/dialers", async (AppDbContext dbContext) =>
{
    var dialers = await dbContext.Dialers.ToListAsync();
    return Results.Ok(dialers);
});

app.MapPost("/dialer", async (Dialer dialer, AppDbContext dbContext) =>
{
    dbContext.Dialers.Add(dialer);
    await dbContext.SaveChangesAsync();
    return Results.Ok(dialer);
});

app.MapGet("/agents", async (AppDbContext dbContext) =>
{
    var agents = await dbContext.Agents.ToListAsync();
    return Results.Ok(agents);
});

app.MapPost("/agent", async (Agent agent, AppDbContext dbContext) =>
{
    dbContext.Agents.Add(agent);
    await dbContext.SaveChangesAsync();
    return Results.Ok(agent);
});

app.MapPost("/client", async (Client client, AppDbContext dbContext) =>
{
    dbContext.Clients.Add(client);
    await dbContext.SaveChangesAsync();
    return Results.Ok(client);
});

app.MapGet("/clients", async (AppDbContext dbContext) =>
{
    var clients = await dbContext.Clients.ToListAsync();
    return Results.Ok(clients);
});

app.MapPost("/api/call", async (CallRequestModel request, AppDbContext dbContext) =>
{
    try
    {
        if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
        {
            throw new Exception("PhoneNumber and Message are required.");
        }

        var call = CallResource.Create(
            to: new PhoneNumber(request.PhoneNumber),
            from: new PhoneNumber(TWILIO_PHONE_NUMBER),
            twiml: new Twilio.TwiML.VoiceResponse().Say("").ToString()
        );

        // Update or create a CallSummary entry
        var callSummary = await dbContext.CallSumary.FirstOrDefaultAsync();
        if (callSummary == null)
        {
            callSummary = new CallSummary
            {
                UnansweredCalls = 0,
                WhatsAppCalls = 0,
                NormalCalls = 1, // Increment NormalCalls for the first call
                CallMadeToday = 1
            };
            dbContext.CallSumary.Add(callSummary);
        }
        else
        {
            callSummary.NormalCalls++;
            callSummary.CallMadeToday++;
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { status = "success", call_sid = call.Sid });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/whatsapp", async (CallRequestModel request) =>
{
    try
    {
        if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
        {
            throw new Exception("PhoneNumber and Message are required.");
        }

        var message = MessageResource.Create(
            to: new PhoneNumber($"whatsapp:{request.PhoneNumber}"),
            from: new PhoneNumber("whatsapp:+14155238886"), 
            body: request.Message
        );

        return Results.Ok(new { status = "success", message_sid = message.Sid });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/login", async (LoginRequest loginRequest, AppDbContext dbContext) =>
{
    var agent = await dbContext.Agents
        .FirstOrDefaultAsync(a => a.Email == loginRequest.Email && a.Password == loginRequest.Password);

    if (agent == null)
    {
        return Results.BadRequest("Invalid email or password.");
    }

    return Results.Ok(new
    {
        Message = "Login successful",
        Agent = agent
    });
});



app.Run();

public record CallRequest(string PhoneNumber, string Message);

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}