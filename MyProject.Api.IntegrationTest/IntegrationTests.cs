using System; //brings in base types like guid
using System.Net; //brings in httpstatus code enum for response assertions
using System.Net.Http.Json; //JsonAsync
using System.Text.Json; //JsonDoc
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.VisualBasic;
using MyProject.Backend.Models;
using Xunit;

namespace MyProject.Api.IntegrationTest
{
    public class IntegrationTests : IClassFixture<MyProjectWebApplicationFactory>
    {

    
        private readonly MyProjectWebApplicationFactory _factory; 

        public IntegrationTests(MyProjectWebApplicationFactory factory) => _factory = factory;

        [Fact] //xUnit attribute marking the following method as a test
        public async Task Register_Login_CreateBand_Workflow()//test method name describigng the end to end workflow
        {
            var client = _factory.CreateClient(); //creates a HttpClient wired to the in memory test server
            var username = "test_" + Guid.NewGuid().ToString("N").Substring(0,8); //generates a short unique username
            var password = "Test#1234"; //always same password for test

            var regResp = await client.PostAsJsonAsync("/api/auth/register", new {Username = username, Password = password}); //post to the register enpdoint
            Assert.True(regResp.StatusCode == HttpStatusCode.Created || regResp.StatusCode == HttpStatusCode.Conflict); //check to make sure the status code is created / a conflict occurs

            var logResp = await client.PostAsJsonAsync("/api/auth/login", new {Username = username, Password = password}); //send the login endpoint
            // Assert.True(logResp.StatusCode == HttpStatusCode.Created || logResp.StatusCode == HttpStatusCode.Conflict); //check to make sure the status code is created / a conflict occurs
            //code above was incorrect, we want to make just status doesn't just exist (401,403) but that we get the 200 we created
            Assert.True(logResp.StatusCode == HttpStatusCode.OK); // checks for 200/ok status code

            var body = JsonDocument.Parse(await logResp.Content.ReadAsStringAsync()).RootElement;
            string token = null;
            if (body.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String) token = t.GetString(); // try "token" (lowercase)
            else if (body.TryGetProperty("Token", out t) && t.ValueKind == JsonValueKind.String) token = t.GetString(); // try "Token" (capitalized)
            Assert.False(string.IsNullOrEmpty(token), "Login did not return a JWT token"); // fail early if token missing


            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); //set the authorization header on the httpclient to use JWT
            var band = new {name = "IT Rockers", genre = "Debuggers", dateTime = "2026-07-07T15:00:00", stage = "testStage"};
            var createResp = await client.PostAsJsonAsync("/api/bands", band); //this method post the actual band
            Assert.True(createResp.StatusCode == HttpStatusCode.OK || createResp.StatusCode ==HttpStatusCode.Created); //test it created the band by comparing the status code of the returned
        }

    }
}