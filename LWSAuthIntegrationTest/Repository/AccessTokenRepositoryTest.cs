using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LWS_Auth.Extension;
using LWS_Auth.Models;
using LWS_Auth.Repository;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LWSAuthIntegrationTest.Repository;

[Collection("Cosmos")]
public class AccessTokenRepositoryTest
{
    private readonly IAccessTokenRepository _accessTokenRepository;
    private readonly Container _accessToken;

    private AccessToken TestAccessToken(string token = "testToken") => new AccessToken
    {
        Id = token,
        UserId = "testUserId"
    };

    public AccessTokenRepositoryTest(CosmosFixture cosmosFixture)
    {
        var configuration = cosmosFixture.TestCosmosConfiguration;
        _accessTokenRepository =
            new AccessTokenRepository(cosmosFixture.CosmosClient, configuration);

        cosmosFixture.CreateAccessTokenContainerAsync(configuration.AccessTokenContainerName)
            .Wait();
        _accessToken = cosmosFixture.CosmosClient.GetContainer(configuration.CosmosDbname,
            configuration.AccessTokenContainerName);
    }

    private async Task<List<AccessToken>> ListAccessTokenAsync()
    {
        return await _accessToken.GetItemLinqQueryable<AccessToken>()
            .CosmosToListAsync();
    }

    [Fact(DisplayName =
        "InsertAccessTokenAsync: InsertAccessTokenAsync should insert one data if there is no duplicated result.")]
    public async Task Is_InsertAccessTokenAsync_Inserts_Data_If_No_Duplicates()
    {
        // Let
        var accessToken = TestAccessToken();

        // Do
        await _accessTokenRepository.InsertAccessTokenAsync(accessToken);

        // Check
        var list = await ListAccessTokenAsync();
        Assert.Single(list);

        var firstEntity = list.First();
        Assert.Equal(accessToken.Id, firstEntity.Id);
        Assert.Equal(accessToken.UserId, firstEntity.UserId);
    }

    [Fact(DisplayName =
        "GetAccessTokenByTokenAsync: GetAccessTokenByTokenAsync should return null if data does not exists.")]
    public async Task Is_GetAccessTokenByTokenAsync_Returns_Null_When_No_Data()
    {
        // let
        var token = "testToken";

        // Do
        var result = await _accessTokenRepository.GetAccessTokenByTokenAsync(token);

        // Check
        Assert.Null(result);
    }

    [Fact(DisplayName =
        "GetAccessTokenByTokenAsync: GetAccessTokenByTokenAsync should return corresponding data if data exists.")]
    public async Task Is_GetAccessTokenByTokenAsync_Returns_Corresponding_Data_Well()
    {
        // Let
        var accessToken = TestAccessToken();
        await _accessToken.CreateItemAsync(accessToken, new PartitionKey(accessToken.UserId));

        // Do
        var result = await _accessTokenRepository.GetAccessTokenByTokenAsync(accessToken.Id);

        // Check
        Assert.NotNull(result);
        Assert.Equal(accessToken.Id, result.Id);
        Assert.Equal(accessToken.UserId, result.UserId);
    }

    [Fact(DisplayName = "ListAccessTokensAsync: ListAccessTokensAsync should return list of data if data exists.")]
    public async Task Is_ListAccessTokensAsync_Return_List_Of_Data_If_Data_Exists()
    {
        // Let
        var toSaveList = new List<AccessToken>
        {
            TestAccessToken(),
            TestAccessToken("hello"),
            TestAccessToken("another")
        };
        foreach (var eachSave in toSaveList)
        {
            await _accessToken.CreateItemAsync(eachSave, new PartitionKey(eachSave.UserId));
        }

        // Do
        var list = await _accessTokenRepository.ListAccessTokensAsync(TestAccessToken().UserId);

        // Check
        Assert.Equal(3, list.Count);
    }
}