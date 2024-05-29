using Clerk.Net.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.IdentityModel.Tokens;
using Mocket.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(builder.Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());

builder.Services.AddClerkApiClient(config =>
{
    config.SecretKey = builder.Configuration["Jwt:Key"];
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        // Authority is the URL of your clerk instance
        x.Authority = builder.Configuration["Jwt:Authority"];
        x.TokenValidationParameters = new TokenValidationParameters()
        {
            // Disable audience validation as we aren't using it
            ValidateAudience = false,
            NameClaimType = ClaimTypes.NameIdentifier,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidIssuer = builder.Configuration["Jwt:Issuer"]
        };
        x.Events = new JwtBearerEvents()
        {
            // Additional validation for AZP claim
            OnTokenValidated = context =>
            {
                var azp = context.Principal?.FindFirstValue("azp");
                // AuthorizedParty is the base URL of your frontend.
                if (string.IsNullOrEmpty(azp) || !azp.Equals(builder.Configuration["Jwt:Azp"]) || !azp.Equals("http://localhost:3000/"))
                    context.Fail("AZP Claim is invalid or missing");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy => policy.RequireClaim("UserId"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Welcome to the Mocket!");

app.Run();

static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
{
    string account = configurationSection["Account"];
    string key = configurationSection["Key"];
    string databaseName = configurationSection["DatabaseName"];
    string containerName = configurationSection["ContainerName"];
    CosmosClient client = new CosmosClient(account, key);
    CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
    DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
    await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
    return cosmosDbService;
}
