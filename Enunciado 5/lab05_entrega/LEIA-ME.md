# Lab05 - Entrega

Esta pasta consolida a atividade do **Laboratório 05 - GraphQL vs REST**.

## Execução consolidada (recomendado)

Para gerar **toda** a entrega de uma vez (Sprints 1, 2, 3 e dashboard Python do Passo 6):

```powershell
dotnet run --project "Enunciado 5\Sprint 4\Sprint4.csproj"
```

## Como reproduzir (passo a passo)

Execute, a partir da raiz do repositório, as três sprints em C# (geram os dados):

```powershell
dotnet run --project "Enunciado 5\Sprint 1\Sprint1.csproj"
dotnet run --project "Enunciado 5\Sprint 2\Sprint2.csproj"
dotnet run --project "Enunciado 5\Sprint 3\Sprint3.csproj"
```

Em seguida, gere o dashboard de visualização (Passo 6) com Python (pandas + matplotlib/seaborn):

```powershell
pip install -r "Enunciado 5\dashboard\requirements.txt"
python "Enunciado 5\dashboard\gerar_dashboard.py"
```

> **Passo 6 do enunciado:** o dashboard Python (`dashboard_lab05_python.html`) é gerado com **pandas**, **matplotlib** e **seaborn**, conforme exigido no Laboratório 05. A Sprint 4 executa este script automaticamente após as três sprints em C#.

O script lê os CSVs produzidos na Sprint 2, processa-os com pandas e gera os gráficos (matplotlib/seaborn), as tabelas e o dashboard HTML.

## Entregáveis principais

- `relatorios/sprint1_plano_experimento.md`: desenho do experimento.
- `relatorios/protocolo_replicacao.md`: protocolo de reprodução.
- `dados/medicoes_lab05.csv`: 1.200 medições coletadas.
- `dados/resumo_estatistico.csv`: estatísticas descritivas por cenário e tratamento.
- `dados/testes_estatisticos.csv`: Mann-Whitney U, Cliff's delta e vereditos.
- `relatorios/relatorio_final_lab05.md`: relatório final em Markdown.
- `relatorios/relatorio_final_lab05.pdf`: relatório final em PDF.
- `dashboards/dashboard_lab05.html`: dashboard visual gerado pela Sprint 3 (C#/SVG).
- `dashboards/dashboard_lab05_python.html`: dashboard do Passo 6 (pandas + matplotlib/seaborn) em formato de apresentação, com navegação por abas (sem rolagem) e por teclado (← →).
- `dashboards/graficos/`: gráficos PNG (RQ1, RQ2, distribuição, redução percentual e visão agregada).
- `dashboards/tabelas/`: tabelas processadas (descritiva e testes estatísticos) em CSV.

## Resultado sintetizado

No experimento controlado, GraphQL apresentou menor mediana agregada de tempo e menor tamanho de resposta que REST. Os resultados apoiam as hipóteses alternativas H1-1 e H1-2 no cenário experimental definido.
