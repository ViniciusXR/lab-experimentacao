# Sprint 4 — Consolidação do Lab05

Esta sprint **não adiciona funcionalidades novas**. Ela reúne e executa, de forma organizada, todo o trabalho já realizado nas Sprints 1, 2 e 3, além do dashboard Python exigido no Passo 6 do enunciado.

## Como executar tudo de uma vez

Na raiz do repositório:

```bash
dotnet run --project "Enunciado 5/Sprint 4/Sprint4.csproj"
```

Sem pausa ao final (útil em CI ou automação):

```bash
dotnet run --project "Enunciado 5/Sprint 4/Sprint4.csproj" -- --no-wait
```

## O que é executado (em ordem)

### 1. Sprint 1 — Lab05S01 (5 pts)

**Passos do enunciado:** 1 (Desenho) e 2 (Preparação)

| Artefato | Descrição |
|----------|-----------|
| `relatorios/sprint1_plano_experimento.md` | Hipóteses, variáveis, tratamentos, ameaças à validade |
| `relatorios/protocolo_replicacao.md` | Passos para reproduzir o experimento |
| `consultas/consultas_rest.json` | Consultas REST dos 5 cenários |
| `consultas/consultas_graphql.graphql` | Consultas GraphQL equivalentes |
| `dados/objetos_experimentais.csv` | Catálogo sintético de objetos |

### 2. Sprint 2 — Lab05S02 (10 pts)

**Passos do enunciado:** 3 (Execução), 4 (Análise) e 5 (Relatório final)

| Artefato | Descrição |
|----------|-----------|
| `dados/medicoes_lab05.csv` | 1.200 medições (5 cenários × 2 tratamentos × 120 trials) |
| `dados/resumo_estatistico.csv` | Média, mediana, desvio, P95 por cenário/tratamento |
| `dados/testes_estatisticos.csv` | Mann-Whitney U, Cliff's delta, vereditos RQ1/RQ2 |
| `dados/metadados_execucao.json` | Ambiente, sementes e parâmetros da coleta |
| `relatorios/relatorio_final_lab05.md` | Introdução, metodologia, resultados, discussão |
| `relatorios/relatorio_final_lab05.pdf` | Relatório final em PDF |

### 3. Sprint 3 — Lab05S03 (dashboard C#)

**Passo do enunciado:** 6 (Dashboard — versão C#/HTML)

| Artefato | Descrição |
|----------|-----------|
| `dashboards/dashboard_lab05.html` | Dashboard HTML com gráficos SVG e tabelas |

### 4. Dashboard Python — Lab05S03 Passo 6

**Passo do enunciado:** 6 (pandas + matplotlib/seaborn)

Script: `../dashboard/gerar_dashboard.py`

| Artefato | Descrição |
|----------|-----------|
| `dashboards/dashboard_lab05_python.html` | Dashboard interativo com abas (RQ1, RQ2, síntese) |
| `dashboards/graficos/rq1_tempo_mediano.png` | RQ1 — tempo mediano por cenário |
| `dashboards/graficos/rq2_tamanho_mediano.png` | RQ2 — tamanho mediano por cenário |
| `dashboards/graficos/distribuicao_tempo.png` | Distribuição dos tempos |
| `dashboards/graficos/reducao_percentual.png` | Redução % GraphQL vs REST |
| `dashboards/graficos/visao_agregada.png` | Visão agregada das métricas |
| `dashboards/tabelas/tabela_descritiva.csv` | Tabela descritiva processada |
| `dashboards/tabelas/tabela_testes.csv` | Tabela de testes estatísticos |

## Perguntas de pesquisa e vereditos

| RQ | Pergunta | Resultado |
|----|----------|-----------|
| **RQ1** | GraphQL é mais rápido que REST? | **H1 apoiada** — −81,54% na mediana agregada |
| **RQ2** | GraphQL tem payload menor? | **H1 apoiada** — −90,33% na mediana agregada |

## Execução individual (opcional)

```bash
dotnet run --project "Enunciado 5/Sprint 1/Sprint1.csproj"
dotnet run --project "Enunciado 5/Sprint 2/Sprint2.csproj"
dotnet run --project "Enunciado 5/Sprint 3/Sprint3.csproj"
pip install -r "Enunciado 5/dashboard/requirements.txt"
python "Enunciado 5/dashboard/gerar_dashboard.py"
```

## Pasta de entrega

Todos os artefatos ficam em:

```
Enunciado 5/lab05_entrega/
```

Consulte também `../README.md` e `../lab05_entrega/LEIA-ME.md`.
