using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Replaces Azure Blob Storage with a fake so uploads can be tested offline.</summary>
public class FakeBlobStorage : IBlobStorageService
{
    public Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default) =>
        Task.FromResult($"https://fake.blob.test/{folder}/{file.FileName}");
}

/// <summary>The normal in-memory API, but with the fake blob storage swapped in.</summary>
public class FakeBlobApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IBlobStorageService, FakeBlobStorage>()));
    }
}

public class UploadTests : IClassFixture<FakeBlobApiFactory>
{
    private readonly FakeBlobApiFactory _factory;
    public UploadTests(FakeBlobApiFactory factory) => _factory = factory;

    private static MultipartFormDataContent ImageForm(string contentType, string fileName = "pic.png")
    {
        var bytes = new ByteArrayContent(new byte[10]);
        bytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { bytes, "file", fileName } };
    }

    private async Task<(HttpClient organiser, int eventId)> OrganiserWithEventAsync()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(org);
        return (org, id);
    }

    [Fact]
    public async Task Organiser_can_upload_banner_for_own_event()
    {
        var (org, id) = await OrganiserWithEventAsync();
        var res = await org.PostAsync($"/api/events/{id}/banner", ImageForm("image/png", "banner.png"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("banners/banner.png", json.GetProperty("url").GetString());
    }

    [Fact]
    public async Task Banner_upload_rejects_non_image_with_400()
    {
        var (org, id) = await OrganiserWithEventAsync();
        var res = await org.PostAsync($"/api/events/{id}/banner", ImageForm("text/plain", "notes.txt"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_upload_event_banner()
    {
        var (_, id) = await OrganiserWithEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var res = await participant.PostAsync($"/api/events/{id}/banner", ImageForm("image/png"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task User_can_upload_profile_picture()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var res = await participant.PostAsync("/api/users/me/picture", ImageForm("image/jpeg", "me.jpg"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
