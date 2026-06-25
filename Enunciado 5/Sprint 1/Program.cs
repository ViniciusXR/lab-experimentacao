using System.Text;

namespace Enunciado5.Sprint1;

internal static class Program
{
    private static async Task Main()
    {
        var labRoot = FindLabRoot();
        var outputRoot = Path.Combine(labRoot, "lab05_entrega");
        var reportsDir = Path.Combine(outputRoot, "relatorios");
        var queriesDir = Path.Combine(outputRoot, "consultas");
        var dataDir = Path.Combine(outputRoot, "dados");

        Directory.CreateDirectory(reportsDir);
        Directory.CreateDirectory(queriesDir);
        Directory.CreateDirectory(dataDir);

        await File.WriteAllTextAsync(
            Path.Combine(reportsDir, "sprint1_plano_experimento.md"),
            BuildExperimentPlan(), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(reportsDir, "protocolo_replicacao.md"),
            BuildReplicationProtocol(), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(queriesDir, "consultas_rest.json"),
            BuildRestQueries(), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(queriesDir, "consultas_graphql.graphql"),
            BuildGraphQlQueries(), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "objetos_experimentais.csv"),
            BuildExperimentalObjects(), Encoding.UTF8);

        Console.WriteLine("Lab05S01 concluido.");
        Console.WriteLine($"Artefatos gerados em: {outputRoot}");
        Console.ReadKey();
    }

    private static string FindLabRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (current.Name.Equals("Enunciado 5", StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            var candidate = Path.Combine(current.FullName, "Enunciado 5");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string BuildExperimentPlan() => """
        # Lab05S01 - Desenho do Experimento

        ## Tema

        Comparacao quantitativa entre consultas REST e GraphQL em um experimento controlado e reprodutivel localmente.

        ## Perguntas de pesquisa

        - **RQ1:** Respostas as consultas GraphQL sao mais rapidas que respostas as consultas REST?
        - **RQ2:** Respostas as consultas GraphQL tem tamanho menor que respostas as consultas REST?

        ## Hipoteses

        ### RQ1 - Tempo de resposta

        - **H0-1:** nao ha diferenca estatisticamente significativa entre o tempo de resposta de REST e GraphQL.
        - **H1-1:** o tempo de resposta de GraphQL e menor que o tempo de resposta de REST.

        ### RQ2 - Tamanho da resposta

        - **H0-2:** nao ha diferenca estatisticamente significativa entre o tamanho das respostas REST e GraphQL.
        - **H1-2:** o tamanho das respostas GraphQL e menor que o tamanho das respostas REST.

        ## Variaveis

        ### Variaveis independentes

        - **Estilo de API:** REST ou GraphQL.
        - **Cenario de consulta:** cinco consultas equivalentes sobre o mesmo dominio de dados.

        ### Variaveis dependentes

        - **Tempo de resposta (ms):** tempo para montar, serializar e consumir a resposta JSON.
        - **Tamanho da resposta (bytes):** quantidade de bytes UTF-8 retornados em cada trial.

        ## Tratamentos

        - **REST:** endpoints com recursos completos e campos extras, simulando over-fetching comum em APIs REST.
        - **GraphQL:** consulta com projecao explicita de campos, retornando apenas os dados solicitados pelo cliente.

        ## Objetos experimentais

        O experimento usa um catalogo local sintetico e deterministico de repositorios, issues, releases e contribuidores. O mesmo snapshot alimenta os dois tratamentos, evitando variacao de rede, autenticacao, cache de provedores externos e mudancas em APIs publicas.

        ## Tipo de projeto experimental

        Projeto intra-sujeitos (within-subject): cada cenario de consulta e executado nos dois tratamentos, REST e GraphQL. A ordem dos trials e embaralhada com semente fixa para reduzir vies de ordem.

        ## Quantidade de medicoes

        - 5 cenarios de consulta.
        - 2 tratamentos.
        - 120 trials por tratamento em cada cenario.
        - Total planejado: 1.200 medicoes.
        - Rodadas de aquecimento: 20 execucoes por cenario/tratamento antes da coleta.

        ## Cenarios de consulta

        | ID | Nome | Objetivo logico | Diferenca esperada |
        |---|------|------------------|--------------------|
        | Q1 | Lista de repositorios | Listar repositorios com metadados principais | REST retorna campos extras; GraphQL projeta somente campos de listagem |
        | Q2 | Detalhe do repositorio | Consultar um repositorio especifico | REST retorna objeto completo; GraphQL retorna campos selecionados |
        | Q3 | Issues recentes | Consultar issues e autores | REST retorna corpo, timeline e metadados; GraphQL retorna dados necessarios |
        | Q4 | Contribuidores | Consultar contribuidores e contribuicoes | REST retorna perfis completos; GraphQL retorna resumo |
        | Q5 | Visao aninhada | Obter repositorio, issues, contribuidores e release | REST soma multiplos endpoints; GraphQL retorna grafo unico |

        ## Analise estatistica planejada

        - Estatisticas descritivas: media, mediana, desvio padrao, minimo, maximo e p95.
        - Teste de Mann-Whitney U para comparacao entre tratamentos, adotando alfa = 0,05.
        - Tamanho de efeito por Cliff's delta.
        - Resposta as RQs baseada em significancia estatistica, direcao da mediana e magnitude pratica da diferenca.

        ## Ameacas a validade

        - **Validade interna:** medicao local reduz ruido de rede, mas tambem remove latencia real de servidores externos. Mitigacao: medir serializacao, montagem de resposta e consumo JSON, que sao etapas comuns aos dois estilos.
        - **Validade externa:** catalogo sintetico pode nao representar todas as APIs reais. Mitigacao: usar cenarios comuns em APIs de repositorios, com objetos aninhados, listas e detalhes.
        - **Validade de construto:** GraphQL foi modelado como projecao de campos, nao como servidor GraphQL completo com parser e resolvedores complexos. Mitigacao: explicitar a decisao e comparar o beneficio de reducao de payload, principal diferenca observavel no laboratorio.
        - **Validade de conclusao:** tempos muito pequenos podem gerar ruido. Mitigacao: aquecimento, repeticoes, ordem embaralhada, uso de mediana e teste nao parametrico.
        """;

    private static string BuildReplicationProtocol() => """
        # Protocolo de Replicacao - Lab05

        ## Ambiente

        - SDK: .NET 10.
        - Sistema operacional: registrado automaticamente na Sprint 2.
        - Execucao sem chamadas de rede e sem dependencias externas.
        - Semente do dataset: 20260624.
        - Semente de randomizacao dos trials: 5052026.

        ## Passos

        1. Executar `dotnet run --project "Enunciado 5/Sprint 1/Sprint1.csproj"` para gerar o plano e os arquivos de preparacao.
        2. Executar `dotnet run --project "Enunciado 5/Sprint 2/Sprint2.csproj"` para coletar medicoes, calcular estatisticas e produzir o relatorio final.
        3. Executar `dotnet run --project "Enunciado 5/Sprint 3/Sprint3.csproj"` para gerar o dashboard HTML.

        ## Saidas esperadas

        - `lab05_entrega/dados/medicoes_lab05.csv`
        - `lab05_entrega/dados/resumo_estatistico.csv`
        - `lab05_entrega/dados/testes_estatisticos.csv`
        - `lab05_entrega/relatorios/relatorio_final_lab05.md`
        - `lab05_entrega/dashboards/dashboard_lab05.html`

        ## Criterios de reproducibilidade

        A execucao deve produzir a mesma quantidade de trials e a mesma estrutura de arquivos. Pequenas diferencas nos tempos sao esperadas porque dependem da maquina, carga do sistema e runtime.
        """;

    private static string BuildRestQueries() => """
        {
          "Q1_LIST_REPOSITORIES": {
            "method": "GET",
            "endpoint": "/api/repos?limit=25",
            "logicalFields": ["id", "name", "owner", "stars", "primaryLanguage"],
            "restBehavior": "Retorna objetos de repositorio completos, incluindo campos nao usados pelo cliente."
          },
          "Q2_REPOSITORY_DETAIL": {
            "method": "GET",
            "endpoint": "/api/repos/{owner}/{name}",
            "logicalFields": ["id", "name", "owner", "stars", "forks", "license", "topics"],
            "restBehavior": "Retorna detalhe completo do repositorio."
          },
          "Q3_RECENT_ISSUES": {
            "method": "GET",
            "endpoint": "/api/repos/{owner}/{name}/issues?limit=40",
            "logicalFields": ["id", "title", "state", "author", "createdAt", "labels"],
            "restBehavior": "Retorna issues completas com corpo, timeline e metadados adicionais."
          },
          "Q4_CONTRIBUTORS": {
            "method": "GET",
            "endpoint": "/api/repos/{owner}/{name}/contributors?limit=30",
            "logicalFields": ["login", "contributions", "company"],
            "restBehavior": "Retorna perfis completos de usuarios."
          },
          "Q5_NESTED_OVERVIEW": {
            "method": "GET",
            "endpoint": "Composicao de /repos, /issues, /contributors e /releases/latest",
            "logicalFields": ["repo", "issues", "contributors", "latestRelease"],
            "restBehavior": "Cliente precisa combinar multiplos endpoints."
          }
        }
        """;

    private static string BuildGraphQlQueries() => """
        query Q1_LIST_REPOSITORIES {
          repositories(first: 25) {
            nodes { id name owner { login } stars primaryLanguage }
          }
        }

        query Q2_REPOSITORY_DETAIL($owner: String!, $name: String!) {
          repository(owner: $owner, name: $name) {
            id name owner { login } stars forks license topics
          }
        }

        query Q3_RECENT_ISSUES($owner: String!, $name: String!) {
          repository(owner: $owner, name: $name) {
            issues(first: 40) {
              nodes { id title state author { login } createdAt labels }
            }
          }
        }

        query Q4_CONTRIBUTORS($owner: String!, $name: String!) {
          repository(owner: $owner, name: $name) {
            contributors(first: 30) {
              nodes { login contributions company }
            }
          }
        }

        query Q5_NESTED_OVERVIEW($owner: String!, $name: String!) {
          repository(owner: $owner, name: $name) {
            id name owner { login }
            stars primaryLanguage
            issues(first: 10) { nodes { title state author { login } } }
            contributors(first: 10) { nodes { login contributions } }
            latestRelease { tagName publishedAt }
          }
        }
        """;

    private static string BuildExperimentalObjects() => """
        scenario_id;scenario_name;rest_endpoint;graphql_operation;planned_trials_per_treatment;logical_result
        Q1;Lista de repositorios;/api/repos?limit=25;Q1_LIST_REPOSITORIES;120;25 repositorios com metadados principais
        Q2;Detalhe do repositorio;/api/repos/{owner}/{name};Q2_REPOSITORY_DETAIL;120;1 repositorio com metadados de detalhe
        Q3;Issues recentes;/api/repos/{owner}/{name}/issues?limit=40;Q3_RECENT_ISSUES;120;40 issues recentes
        Q4;Contribuidores;/api/repos/{owner}/{name}/contributors?limit=30;Q4_CONTRIBUTORS;120;30 contribuidores
        Q5;Visao aninhada;Composicao de 4 endpoints REST;Q5_NESTED_OVERVIEW;120;grafo de repositorio com listas aninhadas
        """;
}
