using Supabase;

namespace EmpLeave.Services.SupabaseServices;

public interface ISupabaseInitializer
{
    Task<Client> InitializeAsync();
}

public class SupabaseInitializer : ISupabaseInitializer
{
    private readonly IConfiguration _configuration;

    public SupabaseInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Client> InitializeAsync()
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:Key"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            throw new InvalidOperationException("Supabase configuration is missing in appsettings.json");

        var client = new Client(supabaseUrl, supabaseKey);
        await client.InitializeAsync();
        
        return client;
    }
}
