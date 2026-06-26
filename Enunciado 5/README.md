# Enunciado 5 — Lab05: GraphQL vs REST

Este diretório contém o experimento controlado do **Laboratório 05**, comparando APIs REST e GraphQL nas perguntas RQ1 (tempo de resposta) e RQ2 (tamanho do payload).

## Execução consolidada (recomendado)

A **Sprint 4** reúne todas as etapas em uma única execução:

```bash
dotnet run --project "Enunciado 5/Sprint 4/Sprint4.csproj"
```

Isso executa, em sequência:

1. **Sprint 1** — desenho do experimento e preparação (Passos 1 e 2)
2. **Sprint 2** — execução, análise estatística e relatório final (Passos 3, 4 e 5)
3. **Sprint 3** — dashboard HTML em C#
4. **Dashboard Python** — Passo 6 do enunciado com **pandas**, **matplotlib** e **seaborn**

## Passo 6 — Dashboard Python

O enunciado exige visualização com pandas/matplotlib/seaborn. Essa etapa é feita pelo script:

```bash
pip install -r "Enunciado 5/dashboard/requirements.txt"
python "Enunciado 5/dashboard/gerar_dashboard.py"
```

**Saídas do Passo 6:**

- `lab05_entrega/dashboards/dashboard_lab05_python.html` — dashboard interativo (abas, RQ1, RQ2, síntese)
- `lab05_entrega/dashboards/graficos/*.png` — figuras geradas
- `lab05_entrega/dashboards/tabelas/*.csv` — tabelas processadas

> A Sprint 4 já executa esse script automaticamente após as Sprints 1–3, desde que Python 3.10+ esteja instalado.

## Estrutura do projeto

| Pasta | Conteúdo |
|-------|----------|
| `Sprint 1/` | Lab05S01 — plano, protocolo, consultas e objetos experimentais |
| `Sprint 2/` | Lab05S02 — 1.200 medições, estatísticas, relatório `.md` e `.pdf` |
| `Sprint 3/` | Lab05S03 — dashboard HTML (`dashboard_lab05.html`) |
| `Sprint 4/` | **Consolidação** — executa tudo e lista a entrega completa |
| `dashboard/` | Script Python do Passo 6 (`gerar_dashboard.py`) |
| `lab05_entrega/` | Todos os artefatos finais gerados |

## Entregáveis principais

- `lab05_entrega/relatorios/sprint1_plano_experimento.md`
- `lab05_entrega/relatorios/relatorio_final_lab05.pdf`
- `lab05_entrega/dados/medicoes_lab05.csv` (1.200 trials)
- `lab05_entrega/dashboards/dashboard_lab05.html` (C#)
- `lab05_entrega/dashboards/dashboard_lab05_python.html` (Python — Passo 6)

## Resultado sintetizado

No cenário experimental controlado, GraphQL apresentou menor mediana de tempo (**−81,54%**) e menor tamanho de resposta (**−90,33%**) que REST, apoiando H1-1 e H1-2.

Detalhes em `lab05_entrega/LEIA-ME.md` e `Sprint 4/README.md`.
