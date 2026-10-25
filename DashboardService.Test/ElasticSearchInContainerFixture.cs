using System.Threading.Tasks;
using DashboardService.DataAccess.Elastic;
using DashboardService.Domain;
using DotNet.Testcontainers.Builders;
using Elastic.Clients.Elasticsearch;
using Testcontainers.Elasticsearch;
using Xunit;

namespace DashboardService.Test;

public class ElasticSearchInContainerFixture : IAsyncLifetime
{
    private ElasticsearchContainer testContainer;
    private string endpoint;
    private readonly string indexName = "pricing_dashboard_test_" + System.Guid.NewGuid().ToString("N");

    public async Task InitializeAsync()
    {
        endpoint = System.Environment.GetEnvironmentVariable("DASHBOARD_TEST_ELASTICSEARCH");
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                testContainer = new ElasticsearchBuilder()
                    .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.9.2")
                    .WithEnvironment("discovery.type", "single-node")
                    .WithEnvironment("xpack.security.enabled", "false")
                    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                    .WithPortBinding(9200, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(q => q.ForPort(9200)))
                    .Build();
                await testContainer.StartAsync();
                endpoint = $"http://{testContainer.Hostname}:{testContainer.GetMappedPublicPort(9200)}";
            }
            var created = await ElasticClient().Indices.CreateAsync(indexName);
            if (!created.IsValidResponse) throw new System.InvalidOperationException(created.DebugInformation);
            InsertData();
        }
        catch { await DisposeAsync(); throw; }
    }

    public async Task DisposeAsync()
    {
        if (endpoint != null) await ElasticClient().Indices.DeleteAsync(indexName);
        if (testContainer != null) await testContainer.DisposeAsync();
    }

    public ElasticsearchClient ElasticClient()
    {
        var settings = new ElasticsearchClientSettings(new System.Uri(endpoint))
            .DefaultMappingFor<PolicyDocument>(m => m.IndexName(indexName).IdProperty(d => d.Number));
        return new ElasticsearchClient(settings);
    }

    private void InsertData()
    {
        var policyRepo = new ElasticPolicyRepository(ElasticClient());
        //products TRI,HSI,FAI,CAR

        var agent = "jimmy.solid";
        //january-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-01-01", "2020-12-31")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-01-15", "2021-01-14")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-01-15", "2021-01-14")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-01-15", "2021-01-14")
                .WithPremium(150M)
                .Build()
        );
        //feb-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-02-01", "2021-01-31")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-02-15", "2021-02-14")
                .WithPremium(80M)
                .Build()
        );
        //march-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(50M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-03-15", "2021-03-14")
                .WithPremium(25M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-03-15", "2021-03-14")
                .WithPremium(15M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-03-15", "2021-03-14")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-03-15", "2021-03-14")
                .WithPremium(250M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-03-15", "2021-03-14")
                .WithPremium(250M)
                .Build()
        );

        //agent danny.solid
        agent = "danny.solid";
        //january-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-01-01", "2020-12-31")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-01-01", "2020-12-31")
                .WithPremium(50M)
                .Build()
        );
        //feb-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-02-01", "2021-01-31")
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-02-01", "2021-01-31")
                .WithPremium(25M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-02-01", "2021-01-31")
                .WithPremium(50M)
                .Build()
        );
        //march-2020
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("TRI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(50M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(50M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("HSI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(50M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(25M)
                .Build()
        );
        policyRepo.Save
        (
            PolicyDocumentBuilder.Create()
                .WithAgent(agent)
                .WithProduct("FAI")
                .WithDates("2020-03-01", "2021-02-28")
                .WithPremium(25M)
                .Build()
        );
    }
}

[CollectionDefinition("ElasticSearch in a container")]
public class ElasticSearchInContainerCollection : ICollectionFixture<ElasticSearchInContainerFixture>
{
}
