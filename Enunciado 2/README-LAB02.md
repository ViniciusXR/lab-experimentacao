# Laboratório 02 (Enunciado 2) — Qualidade Java (CK + GitHub)

Este diretório contém as **Sprints 1–3**: lista de repositórios, coleta CK, consolidado CSV, análise estatística, relatório **Markdown + PDF** e bônus (gráficos e correlações).

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (projeto alvo: `net10.0`).
- **Token GitHub** (Sprint 1, para buscar os 1000 repos): ficheiro `Sprint 1/.github-token`, variável `GITHUB_TOKEN`, ou `--token=...`.
- **Java + `ck.jar`** (ferramenta [CK](https://github.com/mauricioaniche/ck)) para `--coleta-ck` / `--coleta-lote`. Se aparecer `OutOfMemoryError: Java heap space`, use `--ck-xmx=6g` (ou `CK_XMX`) e/ou reduza `--lote-paralelo`.
- Espaço em disco e tempo para clones (a coleta em lote é demorada).

## Um comando (recomendado)

Na pasta **`Enunciado 2/Sprint 3`**:

```bash
dotnet run
```

Isto corre **automaticamente**:

1. **Sprint 1** — se já existir `Sprint 1/lab02_sprint1_output/repos_java_1000.csv`, usa `--skip-fetch` (não precisa de token). Caso contrário, precisa de **token GitHub** (`.github-token`, `GITHUB_TOKEN` ou `--token=...`) para buscar a lista.
2. **Sprint 2** — gera `lab02_medicoes_consolidado.csv`.
3. **Sprint 3** — relatório Markdown, PDF, gráficos, CSVs de análise.

A pasta `lab02_sprint3_output/` é **apagada e recriada** após validar o consolidado.

### Opções úteis

| Argumento | Efeito |
|-----------|--------|
| `--apenas-sprint3` | Só regenera relatório (ignora Sprint 1 e 2); precisa do consolidado já existente. |
| `--refetch` | Força nova descarga da lista na Sprint 1 (não usa `--skip-fetch`). |
| `--gql-page-size=N` | GraphQL: repos por página (predefinição **30**, alinhado ao Lab01). Reduza (ex. 18) se tiver 502. |
| `--gql-pause-ms=N` | GraphQL: pausa em ms entre páginas (predefinição **0**). Use 400–1400 se a rede/proxy falhar. |
| `--rest-pause-ms=N` | REST (modo `--rest`): pausa entre páginas em ms (predefinição **600**). |
| `--coleta-lote --ck-jar=caminho\ck.jar` | Repassado à Sprint 1 — coleta CK em lote (demorado). |
| `--lote-paralelo=N` | Repassado à Sprint 1 — repos processados em paralelo (predefinição 4). |
| `--lote-limpar-work` | Antes do lote, apaga `Sprint 1/lab02_sprint1_output/lote_work` (útil se Windows der *Access denied* em `pack-*.idx`). |
| `--no-pdf` | Só Markdown/CSVs/gráficos, sem PDF. |
| `--input=caminho\consolidado.csv` | Consolidado alternativo. |
| `--autores="Acadêmicos: ..."` | Cabeçalho do PDF. |

Ou use ficheiro `lab02_autores.txt` na pasta da Sprint 3 (ver `lab02_autores.example.txt`).

## Passos manuais (Sprints separadas)

Se preferir correr cada projeto à mão: Sprint 1 → Sprint 2 → Sprint 3 (como antes). Útil para depurar só uma fase.

## Script na raiz do Enunciado 2

```powershell
.\executar-lab02.ps1
```

É equivalente a `dotnet run` na Sprint 3 (inclui S1 e S2). Exemplo: `.\executar-lab02.ps1 --apenas-sprint3`

## O que ainda é manual

- **Apresentação oral** na aula (slides), conforme enunciado.
- Ajustar **nomes** em `lab02_autores.txt` ou `--autores` para o cabeçalho do PDF.

## Estrutura

| Pasta    | Função |
|----------|--------|
| `Sprint 1` | API GitHub, lista 1000 repos, clone + CK |
| `Sprint 2` | Consolidação num único CSV |
| `Sprint 3` | Estatísticas, RQs, gráficos, Markdown, PDF |

Se `com CK: 0` no relatório, o consolidado ainda não tem colunas CBO/DIT/LCOM preenchidas — complete a coleta na Sprint 1 e volte a correr a Sprint 2 e a Sprint 3.
