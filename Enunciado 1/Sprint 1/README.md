# Laboratório 01 — Características de Repositórios Populares do GitHub

**Enunciado 1 · Sprint 1**

## Sobre

Aplicação de console que analisa os repositórios mais populares do GitHub (por estrelas) e responde a **6 questões de pesquisa** sobre suas características, além de uma **atividade bônus** com análises adicionais.

## Questões de Pesquisa

| # | Pergunta | Métrica |
|---|----------|---------|
| 1 | Sistemas populares são maduros/antigos? | Idade do repositório (anos) |
| 2 | Recebem engajamento da comunidade? | Número de forks |
| 3 | Lançam releases com frequência? | Total de releases publicadas |
| 4 | São atualizados regularmente? | Dias desde a última atualização |
| 5 | São escritos em linguagens populares? | Linguagem primária |
| 6 | Mantêm o backlog de issues sob controle? | Número de issues abertas |

## Tecnologias

| Tecnologia | Detalhes |
|---|---|
| **Linguagem** | C# |
| **Framework** | .NET 10.0 |
| **API** | GitHub GraphQL API v4 |
| **Endpoint** | `https://api.github.com/graphql` |
| **UI (console)** | [Spectre.Console](https://spectreconsole.net/) 0.54.0 |

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Personal Access Token** do GitHub (a API GraphQL requer autenticação)

### Como criar o token

1. Acesse [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens)
2. Clique em **Generate new token (classic)**
3. Selecione o escopo `public_repo` (suficiente para repositórios públicos)
4. Copie o token gerado

## Como executar

### 1. Fornecer o token

Escolha **uma** das opções:

```bash
# Opção A: arquivo .github-token na pasta do projeto (recomendado, já está no .gitignore)
echo "ghp_seuTokenAqui" > .github-token

# Opção B: variável de ambiente
$env:GITHUB_TOKEN = "ghp_seuTokenAqui"

# Opção C: argumento na linha de comando
dotnet run -- --token=ghp_seuTokenAqui
```

### 2. Executar

```bash
# Modo padrão (100 repositórios)
dotnet run

# Modo bônus (200 repositórios)
dotnet run -- --bonus
```

## Saída

A aplicação gera:

| Arquivo | Descrição |
|---|---|
| **Console** | Relatório visual com tabelas, gráficos de barra e painéis coloridos (Spectre.Console) |
| `relatorio_lab01_sprint1.txt` | Relatório em texto puro |
| `dados_repositorios.csv` | Dados brutos dos repositórios (separador `;`) |

## Estrutura da consulta GraphQL

```graphql
query($queryString: String!, $first: Int!, $after: String) {
  search(query: $queryString, type: REPOSITORY, first: $first, after: $after) {
    repositoryCount
    pageInfo { hasNextPage, endCursor }
    nodes {
      ... on Repository {
        nameWithOwner
        stargazerCount
        createdAt
        updatedAt
        primaryLanguage { name }
        issues(states: OPEN) { totalCount }
        forkCount
        releases { totalCount }
      }
    }
  }
}
```

Todos os dados são obtidos em poucas chamadas GraphQL (páginas de 30 repos), sem necessidade de requisições REST separadas para cada repositório.
