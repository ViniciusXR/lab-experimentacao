# Laboratório 01 — Características de Repositórios Populares do GitHub

**Enunciado 1 · Sprint 2**

> Relatório gerado automaticamente em 2026-02-28 14:03 com dados de **1000** repositórios.

---

## 1. Introdução

Neste laboratório, analisamos as principais características dos **1 000 repositórios com maior número de estrelas** no GitHub, 
buscando entender como eles são desenvolvidos, com que frequência recebem contribuição externa, lançam releases, entre outras características.

### Hipóteses Informais

| # | Hipótese |
|---|----------|
| H1 | Repositórios populares tendem a ser **maduros** (mediana de idade > 5 anos) |
| H2 | Repositórios populares recebem um **alto número de PRs aceitas** (mediana > 500) |
| H3 | Repositórios populares **lançam releases com frequência** (mediana > 20) |
| H4 | Repositórios populares são **atualizados recentemente** (mediana < 30 dias) |
| H5 | Repositórios populares são escritos em **linguagens populares** (JS, Python, TS, etc.) |
| H6 | Repositórios populares possuem **alto percentual de issues fechadas** (mediana > 70%) |

## 2. Metodologia

- **Fonte de dados:** GitHub GraphQL API v4
- **Critério de seleção:** 1 000 repositórios com maior número de estrelas (`stars:>1000 sort:stars-desc`)
- **Paginação:** consultas de 30 repositórios por página com cursor-based pagination
- **Sumarização:** valores medianos para métricas numéricas; contagem por categoria para linguagens
- **Ferramentas:** C# / .NET 10, Spectre.Console, System.Text.Json

### Métricas coletadas por repositório

| Métrica | Campo GraphQL |
|---------|---------------|
| Idade (anos) | `createdAt` |
| Pull requests aceitas | `pullRequests(states: MERGED) { totalCount }` |
| Total de releases | `releases { totalCount }` |
| Dias desde última atualização | `updatedAt` |
| Linguagem primária | `primaryLanguage { name }` |
| Issues fechadas / total | `issues { totalCount }` + `issues(states: CLOSED) { totalCount }` |

## 3. Resultados

### Top 20 repositórios por estrelas

| Repositório | ⭐ Estrelas | Idade (anos) | PRs aceitas | Releases | Últ. atualiz. (dias) | Linguagem | Issues fechadas/total |
|-------------|-----------|--------------|-------------|----------|---------------------|-----------|----------------------|
| codecrafters-io/build-your-own-x | 470.086 | 7,8 | 155 | 0 | 0 | Markdown | 73,7% |
| sindresorhus/awesome | 441.355 | 11,6 | 693 | 0 | 0 | (não detectada) | 95,6% |
| freeCodeCamp/freeCodeCamp | 437.597 | 11,2 | 27.697 | 0 | 0 | TypeScript | 99,1% |
| public-apis/public-apis | 401.221 | 9,9 | 1.872 | 0 | 0 | Python | 99,6% |
| EbookFoundation/free-programming-books | 383.316 | 12,4 | 7.328 | 0 | 0 | Python | 97,2% |
| kamranahmedse/developer-roadmap | 349.888 | 9,0 | 4.050 | 1 | 0 | TypeScript | 99,1% |
| jwasham/coding-interview-university | 337.500 | 9,7 | 415 | 0 | 0 | (não detectada) | 86,5% |
| donnemartin/system-design-primer | 336.929 | 9,0 | 203 | 0 | 0 | Python | 30,7% |
| vinta/awesome-python | 284.922 | 11,7 | 637 | 0 | 0 | Python | 0,0% |
| awesome-selfhosted/awesome-selfhosted | 275.796 | 10,7 | 2.395 | 1 | 0 | (não detectada) | 100,0% |
| 996icu/996.ICU | 275.567 | 6,9 | 1.072 | 0 | 0 | (não detectada) | 0,0% |
| practical-tutorials/project-based-learning | 259.738 | 8,9 | 182 | 0 | 0 | (não detectada) | 36,1% |
| facebook/react | 243.366 | 12,8 | 12.762 | 118 | 0 | JavaScript | 94,3% |
| openclaw/openclaw | 239.905 | 0,3 | 2.192 | 56 | 0 | TypeScript | 68,2% |
| torvalds/linux | 219.832 | 14,5 | 0 | 0 | 0 | C | 0,0% |
| TheAlgorithms/Python | 218.221 | 9,6 | 3.046 | 0 | 0 | Python | 90,5% |
| vuejs/vue | 209.929 | 12,6 | 1.136 | 249 | 0 | TypeScript | 96,4% |
| trimstray/the-book-of-secret-knowledge | 208.128 | 7,7 | 122 | 0 | 0 | (não detectada) | 0,0% |
| ossu/computer-science | 201.902 | 11,8 | 304 | 0 | 0 | HTML | 98,2% |
| trekhleb/javascript-algorithms | 195.706 | 7,9 | 304 | 0 | 0 | JavaScript | 62,8% |

### RQ 01 — Sistemas populares são maduros/antigos?

| Estatística | Valor |
|-------------|-------|
| **Mediana** | 8,4 anos |
| Mínimo | 0,1 anos |
| Máximo | 17,9 anos |

> ✅ **Hipótese confirmada.** Sistemas populares são, em geral, maduros.

### RQ 02 — Sistemas populares recebem muita contribuição externa?

| Estatística | Valor |
|-------------|-------|
| **Mediana** | 737 PRs aceitas |
| Mínimo | 0 |
| Máximo | 94423 |

> ✅ **Hipótese confirmada.** Recebem muita contribuição externa via PRs.

### RQ 03 — Sistemas populares lançam releases com frequência?

| Estatística | Valor |
|-------------|-------|
| **Mediana** | 40 releases |
| Mínimo | 0 |
| Máximo | 1000 |

> ✅ **Hipótese confirmada.** Lançam releases com boa frequência.

### RQ 04 — Sistemas populares são atualizados com frequência?

| Estatística | Valor |
|-------------|-------|
| **Mediana** | 0 dias |
| Mínimo | 0 dias |
| Máximo | 2 dias |

> ✅ **Hipótese confirmada.** São projetos muito ativos e atualizados frequentemente.

### RQ 05 — Sistemas populares são escritos nas linguagens mais populares?

| Linguagem | Repositórios |
|-----------|-------------|
| Python | 200 |
| TypeScript | 158 |
| JavaScript | 117 |
| (não detectada) | 95 |
| Go | 77 |
| Rust | 53 |
| Java | 47 |
| C++ | 46 |
| C | 25 |
| Jupyter Notebook | 23 |
| Shell | 21 |
| HTML | 18 |
| Ruby | 12 |
| C# | 11 |
| Kotlin | 10 |
| CSS | 8 |
| Vue | 7 |
| PHP | 7 |
| Swift | 7 |
| Dart | 6 |
| Markdown | 5 |
| MDX | 5 |
| Vim Script | 4 |
| Clojure | 4 |
| Dockerfile | 3 |
| Zig | 3 |
| Makefile | 3 |
| Batchfile | 2 |
| Scala | 2 |
| Assembly | 2 |
| Nunjucks | 2 |
| PowerShell | 2 |
| Lua | 2 |
| Haskell | 2 |
| Svelte | 2 |
| TeX | 2 |
| Blade | 1 |
| Roff | 1 |
| Julia | 1 |
| V | 1 |
| LLVM | 1 |
| Elixir | 1 |
| Objective-C | 1 |

> ✅ **Hipótese confirmada.** Predominam linguagens amplamente adotadas na indústria.

### RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?

| Estatística | Valor |
|-------------|-------|
| **Mediana** | 87,8% |
| Mínimo | 5,5% |
| Máximo | 100,0% |
| Repos sem issues | 42 |

> ✅ **Hipótese confirmada.** A maioria dos projetos populares fecha uma proporção alta de issues.

## 4. Discussão

### Resumo — Valores Medianos

| Questão | Métrica | Mediana |
|---------|---------|--------|
| RQ 01 — Maturidade | Idade (anos) | 8,4 |
| RQ 02 — Contribuição | PRs aceitas | 737 |
| RQ 03 — Releases | Total releases | 40 |
| RQ 04 — Atualizações | Dias desde última atualiz. | 0 |
| RQ 05 — Linguagens | Linguagem mais comum | Python |
| RQ 06 — Issues fechadas | Razão fechadas/total | 87,8% |

### Comparação com as hipóteses

| Hipótese | Esperado | Obtido | Resultado |
|----------|----------|--------|-----------|
| H1 — Maturidade | > 5 anos | 8,4 anos | ✅ Confirmada |
| H2 — PRs aceitas | > 500 | 737 | ✅ Confirmada |
| H3 — Releases | > 20 | 40 | ✅ Confirmada |
| H4 — Atualizações | < 30 dias | 0 dias | ✅ Confirmada |
| H5 — Linguagens | JS, Python, TS... | Python | ✅ Confirmada |
| H6 — Issues fechadas | > 70% | 87,8% | ✅ Confirmada |

---

## Tecnologias

| Tecnologia | Detalhes |
|---|---|
| **Linguagem** | C# |
| **Framework** | .NET 10.0 |
| **API** | GitHub GraphQL API v4 |
| **UI (console)** | [Spectre.Console](https://spectreconsole.net/) 0.54.0 |

## Arquivos gerados

| Arquivo | Descrição |
|---|---|
| `dados_repositorios_sprint2.csv` | Dados brutos dos 1 000 repositórios (separador `;`) |
| `README.md` | Este relatório |
